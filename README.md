# Plain Craft Launcher – Ray Custom（PCL RC）

![PCL RC 專案圖示](branding/PCL-RC.webp)

**[官網下載：內建 Microsoft Client ID 的 PCL RC.exe](https://pcl-rc-licenses.ray20123315.workers.dev/download/PCL%20RC.exe)** · [專案發行紀錄](https://github.com/Ray20123315/PCL-RC/releases) · [單裝置下載權限申請](https://pcl-rc-licenses.ray20123315.workers.dev/apply)

PCL RC 是基於 Plain Craft Launcher 2（PCL2）的**非官方個人修改版**，提供 Minecraft 啟動與版本管理、Modrinth 社群資源、登錄檔隔離及下載地區檢查。

## 下載與授權

- **GitHub 開源發行版：** 不內建 Microsoft Client ID，預設啟用 Minecraft 下載地區檢查。開源建置不自動取得 Microsoft 服務使用權限。\n- **官網下載版：** MS-Region，預先設定 Microsoft Client ID，啟用 Minecraft 下載地區檢查。可免費下載／啟動，不需要 KEY。
- **檔名：** 官網下載固定 `PCL RC.exe`；GitHub 的無預設 ID 開源發行檔另以 `PCL-RC-OpenSource-NoMS.exe` 命名，避免混淆。
- **選用 KEY：** 需要解除 PCL RC 內 Minecraft 下載及地區限制的使用者，可以於官網填寫詳細申請，由管理者人工審核，不保證通過。
- 每把 KEY 只能啟用一次，綁定一台 Windows 裝置的不可匯出 P-256 私鑰；啟用入口位於 **設定 → 其他 → PCL RC 下載授權**。授權須透過網路服務驗證，授權遭撤銷或驗證失敗時仍適用原有地區檢查。
- KEY **不提供** Microsoft、Minecraft 或第三方服務的遊戲所有權、帳號登入權限或服務存取保證。

目前為測試發行：Windows CI 建置可驗證，仍須另外完成 Windows 實機的授權、登入、下載與地區行為測試。官網版 Microsoft Client ID 已預設，但尚未確定 Minecraft Services 接受該應用程式；開源原始碼和開源發行版不內建此 ID。

## 專案與授權

- 原始專案：[Meloong-Git/PCL](https://github.com/Meloong-Git/PCL)
- 本專案程式碼源自：[Ray20123315/PCL](https://github.com/Ray20123315/PCL)
- 著作權、第三方資源授權及其他聲明請參閱 [LICENCE](LICENCE)、[NOTICE.md](NOTICE.md)。
- CurseForge API 連線暫停；保留原始解析程式碼，未授權的第三方 API 金鑰不會內建。
- 本專案並非 Minecraft、Microsoft、Mojang、原 PCL 作者或 PCL 社群的官方服務。
