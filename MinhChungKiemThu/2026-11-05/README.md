# Minh chứng kiểm thử ngày 05/11/2026

- Source dự án: D:/MiniSupermarket.API-upload, solution chính gồm API và WinForms.
- Kiểm thử tự động: 147 đạt, 0 thất bại, 0 bỏ qua; cấu hình FeatureVerified. Xem feature-upgrade-final.trx.
- SQL Server thực tế: LocalDB, database MinhKhangMiniSupermarket_Test_20261105. Bán/hủy thử không tác động dữ liệu cửa hàng chính.
- sql_api_evidence.json: số liệu trước/sau, phản hồi thanh toán, rollback thiếu hàng/điểm, KPI và tổng doanh thu.
- Doanh_thu_test.csv: xuất bằng nút trên WinForms, UTF-8 BOM, 7 dòng dữ liệu, tổng thực thu 453.000 đồng.
- Ảnh 01/03/04/05/06 và hộp thoại 02: chụp ứng dụng thật; ảnh 04_loyalty_detail là phần cắt từ ảnh 04 để đọc rõ số tiền.
- Bộ dữ liệu demo: 20 hóa đơn seed được phân bổ vào 7 ngày 30/10–05/11; thêm một hóa đơn đổi điểm và một khách mới để minh họa.
- Báo cáo Word có ảnh tại mục 5.4.3, bảng đối chiếu các tiêu chí giao diện tại phụ lục G.

CSDL chính đã được áp dụng migration DashboardLoyalty, giữ nguyên 21 hóa đơn, 20 sản phẩm và tổng điểm 2.148 so với trước nâng cấp. Không cần drop database để sử dụng các chức năng mới.

## Kiểm tra trước khi đưa lên GitHub

Bản cập nhật biểu đồ được build ở cấu hình Release, không lỗi/cảnh báo. Bộ test chạy lại đạt 149/149, không lỗi hoặc bỏ qua. Xem [nhật ký Release](publish-verification.trx) và [kết quả tổng hợp](publish-verification.json). Nhật ký `feature-upgrade-final.trx` phía trên thuộc lần kiểm tra 147 trường hợp trước khi bổ sung hai ca tìm ngày hóa đơn gần nhất.

## Cập nhật quyền Cashier

Cashier được bán hàng POS và quản lý khách hàng (xem, tìm kiếm, thêm, sửa, xóa), áp dụng ở cả WinForms và API. Bộ test Release chạy lại đạt 156/156, không thất bại hoặc bỏ qua; build không lỗi/cảnh báo. Các ca mới kiểm tra CRUD của Cashier, đúng hai menu, 401/403 cho người chưa đăng nhập/Warehouse, kiểm tra dữ liệu và chặn xóa khách đã có hóa đơn. Xem [kết quả tổng hợp](cashier-customer-verification.json). Hai nhật ký TRX phía trên thuộc các lần kiểm tra trước khi đổi quyền này.
