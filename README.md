# DatingApp

一個用來練習 .NET 後端開發的 **side project**：以 ASP.NET Core Web API 打造交友 App 的後端服務。

> **動機**：_（在這裡寫下你做這個專案的目的，例如：熟悉 .NET 9 Web API、練習 EF Core 與身分驗證、做一個能放進作品集的完整後端。）_

這份 README 同時是專案說明，也是**持續累積的開發紀錄**——往下的「開發日誌」「指令備忘」「技術筆記」三區會隨著開發一路長大。

---

## 技術堆疊

| 項目 | 內容 |
| --- | --- |
| 語言 | C# |
| 框架 | ASP.NET Core Web API |
| 目標框架 | .NET 9.0 (`net9.0`) |
| 專案 SDK | `Microsoft.NET.Sdk.Web` |
| API 文件 | `Microsoft.AspNetCore.OpenApi` 9.0.0 |
| 編譯設定 | `Nullable` enable、`ImplicitUsings` enable |

---

## 快速開始

### 環境需求

- [.NET 9 SDK](https://dotnet.microsoft.com/download)

確認已安裝：

```bash
dotnet --version
```

### 還原與執行

```bash
# 還原套件
dotnet restore

# 啟動 API（從根目錄執行）
dotnet run --project API
```

啟動後，開發環境下可透過 OpenAPI 端點查看 API 文件（預設在 `/openapi/v1.json`，實際路徑與埠號以 `API/Properties/launchSettings.json` 為準）。

---

## 專案結構

```
DatingApp/
├─ API/                       # 後端 Web API 專案
│  ├─ Controllers/            # API 控制器（目前有 WeatherForecastController 範例）
│  ├─ Program.cs              # 應用程式進入點與服務設定
│  └─ API.csproj              # 專案檔（目標框架、套件參考）
├─ DatingApp.sln              # Visual Studio / Rider 方案檔
├─ .gitignore                 # 由 `dotnet new gitignore` 產生
└─ README.md                  # 你正在看的這份文件
```

---

## 開發日誌 Dev Log

> 反時序排列（最新在最上面）。每筆記下：**做了什麼 / 學到什麼 / 踩到什麼坑**。

### 2026-06-28 — 專案初始化

- **做了什麼**：建立 .NET 9 ASP.NET Core Web API 專案（`API`）與方案檔，並用 `dotnet new gitignore` 產生標準 `.gitignore`。
- **學到什麼**：`dotnet new gitignore` 會產生 .NET 官方維護的忽略清單，自動涵蓋 `bin/`、`obj/` 等 build 產物與各家 IDE 暫存檔，不用手動拼湊。
- **踩到什麼坑**：_（暫無）_

---

## 指令備忘 Cheat Sheet

常用指令彙整，方便日後快速查詢。

| 指令 | 用途 |
| --- | --- |
| `dotnet new gitignore` | 在 **repo 根目錄**產生 .NET 官方維護的標準 `.gitignore`（涵蓋 build 產物、IDE 暫存檔等）。需更新清單時可重新執行；本專案的 `.gitignore` 即由此產生。 |
| `dotnet restore` | 還原 NuGet 套件相依。 |
| `dotnet build` | 編譯專案。 |
| `dotnet run --project API` | 從根目錄啟動 `API` 專案。 |
| `dotnet watch run --project API` | 啟動並在程式碼變更時自動重新載入（熱重載）。 |

---

## 技術筆記 / 踩坑紀錄

> 與流水帳式的開發日誌分開，這裡記較深入的技術心得與問題解法，方便日後回顧。

### 範例：`dotnet new gitignore` 的角色

- **情境**：剛建立專案時不希望把 `bin/`、`obj/` 等產物 commit 進版控。
- **解法**：在 repo 根目錄執行 `dotnet new gitignore`，產生官方維護的 `.gitignore`。
- **心得**：比手寫或從網路複製更可靠，且日後可重跑取得最新版本。

_（後續可依此格式：情境 → 解法 → 心得，繼續往下新增。）_
