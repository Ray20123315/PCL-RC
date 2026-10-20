# Plain Craft Launcher – Ray Custom（PCL-RC）使用须知

**PCL-RC 是基于 Plain Craft Launcher 2（PCL2）的第三方非官方修改版。** 不代表原 PCL 开发者、PCL-CE、Microsoft、Mojang、Minecraft、CurseForge 或 Modrinth 官方。

## 功能与版本

- **公开版**：启用地区检查，不内置 Microsoft Client ID。
- **私人测试版**：另有 MS-NoRegion、NoMS-NoRegion 与 MS-Region 三种组合。
- 优先提供 Modrinth 资源，CurseForge 在线 API 暂时停用，保留原有代码与文件。
- Microsoft 登录是否可用，取决于微软应用注册、用户账号及相关服务授权；本项目不保证已获 Minecraft Services 核准。

## 地区与个人数据

**仅启用地区检查的版本**会在首次使用前询问是否允许调用 Windows 设备位置。明确同意后可使用 Windows 高精度位置（GPS、Wi-Fi 或其他来源），并用内置离线国界数据在本机计算国家／地区；**不会将设备经纬度上传到服务器**。

当设备位置不可用时，地区检查版可能参考 Windows 地区设置并向公开 IP 地理定位服务发送普通网络请求，以判断互联网出口地区。外部服务可看到请求的出口 IP；地区识别可能因代理、VPN、定位误差或连接失败而不准确。

**无地区检查版本不会请求 Windows 设备定位权限，也不会启动地理 IP 查询。** 它仍可能因正常 Minecraft 启动、资源搜索、账号验证等其他功能进行网络通信。

## 授权与责任

此说明属于项目使用提示，**不替代或修改原项目的开源授权**。源代码、修改文件与第三方组件须依各自许可使用；请阅读仓库内的 [LICENCE](LICENCE)，并保留原作者著作权与授权声明。

- 原始 PCL 项目：https://github.com/Meloong-Git/PCL
- PCL-RC 代码：https://github.com/Ray20123315/PCL-RC
- GitHub Releases：https://github.com/Ray20123315/PCL-RC/releases

本项目提供的是测试版本。Windows 实际定位、登录、下载及各地法律适用性未经全面验证。启用地区检查不代表保证识别真实地理位置、防止修改客户端，或自动满足所有地区法律要求。
