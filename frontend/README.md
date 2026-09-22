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

Các màn đăng nhập development token, shell/branch switcher, availability, catalog, customer list và customer history đã gọi API thật. Các màn reservations, orders, returns, reports và settings là route/boundary có chủ đích; chúng hiển thị endpoint sẽ nối thay vì mock dữ liệu giả.

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

Production nên cấu hình `VITE_AUTH_LOGIN_URL` tới auth provider. Ô dán JWT chỉ là công cụ local; JWT phải có claim `sub` là UUID trùng `users.auth_subject`.

Vì app dùng history-style URL, reverse proxy cần fallback các route frontend về `index.html`, nhưng không fallback `/api/*`.
