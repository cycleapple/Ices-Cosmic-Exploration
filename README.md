1.0.0.9 補上宇宙計畫工具階段／工具經驗目標的必要研究交付，依 API13 分析需求判斷可交付階段並固定目標職業，避免換戰鬥職後交錯職業。標準模式仍遵守「自動交付」開關；判定依據是各項資料達到分析需求，並非倉儲上限。紀錄包含職業、階段、解鎖、開關、計畫目標與各項數量，方便確認未交付的原因。

1.0.0.8 修正 MF 回報任務後 ID 0 的金牌檢查錯誤、任務已結束仍等待 HUD，以及直接選餌，不依賴 AutoHook。任務目標 `TimeExpired` 表示時間到才回報；若要金牌立即回報請改成金牌。

# Ice's Cosmic Exploration — API13 新基底候選版

1.0.0.7 改用任務視窗分數判定獎牌，避免採集已金牌仍循環及製作提早繳交；修理改用 API13 按鈕逐分類處理，排除空格與不可修理物品，無進展會停止並提示。MissFisher 相容支援由 MissFisher 1.6.5.14 提供。

目前候選版本：**1.0.0.9**，分支 `api13-next-evaluation`。

以 [lilasrepo 的 1.0.0.3 移植原始碼](https://github.com/lilasrepo/Ices-Cosmic-Exploration/tree/8fb4f8510264bf231453fa57b32765b71677f39c) 為基礎，對接 Extended 庫使用的 API13 SDK、.NET 9 與 Artisan。原插件由 [LeontopodiumNivale14](https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration) 開發，遵循本倉庫 AGPL 授權。

## 本輪變更

- 從原始碼重建 ECommons、OtterGui、Pictomancy、Glamourer.Api 與 ICE。
- 修正採集到點交接、收藏品停滯復原、釣魚啟動與 AutoHook 換餌接口。
- Artisan 使用已驗證的暫時求解器、食物與藥水設定，失敗時清除，停止時還原。
- 匯入 0.0.73.x 的任務、採集設定、配方與宇宙計畫，並提供核心繁中介面。

## 舊設定匯入

候選版使用獨立的 `ICE.Next.API13` 設定目錄。首次啟動且尚無候選版設定時，會讀取原 `ICE` 目錄的 `Mission Config.yaml`。

已使用較早候選版的人，可在主視窗選擇「預覽匯入舊版設定」，確認後以「備份目前設定並套用匯入」套用。候選版執行中會停用匯入按鈕。

原 YAML 會保持不變。候選目錄會保存 YAML 備份、`Legacy-Import-Report.json`，以及手動匯入前的候選設定備份。製作設定檔會展開至各配方；原始名稱與內容保存在 YAML 備份。

手動／不自動繳交的任務會保持停用並顯示說明。部分任務等級金牌目標尚無等價條件，會拒絕匯入並提示重新設定。匯入錯誤會阻止開始執行，也可明確選擇略過匯入、使用獨立新設定。

手冊、專家設定檔與技能次數請在 Artisan 中調整。ICE 的對應覆寫控制已移除，已有值可在介面清除。

## 建置與檢查

使用 .NET 10 編譯器支援 C# 14 語法，產物仍以 **.NET 9／API13** 為目標：

```powershell
./evaluation/Build-Api13Candidate.ps1 -DalamudHome <API13-SDK目錄> -Dotnet <dotnet10.exe>
```

套件位於 `ICE/bin/Release/Candidate/ICECandidate/latest.zip`。測試以 .NET 9 執行：

```powershell
dotnet run --project tests/MissionRepair -c Release
dotnet run --project tests/CollectableProgress -c Release
dotnet run --project tests/Api13Contracts -c Release -- . <Artisan的Release目錄> <API13-SDK目錄>
```

目前完整重建 0 錯誤、177 警告；280 項離線檢查通過。檢查包含採集復原、實際編譯組件的接口、Artisan 設定回復，以及 YAML 匯入、備份、重複匯入與來源變更處理。

候選版尚未完成遊戲內驗證，也仍有詳細提示與除錯文字待翻譯。正式 Extended 庫維持 0.0.73.54。測試版在 API13 Extended 庫以「Ice's Cosmic Exploration（新版測試版）」獨立列出，`InternalName=ICECandidate`，使用 `/icecandidate` 開啟。正式版的名稱、版本與 `/ice` 指令保持不變。測試版的 IPC 前綴為 `ICECandidate`。

兩版可分開安裝。執行測試版前請先停用正式版 ICE；正式版載入時，測試版會暫停自動執行。測試版只清除自己套用的 Artisan 配方覆寫。

完整移植紀錄見 [baseline-audit.json](evaluation/baseline-audit.json)。
