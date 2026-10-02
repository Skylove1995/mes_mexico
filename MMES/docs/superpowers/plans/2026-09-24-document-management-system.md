# Kế hoạch Thiết kế & Triển khai Hệ thống Quản lý Tài liệu Nhà máy (Factory Document Management System - DMS)

**Mục tiêu:** Xây dựng hệ thống quản lý tài liệu tập trung chuẩn nhà máy sản xuất (ISO / IATF 16949), hỗ trợ phân loại đa chiều (Loại tài liệu, Bộ phận, Công đoạn, Máy móc, Model), lưu trữ lịch sử phiên bản (Version control), luồng phê duyệt bởi Admin và giao diện Dashboard quản lý 3 cột hiện đại theo đúng thiết kế mẫu.

---

## I. THIẾT KẾ CƠ SỞ DỮ LIỆU SQL SERVER (DATABASE SCHEMA)

Để đảm bảo tính linh hoạt (thêm/bớt dữ liệu Master Data không cần sửa code), CSDL được thiết kế theo mô hình chuẩn hóa (Normalized 3NF) kết hợp bảng liên kết Nhiều-Nhiều (Many-to-Many).

### 1. Các bảng Master Data (Danh mục dùng chung)
- **`DmsDocumentTypes`** (Loại tài liệu - Ảnh 1):
  - `Id` (INT, PK, IDENTITY)
  - `TypeCode` (NVARCHAR(50), UNIQUE) - e.g. `WI`, `SOP`, `PFD`, `PFMEA`, `CONTROL_PLAN`
  - `TypeName` (NVARCHAR(100)) - e.g. `Work Instruction`, `Process`, `Checksheet`, `Standard`
  - `IconClass` (NVARCHAR(50)) - Icon hiển thị ở Sidebar
  - `DisplayOrder` (INT)
  - `IsActive` (BIT, Default: 1)

- **`DmsDepartments`** (Bộ phận):
  - `Id` (INT, PK, IDENTITY)
  - `DeptCode` (NVARCHAR(50), UNIQUE) - e.g. `QA`, `CS`, `PCBA`, `RND`, `SMT`, `FG`, `WH`, `HSEVN`
  - `DeptName` (NVARCHAR(100))
  - `BadgeColor` (NVARCHAR(30)) - Màu badge đại diện (Hex/CSS Class)
  - `IsActive` (BIT, Default: 1)

- **`DmsProcesses`** (Công đoạn sản xuất):
  - `Id` (INT, PK, IDENTITY)
  - `ProcessCode` (NVARCHAR(50), UNIQUE) - e.g. `PRINTING`, `MOUNT`, `SPI`, `AOI`, `MOI`, `COATING`
  - `ProcessName` (NVARCHAR(100))
  - `IsActive` (BIT, Default: 1)

- **`DmsMachines`** (Máy móc thiết bị):
  - `Id` (INT, PK, IDENTITY)
  - `MachineCode` (NVARCHAR(50), UNIQUE) - e.g. `AOI_01`, `AUTO_LABEL`, `AUTO_VISION`, `BONDING_AXXON`
  - `MachineName` (NVARCHAR(100))
  - `ProcessId` (INT, FK -> `DmsProcesses.Id`, Optional)
  - `IsActive` (BIT, Default: 1)

- **`DmsModels`** (Dòng sản phẩm / Model):
  - `Id` (INT, PK, IDENTITY)
  - `ModelCode` (NVARCHAR(50), UNIQUE) - e.g. `AMT`, `AR_HUD`, `AUDI`, `AUDI_CID`
  - `ModelName` (NVARCHAR(100))
  - `CustomerName` (NVARCHAR(100), Optional)
  - `IsActive` (BIT, Default: 1)

---

### 2. Các bảng Quản lý Tài liệu chính
- **`DmsDocuments`** (Bảng thông tin Tài liệu tổng thể):
  - `Id` (INT, PK, IDENTITY)
  - `DocumentNumber` (NVARCHAR(100), UNIQUE, NOT NULL) - Mã tài liệu duy nhất (e.g. `HSEVN-IVI-LQC-WI-024`)
  - `Title` (NVARCHAR(255), NOT NULL) - Tên tài liệu
  - `TypeId` (INT, FK -> `DmsDocumentTypes.Id`, NOT NULL)
  - `DepartmentId` (INT, FK -> `DmsDepartments.Id`, NOT NULL)
  - `CurrentVersion` (NVARCHAR(20), Default: 'v1.0')
  - `Status` (NVARCHAR(20), Default: 'Draft') - `Draft`, `Pending`, `Approved`, `Rejected`, `Obsolete`
  - `CreatedBy` (NVARCHAR(100), NOT NULL) - Người tạo
  - `CreatedAt` (DATETIME2, Default: GETDATE())
  - `ApprovedBy` (NVARCHAR(100), NULL) - Admin phê duyệt
  - `ApprovedAt` (DATETIME2, NULL)
  - `Description` (NVARCHAR(MAX), NULL)

