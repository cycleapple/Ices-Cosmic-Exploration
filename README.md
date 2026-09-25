# Ice's Cosmic Exploration（繁中移植版 · TC13） / Traditional-Chinese Port

> 把月球的苦工變成自動化。<br>
> Turn the moon grind into automation.

**繁體中文**：這是 **[Ice's Cosmic Exploration](https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration)** 的繁體中文客戶端移植版，對應 **FFXIV 7.20 / yanmucorp Dalamud API13（.NET 9）**。本專案僅做相容性移植，**非官方、非原作維護**；所有原始功能與設計著作權歸原作者 **LeontopodiumNivale14**。

**English**: A Traditional-Chinese-client port of **[Ice's Cosmic Exploration](https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration)** targeting **FFXIV 7.20 / yanmucorp Dalamud API13 (.NET 9)**. Compatibility port only — **unofficial and not maintained by the original author**. All original work © **LeontopodiumNivale14**.

---

## 這是什麼 / About

自動化宇宙探索（Cosmic Exploration）的任務循環：重骰任務、自動接取、移動、製作／採集／釣魚、依你設定的評價門檻交件，然後接下一輪。適合刷金評價成就、刷神器工具經驗，或把各職業推到 50 萬點。

Automates the Cosmic Exploration mission loop — reroll, accept, travel, craft/gather/fish, turn in at your chosen rank threshold, repeat. Useful for grinding gold-rank titles, relic tool experience, or 500k points per class.

## 依賴插件 / Required plugins

| 用途 / For | 插件 / Plugin |
|---|---|
| 製作 / Crafting | Artisan |
| 釣魚 / Fishing | AutoHook |
| 採集與移動 / Gathering & travel | vnavmesh |

三者在本插件庫都有對應的繁中移植版，請一併安裝。插件內也有說明視窗，會列出各任務類型的需求。<br>
All three are available as Traditional-Chinese ports in this same repository — install them alongside. The plugin also ships an in-game window explaining what each mission type needs.

## 安裝 / Installation

**繁體中文**
1. 使用 **XIVTCLauncher** 啟動繁體中文客戶端。
2. 遊戲內輸入 `/xlsettings` → 切到 **Experimental** 分頁 → **Custom Plugin Repositories（自訂插件庫）**。
3. 貼上下列網址並按 **+** 儲存：
   ```
   https://raw.githubusercontent.com/lilasrepo/DalamudPlugins/main/pluginmaster.json
   ```
4. 輸入 `/xlplugins`，搜尋 **Ice's Cosmic Exploration (TC13)** → 安裝 → 啟用。

**English**
1. Launch the Traditional-Chinese client with **XIVTCLauncher**.
2. In-game, type `/xlsettings` → **Experimental** tab → **Custom Plugin Repositories**.
3. Add this URL and save with **+**:
   ```
   https://raw.githubusercontent.com/lilasrepo/DalamudPlugins/main/pluginmaster.json
   ```
4. Type `/xlplugins`, search **Ice's Cosmic Exploration (TC13)** → Install → Enable.

## 對應版本 / Compatibility

| 項目 / Item | 版本 / Version |
|---|---|
| 遊戲 / Game | FFXIV 7.20（繁中客戶端 / TC client） |
| Dalamud | yanmucorp API13（.NET 9） |
| 移植自上游 / Ported from upstream | v1.0.0.1 |

## 繁中客戶端的差異 / Differences on the TC client

**繁體中文**
- 繁中客戶端目前**只有 Sinus Ardorum（第一顆星球）**。上游的 Phaenna、Oizys、Auxesia 在此不存在，相關介面會留白而不會報錯。
- 少數上游圖示不在繁中客戶端的資源檔中，會以替代圖示顯示。

**English**
- The TC client currently has **only Sinus Ardorum**. Upstream's Phaenna, Oizys and Auxesia do not exist here; their UI renders blank rather than throwing.
- A few upstream icons are absent from the TC client's assets and fall back to substitutes.

## 原作與授權 / Credits & License

本專案 fork 自 **[LeontopodiumNivale14/Ices-Cosmic-Exploration](https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration)**，授權沿用上游；所有原始功能著作權歸 **LeontopodiumNivale14**。<br>
Forked from **[LeontopodiumNivale14/Ices-Cosmic-Exploration](https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration)**. License follows upstream; all original work © **LeontopodiumNivale14**.

## 免責聲明 / Disclaimer

第三方插件，使用風險自負。**移植相關問題請回報到本 repo 的 Issues，請勿打擾上游原作者。**<br>
Third-party plugin — use at your own risk. **For port-specific issues please open an Issue here; do not contact the upstream author.**
