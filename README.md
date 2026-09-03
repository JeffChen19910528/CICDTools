# deployctl

一個跨平台（Windows / Linux）的**應用程式部署與版本管理工具**。

`deployctl` 不是一般的 CI 執行器，而是專注在「安全地把一份程式碼部署到伺服器上」這件事：
每次部署前一定先備份、每個版本一旦建立就不可修改、失敗時自動嘗試復原，並且每一步操作都有稽核紀錄可查。

適合用來管理內部系統、網站或服務的正式環境部署，取代手動複製檔案、忘記備份、部署到一半出錯不知道怎麼辦的情境。

## 這個工具能做什麼

- **版本管理（Release）**：把一份原始檔案目錄封裝成一個不可變更的版本，並記錄每個檔案的 SHA-256 雜湊值。
- **部署（Deploy）**：將指定版本部署到指定的應用程式／環境／目標（例如 `MyApp / production / web1`）。
- **部署前差異比對（Diff）**：部署前先看清楚哪些檔案會新增、修改、刪除。
- **自動備份（Backup）**：部署或回滾前一定會先備份目前狀態，並驗證備份完整性；備份失敗就不會繼續部署。
- **一鍵回滾（Rollback）**：部署後發現問題，可以直接回到先前的版本，回滾前也會先備份現況。
- **保留政策（Retention）**：自動清理過期備份，但受保護、被回滾候選版本引用、或未滿最少保留數量的備份不會被刪除。
- **歷史紀錄與稽核（History / Audit）**：每一次部署、回滾、備份都會留下時間、操作者、結果的完整紀錄。
- **異常恢復（Recovery）**：如果部署過程中程式意外中斷，下次啟動可以查看未完成的部署並決定如何處理，不會自動硬幹下去。

## 給非資訊人員：如何取得可執行檔

你不需要安裝任何開發工具，只要拿到對應作業系統的**單一執行檔**即可使用：

- Windows → `deployctl.exe`
- Linux → `deployctl`

如果你是拿到原始碼要自己編譯，請參考下方「開發者：如何編譯」章節；編譯完成後，把產生的檔案複製到任何一台同類型系統的電腦上，雙擊或在終端機執行即可，**不需要額外安裝 .NET**。

### Windows 執行方式

在資料夾中按住 Shift 右鍵選「在此開啟 PowerShell 視窗」，或開啟命令提示字元，切換到執行檔所在目錄後輸入：

```powershell
.\deployctl.exe app list
```

### Linux 執行方式

```bash
chmod +x ./deployctl      # 第一次使用需要加上可執行權限
./deployctl app list
```

## 開發者：如何編譯

