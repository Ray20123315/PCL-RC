Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Globalization
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

''' <summary>
''' Minecraft download permission only. CNG private key stays on Windows.
''' Signed offline license files are bound to that key and never grant game ownership.
''' </summary>
Friend Module ModPclRcLicense
    Private Const ApiRoot As String = "https://pcl-rc-licenses.ray20123315.workers.dev"
    Private Const RegistryPath As String = "HKEY_CURRENT_USER\Software\PCLRC"
    Private Const DeviceKeyName As String = "PCLRC-Download-P256-Device-v1"
    Private Const OfflineX As String = "lkzfrhbC-4csUB8EQ3IDF-UfzPdoLAYoFobFr9-K3wk"
    Private Const OfflineY As String = "n7AWfgH4nEwo_PHrJCyiAORSZz-OZG5uXgoxJ_wWirw"
    Private ReadOnly CacheLock As New Object
    Private CacheUntil As DateTime = DateTime.MinValue
    Private CacheAllowed As Boolean = False

    Private Function Base64Url(input As Byte()) As String
        Return Convert.ToBase64String(input).TrimEnd("="c).Replace("+", "-").Replace("/", "_")
    End Function

    Private Function FromBase64Url(input As String) As Byte()
        If String.IsNullOrEmpty(input) OrElse Not Regex.IsMatch(input, "^[A-Za-z0-9_-]+$") Then Throw New FormatException("Base64url 格式错误")
        Dim raw = input.Replace("-", "+").Replace("_", "/")
        raw = raw.PadRight(raw.Length + ((4 - raw.Length Mod 4) Mod 4), "="c)
        Return Convert.FromBase64String(raw)
    End Function

    Private Function PostJson(path As String, payload As Object) As JObject
        Using client As New HttpClient()
            client.Timeout = TimeSpan.FromSeconds(9)
            Using content As New StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                Using response = client.PostAsync(ApiRoot & path, content).GetAwaiter().GetResult()
                    Dim raw = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    If CInt(response.StatusCode) >= 500 Then Throw New HttpRequestException("授权伺服器暂时无法使用。")
                    Dim result = JObject.Parse(raw)
                    If Not response.IsSuccessStatusCode OrElse Not result.Value(Of Boolean)("ok") Then
                        Throw New InvalidOperationException(If(result.Value(Of String)("message"), "授权服务器拒绝请求。"))
                    End If
                    Return result
                End Using
            End Using
        End Using
    End Function

    Private Function OpenDeviceKey(Optional createIfMissing As Boolean = False) As CngKey
        Dim provider = CngProvider.MicrosoftSoftwareKeyStorageProvider
        If CngKey.Exists(DeviceKeyName, provider) Then Return CngKey.Open(DeviceKeyName, provider)
        If Not createIfMissing Then Throw New InvalidOperationException("这台电脑尚未建立 PCL RC 装置密钥，请先导出装置申请文件。")
        Dim parameters As New CngKeyCreationParameters With {
            .Provider = provider,
            .ExportPolicy = CngExportPolicies.None,
            .KeyUsage = CngKeyUsages.Signing
        }
        Return CngKey.Create(CngAlgorithm.ECDsaP256, DeviceKeyName, parameters)
    End Function

    Private Function CurrentPublicJwk(Optional createIfMissing As Boolean = False) As JObject
        Using key = OpenDeviceKey(createIfMissing)
            Using signer As New ECDsaCng(key)
                Dim publicParams = signer.ExportParameters(False)
                Return JObject.FromObject(New With {
                    .kty = "EC", .crv = "P-256",
                    .x = Base64Url(publicParams.Q.X), .y = Base64Url(publicParams.Q.Y)
                })
            End Using
        End Using
    End Function

    Private Function DeviceFingerprint(jwk As JObject) As String
        Dim text = jwk.Value(Of String)("x") & "." & jwk.Value(Of String)("y")
        Using hash = SHA256.Create()
            Return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant()
        End Using
    End Function

    ''' <summary>Offline public-key request. This file contains no private key.</summary>
    Friend Function ExportPclRcDeviceRequest() As String
        Dim jwk = CurrentPublicJwk(True)
        Return JsonConvert.SerializeObject(New With {
            .format = "pclrc-device-request-v1", .public_key = jwk
        }, Formatting.Indented)
    End Function

    Private Function OfflineSignatureValid(payload As String, signature As String) As Boolean
        Dim x = FromBase64Url(OfflineX), y = FromBase64Url(OfflineY)
        Dim sig = FromBase64Url(signature)
        If x.Length <> 32 OrElse y.Length <> 32 OrElse sig.Length <> 64 Then Return False
        ' BCRYPT_ECDSA_PUBLIC_P256_MAGIC "ECS1" + 32-byte coordinate size + X + Y.
        Dim blob(71) As Byte
        Buffer.BlockCopy(New Byte() {&H45, &H43, &H53, &H31, &H20, 0, 0, 0}, 0, blob, 0, 8)
        Buffer.BlockCopy(x, 0, blob, 8, 32)
        Buffer.BlockCopy(y, 0, blob, 40, 32)
        Using pub = CngKey.Import(blob, CngKeyBlobFormat.EccPublicBlob)
            Using verifier As New ECDsaCng(pub)
                verifier.HashAlgorithm = CngAlgorithm.Sha256
                Return verifier.VerifyData(Encoding.UTF8.GetBytes("pclrc-offline-v1." & payload), sig)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Verify immutable signature, original device, rights scope, UTC validity,
    ''' and a best-effort local time high-water mark. Offline revocation is impossible.
    ''' </summary>
    Private Function OfflineExpires(cert As String, Optional updateClock As Boolean = True) As DateTime?
        If String.IsNullOrWhiteSpace(cert) OrElse cert.Length > 12000 Then Return Nothing
        Dim file = JObject.Parse(cert)
        If file.Value(Of String)("format") <> "pclrc-offline-v1" Then Return Nothing
        Dim encoded = file.Value(Of String)("payload")
        Dim signed = file.Value(Of String)("signature")
        If String.IsNullOrEmpty(encoded) OrElse encoded.Length > 6000 OrElse Not OfflineSignatureValid(encoded, signed) Then Return Nothing
        Dim grant = JObject.Parse(Encoding.UTF8.GetString(FromBase64Url(encoded)))
        If grant.Value(Of Integer)("version") <> 1 OrElse grant.Value(Of String)("scope") <> "minecraft-download-region" Then Return Nothing
        If Not Regex.IsMatch(If(grant.Value(Of String)("license_id"), ""), "^[a-f0-9-]{36}$") Then Return Nothing
        Dim device = TryCast(grant("public_key"), JObject)
        If device Is Nothing OrElse device.Value(Of String)("kty") <> "EC" OrElse device.Value(Of String)("crv") <> "P-256" Then Return Nothing
        Dim local = CurrentPublicJwk(False)
        If local.Value(Of String)("x") <> device.Value(Of String)("x") OrElse local.Value(Of String)("y") <> device.Value(Of String)("y") Then Return Nothing
        If grant.Value(Of String)("device_id") <> DeviceFingerprint(local) Then Return Nothing
        Dim started As DateTimeOffset
        If Not DateTimeOffset.TryParse(grant.Value(Of String)("not_before"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, started) Then Return Nothing
        Dim nowTime = DateTime.UtcNow
        If started.UtcDateTime > nowTime Then Return Nothing
        Dim expiration As DateTime = DateTime.MaxValue
        If grant("expires_at") IsNot Nothing AndAlso grant("expires_at").Type <> JTokenType.Null Then
            Dim expiresDate As DateTimeOffset
            If Not DateTimeOffset.TryParse(grant.Value(Of String)("expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, expiresDate) Then Return Nothing
            expiration = expiresDate.UtcDateTime
            If expiration <= nowTime Then Return Nothing
        End If
        Dim saved = Convert.ToString(RegistryUtils.TryRead(RegistryPath, "LastLicenseUtc", ""))
        Dim highest As DateTimeOffset
        If DateTimeOffset.TryParse(saved, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, highest) Then
            If nowTime.AddMinutes(3) < highest.UtcDateTime Then Return Nothing
        End If
        If updateClock Then RegistryUtils.Write(RegistryPath, "LastLicenseUtc", nowTime.ToString("o", CultureInfo.InvariantCulture))
        Return expiration
    End Function

    Friend Function ImportPclRcOfflineLicense(content As String) As String
        Dim expires As DateTime?
        Try
            expires = OfflineExpires(content)
        Catch ex As Exception
            Throw New InvalidOperationException("离线许可文件不合法、签名错误、装置不符或已过期。", ex)
        End Try
        If Not expires.HasValue Then Throw New InvalidOperationException("离线许可文件不合法、签名错误、装置不符或已过期。")
        Dim grant = JObject.Parse(Encoding.UTF8.GetString(FromBase64Url(JObject.Parse(content).Value(Of String)("payload"))))
        RegistryUtils.Write(RegistryPath, "OfflineCertificate", content)
        RegistryUtils.Write(RegistryPath, "LicenseId", grant.Value(Of String)("license_id"))
        SyncLock CacheLock
            CacheUntil = DateTime.MinValue
            CacheAllowed = False
        End SyncLock
        Return If(expires.Value = DateTime.MaxValue, "离线永久授权已导入。", "离线授权已导入，有效期至 " & expires.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))
    End Function

    Friend Function ActivatePclRcLicense(inputKey As String) As String
        Dim value = If(inputKey, "").Trim().ToUpperInvariant()
        If Not Regex.IsMatch(value, "^PCLRC-[A-F0-9]{48}$") Then Throw New ArgumentException("KEY 格式不正确，请输入完整 KEY。")
        Dim existingId = Convert.ToString(RegistryUtils.TryRead(RegistryPath, "LicenseId", ""))
        If Regex.IsMatch(existingId, "^[a-f0-9-]{36}$") Then Throw New InvalidOperationException("此 Windows 用户已有授权。更换装置须联系管理员。")
        Dim result As JObject
        Using key = OpenDeviceKey(True)
            Using signer As New ECDsaCng(key)
                Dim publicParams = signer.ExportParameters(False)
                result = PostJson("/api/license/activate", New With {
                    .key = value,
                    .public_key = New With {.kty = "EC", .crv = "P-256", .x = Base64Url(publicParams.Q.X), .y = Base64Url(publicParams.Q.Y)}
                })
            End Using
        End Using
        Dim licenseId = result.Value(Of String)("license_id")
        If Not Regex.IsMatch(If(licenseId, ""), "^[a-f0-9-]{36}$") Then Throw New InvalidOperationException("伺服器没有回传有效的授权识别码。")
        RegistryUtils.Write(RegistryPath, "LicenseId", licenseId)
        SyncLock CacheLock
            CacheUntil = DateTime.MinValue
            CacheAllowed = False
        End SyncLock
        If Not PclRcLicenseValid(True) Then Throw New InvalidOperationException("KEY 已送出，但无法完成装置验证，请联系管理员；勿再次使用 KEY。")
        Dim expiry = result.Value(Of String)("expires_at")
        Return If(String.IsNullOrEmpty(expiry), "已启用永久授权。", "授权已启用，至 " & expiry & " (UTC) 有效。")
    End Function

    Private Function VerifyOnline(licenseId As String) As DateTime?
        Dim challenge = PostJson("/api/license/challenge", New With {.license_id = licenseId})
        Dim msg = challenge.Value(Of String)("message")
        Dim challengeId = challenge.Value(Of String)("challenge_id")
        If String.IsNullOrEmpty(msg) OrElse Not Regex.IsMatch(If(challengeId, ""), "^[a-f0-9-]{36}$") Then Return Nothing
        Dim signature As Byte()
        Using key = OpenDeviceKey(False)
            Using signer As New ECDsaCng(key)
                signer.HashAlgorithm = CngAlgorithm.Sha256
                signature = signer.SignData(Encoding.UTF8.GetBytes(msg))
            End Using
        End Using
        If signature.Length <> 64 Then Throw New CryptographicException("Unexpected ECDSA signature format.")
        Dim verify = PostJson("/api/license/verify", New With {.license_id = licenseId, .challenge_id = challengeId, .signature = Base64Url(signature)})
        If Not verify.Value(Of Boolean)("active") Then Return Nothing
        Dim expiryText = verify.Value(Of String)("expires_at")
        If String.IsNullOrEmpty(expiryText) Then Return DateTime.MaxValue
        Dim result As DateTimeOffset
        If Not DateTimeOffset.TryParse(expiryText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, result) Then Return Nothing
        If result.UtcDateTime <= DateTime.UtcNow Then Return Nothing
        Return result.UtcDateTime
    End Function

    ''' <summary>Prefer live verification. Only transport failure may use previously imported offline file.</summary>
    Friend Function PclRcLicenseValid(Optional forceRefresh As Boolean = False) As Boolean
        Dim licenseId = Convert.ToString(RegistryUtils.TryRead(RegistryPath, "LicenseId", ""))
        If Not Regex.IsMatch(If(licenseId, ""), "^[a-f0-9-]{36}$") Then Return False
        SyncLock CacheLock
            If Not forceRefresh AndAlso DateTime.UtcNow < CacheUntil Then Return CacheAllowed
        End SyncLock
        Dim expiry As DateTime? = Nothing
        Dim success As Boolean = False
        Dim networkUnavailable As Boolean = False
        Try
            expiry = VerifyOnline(licenseId)
            success = expiry.HasValue
        Catch ex As HttpRequestException
            networkUnavailable = True
        Catch ex As System.Threading.Tasks.TaskCanceledException
            networkUnavailable = True
        Catch ex As Exception
            ' A server-side explicit invalid / revoked / expired denial is NOT an offline fallback.
            success = False
        End Try
        If networkUnavailable Then
            Try
                expiry = OfflineExpires(Convert.ToString(RegistryUtils.TryRead(RegistryPath, "OfflineCertificate", "")))
                success = expiry.HasValue
            Catch
                success = False
            End Try
        End If
        SyncLock CacheLock
            CacheAllowed = success
            Dim nextTime = DateTime.UtcNow.AddMinutes(If(success, 10, 0.25))
            CacheUntil = If(success AndAlso expiry.HasValue AndAlso expiry.Value < nextTime, expiry.Value, nextTime)
        End SyncLock
        Return success
    End Function
End Module
