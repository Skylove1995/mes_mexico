# Tổng Quan Hệ Thống MMES (Manufacturing Execution System)

## 1. Ngôn ngữ & Công nghệ

**Backend**
- C# / ASP.NET Core 8.0 (MVC pattern, không phải API thuần)
- Entity Framework Core 8.0.7 (ORM)
- SignalR (real-time, dùng cho Hub như `SnakeHub`)
- Kestrel làm web server nội bộ, lắng nghe cổng **9999**

**Frontend**
- Chủ yếu: Razor Views (`.cshtml`) + JavaScript thuần + CSS trong `wwwroot`
- Một phần nhỏ dùng React: `MMES/Frontend/audit-document-react` (React 18 + TypeScript + Vite + @antv/x6 để vẽ sơ đồ), build ra bundle rồi nhúng vào view

**Database (đa nguồn, kết nối song song)**
- MySQL 8.0 (Pomelo.EntityFrameworkCore.MySql) — DB chính `mex_mes`, quản lý AOI/SPI, kế hoạch sản xuất, thiết bị, nhân sự
- Oracle (Oracle.EntityFrameworkCore) — DB tích hợp hệ MES cũ `Hsevnpdb`, quản lý tồn kho, lệnh sản xuất
- SQL Server (Microsoft.Data.SqlClient) — kết nối BioStar2 (chấm công/kiểm soát ra vào)
- PostgreSQL (Npgsql) — kết nối hệ Wismax

**Thư viện phụ trợ đáng chú ý**
- ClosedXML, Spire.XLS/Doc/Presentation — đọc/ghi Excel, Word, PowerPoint
- CsvHelper — import/export CSV (log máy AOI/SPI)
- PDFsharp, PuppeteerSharp — xuất PDF, chụp trang web thành ảnh/PDF
- Telegram.Bot — bot Telegram gửi cảnh báo
- EFCore.BulkExtensions.MySql — bulk insert/update hiệu năng cao

## 2. Kiến trúc & Cấu trúc thư mục

```
MMES/
├── Controllers/        # MVC controllers + API endpoints
│   ├── ViewController/ # Điều hướng UI theo domain (SMT, Quality, SCM, HR...)
│   └── Api/             # Endpoint trả JSON cho frontend JS/React gọi
├── Models/              # Entity/DTO, có submodule Models/HSMES cho Oracle
├── Views/                # Razor views (.cshtml)
├── Services/            # Business logic + Background Services (IHostedService)
├── Data/                # DbContext (MMesDbContext, HsMesDbContext...)
├── Hubs/                 # SignalR hubs
├── Migrations/            # EF Core migrations (MySQL)
├── Frontend/audit-document-react/  # SPA React con, build riêng rồi nhúng
└── wwwroot/               # Static assets (JS/CSS/lib/images/uploads)
```

**Mô hình xử lý nền (Background Services)** — đây là điểm đặc trưng của hệ thống:
mỗi nghiệp vụ tự động (quét log AOI, cảnh báo hết hạn mẫu, đồng bộ dữ liệu khách hàng, cảnh báo qua Telegram/email...) được cài như một `IHostedService` riêng, đăng ký trong `Program.cs`. Ví dụ: `AoiScanService` quét thư mục log CSV mỗi 2 phút, `AuditEmailBackgroundService` gửi email nhắc audit, `TelegramBotService` chạy song song vừa là hosted service vừa inject được vào controller.

## 3. Deploy hiện tại