- **`DmsDocumentVersions`** (Lịch sử các phiên bản File):
  - `Id` (INT, PK, IDENTITY)
  - `DocumentId` (INT, FK -> `DmsDocuments.Id`, NOT NULL)
  - `VersionNumber` (NVARCHAR(20), NOT NULL) - e.g. `v1.0`, `v1.1`, `v2.0`
  - `FilePath` (NVARCHAR(500), NOT NULL) - Đường dẫn file vật lý trên Server
  - `FileName` (NVARCHAR(255), NOT NULL) - Tên file gốc người dùng upload
  - `FileSize` (BIGINT) - Dung lượng (Bytes)
  - `FileExtension` (NVARCHAR(10)) - `.pdf`, `.docx`, `.xlsx`
  - `FileHash` (NVARCHAR(64)) - Mã SHA256/MD5 chống trùng lặp/giả mạo
  - `ChangeSummary` (NVARCHAR(MAX)) - Tóm tắt nội dung thay đổi phiên bản
  - `UploadedBy` (NVARCHAR(100), NOT NULL)
  - `UploadedAt` (DATETIME2, Default: GETDATE())
  - `IsActiveVersion` (BIT, Default: 1) - Đang là bản phát hành hiện tại

---

### 3. Bảng Liên kết Đa chiều (Many-to-Many Mappings)
Do 1 tài liệu có thể áp dụng cho **nhiều Model**, **nhiều Công đoạn** hoặc **nhiều Máy**:

- **`DmsDocumentProcesses`**:
  - `DocumentId` (INT, FK -> `DmsDocuments.Id`)
  - `ProcessId` (INT, FK -> `DmsProcesses.Id`)
  - PRIMARY KEY (`DocumentId`, `ProcessId`)

- **`DmsDocumentMachines`**:
  - `DocumentId` (INT, FK -> `DmsDocuments.Id`)
  - `MachineId` (INT, FK -> `DmsMachines.Id`)
  - PRIMARY KEY (`DocumentId`, `MachineId`)

- **`DmsDocumentModels`**:
  - `DocumentId` (INT, FK -> `DmsDocuments.Id`)
  - `ModelId` (INT, FK -> `DmsModels.Id`)
  - PRIMARY KEY (`DocumentId`, `ModelId`)

---

## II. QUY TRÌNH & THÔNG TIN NHẬP LIỆU (DOCUMENT INTAKE WORKFLOW)

### 1. Thông tin cần thu thập khi Upload Tài liệu mới (`Upload New Document`)
Khi người dùng bấm **`Upload New Document`**, hệ thống mở Modal Popup yêu cầu nhập:

1. **Mã Tài liệu (Document ID / Number):** Tự động gợi ý theo format nhà máy (VD: `HSEVN-IVI-{Dept}-{Type}-{No}`) hoặc cho nhập tay.
2. **Tên Tài liệu (Document Title):** Bắt buộc (VD: *HƯỚNG DẪN TẠO CHƯƠNG TRÌNH MÁY MOI/AOI*).
3. **Loại Tài liệu (Type):** Dropdown chọn từ `DmsDocumentTypes` (Work Instruction, Checksheet, Standard, PFD, PFMEA, v.v.).
4. **Bộ phận Sở hữu (Department):** Dropdown chọn từ `DmsDepartments` (QA, CS, PCBA, SMT, R&D, WH, v.v.).
5. **Phiên bản khởi tạo (Version):** Mặc định `v1.0`.
6. **Công đoạn áp dụng (Processes):** Multi-select Checkbox (VD: AOI, SPI, Coating...).
7. **Máy móc áp dụng (Machines):** Multi-select Checkbox (VD: AOI_01, AUTO_LABEL...).
8. **Dòng sản phẩm áp dụng (Models):** Multi-select Checkbox (VD: AUDI, AR HUD, All Models...).
9. **File đính kèm (Document File):** Định dạng `.pdf`, `.docx`, `.xlsx`, `.pptx` (Giới hạn max 50MB).
10. **Mô tả / Ghi chú (Notes):** Không bắt buộc.

---

### 2. Luồng Phê duyệt & Cập nhật Version (Workflow Steps)

