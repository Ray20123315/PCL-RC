Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

''' <summary>
''' PCL RC device-bound permission for Minecraft downloads. The launcher itself
''' remains downloadable and usable without a key. No hardware identifiers or
''' private keys are sent to the authorization service.
''' </summary>
Friend Module ModPclRcLicense
    Private Const ApiRoot As String = "https://pcl-rc-licenses.ray20123315.workers.dev"
    Private Const RegistryPath As String = "HKEY_CURRENT_USER\Software\PCLRC"
    Private Const DeviceKeyName As String = "PCLRC-Download-P256-Device-v1"
    Private ReadOnly CacheLock As New Object
    Private CacheUntil As DateTime = DateTime.MinValue
    Private CacheAllowed As Boolean = False

    Private Function Base64Url(input As Byte()) As String
        Return Convert.ToBase64String(input).TrimEnd("="c).Replace("+", "-").Replace("/", "_")
    End Function

    Private Function PostJson(path As String, payload As Object) As JObject
        Using client As New HttpClient()
            client.Timeout = TimeSpan.FromSeconds(9)
            Using content As New StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                Using response = client.PostAsync(ApiRoot & path, content).GetAwaiter().GetResult()
                    Dim raw = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    Dim result = JObject.Parse(raw)
                    If Not response.IsSuccessStatusCode OrElse Not result.Value(Of Boolean)("ok") Then
                        Throw New InvalidOperationException(If(result.Value(Of String)("message"), "授权服务器拒绝请求。"))
                    End If
                    Return result
                End Using
            End Using
        End Using
    End Function

    Private Function OpenDeviceKey() As CngKey
        Dim provider = CngProvider.MicrosoftSoftwareKeyStorageProvider
        If CngKey.Exists(DeviceKeyName, provider) Then Return CngKey.Open(DeviceKeyName, provider)
        Dim parameters As New CngKeyCreationParameters With {
            .Provider = provider,
            .ExportPolicy = CngExportPolicies.None,
            .KeyUsage = CngKeyUsages.Signing
        }
        Return CngKey.Create(CngAlgorithm.ECDsaP256, DeviceKeyName, parameters)
    End Function

    ''' <summary>
    ''' Redeem the key once for a persistent, non-exportable per-user Windows CNG
    ''' P-256 key. The server never receives the key after redemption.
    ''' </summary>
    Friend Function ActivatePclRcLicense(inputKey As String) As String
        Dim value = If(inputKey, "").Trim().ToUpperInvariant()
        If Not Regex.IsMatch(value, "^PCLRC-[A-F0-9]{48}$") Then
            Throw New ArgumentException("KEY 格式不正确，请从审核通过的通知中复制完整 KEY。")
        End If
        Dim existingId = Convert.ToString(RegistryUtils.TryRead(RegistryPath, "LicenseId", ""))
        If Regex.IsMatch(existingId, "^[a-f0-9-]{36}$") Then
            Throw New InvalidOperationException("此 Windows 用户已经登记过授权。若要更换设备，请联系管理员处理。")
        End If
        Dim result As JObject
        Using key = OpenDeviceKey()
            Using signer As New ECDsaCng(key)
                Dim publicParams = signer.ExportParameters(False)
                result = PostJson("/api/license/activate", New With {
                    .key = value,
                    .public_key = New With {
                        .kty = "EC", .crv = "P-256",
                        .x = Base64Url(publicParams.Q.X), .y = Base64Url(publicParams.Q.Y)
                    }
                })
            End Using
        End Using
        Dim licenseId = result.Value(Of String)("license_id")
        If Not Regex.IsMatch(If(licenseId, ""), "^[a-f0-9-]{36}$") Then
            Throw New InvalidOperationException("服务器没有回传有效的授权识别码。请联系管理员确认授权状态。")
        End If
        RegistryUtils.Write(RegistryPath, "LicenseId", licenseId)
        SyncLock CacheLock
            CacheUntil = DateTime.MinValue
            CacheAllowed = False
        End SyncLock
        If Not PclRcLicenseValid(True) Then
            Throw New InvalidOperationException("KEY 已送出，但本机无法完成装置签名验证。请检查网路和 Windows 用户密钥，并联系管理员；请勿再次使用 KEY。")
        End If
        Return "本机授权已启用，Minecraft 下载与地区限制已解除。"
    End Function

    ''' <summary>
    ''' A successful server challenge proves possession of the original device
    ''' private key and verifies the license has not been revoked. No offline
    ''' license override: failed verification falls back to region restrictions.
    ''' </summary>
    Friend Function PclRcLicenseValid(Optional forceRefresh As Boolean = False) As Boolean
        Dim licenseId = Convert.ToString(RegistryUtils.TryRead(RegistryPath, "LicenseId", ""))
        If Not Regex.IsMatch(If(licenseId, ""), "^[a-f0-9-]{36}$") Then Return False
        SyncLock CacheLock
            If Not forceRefresh AndAlso DateTime.UtcNow < CacheUntil Then Return CacheAllowed
        End SyncLock
        Dim success As Boolean = False
        Try
            Dim challenge = PostJson("/api/license/challenge", New With {.license_id = licenseId})
            Dim msg = challenge.Value(Of String)("message")
            Dim challengeId = challenge.Value(Of String)("challenge_id")
            If String.IsNullOrEmpty(msg) OrElse Not Regex.IsMatch(If(challengeId, ""), "^[a-f0-9-]{36}$") Then Return False
            Dim signature As Byte()
            Using key = OpenDeviceKey()
                Using signer As New ECDsaCng(key)
                    signer.HashAlgorithm = CngAlgorithm.Sha256
                    signature = signer.SignData(Encoding.UTF8.GetBytes(msg))
                End Using
            End Using
            If signature.Length <> 64 Then Throw New CryptographicException("Unexpected ECDSA signature format.")
            Dim verify = PostJson("/api/license/verify", New With {
                .license_id = licenseId,
                .challenge_id = challengeId,
                .signature = Base64Url(signature)
            })
            success = verify.Value(Of Boolean)("active")
        Catch
            success = False
        Finally
            SyncLock CacheLock
                CacheAllowed = success
                CacheUntil = DateTime.UtcNow.AddMinutes(If(success, 10, 0.25))
            End SyncLock
        End Try
        Return success
    End Function
End Module
