# 故障排除指南

本文件記錄專案開發過程中遇到的問題及解決方案。

## 📋 快速導航

- [Angular 變更偵測問題](#angular-變更偵測問題)
  - [Zoneless 模式下資料無法顯示](#問題zoneless-模式下資料無法顯示)
- [CORS 相關問題](#cors-相關問題)
  - [ERR_CONNECTION_REFUSED - 後端伺服器未執行](#問題err_connection_refused---後端伺服器未執行)
  - [CORS 設定說明](#cors-設定說明)
- [資料庫問題](#資料庫問題)
- [前端建構問題](#前端建構問題)
- [CORS 知識補充](#cors-知識補充)
- [參考資源](#參考資源)

---

## Angular 變更偵測問題

### 問題：Zoneless 模式下資料無法顯示

**發生日期**: 2025年10月26日  
**問題類型**: 🏷️ Angular 變更偵測

---

#### 📋 問題摘要

在 Angular 20 Zoneless Change Detection 模式下，HTTP 請求成功取得資料，但資料無法顯示在瀏覽器上，即使重新整理頁面也無效。

**典型症狀**:
- ✅ Network 標籤顯示 API 請求成功（200 OK）
- ✅ Console 沒有顯示任何錯誤
- ❌ 畫面上沒有顯示資料
- ❌ 重新整理後資料仍然不顯示

---

#### 🔍 根本原因與原理

> 💡 **關鍵概念**: Zoneless 模式移除了自動變更偵測，需要明確通知框架資料已變更。

##### 本專案的特殊設定

本專案在 `app.config.ts` 中啟用了 `provideZonelessChangeDetection()`：

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),  // ⚠️ 關鍵設定
    // ...
  ]
};
```

##### Zone.js vs Zoneless 的運作差異

| 特性 | Zone.js（傳統） | Zoneless（本專案） |
|------|----------------|-------------------|
| 自動偵測 | ✅ 任何非同步操作後自動觸發 | ❌ 不會自動觸發 |
| HTTP 請求 | ✅ 請求完成後自動更新畫面 | ❌ 需要手動通知或使用 Signal |
| 屬性賦值 | ✅ `this.x = value` 會觸發更新 | ❌ 不會觸發更新 |
| 效能 | ⚠️ 較慢（檢查整個元件樹） | ✅ 更快（精確更新） |

##### 執行流程比較

```
【傳統模式 with Zone.js】
HTTP 請求完成
  ↓
Zone.js 攔截非同步操作
  ↓
自動執行變更偵測
  ↓
✅ UI 自動更新

【Zoneless 模式】
HTTP 請求完成
  ↓
this.members = response
  ↓
⚠️ 沒有 Zone.js 監控
  ↓
❌ 沒有觸發變更偵測
  ↓
❌ 畫面保持不變
```

##### 為什麼傳統寫法會失敗

```typescript
// ❌ 在 Zoneless 模式下無法正常運作
export class App implements OnInit {
  protected members: any;  // 普通屬性

  ngOnInit(): void {
    this.http.get('https://localhost:5001/api/members').subscribe({
      next: response => {
        this.members = response;  // ❌ 直接賦值不會觸發 UI 更新
        // Angular 不知道 this.members 變了，因為沒有 Zone.js 監控
      }
    });
  }
}
```

📝 **原理**: 在 Zoneless 模式下，直接賦值給普通屬性不會通知 Angular 框架，因此不會觸發 UI 更新。

---

#### 🛠️ 解決方案

##### 使用 Signal 管理狀態（推薦）

**步驟 1: 導入必要的模組並宣告 Signal**

```typescript
import { Component, inject, OnInit, signal } from '@angular/core';


@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private http = inject(HttpClient);
  
  // ✅ 使用 Signal 管理狀態
 protected members = signal<any>([]);

  ngOnInit(): void {
    this.http.get<Member[]>('https://localhost:5001/api/members').subscribe({
      next: response => this.members.set(response),  // ✅ 使用 .set() 更新
      error: err => console.error('載入會員資料失敗：', err),
      complete: () => console.log('請求完成')
    });
  }
}
```

📝 **原理**: Signal 是 Angular 的反應式狀態容器，框架會自動追蹤其變化。使用 `.set()` 方法會通知所有訂閱者（包括 Template）資料已更新。

**步驟 2: 在 Template 中讀取 Signal 值**

```html
<!-- ✅ 使用 () 讀取 Signal 值 -->
@for (member of members(); track member.id) {
  <li>{{ member.displayName }}</li>
}
```

📝 **原理**: `members()` 會建立訂閱關係，當 Signal 值變更時會自動重新渲染受影響的 DOM。

---

#### ✅ 驗證步驟

- [ ] **檢查 Signal 宣告**
  ```typescript
  // 確認有使用 signal()
  protected members = signal<any>([]);
  ```

- [ ] **檢查更新方式**
  ```typescript
  // 確認使用 .set() 更新
  this.members.set(response);
  ```

- [ ] **檢查 Template 語法**
  ```html
  <!-- 確認有加 () 讀取 Signal -->
  @for (member of members(); track member.id) {
    ...
  }
  ```

- [ ] **執行測試**
  1. 啟動後端：`cd API && dotnet run`
  2. 啟動前端：`cd client && npm start`
  3. 開啟瀏覽器訪問 `http://localhost:4200`
  4. ✅ 應該會看到資料正確顯示
  5. ✅ 重新整理後資料仍然顯示

---

#### ⚠️ 注意事項與最佳實踐

**Zoneless 模式下這些操作也不會觸發 UI 更新**:

```typescript
// ❌ 這些都不會觸發變更偵測
setTimeout(() => this.count++, 1000);
element.addEventListener('click', () => this.flag = true);
this.http.get(...).subscribe(data => this.data = data);

// ✅ 必須使用 Signal
setTimeout(() => this.count.update(c => c + 1), 1000);
element.addEventListener('click', () => this.flag.set(true));
this.http.get(...).subscribe(data => this.data.set(data));
```

**生產環境考量**:
- Zoneless 模式效能更好，但需團隊統一使用 Signal 模式
- 建議在團隊內建立編碼規範，禁止在 Zoneless 專案中直接賦值屬性
- 使用 TypeScript strict mode 和 ESLint 規則來強制型別安全

---

#### 📚 延伸閱讀

- [Angular Signals 官方文件](https://angular.dev/guide/signals) - Signal 完整說明
- [Zoneless Change Detection RFC](https://github.com/angular/angular/discussions/49685) - 設計決策討論
- [toSignal() API 參考](https://angular.dev/api/core/rxjs-interop/toSignal) - Observable 轉 Signal

---

#### 📁 相關檔案

- `client/src/app/app.config.ts` - Zoneless 模式設定
- `client/src/app/app.ts` - 元件程式碼
- `client/src/app/app.html` - Template

**解決狀態**: ✅ 已解決  
**相關問題**: [CORS 設定說明](#cors-設定說明)

---

## CORS 相關問題

### 問題：ERR_CONNECTION_REFUSED - 後端伺服器未執行

**發生日期**: 2025年10月25日  
**問題類型**: 🏷️ 連線問題

---

#### 📋 問題摘要

前端嘗試呼叫後端 API 時發生連線被拒絕錯誤，無法取得資料。

**典型錯誤訊息**:
```
GET https://localhost:5001/api/members net::ERR_CONNECTION_REFUSED
HttpErrorResponse {headers: _HttpHeaders, status: 0, statusText: 'Unknown Error'}
```

---

#### 🔍 根本原因與原理

> 💡 **關鍵概念**: 這**不是 CORS 錯誤**，而是 TCP 連線層級的問題。

##### 錯誤訊息解析

| 訊息 | 意義 | 原因 |
|------|------|------|
| `ERR_CONNECTION_REFUSED` | 連線被拒絕 | 目標連接埠沒有程式監聽 |
| `status: 0` | HTTP 狀態碼為 0 | 請求未到達伺服器 |
| `statusText: 'Unknown Error'` | 未知錯誤 | 瀏覽器無法建立連線 |

##### 網路請求流程

```
【正常流程】
瀏覽器發送請求
  ↓
DNS 解析 localhost → 127.0.0.1
  ↓
TCP 連線到 127.0.0.1:5001
  ↓
✅ 伺服器接收請求
  ↓
✅ 回傳 HTTP 200 + 資料

【錯誤流程】
瀏覽器發送請求
  ↓
DNS 解析 localhost → 127.0.0.1
  ↓
嘗試 TCP 連線到 127.0.0.1:5001
  ↓
❌ 連接埠 5001 沒有程式監聽
  ↓
❌ 作業系統回傳 Connection Refused
  ↓
❌ 瀏覽器顯示 ERR_CONNECTION_REFUSED
```

##### 常見原因

1. **後端伺服器未啟動** ← 最常見
2. 防火牆阻擋連接埠
3. 伺服器監聽不同的連接埠
4. SSL/TLS 憑證問題導致伺服器啟動失敗

---

#### 🛠️ 解決方案

##### 步驟 1: 啟動後端伺服器

```bash
cd API
dotnet run
```

**預期輸出**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

📝 **說明**: 確認終端機顯示正確的監聽位址和連接埠。

##### 步驟 2: 驗證 API 可訪問

在瀏覽器中訪問：
```
https://localhost:5001/api/members
```

**預期結果**: 應該看到 JSON 格式的會員資料（或空陣列 `[]`）。

📝 **說明**: 直接訪問 API 端點可以排除前端程式碼問題。

##### 步驟 3: 信任開發憑證（如果看到憑證警告）

```bash
dotnet dev-certs https --trust
```

**適用情況**: 首次執行專案或重新安裝 .NET SDK 後。

📝 **說明**: HTTPS 開發憑證需要被信任，否則瀏覽器會拒絕連線。

##### 步驟 4: 檢查防火牆設定

**Windows**:
```powershell
# 檢查連接埠是否被阻擋
netstat -ano | findstr :5001
```

**預期輸出**: 應該看到類似 `TCP    0.0.0.0:5001    0.0.0.0:0    LISTENING    12345`

📝 **說明**: 如果沒有輸出，表示沒有程式監聽該連接埠。

---

#### ✅ 驗證步驟

- [ ] **檢查後端是否執行**
  ```bash
  # 終端機應顯示
  Now listening on: https://localhost:5001
  ```

- [ ] **瀏覽器驗證 API**
  - 訪問 `https://localhost:5001/api/members`
  - ✅ 應顯示 JSON 資料（非錯誤頁面）

- [ ] **前端測試**
  1. 啟動前端：`cd client && npm start`
  2. 開啟 `http://localhost:4200`
  3. ✅ Console 應顯示成功取得資料
  4. ✅ 沒有 `ERR_CONNECTION_REFUSED` 錯誤

- [ ] **網路監控**
  - 開啟開發者工具 → Network 標籤
  - ✅ 請求狀態應為 `200 OK`（非 `(failed)`）

---

#### ⚠️ 注意事項與最佳實踐

**除錯技巧**:

```bash
# 檢查哪個程式佔用 5001 連接埠
netstat -ano | findstr :5001

# 如果連接埠被佔用，終止該程式
taskkill /PID <PID> /F
```

**開發環境建議**:
- 使用專案啟動工具（如 VS Code tasks）同時啟動前後端
- 設定 `launchSettings.json` 確保連接埠一致
- 在 `.gitignore` 中排除 SSL 憑證檔案

**生產環境考量**:
- 生產環境通常不會遇到此問題（伺服器由容器編排系統管理）
- 使用健康檢查 (Health Check) 確保服務可用
- 設定適當的逾時和重試機制

---

#### 📚 延伸閱讀

- [ASP.NET Core 應用程式啟動](https://learn.microsoft.com/zh-tw/aspnet/core/fundamentals/startup) - 官方文件
- [dotnet run 命令](https://learn.microsoft.com/zh-tw/dotnet/core/tools/dotnet-run) - CLI 參考

---

#### 📁 相關檔案

- `API/Program.cs` - 伺服器啟動設定
- `API/Properties/launchSettings.json` - 連接埠設定
- `client/src/app/app.ts` - 前端 API 呼叫程式碼

**解決狀態**: ✅ 已解決  
**相關問題**: [CORS 設定說明](#cors-設定說明)

---

### CORS 設定說明

**實施日期**: 2025年10月25日  
**問題類型**: 🏷️ CORS 跨來源資源共用

---

#### 📋 問題摘要

當後端伺服器正常執行後，可能會遇到真正的 CORS 錯誤：

```
Access to XMLHttpRequest at 'https://localhost:5001/api/members' 
from origin 'http://localhost:4200' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

---

#### 🔍 根本原因與原理

> 💡 **關鍵概念**: 不同通訊協定或連接埠會觸發瀏覽器的跨來源安全限制。

##### 本專案的跨來源情況

**前端執行在**: `http://localhost:4200`  
**後端執行在**: `https://localhost:5001`

**差異分析**:
- ❌ 通訊協定不同 (http vs https)
- ❌ 連接埠不同 (4200 vs 5001)

→ **需要 CORS 設定**

##### 什麼是「相同來源」？

來源 = `通訊協定://網域:連接埠`

| URL | 通訊協定 | 網域 | 連接埠 | 與 `http://localhost:4200` 比較 |
|-----|---------|------|--------|--------------------------------|
| `http://localhost:4200` | http | localhost | 4200 | ✅ 相同來源 |
| `https://localhost:4200` | https | localhost | 4200 | ❌ 不同（協定） |
| `http://localhost:5001` | http | localhost | 5001 | ❌ 不同（埠） |
| `http://127.0.0.1:4200` | http | 127.0.0.1 | 4200 | ❌ 不同（網域） |

---

#### 🛠️ 已實施的解決方案

在 `API/Program.cs` 中已加入 CORS 設定：

```csharp

var builder = WebApplication.CreateBuilder(args);

...

// 加入 CORS 服務
builder.Services.AddCors();

var app = builder.Build();

// ⚠️ 重要：UseCors 必須在 MapControllers 之前呼叫
app.UseCors(policyBuilder => policyBuilder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

...

app.Run();
```

📝 **關鍵說明**: 
- `app.UseCors(...)` **必須放在** `app.MapControllers()` **之前**
- 如果順序錯誤，CORS 設定將不會生效

##### 設定說明

| 設定方法 | 說明 | 用途 |
|---------|------|------|
| `AllowAnyHeader()` | 允許任何 HTTP 標頭 | 接受各種請求標頭 |
| `AllowAnyMethod()` | 允許任何 HTTP 方法 | 支援 GET, POST, PUT, DELETE 等 |
| `WithOrigins(...)` | 指定允許的來源 | 只允許特定網域訪問 |

---

#### ✅ 驗證步驟

- [ ] **啟動後端伺服器**
  ```bash
  cd API
  dotnet run
  ```
  預期輸出：`Now listening on: https://localhost:5001`

- [ ] **啟動前端應用程式**
  ```bash
  cd client
  npm start
  ```
  預期輸出：`Application bundle generation complete.`

- [ ] **瀏覽器驗證**
  1. 開啟 `http://localhost:4200`
  2. 開啟開發者工具 Console (F12)
  3. ✅ 應該會看到 API 回傳的資料
  4. ✅ 沒有 CORS 錯誤訊息

- [ ] **Network 標籤檢查**
  - 檢視回應標頭 (Response Headers)
  - ✅ 應包含 `Access-Control-Allow-Origin: http://localhost:4200`

---

#### ⚠️ 注意事項與最佳實踐

**⚠️ 目前的設定僅適用於開發環境**

**不要在生產環境使用**:
- ❌ `AllowAnyOrigin()` - 允許所有來源
- ❌ `AllowAnyHeader()` - 除非確實需要
- ❌ `AllowAnyMethod()` - 除非確實需要

**生產環境應使用更嚴格的設定**:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")  // 實際的生產網域
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization");
    });
});

app.UseCors("Production");
```

**除錯技巧**:

使用 curl 測試 CORS 標頭：
```bash
curl https://localhost:5001/api/members \
  -H "Origin: http://localhost:4200" \
  -k -v
```

---

#### 📚 延伸閱讀

- [MDN - CORS 跨來源資源共用](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/CORS)
- [ASP.NET Core - 啟用 CORS](https://learn.microsoft.com/zh-tw/aspnet/core/security/cors)

---

#### 📁 相關檔案

- `API/Program.cs` - CORS 設定位置

**解決狀態**: ✅ 已實施  
**相關問題**: [ERR_CONNECTION_REFUSED](#問題err_connection_refused---後端伺服器未執行)

---

## 資料庫問題

### 問題：資料庫連線與初始化

**實施日期**: 專案初始化  
**問題類型**: 🏷️ 資料庫設定

---

#### 📋 問題摘要

專案使用 SQLite 資料庫，首次執行或克隆專案後需要正確設定資料庫連線並執行遷移。

**常見症狀**:
- ❌ 應用程式啟動時找不到資料庫檔案
- ❌ API 請求回傳資料庫相關錯誤
- ❌ Entity Framework 遷移失敗

---

#### 🔍 根本原因與原理

> 💡 **關鍵概念**: Entity Framework Core 需要先建立資料庫結構才能存取資料。

##### 本專案的資料庫架構

**資料庫類型**: SQLite  
**連線字串位置**: `API/appsettings.json`  
**資料庫檔案**: `API/dating.db`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "data source=dating.db"
  }
}
```

##### Entity Framework Core 運作流程

```
【初始化流程】
開發者建立 Entity 類別
  ↓
執行 dotnet ef migrations add
  ↓
生成遷移檔案 (Migrations/)
  ↓
執行 dotnet ef database update
  ↓
✅ 建立實體資料庫和資料表
  ↓
✅ 應用程式可以存取資料

【常見錯誤流程】
克隆專案
  ↓
直接執行 dotnet run
  ↓
❌ dating.db 不存在
  ↓
❌ 應用程式無法啟動或查詢失敗
```

##### 為什麼需要執行遷移？

| 情況 | 原因 | 需要動作 |
|------|------|---------|
| 首次執行專案 | 資料庫檔案不存在 | 執行 `dotnet ef database update` |
| 克隆專案 | `.db` 檔案通常在 `.gitignore` 中 | 執行 `dotnet ef database update` |
| 修改 Entity | 資料庫結構需要更新 | 新增遷移 + 更新資料庫 |
| 切換分支 | 不同分支可能有不同結構 | 檢查並更新遷移 |

---

#### 🛠️ 解決方案

##### 步驟 1: 確認 Entity Framework 工具已安裝

```bash
# 檢查是否已安裝 EF Core CLI 工具
dotnet ef --version
```

**預期輸出**: 類似 `Entity Framework Core .NET Command-line Tools 9.0.0`

📝 **說明**: 如果顯示 `command not found`，需要先安裝工具。

**安裝 EF Core 工具**（如果需要）:
```bash
dotnet tool install --global dotnet-ef
```

##### 步驟 2: 檢查現有遷移

```bash
cd API
dotnet ef migrations list
```

**預期輸出**: 應顯示專案中的遷移檔案列表。

📝 **說明**: 確認 `Data/Migrations/` 目錄存在且包含遷移檔案。

##### 步驟 3: 執行資料庫更新

```bash
cd API
dotnet ef database update
```

**預期輸出**:
```
Build started...
Build succeeded.
Applying migration '20251026_InitialCreate'.
Done.
```

📝 **說明**: 此命令會根據遷移檔案建立或更新資料庫結構。

##### 步驟 4: 驗證資料庫檔案已建立

```bash
# Windows PowerShell
ls API/dating.db

# 或檢查檔案是否存在
Test-Path API/dating.db
```

**預期結果**: 應該看到 `dating.db` 檔案，大小不為 0。

📝 **說明**: 資料庫檔案成功建立後，應用程式才能正常存取資料。

---

#### ✅ 驗證步驟

- [ ] **檢查 EF Core 工具**
  ```bash
  dotnet ef --version
  # 應顯示版本號
  ```

- [ ] **檢查遷移檔案**
  ```bash
  ls API/Data/Migrations/
  # 應包含遷移檔案（.cs 檔）
  ```

- [ ] **確認資料庫檔案存在**
  ```bash
  ls API/dating.db
  # 應顯示檔案且大小 > 0
  ```

- [ ] **測試應用程式**
  1. 啟動後端：`cd API && dotnet run`
  2. 訪問 API：`https://localhost:5001/api/members`
  3. ✅ 應回傳資料（或空陣列 `[]`）而非資料庫錯誤

---

#### ⚠️ 注意事項與最佳實踐

**常見錯誤排除**:

```bash
# 錯誤 1: "No migrations found"
# 解決：確認 Migrations 資料夾存在
ls API/Data/Migrations/

# 錯誤 2: "Unable to create database"
# 解決：檢查資料夾權限
cd API
mkdir -p Data  # 確保 Data 資料夾存在

# 錯誤 3: "Build failed"
# 解決：先確保專案可以編譯
dotnet build
```

**開發工作流程**:

```bash
# 1. 修改 Entity 類別
# 2. 新增遷移
dotnet ef migrations add AddNewProperty

# 3. 檢查生成的遷移檔案
cat Data/Migrations/*_AddNewProperty.cs

# 4. 套用到資料庫
dotnet ef database update

# 5. 如果需要回滾
dotnet ef database update PreviousMigrationName
```

**生產環境考量**:
- 不要將 `.db` 檔案提交到版控（已在 `.gitignore`）
- 生產環境建議使用 SQL Server / PostgreSQL / MySQL
- 使用自動化部署腳本執行遷移
- 備份資料庫後再執行破壞性遷移

**除錯技巧**:

```bash
# 檢視資料庫內容（需安裝 SQLite CLI）
sqlite3 API/dating.db
# 在 SQLite 提示符下
.tables              # 列出所有資料表
.schema Users        # 查看 Users 資料表結構
SELECT * FROM Users; # 查詢資料
.exit                # 離開
```

---

#### 📚 延伸閱讀

- [Entity Framework Core 遷移](https://learn.microsoft.com/zh-tw/ef/core/managing-schemas/migrations/) - 官方文件
- [SQLite 官方文件](https://www.sqlite.org/docs.html) - SQLite 使用指南
- [dotnet ef 命令參考](https://learn.microsoft.com/zh-tw/ef/core/cli/dotnet) - EF Core CLI 工具

---

#### 📁 相關檔案

- `API/appsettings.json` - 資料庫連線字串
- `API/Data/AppDbContext.cs` - EF Core 資料庫上下文
- `API/Data/Migrations/` - 遷移檔案目錄
- `API/Entities/` - 實體模型定義

**解決狀態**: ✅ 已記錄  
**相關問題**: 無

---

## 前端建構問題

### 問題：Angular 開發伺服器設定與常見錯誤

**實施日期**: 專案初始化  
**問題類型**: 🏷️ 前端建構

---

#### 📋 問題摘要

Angular 開發伺服器的啟動、設定與常見連接埠衝突問題。

**常見症狀**:
- ❌ 連接埠 4200 已被佔用
- ❌ 應用程式無法啟動
- ❌ 編譯錯誤或相依套件問題

---

#### 🔍 根本原因與原理

> 💡 **關鍵概念**: Angular CLI 開發伺服器預設使用 4200 連接埠，如果被佔用會啟動失敗。

##### Angular 開發伺服器預設設定

**預設連接埠**: `4200`  
**預設位址**: `http://localhost:4200`  
**啟動指令**: `ng serve` 或 `npm start`

##### 開發伺服器運作流程

```
【正常啟動流程】
執行 ng serve
  ↓
Angular CLI 檢查 4200 連接埠
  ↓
✅ 連接埠可用
  ↓
編譯 TypeScript → JavaScript
  ↓
啟動開發伺服器
  ↓
✅ 監聽 http://localhost:4200

【連接埠衝突流程】
執行 ng serve
  ↓
Angular CLI 檢查 4200 連接埠
  ↓
❌ 連接埠已被佔用
  ↓
❌ 顯示錯誤：Port 4200 is already in use
  ↓
❌ 啟動失敗
```

##### 常見連接埠佔用原因

| 原因 | 說明 | 檢測方法 |
|------|------|---------|
| 之前的實例未關閉 | `ng serve` 在背景執行 | 檢查終端機分頁 |
| 其他應用程式 | 其他程式佔用 4200 | `netstat -ano \| findstr :4200` |
| 殭屍程序 | 程序異常終止但未釋放埠 | 使用工作管理員終止 |

---

#### 🛠️ 解決方案

##### 方案 1: 使用預設連接埠啟動（推薦）

**步驟 1: 安裝相依套件**

```bash
cd client
npm install
```

**預期輸出**: 應顯示套件安裝進度，最後顯示 `added X packages`。

📝 **說明**: 首次克隆專案或刪除 `node_modules` 後需要執行。

**步驟 2: 啟動開發伺服器**

```bash
npm start
# 或
ng serve
```

**預期輸出**:
```
✔ Browser application bundle generation complete.
** Angular Live Development Server is listening on localhost:4200 **
```

📝 **說明**: 成功啟動後可在 `http://localhost:4200` 訪問應用程式。

##### 方案 2: 處理連接埠衝突

**步驟 1: 找出佔用連接埠的程序**

```powershell
# Windows PowerShell
netstat -ano | findstr :4200
```

**預期輸出**:
```
TCP    0.0.0.0:4200    0.0.0.0:0    LISTENING    12345
```

📝 **說明**: 最後一欄數字（12345）是程序 ID (PID)。

**步驟 2: 終止佔用的程序**

```powershell
# 終止特定 PID
taskkill /PID 12345 /F
```

📝 **說明**: `/F` 強制終止程序。確認 PID 正確再執行。

**步驟 3: 重新啟動開發伺服器**

```bash
cd client
npm start
```

##### 方案 3: 使用不同連接埠

**步驟 1: 指定自訂連接埠**

```bash
ng serve --port 4201
```

**預期輸出**:
```
** Angular Live Development Server is listening on localhost:4201 **
```

📝 **說明**: 應用程式將在 `http://localhost:4201` 可用。

**步驟 2: 更新後端 CORS 設定**

如果使用非預設連接埠，需要更新 `API/Program.cs` 中的 CORS 設定：

```csharp
app.UseCors(policyBuilder => policyBuilder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins(
        "http://localhost:4200", 
        "https://localhost:4200",
        "http://localhost:4201",  // ✅ 新增自訂連接埠
        "https://localhost:4201"
    ));
```

📝 **說明**: CORS 需要明確允許新的來源，否則會出現跨來源錯誤。

---

#### ✅ 驗證步驟

- [ ] **檢查相依套件已安裝**
  ```bash
  ls client/node_modules/
  # 應包含大量套件資料夾
  ```

- [ ] **檢查開發伺服器已啟動**
  - 終端機應顯示 `listening on localhost:4200`
  - 沒有錯誤訊息

- [ ] **瀏覽器驗證**
  1. 訪問 `http://localhost:4200`
  2. ✅ 應顯示應用程式首頁（非錯誤頁面）
  3. ✅ Console 無編譯錯誤

- [ ] **熱重載測試**
  1. 修改 `client/src/app/app.html`
  2. 儲存檔案
  3. ✅ 瀏覽器應自動重新載入並顯示更改

---

#### ⚠️ 注意事項與最佳實踐

**常見問題排除**:

```bash
# 問題 1: "Cannot find module"
# 解決：刪除並重新安裝相依套件
rm -rf node_modules package-lock.json
npm install

# 問題 2: "Compilation errors"
# 解決：檢查 TypeScript 版本和設定
ng version
cat tsconfig.json

# 問題 3: "ENOSPC: System limit for number of file watchers reached"
# 解決：增加檔案監視器限制（Linux/Mac）
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

**開發環境建議**:
- 使用 `npm start` 而非直接 `ng serve`（可在 `package.json` 中自訂參數）
- 啟用 `--open` 自動開啟瀏覽器：`ng serve --open`
- 使用 `--poll` 解決檔案監視問題：`ng serve --poll=2000`
- 在 `angular.json` 中設定預設選項，避免每次輸入參數

**生產環境建構**:

```bash
# 建構生產版本
ng build --configuration production

# 輸出位置
ls client/dist/

# 建構選項
ng build --configuration production --optimization --source-map=false
```

**除錯技巧**:

```bash
# 顯示詳細建構資訊
ng serve --verbose

# 檢查 Angular CLI 版本
ng version

# 清除快取
ng cache clean

# 檢查連接埠使用情況
netstat -ano | findstr :4200
```

---

#### 📚 延伸閱讀

- [Angular CLI 文件](https://angular.dev/cli) - 官方 CLI 指南
- [ng serve 命令參考](https://angular.dev/cli/serve) - 完整參數說明
- [Angular 開發伺服器設定](https://angular.dev/tools/cli/serve) - 進階設定

---

#### 📁 相關檔案

- `client/angular.json` - Angular CLI 設定檔
- `client/package.json` - NPM 腳本和相依套件
- `client/tsconfig.json` - TypeScript 編譯設定
- `client/.angular/` - Angular CLI 快取目錄

**解決狀態**: ✅ 已記錄  
**相關問題**: [CORS 設定說明](#cors-設定說明)

---

## CORS 知識補充

### 主題：跨來源資源共用 (CORS) 完整指南

**文件類型**: 📚 知識庫  
**難度等級**: ⭐⭐⭐ 中級

---

#### 📋 概念摘要

CORS (Cross-Origin Resource Sharing) 是瀏覽器實施的安全機制，用於控制不同來源之間的資源存取。理解 CORS 對於前後端分離開發至關重要。

**核心要點**:
- 🔒 瀏覽器安全機制，防止惡意網站竊取資料
- 🌐 限制網頁只能向相同來源的伺服器發送請求
- ✅ 需要伺服器明確允許跨來源請求
- ⚠️ 只影響瀏覽器，不影響 Postman、curl 等工具

---

#### 🔍 深入理解 CORS 原理

> 💡 **關鍵概念**: 來源 (Origin) = `通訊協定://網域:連接埠`，三者必須完全相同才算同源。

##### 什麼是「相同來源」(Same-Origin)？

來源由三個部分組成：

```
https://example.com:443/api/data
│       │           │    │
│       │           │    └─ 路徑 (不影響來源判斷)
│       │           └────── 連接埠
│       └────────────────── 網域
└─────────────────────────── 通訊協定
```

**來源 = `通訊協定://網域:連接埠`**

只有當**所有三個部分**都相同時，才算是同源。

##### 同源判斷範例

基準 URL: `http://localhost:4200`

| URL | 通訊協定 | 網域 | 連接埠 | 是否同源 | 差異原因 |
|-----|---------|------|--------|---------|---------|
| `http://localhost:4200` | http | localhost | 4200 | ✅ 同源 | 完全相同 |
| `http://localhost:4200/api` | http | localhost | 4200 | ✅ 同源 | 路徑不影響 |
| `https://localhost:4200` | https | localhost | 4200 | ❌ 跨來源 | 通訊協定不同 |
| `http://localhost:5001` | http | localhost | 5001 | ❌ 跨來源 | 連接埠不同 |
| `http://127.0.0.1:4200` | http | 127.0.0.1 | 4200 | ❌ 跨來源 | 網域不同 |
| `http://example.com:4200` | http | example.com | 4200 | ❌ 跨來源 | 網域不同 |

##### 本專案的跨來源情況分析

**前端 Angular 應用程式**:
```
http://localhost:4200
│     │         │
│     │         └─ 連接埠: 4200
│     └─────────── 網域: localhost
└───────────────── 通訊協定: http
```

**後端 ASP.NET Core API**:
```
https://localhost:5001
│      │         │
│      │         └─ 連接埠: 5001
│      └─────────── 網域: localhost
└────────────────── 通訊協定: https
```

**差異分析**:
1. ❌ **通訊協定不同**: `http` vs `https`
2. ❌ **連接埠不同**: `4200` vs `5001`

→ **結論**: 屬於跨來源請求，需要 CORS 設定

---

#### 🔍 CORS 運作流程詳解

##### 簡單請求 (Simple Request)

**判斷條件** (必須同時滿足):
- HTTP 方法為 `GET`、`HEAD` 或 `POST`
- 只使用安全的標頭 (Accept, Accept-Language, Content-Language, Content-Type)
- Content-Type 僅限：`application/x-www-form-urlencoded`、`multipart/form-data`、`text/plain`

**執行流程**:

```
【步驟 1】瀏覽器發送請求
Browser → GET /api/members
          Origin: http://localhost:4200
          ↓
       Server

【步驟 2】伺服器處理並回應
Browser ← HTTP 200 OK
          Access-Control-Allow-Origin: http://localhost:4200
          Content-Type: application/json
          [資料內容]
          ↓
       Server

【步驟 3】瀏覽器檢查 CORS 標頭
if (Access-Control-Allow-Origin 包含 Origin) {
    ✅ 允許 JavaScript 存取回應資料
} else {
    ❌ 阻擋存取，在 Console 顯示 CORS 錯誤
}
```

##### 預檢請求 (Preflight Request)

**觸發條件** (任一條件):
- 使用 `PUT`、`DELETE`、`PATCH` 等方法
- 使用自訂標頭 (如 `Authorization`、`X-Custom-Header`)
- Content-Type 為 `application/json`

**執行流程**:

```
【步驟 1】瀏覽器發送預檢請求 (OPTIONS)
Browser → OPTIONS /api/members
          Origin: http://localhost:4200
          Access-Control-Request-Method: PUT
          Access-Control-Request-Headers: Content-Type, Authorization
          ↓
       Server

【步驟 2】伺服器回應預檢結果
Browser ← HTTP 204 No Content
          Access-Control-Allow-Origin: http://localhost:4200
          Access-Control-Allow-Methods: GET, POST, PUT, DELETE
          Access-Control-Allow-Headers: Content-Type, Authorization
          Access-Control-Max-Age: 86400  (預檢結果快取 24 小時)
          ↓
       Server

【步驟 3】瀏覽器檢查預檢回應
if (伺服器允許該方法和標頭) {
    ✅ 繼續發送實際請求
} else {
    ❌ 阻擋請求，顯示 CORS 錯誤
}

【步驟 4】發送實際請求 (如果預檢通過)
Browser → PUT /api/members
          Origin: http://localhost:4200
          Content-Type: application/json
          Authorization: Bearer token
          [請求資料]
          ↓
       Server

【步驟 5】伺服器處理並回應
Browser ← HTTP 200 OK
          Access-Control-Allow-Origin: http://localhost:4200
          [回應資料]
          ↓
       Server
```

---

#### 🛠️ 如何判斷是否為 CORS 錯誤

##### ✅ 真正的 CORS 錯誤特徵

**典型錯誤訊息**:
```
Access to XMLHttpRequest at 'https://localhost:5001/api/members' 
from origin 'http://localhost:4200' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

**關鍵字**:
- ✅ "**blocked by CORS policy**"
- ✅ "**Access-Control-Allow-Origin**"
- ✅ "**Preflight**"
- ✅ "**CORS header**"

**Network 標籤特徵**:
- 請求狀態顯示為 `200` 或 `204`（但 Console 有錯誤）
- 可以看到伺服器的回應
- Response Headers 中缺少 CORS 相關標頭

##### ❌ 常被誤認為 CORS 的錯誤

| 錯誤訊息 | 實際原因 | 如何辨別 |
|---------|---------|---------|
| `ERR_CONNECTION_REFUSED` | 伺服器未執行 | status: 0，連線層級問題 |
| `ERR_NAME_NOT_RESOLVED` | DNS 解析失敗 | 網域名稱錯誤 |
| `ERR_CERT_AUTHORITY_INVALID` | SSL 憑證問題 | HTTPS 憑證未信任 |
| `404 Not Found` | API 端點不存在 | 路徑錯誤 |
| `401 Unauthorized` | 認證失敗 | 缺少或無效的驗證令牌 |
| `500 Internal Server Error` | 伺服器端錯誤 | 後端程式錯誤 |

**快速判斷法**:
```
if (錯誤訊息包含 "CORS" 或 "Access-Control") {
    → 這是 CORS 錯誤
} else if (status === 0) {
    → 連線問題 (伺服器未執行、網路錯誤、憑證問題)
} else if (status >= 400) {
    → HTTP 錯誤 (API 問題、認證問題、伺服器錯誤)
}
```

---

#### ✅ 除錯步驟與工具

##### 步驟 1: 使用瀏覽器開發者工具

**Console 標籤**:
1. 開啟開發者工具 (F12)
2. 切換到 Console 標籤
3. 🔍 搜尋關鍵字 "CORS" 或 "Access-Control"
4. 如果看到紅色錯誤訊息 → 確認為 CORS 問題

**Network 標籤**:
1. 切換到 Network 標籤
2. 重新載入頁面觸發請求
3. 點擊失敗的請求
4. 檢查 **Response Headers**

**應該包含的 CORS 標頭**:
```http
Access-Control-Allow-Origin: http://localhost:4200
Access-Control-Allow-Methods: GET, POST, PUT, DELETE
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Credentials: true
```

**如果缺少或值不正確 → CORS 設定問題**

##### 步驟 2: 使用 curl 測試 (繞過瀏覽器限制)

**測試 API 是否正常運作**:
```bash
# 基本請求 (不受 CORS 限制)
curl https://localhost:5001/api/members -k

# 預期：應回傳 JSON 資料，表示 API 本身正常
```

📝 **說明**: curl 不受瀏覽器 CORS 限制，如果成功表示問題出在 CORS 設定。

**測試 CORS 標頭**:
```bash
# 模擬瀏覽器發送 Origin 標頭
curl https://localhost:5001/api/members \
  -H "Origin: http://localhost:4200" \
  -k -v
```

**檢查輸出中的回應標頭**:
```
< Access-Control-Allow-Origin: http://localhost:4200
```

如果有此標頭 → CORS 設定正確  
如果沒有 → 需要設定 CORS

##### 步驟 3: 測試預檢請求

```bash
# 發送 OPTIONS 請求
curl -X OPTIONS https://localhost:5001/api/members \
  -H "Origin: http://localhost:4200" \
  -H "Access-Control-Request-Method: PUT" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -k -v
```

**預期回應標頭**:
```
< Access-Control-Allow-Origin: http://localhost:4200
< Access-Control-Allow-Methods: GET, POST, PUT, DELETE
< Access-Control-Allow-Headers: Content-Type
```

---

#### ⚠️ 安全注意事項

**❌ 危險設定（僅限開發環境）**:

```csharp
// 🚨 危險：允許任何來源
app.UseCors(policy => policy.AllowAnyOrigin());

// 🚨 危險：允許任何標頭和方法
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());
```

**為什麼危險？**
- 任何網站都可以呼叫你的 API
- 容易遭受 CSRF (跨站請求偽造) 攻擊
- 無法限制惡意來源

**✅ 生產環境建議設定**:

```csharp
// ✅ 安全：明確指定允許的來源
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://yourdomain.com",
                "https://www.yourdomain.com"
            )
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials();  // 如果需要傳送 Cookie
    });
});

app.UseCors("Production");
```

**最佳實踐**:
1. ✅ 使用白名單機制，明確列出允許的來源
2. ✅ 只允許必要的 HTTP 方法
3. ✅ 只允許必要的標頭
4. ✅ 根據環境使用不同的 CORS 策略
5. ✅ 定期審查 CORS 設定

**環境區分設定範例**:

```csharp
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // 開發環境：寬鬆設定
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}
else
{
    // 生產環境：嚴格設定
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins("https://yourdomain.com")
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Content-Type", "Authorization");
        });
    });
}

