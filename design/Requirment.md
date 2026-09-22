Quy trình:

1. Khách inbox hỏi váy
2. Staff check váy có bận hay không
3. Staff tư vấn size, tên váy, giá thuê, tiền cọc,...
4. Staff nhận chuyển khoản tiền cọc(cọc giữ slot = 100k / cọc 50% giá trị váy + ảnh CCCD / cọc 100% giá trị váy - ko cần cccd)
5. Staff lấy mã váy, mã phụ kiện, mã OTP, Link điền form gửi khách(mục đích của mã OTP là để tránh việc khách điền form spam nộp lung tung)
6. Sau khi khách điền form, gửi form => hiện đơn về hệ thống => hiển thị các trạng thái, ngày thuê,...
7. Sau khi khách trả váy về, tính toán các chi phí để hoàn tiền cho khách theo 2 trường hợp
TH1: không có hư hại => tiền hoàn khách = tiền cọc (theo gói 50 hoặc 100) - tiền phí thuê
TH2: Có hư hại => tiền hoàn khách = tiền cọc (theo gói 50 hoặc 100) - tiền phí thuê - tiền phí xử lý hư hại
8. Hoàn tiền khách => chuyển trạng thái đơn, váy/phụ kiện => váy available trở lại sau clean time 
9. Hoàn tất vòng đời cho thuê 1 chiếc váy


Các thông tin đang được quản lý của váy, size, giá váy, gói thuê, giá thuê theo gói
Afrodille gấm	S	S: 84 x 64-66 x 88	1.200.000 đ	12 h	190.000₫
Afrodille gấm	S	S: 84 x 64-66 x 88	1.200.000 đ	1 day	220.000₫
Afrodille gấm	S	S: 84 x 64-66 x 88	1.200.000 đ	3 days	320.000₫
Afrodille gấm	L	L: 92 x 72-74 x 96	1.200.000 đ	12 h	190.000₫
Afrodille gấm	L	L: 92 x 72-74 x 96	1.200.000 đ	1 day	220.000₫
Afrodille gấm	L	L: 92 x 72-74 x 96	1.200.000 đ	3 days	320.000₫
Afrodille trắng	S	S fit M : 84 x 64-66 x 88	1.550.000 đ	12 h	240.000₫

Lưu ý:
1. Nếu khách muốn thuê nhiều hơn 3 ngày thì từ ngày thứ 4 trở đi mỗi ngày sẽ tính 10% giá thuê 1 ngày
2. Đến cuối cùng khách hàng sẽ cần cọc theo 1 trong hai gói là 50% + CCCD hoặc 100% giá váy, nếu khách đã cọc giữ slot 100k trước thì số tiền còn lại cần cọc của khách là total cọc - 100k 

Hệ thống cần đáp ứng được những yếu tố sau đây:
1. Quản lý product (váy / phụ kiện,...)
2. Quản lý product đó có đang bận không
3. Quản lý trạng thái đơn của khách, các thông tin khách, trạng thái ship,...
4. Manager có khả năng tự setting giá thuê theo gói, giá váy, phần trăm giá nếu gói thuê > 4 ngày
5. Quản lý đưuọc só tiền khách cần cọc còn lại  
6. Quản lý được số tiền shop cần hoàn lại khách (cách tính theo 2 trường hợp ở mục 7 phía trên)
7. Thông báo các đơn mới đẩy về shop, các thông tin nhân viên vừa chỉnh sửa để manager nắm được thông tin nhanh chóng
8. Performance tối ưu
9. Responsive và UI/UX tối ưu cho giao diện mobile