- **Không có Docker, không có CI/CD pipeline** (không tìm thấy Dockerfile hay file `.yml` nào trong repo).
- Deploy bằng **Publish Profile kiểu FileSystem** (`Properties/PublishProfiles/FolderProfile.pubxml`):
  - Build cấu hình Release, publish thẳng ra thư mục `bin\Release\net8.0\publish\`
  - Có sinh `web.config` (dấu hiệu host bằng **IIS** + ASP.NET Core Module, hoặc chạy Kestrel đứng sau reverse proxy)
- Ứng dụng tự lắng nghe cổng 9999 qua Kestrel (`options.ListenAnyIP(9999)` trong `Program.cs`), nên khả năng cao có một IIS/Nginx đứng trước làm reverse proxy sang cổng này.
- Frontend React con (`audit-document-react`) build riêng bằng `npm run build` (Vite) rồi output được copy/nhúng vào `wwwroot` — build tách biệt khỏi vòng publish .NET, phải nhớ build tay hoặc thêm script trước khi publish.
- Connection string tới MySQL đang hardcode trong `Data/MMesDbContext.cs` (ghi chú kỹ thuật, cần lưu ý khi đổi môi trường).

**Tóm tắt quy trình deploy thủ công:**
1. `cd MMES/Frontend/audit-document-react && npm install && npm run build` (nếu có sửa phần React)
2. Visual Studio hoặc `dotnet publish -c Release` dùng profile `FolderProfile`
3. Copy nội dung `bin\Release\net8.0\publish\` lên server
4. Cấu hình IIS site trỏ vào thư mục đó, hoặc chạy trực tiếp `MMES.exe`/`dotnet MMES.dll` sau reverse proxy tới cổng 9999

## 4. Muốn xây webapp tương tự — cần làm gì

### Bước 1 — Chọn stack nền
- ASP.NET Core 8 MVC (hoặc Razor Pages nếu đơn giản hơn) là lựa chọn hợp lý nếu team quen C#, cần tích hợp nhiều DB doanh nghiệp (MySQL/Oracle/SQL Server) và cần Windows/IIS hosting.
- Nếu không bị ràng buộc hệ thống cũ, cân nhắc ASP.NET Core Web API + SPA (React/Vue) tách biệt hoàn toàn — dễ scale, dễ test hơn mô hình MVC pha Razor + JS rời rạc như hiện tại.

### Bước 2 — Thiết kế data layer
- Dùng EF Core, một `DbContext` cho mỗi nguồn dữ liệu nếu phải tích hợp nhiều DB (như MMES đang làm).
- Đặt connection string trong `appsettings.json` / User Secrets / biến môi trường — **không hardcode** (khác với cách MMES đang làm, đây là nợ kỹ thuật nên tránh).
- Viết Migration ngay từ đầu để version hoá schema.

### Bước 3 — Business logic & tác vụ nền
- Tách logic nghiệp vụ vào `Services/`, không nhét thẳng vào Controller.
- Tác vụ định kỳ (quét file, gửi cảnh báo, đồng bộ dữ liệu) implement `IHostedService`/`BackgroundService`, đăng ký qua `AddHostedService` — mô hình này của MMES dùng tốt, nên tái dùng.
- Nếu cần real-time (dashboard, thông báo tức thời) dùng SignalR Hub.

### Bước 4 — Frontend
- Razor View đơn giản, ít tương tác → dùng .cshtml + JS thuần như MMES.
- Component phức tạp, nhiều tương tác (vẽ sơ đồ, form động) → tách thành SPA nhỏ (React/Vite) build riêng rồi nhúng, giống module `audit-document-react`. Cách này giữ core đơn giản mà vẫn có chỗ cho phần UI phức tạp.

### Bước 5 — Auth & bảo mật
- MMES hiện dùng Session-based (`AddSession`, cookie `MMES`), phần `AddAuthentication` bị comment out — nghĩa là đang tự quản lý login qua session thủ công, chưa dùng cookie auth chuẩn của ASP.NET Identity.
- Khuyến nghị cho hệ thống mới: dùng `AddAuthentication().AddCookie()` chuẩn hoặc ASP.NET Identity/JWT thay vì tự chế qua session, dễ bảo trì và audit hơn.

### Bước 6 — Deploy
- Thêm Dockerfile để container hoá ngay từ đầu (MMES hiện chưa có, nên phải deploy thủ công qua IIS).
- Thêm pipeline CI/CD (GitHub Actions/Azure DevOps) build + test + publish tự động, tránh publish tay như hiện tại.
- Nếu vẫn cần chạy trên Windows Server nội bộ (thường gặp ở nhà máy/manufacturing), IIS + ASP.NET Core Module vẫn là lựa chọn hợp lệ, nhưng nên container hoá song song để dễ di chuyển sau này.

### Bước 7 — Testing
- Dự án có sẵn project test riêng (`MMES.Tests`, dùng MSTest/xUnit theo cấu trúc thư mục domain). Nên set up test project song song với source từ ngày đầu, không để dồn về sau.

## 5. Điểm nên tránh lặp lại (nợ kỹ thuật quan sát được)
- Connection string hardcode trong code — nên đưa ra config/secret ngay từ đầu.
- Nhiều file rác/tạm ở root project (`*.txt`, `*.py`, `temp_*`) lẫn vào source — nên có `.gitignore` chặt và thư mục `scripts/`/`tmp/` riêng ngoài git.
- Không có Docker/CI — nên thiết lập từ đầu dự án mới, tránh nợ lại như hiện tại.
- Auth tự chế qua Session thay vì dùng cơ chế chuẩn của framework — nên dùng Identity/cookie auth chuẩn.
