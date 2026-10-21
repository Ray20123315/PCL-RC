# Plain Craft Launcher – Ray Custom（PCL RC）

![PCL RC 專案圖示](branding/PCL-RC.webp)

> **安全公告：請只透過 PCL RC 官方網站取得執行檔。** 不要執行來路不明的「恢復 Minecraft 下載」「免限制」「破解版」啟動器。軟體名稱、圖示相同不代表是官方檔案。詳見 [下載安全須知](SECURITY.md)。

**目前官方下載入口：** https://pcl-rc-licenses.ray20123315.workers.dev/  
**預計使用的官方網域：** `pclrc.ray2025.com`（**尚未啟用**；正式啟用前請勿視為官方下載站）  
**原始碼與授權聲明：** 本 GitHub 儲存庫。

PCL RC 是基於 Plain Craft Launcher 2（PCL2）的**非官方個人修改版**，提供 Minecraft 啟動、版本管理、Modrinth 社群資源、登錄檔隔離及下載地區檢查。

## 發行與授權原則

- **本儲存庫只作原始碼發布，不再從 GitHub Actions 上傳 EXE 或打包發行檔。** 正式網站提供唯一指定的下載入口。
- **重要遷移狀態：** 之前 GitHub Releases 留下的 EXE／ZIP 暫時仍可能可取得；在官網完成獨立託管、校驗與切換前，不能先刪除其目前所依賴的檔案。完成遷移後會清除歷史 Release 執行檔。
- 官網 Windows 執行檔固定命名為 `PCL RC.exe`，採 MS-Region（預先設定 Microsoft Client ID、啟用地區檢查）。**公開原始碼不內建 Microsoft Client ID**。
- 啟動器本身可以免費下載與啟動；KEY 只用來解除 PCL RC 內 Minecraft 下載與地區限制，不提供 Microsoft／Minecraft 帳號或遊戲所有權。
- KEY 核發需要人工審核，一次性兌換並綁定單一裝置。年／月／天／時／分／秒、永久及簽章離線授權仍處於測試分支，尚未正式發行。
- 目前 Windows CI 能編譯專案，但 KEY 啟用、登入與下載限制的完整 Windows 實機測試仍待完成。

## 驗證下載檔案

正式下載區應提供 EXE 的 SHA-256，Windows 使用者可在 PowerShell 執行：

```powershell
Get-FileHash -Algorithm SHA256 '.\PCL RC.exe'
```

只有雜湊和**可信官方管道另行公布的值**相符，才能確認內容一致；雜湊本身不代表檔案必然無惡意程式。請保留 Windows Defender／SmartScreen 防護，不要為了安裝而關閉安全功能。更多說明見 [SECURITY.md](SECURITY.md)。

## 專案與授權

- 原始專案：[Meloong-Git/PCL](https://github.com/Meloong-Git/PCL)
- 本專案程式碼源自：[Ray20123315/PCL](https://github.com/Ray20123315/PCL)
- 著作權、第三方授權與其他聲明：[LICENCE](LICENCE)、[NOTICE.md](NOTICE.md)。
- CurseForge API 連線暫停，原始解析程式碼保留，不內建未經授權的第三方 API Secret。
- 本專案並非 Minecraft、Microsoft、Mojang 或原 PCL 作者的官方服務。