var app = builder.Build();

// 根據環境套用不同策略
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}
```

---

#### 📚 延伸閱讀

**基礎概念**:
- [MDN - CORS 跨來源資源共用](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/CORS) - 完整的 CORS 說明
- [MDN - 同源政策](https://developer.mozilla.org/zh-TW/docs/Web/Security/Same-origin_policy) - 同源政策詳解

**ASP.NET Core 實作**:
- [ASP.NET Core - 啟用 CORS](https://learn.microsoft.com/zh-tw/aspnet/core/security/cors) - 官方文件
- [CORS 中介軟體選項](https://learn.microsoft.com/zh-tw/dotnet/api/microsoft.aspnetcore.cors.infrastructure.corspolicy) - API 參考

**進階主題**:
- [CORS 與 Cookie](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS#requests_with_credentials) - Credentials 模式
- [Preflight Request 優化](https://developer.mozilla.org/en-US/docs/Glossary/Preflight_request) - 減少預檢請求

---

#### 📊 CORS 決策樹

```
是否為跨來源請求？
  ├─ 否 → 不需要 CORS（同源請求）
  └─ 是 → 繼續
       │
       ├─ 是否為簡單請求？
       │    ├─ 是 → 伺服器需回傳 Access-Control-Allow-Origin
       │    └─ 否 → 需要預檢請求 (OPTIONS)
       │             │
       │             └─ 伺服器需回傳完整 CORS 標頭
       │
       └─ 開發環境 vs 生產環境？
            ├─ 開發 → 可使用 AllowAnyOrigin (但不推薦)
            └─ 生產 → 必須使用白名單 WithOrigins
