# Kế hoạch Thiết kế Theme Sáng/Tối theo giờ cho Trang /Quality/OqcScanout

**Mục tiêu:** Chuyển đổi giao diện trang `/Quality/OqcScanout` từ chế độ Dark cố định sang hệ thống Theme tự động theo giờ (Sáng: 06:00–17:59, Tối: 18:00–05:59), đồng bộ 100% với bảng màu hệ thống (`theme.css`) và script tự động cập nhật real-time (`theme-clock.js`).

**Kiến trúc:**
1. **`wwwroot/css/oqc-scanout.css` (Mới):** Đĩnh nghĩa các style đặc thù cho trang OQC Scanout (Header control, Matrix Table, Heatmap cells, Loading Overlay, Chart Container) dựa trên biến CSS Token (`--bg-app`, `--bg-surface`, `--text-primary`, `--border-subtle`, v.v.).
2. **`Views/Quality/OqcScanout.cshtml` (Cập nhật):**
   - Thay thế script nhúng cứng `data-theme="dark"` thành snippet kiểm tra giờ khởi tạo.
   - Nhúng `theme.css`, `dashboard.css`, `oqc-scanout.css` và `theme-clock.js`.
   - Refactor các lớp màu Tailwind hardcoded (như `bg-[#0B0F17]`, `bg-slate-900`, `border-slate-800`) thành các lớp theme-aware.
   - Cập nhật hàm render `Chart.js` để màu sắc các đường lưới (grid), nhãn (ticks), tooltip tự động đổi theo Theme hiện tại.

---

## Danh sách công việc thực hiện (Tasks)

### Task 1: Tạo CSS Theme cho OQC Scanout (`wwwroot/css/oqc-scanout.css`)
- **Mục đích:** Khai báo các lớp CSS dùng chung biến CSS Token từ `theme.css`.
- **Cấu hình lớp màu:**
  - Nền trang & Container chính (`--bg-app`, `--text-primary`).
  - Thanh công cụ & Card container (`--bg-surface`, `--border-subtle`).
  - Ma trận sản lượng (Heatmap table):
    - Đèn tín hiệu High (`>=4000`), Normal (`3000-3999`), Low (`1500-2999`), Critical (`<1500`) tương phản tốt cả nền sáng lẫn tối.
  - Hàng Subtotal & Grand Total phân biệt rõ ca Đêm (Night Shift) và ca Ngày (Day Shift).

### Task 2: Refactor HTML View (`Views/Quality/OqcScanout.cshtml`)
- **Mục đích:** Gắn nhúng hệ thống Theme đồng bộ và loại bỏ hardcode Dark mode.
- **Chi tiết:**
  - Cập nhật `<head>` với mã kiểm tra giờ đồng bộ và nhúng `theme.css`, `oqc-scanout.css`.
  - Nhúng `theme-clock.js` ở cuối `<body>`.
  - Thay thế các class Tailwind nền đen/xám tối cứng thành các class theme hóa.

### Task 3: Cập nhật JavaScript Chart.js & Sự kiện Đổi Theme
- **Mục đích:** Biểu đồ Hourly Scanout Output & UPH Trend hiển thị rõ ràng trên cả Theme Sáng và Tối.
- **Chi tiết:**
  - Trong hàm `renderCharts()`, tự động phát hiện theme (`light` hoặc `dark`).
  - Điều chỉnh màu chữ trục X/Y, đường kẻ ngang (grid line), nền tooltip phù hợp.
  - Đăng ký observer để tự động re-render biểu đồ khi Theme chuyển đổi tự động (mỗi 60s qua `theme-clock.js`).

### Task 4: Kiểm tra & Xác minh (Verification)
- **Mục đích:** Đảm bảo không lỗi hiển thị, độ tương phản chữ đạt WCAG AA, không bị flash sai màu khi tải trang.
- **Cách kiểm tra:**
  - Thử nghiệm trên trình duyệt bằng cách đổi `data-theme` thủ công hoặc giả lập giờ qua Console.
  - Kiểm tra các thành phần: Filter ngày/ca, Bảng Ma trận sản lượng (Matrix), Progress bar Loading, và Chart.js.
