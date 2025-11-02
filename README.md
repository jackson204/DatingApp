# DatingApp

這是一個使用 .NET 9.0 和 Angular 20 開發的約會應用程式專案。

## 技術堆疊

### 後端
- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core

### 前端
- Angular 20
- TypeScript
- Standalone Components

## 前端開發必要擴充套件

本專案推薦安裝以下 VS Code 擴充套件以提升開發體驗：

### 1. ESLint
- **套件名稱**: `dbaeumer.vscode-eslint`
- **發行者**: Microsoft
- **描述**: 整合 ESLint JavaScript 到 VS Code
- **功能**:
  - 即時程式碼檢查
  - 自動修復程式碼品質問題
  - 支援 TypeScript 和 JavaScript
  - 可自訂規則配置

### 2. Angular Language Service
- **套件名稱**: `angular.ng-template`
- **發行者**: Angular
- **描述**: Angular 範本的編輯器服務
- **功能**:
  - Angular 範本語法高亮
  - 智能程式碼完成
  - 錯誤檢測和診斷
  - 範本內的導航支援
  - 型別檢查

## 專案結構

```
DatingApp/
├── API/                    # 後端 API 專案
│   ├── Controllers/        # API 控制器
│   ├── Data/              # 資料庫上下文和遷移
│   ├── Entities/          # 實體模型
│   └── Program.cs         # 應用程式進入點
└── client/                # 前端 Angular 專案
    └── src/
        └── app/           # Angular 應用程式
```

## 開始使用

### 後端設定

```bash
cd API
dotnet restore
dotnet run
```

### 前端設定

```bash
cd client
npm install
ng serve
```

## 開發環境需求

- .NET 9.0 SDK
- Node.js (LTS 版本)
- Angular CLI
- Visual Studio Code (推薦)

## 📚 文件

- [故障排除指南](TROUBLESHOOTING.md) - 常見問題與解決方案
- [前端開發必要擴充套件](#前端開發必要擴充套件) - VS Code 擴充套件推薦

## 🆘 遇到問題？

開發過程中遇到問題，請先查看 [故障排除指南](TROUBLESHOOTING.md)。

常見問題快速連結：
- [[ApiController] 參數綁定問題](TROUBLESHOOTING.md#問題apicontroller-導致參數必須從-query-string-綁定)
- [ERR_CONNECTION_REFUSED 錯誤](TROUBLESHOOTING.md#問題err_connection_refused---後端伺服器未執行)
- [CORS 設定說明](TROUBLESHOOTING.md#cors-設定說明)
- [Zoneless Change Detection 資料無法顯示](TROUBLESHOOTING.md#問題zoneless-模式下資料無法顯示)
- [資料庫連線問題](TROUBLESHOOTING.md#資料庫問題)

## 重要技術說明

### Angular Zoneless Change Detection

本專案採用 Angular 20 的 **Zoneless Change Detection** 模式，這是 Angular 新一代的變更偵測機制。

**關鍵特性**：
- ✅ 更高的執行效能（精確更新 DOM）
- ✅ 更小的 bundle 大小（不需要 Zone.js）
- ⚠️ 需要使用 Signal 管理狀態
- ⚠️ 不會自動觸發變更偵測

**開發注意事項**：
- 所有動態資料必須使用 `signal()` 宣告
- HTTP 請求回應需用 `.set()` 更新 Signal
- 或使用 `toSignal()` 將 Observable 轉換為 Signal

詳細說明請參考 [Zoneless 模式疑難排解](TROUBLESHOOTING.md#問題zoneless-模式下資料無法顯示)。

---

## 📅 更新日誌

- **2025年11月2日**: 新增 [[ApiController] 參數綁定問題分析與解決方案](TROUBLESHOOTING.md#問題apicontroller-導致參數必須從-query-string-綁定)
- **2025年10月26日**: 新增 [Zoneless Change Detection 問題分析與解決方案](TROUBLESHOOTING.md#問題zoneless-模式下資料無法顯示)
- **2025年10月25日**: 新增 [CORS 設定](TROUBLESHOOTING.md#cors-設定說明)與[連線問題排除指南](TROUBLESHOOTING.md#問題err_connection_refused---後端伺服器未執行)
- **2025年10月25日**: 專案初始化與基礎架構建立