```
[Người dùng Upload] ──> Tạo bản ghi Document (Trạng thái: Draft / Pending)
                             │
                             ├──> Admin xem xét ──> [Từ chối (Rejected)] ──> Báo người dùng sửa
                             │
                             └──> [Admin Phê duyệt (Approved)]
                                       │
                                       ├──> Tài liệu hiển thị công khai trên Dashboard cho toàn nhà máy
                                       │
                                       └──> Khi cần sửa đổi ──> Nút [Update Version]
                                                                     │
                                                                     ├──> Nhập Version mới (e.g. v1.1 / v2.0)
                                                                     ├──> Tải File mới + Nhập Lý do sửa đổi (Change Summary)
                                                                     └──> Admin Duyệt phiên bản mới ──> Bản mới thành Active
```

---

## III. THIẾT KẾ GIAO DIỆN FRONTEND (ĐỐI CHIẾU 100% VỚI ÁNH 2)

Giao diện được xây dựng chuẩn Responsive, hỗ trợ chế độ Theme Sáng/Tối tự động của MES 3.0 với 3 khu vực chính:

### 1. Sidebar Trái (Category Navigation & Counts)
- Hiển thị danh sách 11 Loại Tài liệu (từ `DmsDocumentTypes`) kèm Icon và Badge đếm số lượng tài liệu `Approved`:
  - `All Categories` (Tổng số)
  - `Process` (210)
  - `Checksheet` (205)
  - `Standard` (95)
  - `Common` (0)
  - `CSR` (7)
  - `IFP` (2)
  - `Work Instruction` (550)
  - `PFD` (10)
  - `PFMEA` (0)
  - `Maintenance` (41)
  - `Control Plan` (42)
- Nhấp vào mục nào sẽ tự động lọc danh sách ở giữa theo Type đó.

### 2. Khu vực Trung tâm (Header Bar, Dept Filter Tabs & Main Table)
- **Top Header:** Tiêu đề `DOCUMENT MANAGEMENT DASHBOARD`.
- **Action Buttons:**
  - Nút `Upload New Document` (Màu xanh dương) -> Mở Modal Upload.
  - Nút `Update Version` (Màu xanh lá) -> Mở Modal chọn tài liệu và cập nhật file mới.
- **Search Bar:** Tìm kiếm nhanh theo mã Document ID, Tiêu đề hoặc Người tạo.
- **Department Filter Pills (Scroll ngang):** `All Departments`, `CS`, `ESD`, `ESH`, `HR`, `HSEVN`, `IQC`, `IT`, `LQC SMT`, `OQC`, `PCBA`, `PM PCBA`, `PM SMT`, `QA`, `RD`, `REPAIR`.
- **Main Table (Bảng danh sách tài liệu):**
  - Cột: `Document ID` (Font mono bold), `Document Title`, `Type` (Badge xanh lá), `Version` (e.g. `v1.0`), `Status` (Badge xám/xanh `Draft`/`Approved`), `Department` (Badge màu đại diện bộ phận).
  - Phân trang (Pagination) & Tải dữ liệu bất đồng bộ (AJAX/Fetch).

### 3. Sidebar Phải (Advanced Filters - Bộ lọc Nâng cao)
- **Filter by Process:** Checkbox danh sách Công đoạn (All Processes, AOI, Auto label, Buffer, Coating...).
- **Filter by Machine:** Checkbox danh sách Máy (All Machines, AOI, AUTO LABEL, AUTO VISION, BONDING AXXON...).
- **Filter by Model:** Checkbox danh sách Model (All Models, AMT, AR HUD, AUDI, AUDI CID...).
- **Filter by Status:** Checkbox danh trạng thái (All Statuses, Draft, Approved, Pending Review...).

---

## IV. LỘ TRÌNH TRIỂN KHAI CỤ THỂ (IMPLEMENTATION STEPS)

- **Bước 1:** Viết SQL DDL Scripts tạo toàn bộ 8 bảng CSDL (`DmsDocumentTypes`, `DmsDepartments`, `DmsProcesses`, `DmsMachines`, `DmsModels`, `DmsDocuments`, `DmsDocumentVersions`, các bảng liên kết) và chèn dữ liệu mẫu (Seed Data).
- **Bước 2:** Xây dựng C# Models, DTOs & Services backend trong `MMES` project (`DmsService.cs`, `IDmsService.cs`).
- **Bước 3:** Xây dựng API Controller (`Controllers/Api/DocumentManagementApiController.cs`) phục vụ AJAX Data Fetch, Filter, Upload File & Approval.
- **Bước 4:** Xây dựng Giao diện Razor View `Views/Document/Index.cshtml`, CSS `wwwroot/css/dms-dashboard.css` & JavaScript `wwwroot/js/dms-dashboard.js`.
- **Bước 5:** Xây dựng Modal Quản lý Master Data (Thêm/Sửa/Xóa Department, Process, Machine, Model) dành cho Admin.
- **Bước 6:** Kiểm thử tích hợp (Upload file, Filter đa chiều, Phê duyệt tài liệu, Tải xuống file).