需求：安裝 [.NET SDK 8.0 以上版本](https://dotnet.microsoft.com/download)。

### 建置與測試（開發用）

```bash
dotnet build          # 建置整個方案
dotnet test           # 執行所有單元測試
```

### 編譯成 Windows 執行檔

在 Windows 上開啟 PowerShell，於專案根目錄執行：

```powershell
.\build-windows.ps1
```

執行完成後，`publish\windows\deployctl.exe` 就是可以直接複製給其他 Windows 使用者的單一執行檔（自帶 .NET runtime，對方電腦不需要另外安裝任何東西）。

### 編譯成 Linux 執行檔

在有安裝 .NET SDK 的機器上（Windows 也可以跨平台編譯 Linux 版本）執行：

```bash
./build-linux.sh
```

執行完成後，`publish/linux/deployctl` 就是可以直接複製給其他 Linux（x64）使用者的單一執行檔。

> 也可以手動執行對應的 `dotnet publish` 指令，兩支腳本內部呼叫的都是標準的
> `dotnet publish -r <win-x64|linux-x64> --self-contained -p:PublishSingleFile=true`。

## 資料儲存位置

`deployctl` 會把資料庫、版本檔案、備份檔案分別存在以下位置（可用環境變數覆寫）：

| 用途 | 環境變數 | 預設位置 |
|---|---|---|
| 資料庫與設定 | `DEPLOYCTL_DATA` | Windows: `%APPDATA%\deployctl`　Linux: `~/.config/deployctl` |
| 備份檔案 | `DEPLOYCTL_BACKUPS` | `<DEPLOYCTL_DATA>\backups` |
| 版本檔案 | `DEPLOYCTL_RELEASES` | `<DEPLOYCTL_DATA>\releases` |

如果想把資料放在別的磁碟或路徑，設定環境變數後再執行即可，例如：

```powershell
$env:DEPLOYCTL_DATA = "D:\deployctl-data"
.\deployctl.exe app list
```

## 操作指南

以下用一個完整範例，示範從「登記應用程式」到「部署、回滾」的完整流程。

### 1. 登記應用程式、環境與部署目標

```bash
# 建立一個應用程式
deployctl app create MyApp --description "公司內部網站"

# 在應用程式底下新增環境（例如 production），--require-approval 代表此環境需要額外核准
deployctl app add-env MyApp production --require-approval

# 在環境底下新增部署目標（實際會被部署檔案的機器與路徑）
deployctl app add-target MyApp production web1 /var/www/myapp --os linux --host 192.168.1.10

# 查看目前登記的所有應用程式、環境、目標
deployctl app list
```

### 2. 建立版本（Release）

把要部署的原始檔案準備好放在一個資料夾，然後封裝成版本：

```bash
deployctl release create MyApp 1.0.0 ./dist --operator alice --commit a1b2c3d --notes "首次上線"

# 查看某應用程式的所有版本
deployctl release list MyApp

# 查看單一版本的詳細內容（含檔案清單與 SHA-256）
deployctl release show MyApp-1.0.0
```

> 版本一旦建立就不能修改，如果程式碼有更新，請建立新的版本號（例如 `1.0.1`）。

### 3. 部署前先看差異

正式部署前，建議先確認會影響哪些檔案：

```bash
# 比對「新版本」與「目標機器目前部署的內容」
deployctl deploy diff MyApp production web1 1.0.0

# 加上 --show-unchanged 可以連未變動的檔案也一併列出
deployctl deploy diff MyApp production web1 1.0.0 --show-unchanged
```

輸出中：
- `+` 綠色代表新增的檔案
- `~` 黃色代表修改的檔案（文字檔會顯示逐行差異）
- `-` 紅色代表會被刪除的檔案

### 4. 部署

```bash
# 先用 --dry-run 模擬部署，不會真的修改任何檔案
deployctl deploy run MyApp production web1 1.0.0 --dry-run

# 正式部署（沒有加 --yes 會先跳出警告與確認提示）
deployctl deploy run MyApp production web1 1.0.0

# 在自動化腳本中略過互動確認
deployctl deploy run MyApp production web1 1.0.0 --yes
```

部署會依序執行：驗證 → 差異比對 → **建立並驗證備份**（失敗就停止，不會繼續部署）→ 部署檔案 → 檢查檔案雜湊值是否正確 → 寫入部署紀錄。任何一步失敗，系統都會嘗試自動回滾，讓目標機器不會停留在部署到一半的狀態。

### 5. 發現問題？直接回滾

```bash
deployctl deploy rollback MyApp production web1 1.0.0 --yes
```

回滾前一樣會先備份目前狀態，避免回滾本身又造成資料遺失。

### 6. 備份管理

```bash
# 手動建立一次備份（不透過部署流程）
deployctl backup create MyApp production web1

# 查看所有備份
deployctl backup list MyApp production web1

# 標記重要備份為「保護」，保護後不會被保留政策自動清除
deployctl backup protect BKP-20260101-120000 --protect

# 取消保護
deployctl backup protect BKP-20260101-120000 --protect false

# 依保留政策清理過期備份（受保護、回滾候選、未達最少保留數量者不會被刪除）
deployctl backup cleanup MyApp production web1
```

### 7. 查看歷史紀錄與稽核軌跡

```bash
# 查看某目標的部署歷史（含成功、失敗、回滾紀錄）
deployctl history deployments MyApp production web1

# 查看稽核事件（所有操作的完整軌跡，含操作者、時間、結果）
deployctl history audit MyApp --environment production --limit 100

# 不指定應用程式，查看全部稽核事件
deployctl history audit
```

### 8. 部署中斷後的異常恢復

如果部署過程中電腦當機或程式被強制關閉，重新啟動後可以查看是否有未完成的部署：

```bash
deployctl recovery status
```

系統**不會自動猜測**該怎麼處理未完成的部署，而是列出來讓你決定，例如手動標記為失敗：

```bash
deployctl recovery mark-failed DEP-20260101-120000
```

## 常見問題

**Q: 執行 `.exe` 或執行檔時被防毒軟體擋下來怎麼辦？**
自行編譯產生的單一執行檔沒有數位簽章，部分防毒軟體或 Windows SmartScreen 可能會攔截未知來源的執行檔。這是正常現象，確認來源可信後選擇「仍要執行」即可；如需長期在多台機器散布，建議另外簽署程式碼憑證。

**Q: 一定要用 `--yes` 才能在腳本裡自動化嗎？**
是的，正式環境的部署與回滾預設一定要手動確認（`Continue? [y/N]`），只有明確加上 `--yes` 才會跳過，避免自動化腳本誤觸生產環境部署。

**Q: 資料庫或版本檔案不小心刪除了怎麼辦？**
部署歷史（資料庫）與備份保留策略是分開設計的：刪除舊備份不會影響部署歷史紀錄；但資料庫本身（`DEPLOYCTL_DATA` 目錄）若遺失，會遺失所有應用程式登記資訊與歷史紀錄，建議定期備份此目錄本身。

## 專案架構（開發者參考）

```
Deployment.Domain          — 核心實體（Release、Deployment、Backup、Target、RetentionPolicy）
Deployment.Application     — 業務邏輯服務與介面定義（見下方「低耦合設計」）
Deployment.Infrastructure  — 平台實作（檔案系統、SQLite 資料庫、鎖定機制等）
Deployment.CLI             — deployctl 命令列介面（本文件涵蓋的操作對象）
Deployment.Tests           — 單元測試與整合測試
```

作業系統相關邏輯一律透過介面隔離（`IFileSystem`、`IChecksumService`、`ILockService` 等），核心引擎不會有任何 Windows 或 Linux 專屬程式碼。

### 低耦合設計

`Deployment.Application` 內的每個業務服務都對外暴露介面（`IReleaseService`、`IBackupService`、`IDiffService`、`IDeploymentService`、`IRetentionService`），並以介面而非具體類別互相依賴、被 CLI 呼叫、被 DI 容器註冊——調整某個服務的實作不會牽動其他服務或 CLI 層。

幾個共用邏輯也集中管理，避免重複：

- **`ITargetResolver`**：統一負責「應用程式／環境／目標」三層查找與找不到時的錯誤訊息，取代原本在 `BackupService`、`DeploymentService`、`RetentionService`、CLI 的 `deploy diff` 指令中各自重複的查找程式碼。
- **`IFileSystem.CopyDirectory`**：統一負責遞迴複製整個目錄（部署、回滾、備份還原都會用到），取代三處幾乎相同的複製迴圈。
- **`ByteFormatter`**（CLI 層）：統一負責位元組數字轉換成人類可讀格式（B/KB/MB/GB），取代 `release show` 與 `backup list` 指令中重複的格式化程式碼。

這些抽象讓新增功能（例如未來的遠端部署、Web UI）可以直接重用現有服務介面，而不需要碰觸實作細節。