```

---

**文件類型**: 📚 知識庫  
**更新日期**: 2025年10月26日  
**相關問題**: [CORS 設定說明](#cors-設定說明) | [ERR_CONNECTION_REFUSED](#問題err_connection_refused---後端伺服器未執行)


---

## 參考資源

### 官方文件

#### Angular
- [Angular Signals](https://angular.dev/guide/signals) - Signal 反應式狀態管理
- [Zoneless Change Detection](https://github.com/angular/angular/discussions/49685) - Zoneless 模式討論
- [toSignal() API](https://angular.dev/api/core/rxjs-interop/toSignal) - Observable 轉 Signal
- [Angular HttpClient](https://angular.dev/guide/http) - HTTP 客戶端指南

#### .NET & CORS
- [MDN - CORS 跨來源資源共用](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/CORS)
- [ASP.NET Core - 啟用 CORS](https://learn.microsoft.com/zh-tw/aspnet/core/security/cors)

### 相關工具

- [Postman](https://www.postman.com/) - API 測試工具
- [curl](https://curl.se/) - 命令列 HTTP 客戶端
- [Browser DevTools](https://developer.chrome.com/docs/devtools/) - 瀏覽器開發者工具

### 專案相關文件

- [`DatingApp/client/README.md`](DatingApp/client/README.md ) - 前端專案說明
- `API/README.md` - 後端專案說明（待建立）



---

## 📊 問題統計

| 問題類型 | 解決數量 | 狀態 |
|---------|---------|------|
| Angular 變更偵測 | 1 | ✅ 已解決 |
| CORS 相關 | 2 | ✅ 已解決 |
| 資料庫問題 | 1 | ✅ 已記錄 |
| 前端建構 | 1 | ✅ 已記錄 |

---

## 📅 問題解決日誌

### 2025年10月26日
- ✅ **[Angular 變更偵測]** 解決 Zoneless 模式下資料無法顯示的問題
  - 根本原因：使用 `provideZonelessChangeDetection()` 後，傳統屬性賦值不會觸發 UI 更新
  - 解決方案：改用 Signal 或 toSignal() 管理狀態
  - 影響範圍：所有使用 HTTP 請求的元件
  - 參考：[詳細說明](#問題zoneless-模式下資料無法顯示)

### 2025年10月25日
- ✅ **[CORS]** 設定跨來源資源共用政策
  - 問題：前端無法訪問後端 API
  - 解決：在 `Program.cs` 中加入 CORS 中介軟體
  - 參考：[CORS 設定說明](#cors-設定說明)

- ✅ **[連線]** 解決 ERR_CONNECTION_REFUSED 錯誤
  - 問題：後端伺服器未啟動
  - 解決：啟動 API 專案並信任開發憑證
  - 參考：[連線問題排除](#問題err_connection_refused---後端伺服器未執行)

---

**文件版本**: 1.1.0  
**維護者**: DatingApp 開發團隊
