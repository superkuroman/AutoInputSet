# GameAutomation 開發文件

GameAutomation 是一套以 C#、WPF、.NET 10 和 Windows API 開發的 Windows 遊戲自動化工具。

目前專案仍在早期開發階段，已完成視窗搜尋、Client Area 畫面擷取，以及滑鼠位置的 Pixel／Normalized Coordinate 顯示。滑鼠輸入、腳本執行、圖片比對與腳本編輯器尚未完成。

## 系統需求

- Windows 11 x64
- Visual Studio 2026，並安裝 .NET 桌面開發工作負載
- .NET 10 SDK
- 建議使用 x64 平台

若使用 Framework-dependent 發行版本，執行電腦還需要安裝 .NET 10 Desktop Runtime。Self-contained 發行版本不需要另外安裝 Runtime。

## 方案結構

```text
GameAutomation.slnx
│
├─ GameAutomation.Core       共用底層功能，沒有 UI
├─ GameAutomation.Capture    遊戲視窗選擇與畫面擷取工具
├─ GameAutomation.Editor     自動化腳本編輯器（尚未實作）
└─ GameAutomation.Runner     自動化腳本執行器（尚未實作）
```

引用關係固定如下：

```text
Capture ─┐
Editor ──┼──> Core
Runner ──┘
```

Capture、Editor 和 Runner 不應互相引用。

## 各專案用途

### GameAutomation.Core

所有程式共用的底層功能。

目前已有：

- `NormalizedPoint`：保存 `0～1` 的標準化座標並轉換成 Pixel 座標
- `NormalizedRect`：保存 `0～1` 的標準化矩形範圍
- `WindowFinder`：列出具有 Client Area 的可見 Windows 視窗
- `WindowCapture`：使用 Win32/GDI 擷取指定視窗的 Client Area
- `GameWindowInfo`：遊戲視窗資訊
- `CapturedFrame`：擷取後的 BGRA 畫面資料

規劃加入：

- 滑鼠與鍵盤輸入
- Script 與 ScriptAction 模型
- JSON 讀取與儲存
- Template 圖片比對
- ScriptExecutor

Core 是 Class Library，不能直接啟動。

### GameAutomation.Capture

目前唯一具有實際操作功能的 WPF 程式。

使用方式：

1. 啟動 `GameAutomation.Capture`。
2. 從「遊戲視窗」下拉清單選擇目標視窗。
3. 如果目標視窗未出現，按「重新整理」。
4. 按「擷取畫面」。
5. 將滑鼠移到 Screenshot 上，底部會顯示 Pixel 與 Normalized 座標。

Normalized 座標不受圖片在 WPF 中的顯示尺寸或留白影響。例如：

```text
Pixel:       1453, 822
Normalized:  0.756771, 0.761111
```

目前尚未支援：

- 點擊保存座標
- 框選 Template
- 儲存 PNG
- 將結果寫入 Script

### GameAutomation.Editor

預定用來建立與編輯自動化腳本，目前只有空白 WPF 視窗。

規劃功能：

- Action List
- Property Panel
- 新增、刪除與排序動作
- Script JSON 開啟與儲存
- Template 選擇
- Wait、Click、Delay、WaitImage 等動作設定

### GameAutomation.Runner

預定用來讀取腳本並執行自動化流程，目前只會輸出：

```text
Hello, World!
```

規劃使用方式：

```powershell
GameAutomation.Runner.exe Scripts\zzz_daily.json
```

目前尚未支援腳本參數、滑鼠輸入或自動化執行。

## 在 Visual Studio 中使用

1. 開啟 `GameAutomation.slnx`。
2. 等待 NuGet Restore 和方案載入完成。
3. 在 Solution Explorer 對要啟動的專案按右鍵。
4. 選擇「Set as Startup Project」。
5. 按 `F5` 偵錯，或按 `Ctrl+F5` 不偵錯啟動。

目前建議將 `GameAutomation.Capture` 設為啟動專案。

建置整個方案：

```powershell
dotnet build .\GameAutomation.slnx --configuration Debug
```

專案的一般建置輸出會放在：

```text
ROM\Debug\
ROM\Release\
```

## 建立發行版本

在方案根目錄執行：

```powershell
.\Publish-All.ps1
```

腳本會依序發行 Capture、Editor 和 Runner，並將三個 EXE 放入同一個資料夾。它會建立兩種版本：

```text
ROM\Publish\
├─ FrameworkDependent\
└─ SelfContained\
```

### FrameworkDependent

- 發行體積較小
- 需要目標電腦安裝 .NET 10 Desktop Runtime
- 適合開發與自己的電腦使用

### SelfContained

- 內含 .NET 10 與 WPF Runtime
- 目標電腦不需要另外安裝 .NET
- 發行資料夾較大
- 適合壓縮成 ZIP 提供給其他使用者

可指定組態與 CPU 架構：

```powershell
.\Publish-All.ps1 -Configuration Release -Runtime win-x64
```

支援的 Runtime：

- `win-x64`
- `win-arm64`

每次執行腳本都會重新建立 `ROM\Publish\FrameworkDependent` 與 `ROM\Publish\SelfContained`。不會刪除 `ROM\Debug` 或 `ROM\Release`。

## 發行資料夾使用方式

發行後的主要結構如下：

```text
GameAutomation/
├─ GameAutomation.Capture.exe
├─ GameAutomation.Editor.exe
├─ GameAutomation.Runner.exe
├─ GameAutomation.Core.dll
├─ Templates/
├─ Scripts/
└─ Logs/
```

目前可以實際使用的是：

```text
GameAutomation.Capture.exe
```

其他程式目前狀態：

- `GameAutomation.Editor.exe`：只能開啟空白視窗
- `GameAutomation.Runner.exe`：只會輸出 `Hello, World!`
- `GameAutomation.Core.dll`：共用程式庫，不能直接開啟

請保留 EXE、DLL 和相關 JSON 檔案的相對位置，不要只單獨複製其中一個 EXE。

## 外部資料夾

### Templates

預定保存遊戲畫面比對所使用的 PNG 圖片：

```text
Templates/
├─ MainMenu.png
└─ ClaimButton.png
```

### Scripts

預定保存自動化腳本：

```text
Scripts/
└─ zzz_daily.json
```

### Logs

預定保存 Runner 的執行紀錄。此資料夾不需要提交到 Git。

程式存取外部檔案時，應以 EXE 所在位置為基準：

```csharp
string templatePath = Path.Combine(
    AppContext.BaseDirectory,
    "Templates",
    "ClaimButton.png");
```

不要依賴目前工作目錄，否則從捷徑或其他位置啟動時可能找不到檔案。

## Git 與換行格式

- 專案文字檔統一使用 CRLF
- `.gitattributes` 控制 Git checkout 的換行格式
- `.editorconfig` 控制 Visual Studio 與編輯器的儲存格式
- `ROM/`、`bin/`、`obj/` 和 `.vs/` 不提交到 Git
- `Templates/` 與 `Scripts/` 的實際內容可以依需求提交

## 目前開發順序

建議依照以下順序繼續：

1. Capture 點擊 Screenshot 並保存 `NormalizedPoint`。
2. Core 實作 `SendInput`。
3. Runner 使用 normalized 座標點擊遊戲 Client Area。
4. 建立最小 Script JSON 格式。
5. 加入 Template 框選與 PNG 儲存。
6. 加入圖片比對。
7. 最後開發 Editor UI。

第一個重要里程碑是：

```text
Capture 記錄座標
→ 儲存 NormalizedPoint
→ Runner 讀取座標
→ 成功點擊遊戲中的相同位置
```

