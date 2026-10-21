# PCL RC 官方下載與程式碼說明

PCL RC 以龍騰貓躍的 Plain Craft Launcher 2（PCL2）為基礎。**開發者聲明本專案未加入病毒或惡意程式碼**，並公開原始碼供使用者檢視。

- **專案原始碼：** https://github.com/Ray20123315/PCL-RC/
- **目前的下載網站：** https://pcl-rc-licenses.ray20123315.workers.dev/
- **規劃中的專屬網址：** https://pclrc.ray2025.com/（DNS 與下載路由尚未驗證啟用）
- **執行檔：** 僅由官方網站提供 `PCL RC.exe`。GitHub 僅用於發布原始碼與 CI 建置驗證。

下載頁會公布對應 EXE 的 SHA-256。使用者可透過 PowerShell 核對：

```powershell
Get-FileHash -Algorithm SHA256 '.\PCL RC.exe'
```

此為專案發布者的原始碼與下載聲明，不是獨立第三方惡意程式檢測報告。PCL RC 並非 Minecraft、Microsoft 或原 PCL 作者的官方發行版。
