# 故障排除指南

本文件記錄專案開發過程中遇到的問題及解決方案。

## 📋 快速導航

- [CORS 相關問題](#cors-相關問題)
  - [ERR_CONNECTION_REFUSED - 後端伺服器未執行](#問題err_connection_refused---後端伺服器未執行)
  - [CORS 設定說明](#cors-設定說明)
- [資料庫問題](#資料庫問題)
- [前端建構問題](#前端建構問題)
- [CORS 知識補充](#cors-知識補充)
- [參考資源](#參考資源)

---

## CORS 相關問題

### 問題：ERR_CONNECTION_REFUSED - 後端伺服器未執行

**發生日期**: 2025年10月25日

**問題描述**:

前端 Angular 應用程式嘗試呼叫後端 API 時，瀏覽器 Console 顯示以下錯誤：

```
GET https://localhost:5001/api/members net::ERR_CONNECTION_REFUSED
HttpErrorResponse {headers: _HttpHeaders, status: 0, statusText: 'Unknown Error'}
```

**錯誤分析**:

- 這**不是真正的 CORS 錯誤**，而是連線被拒絕錯誤
- `status: 0` 表示請求根本沒有到達伺服器
- 原因：後端 API 伺服器未啟動或未監聽正確的連接埠

**解決方案**:

1. **啟動後端伺服器**:
   ```bash
   cd API
   dotnet run
   ```

2. **確認伺服器正在執行**:
   - 確認終端機顯示 `Now listening on: https://localhost:5001`
   - 在瀏覽器中訪問 `https://localhost:5001/api/members` 驗證 API 可訪問

3. **信任開發憑證**（如果看到憑證警告）:
   ```bash
   dotnet dev-certs https --trust
   ```

4. **檢查防火牆**:
   - 確認防火牆沒有阻擋 5001 連接埠

**相關檔案**:
- `client/src/app/app.ts` - 前端發送請求的程式碼
- `API/Program.cs` - 後端啟動設定

**狀態**: ✅ 已解決

---

### CORS 設定說明

**實施日期**: 2025年10月25日

**問題背景**:

當後端伺服器正常執行後，可能會遇到真正的 CORS 錯誤：

```
Access to XMLHttpRequest at 'https://localhost:5001/api/members' 
from origin 'http://localhost:4200' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

**原因**:
- 前端執行在 `http://localhost:4200`
- 後端執行在 `https://localhost:5001`
- 不同的通訊協定和連接埠 = 跨來源請求
- 需要後端設定 CORS 政策來允許前端的請求

**已實施的解決方案**:

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

**⚠️ 關鍵注意事項**:
- `app.UseCors(...)` **必須放在** `app.MapControllers()` **之前**
- 如果順序錯誤，CORS 設定將不會生效，仍然會看到 CORS 錯誤

**設定說明**:
- `AllowAnyHeader()`: 允許任何 HTTP 標頭
- `AllowAnyMethod()`: 允許任何 HTTP 方法 (GET, POST, PUT, DELETE 等)
- `WithOrigins(...)`: 只允許來自指定來源的請求

**測試步驟**:

1. **啟動後端伺服器**:
   ```bash
   cd API
   dotnet run
   ```

2. **啟動前端應用程式**:
   ```bash
   cd client
   npm start
   ```

3. **驗證**:
   - 開啟瀏覽器訪問 `http://localhost:4200`
   - 開啟開發者工具 Console (F12)
   - 應該會看到 API 回傳的資料，而不是 CORS 錯誤

**安全注意事項**:

⚠️ **目前的設定僅適用於開發環境**

生產環境應該使用更嚴格的 CORS 設定：

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

**不要在生產環境使用**:
- ❌ `AllowAnyOrigin()`
- ❌ `AllowAnyHeader()` (除非確實需要)
- ❌ `AllowAnyMethod()` (除非確實需要)

**狀態**: ✅ 已實施

---

## 資料庫問題

### 資料庫連線設定

**設定檔案**: `API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "data source=dating.db"
  }
}
```

**說明**:
- 使用 SQLite 資料庫
- 資料庫檔案位於 `API/dating.db`
- 首次執行需要執行資料庫遷移

**初始化資料庫**:

```bash
cd API
dotnet ef database update
```

**常見問題**:

1. **找不到資料庫檔案**
   - 確認已執行 `dotnet ef database update`
   - 檢查 `API/` 目錄下是否有 `dating.db` 檔案

2. **遷移失敗**
   - 檢查 Entity Framework Core 套件是否已安裝
   - 確認 `Data/Migrations/` 目錄存在

---

## 前端建構問題

### Angular 開發伺服器設定

**啟動指令**:
```bash
cd client
npm start
# 或
ng serve
```

**預設設定**:
- 連接埠: `4200`
- 位址: `http://localhost:4200`

**常見問題**:

1. **連接埠被佔用**
   ```
   Error: Port 4200 is already in use
   ```
   
   **解決方案**:
   ```bash
   # 使用不同的連接埠
   ng serve --port 4201
   ```

---

## CORS 知識補充

### 什麼是 CORS？

**CORS** (Cross-Origin Resource Sharing) = 跨來源資源共用

- 瀏覽器的安全機制，防止惡意網站竊取資料
- 限制網頁只能向**相同來源**的伺服器發送請求

### 什麼是「相同來源」？

來源 = `通訊協定://網域:連接埠`

**只有當這三個部分完全相同時，才算是同源**

| URL | 通訊協定 | 網域 | 連接埠 | 與 `http://localhost:4200` 比較 |
|-----|---------|------|--------|--------------------------------|
| `http://localhost:4200` | http | localhost | 4200 | ✅ 相同來源 |
| `https://localhost:4200` | https | localhost | 4200 | ❌ 不同（協定） |
| `http://localhost:5001` | http | localhost | 5001 | ❌ 不同（埠） |
| `http://127.0.0.1:4200` | http | 127.0.0.1 | 4200 | ❌ 不同（網域） |

### 本專案的跨來源情況

**前端 Angular**: `http://localhost:4200`  
**後端 API**: `https://localhost:5001`

**差異**:
- ❌ 通訊協定不同 (http vs https)
- ❌ 連接埠不同 (4200 vs 5001)

→ **需要 CORS 設定**

### 如何判斷是否為 CORS 錯誤？

#### ✅ 真正的 CORS 錯誤會顯示：

```
Access to XMLHttpRequest at 'https://localhost:5001/api/members' 
from origin 'http://localhost:4200' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

關鍵字：
- "blocked by CORS policy"
- "Access-Control-Allow-Origin"

#### ❌ 不是 CORS 錯誤：

- `ERR_CONNECTION_REFUSED` → 伺服器未執行
- `ERR_NAME_NOT_RESOLVED` → DNS 解析失敗
- `ERR_CERT_AUTHORITY_INVALID` → SSL 憑證問題
- `status: 0` 通常表示連線問題，不是 CORS

### CORS 運作流程

#### 簡單請求 (Simple Request)

```
1. 瀏覽器發送請求
   Browser → GET /api/members → Server

2. 伺服器回應 + CORS Headers
   Server → Response + Access-Control-Allow-Origin: http://localhost:4200 → Browser

3. 瀏覽器檢查
   ✅ Headers 正確 → 允許 JavaScript 存取資料
   ❌ Headers 缺少 → 阻擋請求，顯示錯誤
```

#### 預檢請求 (Preflight Request)

對於複雜請求（如 PUT、DELETE 或自訂 headers），瀏覽器會先發送 OPTIONS 請求：

```
1. 預檢請求
   Browser → OPTIONS /api/members → Server

2. 預檢回應
   Server → Access-Control-Allow-Methods: GET, POST, PUT, DELETE → Browser

3. 實際請求（如果預檢通過）
   Browser → PUT /api/members → Server
```

### 除錯技巧

#### 1. 檢查瀏覽器開發者工具

**Console 標籤**:
- 查看是否有 CORS 錯誤訊息

**Network 標籤**:
1. 重新載入頁面觸發請求
2. 點擊失敗的請求
3. 查看 **Response Headers**

**應該包含**:
```
Access-Control-Allow-Origin: http://localhost:4200
Access-Control-Allow-Methods: GET, POST, PUT, DELETE
Access-Control-Allow-Headers: Content-Type, Authorization
```

#### 2. 使用 curl 測試

```bash
# 測試 API 是否正常運作（不受 CORS 限制）
curl https://localhost:5001/api/members -k

# 測試 CORS headers
curl https://localhost:5001/api/members \
  -H "Origin: http://localhost:4200" \
  -k -v
```


---

## 參考資源

### 官方文件

- [MDN - CORS 跨來源資源共用](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/CORS)
- [ASP.NET Core - 啟用 CORS](https://learn.microsoft.com/zh-tw/aspnet/core/security/cors)
- [Angular - HttpClient](https://angular.dev/guide/http)

### 相關工具

- [Postman](https://www.postman.com/) - API 測試工具
- [curl](https://curl.se/) - 命令列 HTTP 客戶端
- [Browser DevTools](https://developer.chrome.com/docs/devtools/) - 瀏覽器開發者工具

### 專案相關文件

- [`DatingApp/client/README.md`](DatingApp/client/README.md ) - 前端專案說明
- `API/README.md` - 後端專案說明（待建立）



---

**文件版本**: 1.0.0  
**最後更新**: 2025年10月25日  
**維護者**: DatingApp 開發團隊
