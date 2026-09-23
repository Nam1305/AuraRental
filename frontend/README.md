# Aura Rental frontend

Frontend là React + TypeScript + Vite, tổ chức theo feature để code của một màn hình nằm gần type và API của chính nó.

```text
src/
├── app/                    App root, route map, shell và global styles
├── features/
│   ├── session/            User, branch selection và quyền phiên làm việc
│   ├── availability/       Tra cứu đồ trống
│   ├── catalog/            Catalog/kho theo branch
│   ├── customers/          CRM và lịch sử thuê
│   ├── reservations/       Boundary màn giữ chỗ
│   ├── orders/             Boundary màn đơn hàng
│   ├── returns/            Boundary inspection/refund
│   ├── reports/            Boundary báo cáo
│   └── settings/           Boundary cấu hình
└── shared/                 HTTP client, hooks, formatters và UI dùng chung
```

## Nguyên tắc

- Mobile-first: bottom navigation, form một cột và card list; desktop mới mở sidebar/grid.
- Feature không import trực tiếp code nội bộ của feature khác, ngoại trừ session là cross-cutting concern.
- Mọi request đi qua `shared/api/http-client.ts`; client tự gắn access token và chỉ gắn `X-Branch-Id` khi API cần branch.
- `SessionProvider` lấy branch từ `GET /api/v1/me`. Một branch thì tự chọn; nhiều branch thì giữ lựa chọn hợp lệ gần nhất.
- Không tính availability, giá, cọc hoặc tiền hoàn ở frontend. UI chỉ hiển thị kết quả backend.
- Không tạo một `types.ts` toàn cục khổng lồ; type thuộc feature đặt cạnh API của feature.

## Trạng thái hiện tại

Các màn đăng nhập email/username + password, shell/branch switcher, availability, catalog, customer list/history, giữ chỗ, đơn hàng và trả đồ đã gọi API thật.

- Kho sản phẩm: staff và manager được tạo mẫu, thêm size, sửa giá `1D/2D/3D`, thêm mã vật lý và cập nhật trạng thái kho trong chi nhánh đang đăng nhập. Chỉ manager được sửa metadata dùng chung hoặc archive/khôi phục mẫu.
- Giữ chỗ: tìm khách, kiểm tra mã đồ trống, chọn gói thuê, xem báo giá, ghi nhận cọc, cấp lại OTP và hủy.
- Đơn hàng: theo dõi timeline, xác minh CCCD, bổ sung cọc, chuẩn bị đồ, giao/nhận và hủy theo trạng thái được backend cho phép.
- Trả đồ: kiểm tra từng món, lập/gửi/duyệt phiếu hoàn, ghi nhận thu thêm và hoàn tất đối soát. Các bước duyệt/settle chỉ hiện với manager.

Reports và settings hiện vẫn là route/boundary có chủ đích. Ảnh sản phẩm và ảnh hư hại được chọn từ máy staff/manager; frontend xin presigned PUT URL từ backend, upload thẳng lên R2, rồi gửi `objectPath` cho API nghiệp vụ.

## Chạy local

```bash
cp .env.example .env
npm install
npm run dev
```

Build kiểm tra type và bundle:

```bash
npm run build
```

Frontend gọi `POST /api/v1/auth/login`, lưu access token nhận từ backend và dùng token đó cho các API nội bộ. Role và quyền chi nhánh luôn lấy từ `GET /api/v1/me`, không lấy từ input đăng nhập.

Vì app dùng history-style URL, reverse proxy cần fallback các route frontend về `index.html`, nhưng không fallback `/api/*`.
