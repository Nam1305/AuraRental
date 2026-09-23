# Kế hoạch module và API — Aura Rental

> Tài liệu để review trước khi phát triển frontend Next.js và backend .NET. Phạm vi bám theo `database/schema.sql` và mockup trong `aura-rental-mockup`. Giao diện nội bộ ưu tiên mobile; khách chỉ dùng form OTP.

## 1. Mục tiêu và nguyên tắc

- Mỗi nhân viên có tài khoản riêng và được gán chi nhánh qua `user_branches`.
- Mọi API nghiệp vụ nội bộ chạy trong ngữ cảnh một chi nhánh.
- Staff chỉ nhìn thấy kho, lịch, bảng giá, reservation và đơn của chi nhánh được cấp quyền.
- Sản phẩm/variant dùng chung toàn hệ thống; mã vật lý và giá thuê thuộc chi nhánh.
- Staff không tạo order thủ công. Order chỉ được sinh khi khách submit form OTP hợp lệ.
- Frontend không tự tính giá cuối, tiền cọc, tiền hoàn hoặc availability. Backend luôn tính lại.
- Tiền dùng số nguyên VND, không gửi chuỗi đã format như `"260.000đ"`.
- Thời gian dùng ISO 8601 có timezone, ví dụ `2026-09-21T14:00:00+07:00`.
- Không xóa dữ liệu đã phát sinh giao dịch. Dùng trạng thái `CANCELLED`, `VOIDED`, `RETIRED` hoặc `isActive`.

## 2. Kiến trúc API chung

### 2.1. Base URL và xác thực

```text
/api/v1
Authorization: Bearer <access-token>
X-Branch-Id: <branch-uuid>
```

`X-Branch-Id` bắt buộc với API theo chi nhánh. Backend kiểm tra:

1. Token hợp lệ và claim `sub` map được tới `users.id`.
2. User đang active.
3. Có dòng tương ứng trong `user_branches`.
4. Chi nhánh đang active.

Nếu user cố gửi branch khác quyền, trả `403 BRANCH_ACCESS_DENIED`.

### 2.2. Response chuẩn

Thành công:

```json
{
  "data": {},
  "meta": {
    "requestId": "req_01J..."
  }
}
```

Danh sách phân trang:

```json
{
  "data": [],
  "meta": {
    "nextCursor": "eyJpZCI6Ii4uLiJ9",
    "hasMore": true,
    "requestId": "req_01J..."
  }
}
```

Lỗi:

```json
{
  "error": {
    "code": "INVENTORY_NOT_AVAILABLE",
    "message": "Mã váy không còn trống trong khoảng thời gian đã chọn.",
    "fields": {
      "inventoryItemId": "Mã này vừa được giữ bởi đơn khác."
    },
    "requestId": "req_01J..."
  }
}
```

### 2.3. Idempotency và concurrency

Các API tạo reservation, ghi payment, submit form khách, duyệt refund và đối soát tiền phải nhận:

```text
Idempotency-Key: <uuid-do-client-tao>
```

Cùng key và cùng payload phải trả lại kết quả cũ. Cùng key nhưng payload khác trả `409 IDEMPOTENCY_KEY_REUSED`.

Backend phải transaction + lock các `inventory_items` theo thứ tự ID cố định trước khi tạo/đổi reservation. Không tin kết quả availability cũ trên màn hình.

### 2.4. Quy ước status

```text
User role: STAFF | MANAGER
Inventory: USABLE | MAINTENANCE | RETIRED | LOST
Reservation: ACTIVE | CONVERTED_TO_ORDER | CANCELLED
Order: PENDING_DEPOSIT | PENDING_VERIFICATION | CONFIRMED |
       PREPARING | RENTING | INSPECTING | COMPLETED | CANCELLED
Payment type: SLOT_DEPOSIT | TARGET_DEPOSIT | ADDITIONAL_COLLECTION |
              REFUND | CANCELLATION_REFUND
Payment status: RECORDED | CONFIRMED | VOIDED
Item condition: GOOD | DAMAGED | MISSING
Refund: DRAFT | SUBMITTED | APPROVED | SUPERSEDED | VOIDED
```

## 3. Bản đồ màn hình và module

| Màn hình | Module chính | API đọc | API thao tác |
|---|---|---|---|
| Đăng nhập | Auth | `GET /me` | `POST /auth/login` |
| Chọn chi nhánh | Branch access | `GET /me` | Không cần API nếu chỉ đổi state frontend |
| Hôm nay | Dashboard | `GET /dashboard/today` | Điều hướng sang module liên quan |
| Tìm đồ trống | Availability | `GET /availability` | Chọn mã để mở báo giá |
| Giữ chỗ | Reservation/quote | `POST /quotes` | `POST /reservations` |
| Link và OTP | Reservation | dữ liệu từ create/detail | `POST /reservations/{id}/otp/reissue` |
| Form khách | Public rental form | `GET /public/rental-forms/{token}` | `POST /public/rental-forms/{token}/submit` |
| Danh sách đơn | Orders | `GET /orders` | Không tạo order tại đây |
| Chi tiết đơn | Orders/payments/fulfillment | `GET /orders/{id}` | cọc, CCCD checklist, giao/nhận |
| Trả đồ | Return/refund | `GET /returns`, `GET /orders/{id}` | inspection, submit, approve, settle |
| Kho sản phẩm | Catalog/inventory/pricing | `GET /products` | tạo/sửa sản phẩm, giá, mã vật lý |
| Hồ sơ khách | Customers | `GET /customers/{id}` | sửa liên hệ, xem lịch sử |
| Báo cáo | Reports | `GET /reports/summary` | Không có write API |
| Cấu hình | Branch/admin/settings | các GET tương ứng | cập nhật branch, staff, settings |

---

## 4. Module Auth và quyền chi nhánh

### Mô tả

Xác định nhân viên là ai, thuộc vai trò nào và được thao tác tại chi nhánh nào. Staff một chi nhánh được vào thẳng chi nhánh đó; manager hoặc user nhiều chi nhánh được chọn.

### Màn hình: Đăng nhập

- Input: email hoặc username và password; role luôn lấy từ backend.
- Thành công: lưu access token, gọi `GET /me`, xác định active branch.
- Không cho frontend tự gán role hoặc danh sách branch.

### API: đăng nhập

Backend Aura Rental trực tiếp xác thực password hash và phát access token nội bộ:

```http
POST /api/v1/auth/login
Content-Type: application/json
```

```json
{
  "identifier": "hanoi",
  "password": "********"
}
```

```json
{
  "data": {
    "accessToken": "eyJ...",
    "tokenType": "Bearer",
    "expiresAt": "2026-09-22T15:00:00Z"
  }
}
```

`users.password_hash` lưu hash PBKDF2 có salt, không lưu password gốc. Username và email được chuẩn hóa chữ thường. JWT có `sub = users.id`; quyền branch vẫn đọc từ database ở mỗi request.

### API: thông tin phiên đăng nhập

```http
GET /api/v1/me
```

Response:

```json
{
  "data": {
    "id": "usr_uuid",
    "name": "Minh Lan",
    "email": "staff.hanoi@aurarental.vn",
    "role": "STAFF",
    "branches": [
      {
        "id": "branch_hn_uuid",
        "code": "HN",
        "name": "Hà Nội · Nguyễn Trãi",
        "address": "128 Nguyễn Trãi, Thanh Xuân"
      }
    ],
    "suggestedBranchId": "branch_hn_uuid"
  }
}
```

Quy tắc frontend:

- `branches.length === 0`: hiện màn không có quyền truy cập.
- `branches.length === 1`: tự đặt branch đó, bộ chọn branch chỉ hiển thị read-only.
- `branches.length > 1`: dùng branch gần nhất trong local storage nếu vẫn còn quyền; nếu không thì dùng `suggestedBranchId`.
- Backend vẫn kiểm tra quyền ở từng request; local storage không phải nguồn bảo mật.

---

## 5. Module Dashboard — Hôm nay

### Mô tả

Tóm tắt công việc cần xử lý của đúng chi nhánh: chờ cọc, giữ chỗ đang hoạt động, giao nhận, trả đồ và hoàn tiền.

### Màn hình: Hôm nay

- Hiển thị tên chi nhánh rõ ở top bar.
- Card số liệu là nút điều hướng tới danh sách đã lọc.
- Chỉ lấy dữ liệu 7 ngày gần nhất hoặc ngày đang xem, không tải toàn bộ đơn.

### API: dashboard ngày

```http
GET /api/v1/dashboard/today?date=2026-09-21
X-Branch-Id: branch_sg_uuid
```

Response:

```json
{
  "data": {
    "date": "2026-09-21",
    "branch": {
      "id": "branch_sg_uuid",
      "code": "SG",
      "name": "Sài Gòn · Quận 3"
    },
    "counters": {
      "pendingDeposit": 3,
      "activeReservations": 2,
      "returnsDue": 1,
      "refundsWaitingApproval": 2
    },
    "nextTask": {
      "type": "CONFIRM_DEPOSIT",
      "referenceId": "order_uuid",
      "title": "Xác nhận cọc · Khánh Linh",
      "subtitle": "600000 · Afrodille gấm S",
      "dueAt": "2026-09-21T11:30:00+07:00"
    },
    "tasks": [
      {
        "type": "DELIVERY",
        "referenceId": "order_uuid",
        "customerName": "Uyên Nhi",
        "label": "Giao đồ",
        "at": "2026-09-21T13:00:00+07:00"
      }
    ]
  }
}
```

Không tạo bảng dashboard; số liệu được query từ reservation/order/refund hiện có và có thể cache ngắn 30–60 giây.

---

## 6. Module Availability — tìm đồ trống

### Mô tả

Staff dùng trong lúc chat Instagram để biết mẫu/size nào còn trống tại chi nhánh và báo đúng giá của chi nhánh đó.

### Màn hình: Tìm đồ trống

- Input tối thiểu: từ khóa, size, ngày giờ nhận, ngày giờ trả.
- Chỉ trả mã vật lý thuộc active branch.
- Kết quả kèm bảng giá các gói đang bán tại branch.
- Không coi kết quả search là giữ chỗ; chỉ `POST /reservations` mới khóa lịch.

### API: tìm availability

```http
GET /api/v1/availability?query=Afrodille&size=S&startAt=2026-09-25T14:00:00%2B07:00&endAt=2026-09-28T14:00:00%2B07:00&limit=20
X-Branch-Id: branch_sg_uuid
```

Response:

```json
{
  "data": {
    "criteria": {
      "startAt": "2026-09-25T14:00:00+07:00",
      "endAt": "2026-09-28T14:00:00+07:00"
    },
    "groups": [
      {
        "productId": "product_uuid",
        "variantId": "variant_uuid",
        "productName": "Afrodille gấm",
        "size": "S",
        "measurements": "84 × 64–66 × 88",
        "availableCount": 7,
        "prices": [
          { "packageCode": "1D", "label": "1 ngày", "price": 260000 },
          { "packageCode": "2D", "label": "2 ngày", "price": 360000 },
          { "packageCode": "3D", "label": "3 ngày", "price": 440000 }
        ],
        "items": [
          {
            "inventoryItemId": "inventory_uuid",
            "assetCode": "SG-AF-GAM-S-04",
            "status": "USABLE",
            "availableForWholePeriod": true
          }
        ]
      }
    ]
  },
  "meta": {
    "nextCursor": null,
    "hasMore": false
  }
}
```

Validation/lỗi:

- `400 INVALID_TIME_RANGE`: `endAt <= startAt`.
- `400 RENTAL_PERIOD_TOO_LONG`: vượt giới hạn vận hành.
- `404 BRANCH_NOT_FOUND` hoặc `403 BRANCH_ACCESS_DENIED`.
- Item `MAINTENANCE`, `RETIRED`, `LOST`, cleaning chưa xong hoặc giao lịch sẽ không xuất hiện.

### API: tổng quan kho theo mẫu/size

```http
GET /api/v1/inventory/summary?query=Afrodille&cursor=&limit=20
X-Branch-Id: branch_sg_uuid
```

Response item:

```json
{
  "variantId": "variant_uuid",
  "productName": "Afrodille gấm",
  "size": "S",
  "total": 11,
  "usableNow": 7,
  "renting": 3,
  "reserved": 1,
  "cleaning": 0,
  "maintenance": 0
}
```

---

## 7. Module Customers — hồ sơ và lịch sử thuê

### Mô tả

Tìm lại khách theo số điện thoại/Instagram, tránh tạo trùng và xem lịch sử đơn ở tất cả chi nhánh mà user có quyền.

### API: tìm khách

```http
GET /api/v1/customers?query=0908123456&limit=10
```

Response item:

```json
{
  "id": "customer_uuid",
  "name": "Ngọc Anh",
  "phone": "84908123456",
  "instagramHandle": "ngocanh",
  "address": "112 Võ Văn Tần, Quận 3",
  "completedOrderCount": 4,
  "lastOrderAt": "2026-08-11T09:30:00+07:00"
}
```

### API: tạo khách

```http
POST /api/v1/customers
Idempotency-Key: <uuid>
```

```json
{
  "name": "Ngọc Anh",
  "phone": "0908123456",
  "instagramHandle": "ngocanh",
  "address": "112 Võ Văn Tần, Quận 3"
}
```

Response `201`:

```json
{
  "data": {
    "id": "customer_uuid",
    "name": "Ngọc Anh",
    "phone": "84908123456",
    "instagramHandle": "ngocanh",
    "address": "112 Võ Văn Tần, Quận 3"
  }
}
```

Backend chuẩn hóa phone trước khi insert. Nếu đã tồn tại, trả `409 CUSTOMER_PHONE_EXISTS` cùng `existingCustomerId` để frontend cho chọn khách cũ.

### API: sửa thông tin mặc định

```http
PATCH /api/v1/customers/{customerId}
```

```json
{
  "name": "Nguyễn Ngọc Anh",
  "instagramHandle": "ngocanh.new",
  "address": "Địa chỉ mặc định mới"
}
```

Không sửa snapshot `customerName`, `customerPhone`, `deliveryAddress` trên đơn cũ.

### API: chi tiết và lịch sử đơn

```http
GET /api/v1/customers/{customerId}
GET /api/v1/customers/{customerId}/orders?status=COMPLETED&cursor=&limit=20
```

Response lịch sử item:

```json
{
  "orderId": "order_uuid",
  "orderNo": "SG-260811-004",
  "branch": { "id": "branch_sg_uuid", "code": "SG", "name": "Sài Gòn · Quận 3" },
  "status": "COMPLETED",
  "rentalStartAt": "2026-08-11T14:00:00+07:00",
  "rentalEndAt": "2026-08-13T14:00:00+07:00",
  "items": [
    { "productName": "Afrodille gấm", "size": "S", "assetCode": "SG-AF-GAM-S-04" }
  ],
  "rentalFee": 360000,
  "processingFee": 0,
  "createdAt": "2026-08-09T10:20:00+07:00"
}
```

---

## 8. Module Quote và Reservation — báo giá, nhận cọc, giữ lịch

### Mô tả

Staff chọn mã đang trống, gói thuê và loại cọc. Backend trả báo giá; chỉ sau khi staff xác nhận đã nhận tiền mới tạo reservation và khóa lịch.

### Màn hình: Giữ chỗ

Thứ tự mobile:

1. Chi nhánh đang thao tác.
2. Khách hàng.
3. Mã váy/phụ kiện đã chọn.
4. Ngày nhận/trả.
5. Gói và giá theo branch.
6. Kế hoạch cọc.
7. Giao dịch đã nhận.
8. Xác nhận giữ mã và tạo OTP.

### API: tính báo giá

```http
POST /api/v1/quotes
X-Branch-Id: branch_sg_uuid
```

```json
{
  "customerId": "customer_uuid",
  "rentalStartAt": "2026-09-25T14:00:00+07:00",
  "rentalEndAt": "2026-09-28T14:00:00+07:00",
  "depositPlan": "FIFTY_WITH_ID",
  "items": [
    {
      "inventoryItemId": "inventory_uuid",
      "packageCode": "3D"
    }
  ]
}
```

Response:

```json
{
  "data": {
    "branchId": "branch_sg_uuid",
    "currency": "VND",
    "items": [
      {
        "inventoryItemId": "inventory_uuid",
        "assetCode": "SG-AF-GAM-S-04",
        "productName": "Afrodille gấm",
        "size": "S",
        "packageCode": "3D",
        "rentalPrice": 440000,
        "replacementValue": 1200000
      }
    ],
    "rentalFee": 440000,
    "depositRequired": 600000,
    "slotDepositAmount": 100000,
    "expiresAt": "2026-09-21T21:15:00+07:00"
  }
}
```

`expiresAt` chỉ là hạn dùng của kết quả quote trên UI, không khóa đồ và không lưu database.

### API: tạo reservation sau khi nhận tiền

```http
POST /api/v1/reservations
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "customerId": "customer_uuid",
  "rentalStartAt": "2026-09-25T14:00:00+07:00",
  "rentalEndAt": "2026-09-28T14:00:00+07:00",
  "depositPlan": "FIFTY_WITH_ID",
  "items": [
    {
      "inventoryItemId": "inventory_uuid",
      "packageCode": "3D"
    }
  ],
  "receivedPayment": {
    "type": "SLOT_DEPOSIT",
    "amount": 100000,
    "method": "BANK_TRANSFER",
    "transactionRef": "VCB-260921-9981",
    "proofPath": "payments/2026/09/proof_uuid.jpg",
    "paidAt": "2026-09-21T20:42:00+07:00"
  }
}
```

Response `201`:

```json
{
  "data": {
    "id": "reservation_uuid",
    "reservationNo": "SG-R-260921-017",
    "status": "ACTIVE",
    "branch": { "id": "branch_sg_uuid", "code": "SG", "name": "Sài Gòn · Quận 3" },
    "customer": { "id": "customer_uuid", "name": "Ngọc Anh", "phone": "84908123456" },
    "deposit": {
      "plan": "FIFTY_WITH_ID",
      "required": 600000,
      "confirmedReceived": 100000,
      "remaining": 500000
    },
    "items": [
      {
        "inventoryItemId": "inventory_uuid",
        "assetCode": "SG-AF-GAM-S-04",
        "packageCode": "3D",
        "rentalPrice": 440000
      }
    ],
    "customerForm": {
      "url": "https://app.aurarental.vn/r/opaque-token",
      "otp": "482917",
      "expiresAt": "2026-09-22T20:42:00+07:00"
    },
    "createdAt": "2026-09-21T20:42:10+07:00"
  }
}
```

Backend trong một transaction:

- Kiểm tra user có quyền branch.
- Kiểm tra tất cả inventory item thuộc branch và `USABLE`.
- Lock và kiểm tra lại lịch trùng + cleaning buffer.
- Lấy giá từ `branch_rental_prices`; bỏ qua mọi giá client tự gửi.
- Snapshot giá vào `reservation_items`.
- Tạo payment đã được staff xác nhận theo nghiệp vụ hiện tại.
- Hash OTP/token trước khi lưu; chỉ trả OTP thô đúng lúc tạo/cấp lại.

Lỗi chính:

- `409 INVENTORY_NOT_AVAILABLE`.
- `409 PRICE_CHANGED`: trả quote mới để staff xác nhận lại.
- `400 PAYMENT_AMOUNT_INVALID`.
- `400 PACKAGE_NOT_OFFERED_AT_BRANCH`.

### API: danh sách reservation

```http
GET /api/v1/reservations?status=ACTIVE&query=Ngoc%20Anh&cursor=&limit=20
X-Branch-Id: branch_sg_uuid
```

Response item:

```json
{
  "id": "reservation_uuid",
  "reservationNo": "SG-R-260921-017",
  "status": "ACTIVE",
  "customer": {
    "id": "customer_uuid",
    "name": "Ngọc Anh",
    "phoneMasked": "0908 *** 456"
  },
  "itemSummary": "Afrodille gấm S · SG-AF-GAM-S-04",
  "rentalStartAt": "2026-09-25T14:00:00+07:00",
  "rentalEndAt": "2026-09-28T14:00:00+07:00",
  "depositConfirmed": 100000,
  "depositRemaining": 500000,
  "formStatus": "NOT_SUBMITTED"
}
```

### API: chi tiết reservation

```http
GET /api/v1/reservations/{reservationId}
X-Branch-Id: branch_sg_uuid
```

Trả đầy đủ customer, lịch, items snapshot, tổng tiền confirmed và trạng thái form; không trả `otp_hash` hoặc `form_token_hash`.

### API: đổi lịch, mã hoặc gói trước khi có order

Chỉ áp dụng reservation `ACTIVE` chưa convert. Backend lock mã cũ và mã mới, tính lại toàn bộ giá/cọc.

```http
PUT /api/v1/reservations/{reservationId}/rental-selection
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "rentalStartAt": "2026-09-26T14:00:00+07:00",
  "rentalEndAt": "2026-09-29T14:00:00+07:00",
  "items": [
    {
      "inventoryItemId": "new_inventory_uuid",
      "packageCode": "3D"
    }
  ],
  "reason": "Khách đổi size"
}
```

Response:

```json
{
  "data": {
    "reservationId": "reservation_uuid",
    "status": "ACTIVE",
    "items": [
      {
        "inventoryItemId": "new_inventory_uuid",
        "assetCode": "SG-AF-GAM-L-02",
        "packageCode": "3D",
        "rentalPrice": 460000
      }
    ],
    "depositRequired": 700000,
    "depositConfirmed": 100000,
    "depositRemaining": 600000,
    "priceChanged": true
  }
}
```

Không cho đổi sang mã thuộc branch khác. Nếu cần phục vụ khách ở chi nhánh khác, hủy reservation cũ theo policy rồi tạo reservation mới tại branch mới.

### API: cấp lại OTP/link

```http
POST /api/v1/reservations/{reservationId}/otp/reissue
Idempotency-Key: <uuid>
```

```json
{
  "reason": "Khách làm mất tin nhắn cũ"
}
```

Response:

```json
{
  "data": {
    "formUrl": "https://app.aurarental.vn/r/new-opaque-token",
    "otp": "613204",
    "expiresAt": "2026-09-22T21:00:00+07:00"
  }
}
```

### API: hủy reservation

```http
POST /api/v1/reservations/{reservationId}/cancel
```

```json
{
  "reason": "Khách hủy thuê",
  "paymentDecision": "KEEP_DEPOSIT"
}
```

`paymentDecision` trong MVP là dữ liệu để backend thực hiện policy đã thống nhất; nếu cần hoàn cọc hủy thì tạo payment `CANCELLATION_REFUND`, không sửa/xóa payment cũ.

---

## 9. Module Public Rental Form — form OTP của khách

### Mô tả

Khách mở link staff gửi, xem đúng chi nhánh/mã/gói/giá, nhập OTP và thông tin giao nhận. Không tải CCCD.

### API: tải thông tin form

```http
GET /api/v1/public/rental-forms/{token}
```

Response:

```json
{
  "data": {
    "reservationNo": "SG-R-260921-017",
    "branch": {
      "name": "Sài Gòn · Quận 3",
      "address": "112 Võ Văn Tần, Quận 3"
    },
    "items": [
      {
        "productName": "Afrodille gấm",
        "size": "S",
        "assetCode": "SG-AF-GAM-S-04",
        "packageCode": "3D",
        "packageLabel": "3 ngày",
        "rentalPrice": 440000
      }
    ],
    "rentalStartAt": "2026-09-25T14:00:00+07:00",
    "rentalEndAt": "2026-09-28T14:00:00+07:00",
    "depositPlan": "FIFTY_WITH_ID",
    "depositConfirmed": 100000,
    "depositRemaining": 500000,
    "otpRequired": true
  }
}
```

Không trả tên/số điện thoại khách đã lưu trước khi OTP hợp lệ.

### API: submit form và tự tạo order

```http
POST /api/v1/public/rental-forms/{token}/submit
Idempotency-Key: <uuid>
```

```json
{
  "otp": "482917",
  "customerName": "Nguyễn Ngọc Anh",
  "customerPhone": "0908123456",
  "deliveryAddress": "112 Võ Văn Tần, Quận 3, TP.HCM"
}
```

Response `201`:

```json
{
  "data": {
    "orderId": "order_uuid",
    "orderNo": "SG-260921-018",
    "status": "PENDING_DEPOSIT",
    "branchName": "Sài Gòn · Quận 3",
    "depositRemaining": 500000,
    "message": "Shop đã nhận thông tin và sẽ liên hệ xác nhận."
  }
}
```

Backend transaction:

- Rate-limit theo token/IP và giới hạn số lần OTP sai.
- Verify token/OTP constant-time, chưa hết hạn, chưa dùng.
- Lock reservation và inventory items.
- Kiểm tra branch, status, availability lần cuối.
- Tạo order + order items từ snapshot reservation.
- Copy `customer_id` và `branch_id` từ reservation, không nhận từ payload public.
- Consume OTP và chuyển reservation sang `CONVERTED_TO_ORDER`.
- Retry cùng idempotency key trả lại cùng order.

Lỗi hiển thị thân thiện:

- `400 OTP_INVALID`.
- `410 FORM_EXPIRED`.
- `409 FORM_ALREADY_SUBMITTED` kèm orderNo hiện có.
- `409 RESERVATION_NO_LONGER_AVAILABLE`: hướng khách liên hệ shop.

---

## 10. Module Orders — danh sách và chi tiết đơn

### Mô tả

Theo dõi đơn được sinh từ form khách, tiền cọc, CCCD checklist, giao/nhận và trạng thái hoàn tất.

### API: danh sách đơn

```http
GET /api/v1/orders?status=PENDING_DEPOSIT,PENDING_VERIFICATION&query=Ngoc%20Anh&from=2026-09-01&to=2026-09-30&cursor=&limit=20
X-Branch-Id: branch_sg_uuid
```

Response item:

```json
{
  "id": "order_uuid",
  "orderNo": "SG-260921-018",
  "status": "PENDING_DEPOSIT",
  "customer": {
    "id": "customer_uuid",
    "name": "Ngọc Anh",
    "phoneMasked": "0908 *** 456"
  },
  "itemSummary": "Afrodille gấm · S",
  "rentalStartAt": "2026-09-25T14:00:00+07:00",
  "rentalEndAt": "2026-09-28T14:00:00+07:00",
  "depositRequired": 600000,
  "depositConfirmed": 100000,
  "depositRemaining": 500000,
  "createdAt": "2026-09-21T20:50:00+07:00"
}
```

### API: chi tiết đơn

```http
GET /api/v1/orders/{orderId}
X-Branch-Id: branch_sg_uuid
```

Response rút gọn:

```json
{
  "data": {
    "id": "order_uuid",
    "orderNo": "SG-260921-018",
    "status": "PENDING_DEPOSIT",
    "branch": { "id": "branch_sg_uuid", "code": "SG", "name": "Sài Gòn · Quận 3" },
    "customer": {
      "id": "customer_uuid",
      "nameSnapshot": "Nguyễn Ngọc Anh",
      "phoneSnapshot": "0908123456",
      "deliveryAddressSnapshot": "112 Võ Văn Tần, Quận 3"
    },
    "rental": {
      "startAt": "2026-09-25T14:00:00+07:00",
      "endAt": "2026-09-28T14:00:00+07:00"
    },
    "items": [
      {
        "orderItemId": "order_item_uuid",
        "inventoryItemId": "inventory_uuid",
        "productName": "Afrodille gấm",
        "size": "S",
        "assetCode": "SG-AF-GAM-S-04",
        "packageCode": "3D",
        "rentalPrice": 440000,
        "condition": null
      }
    ],
    "deposit": {
      "required": 600000,
      "confirmed": 100000,
      "remaining": 500000
    },
    "identityVerification": {
      "required": true,
      "verified": false,
      "verifiedBy": null,
      "verifiedAt": null
    },
    "delivery": {
      "status": "NOT_STARTED",
      "trackingCode": null,
      "deliveredAt": null
    },
    "returnDelivery": {
      "status": "NOT_STARTED",
      "trackingCode": null,
      "returnedAt": null
    },
    "payments": [],
    "allowedActions": ["RECORD_TARGET_DEPOSIT", "VERIFY_IDENTITY"]
  }
}
```

`allowedActions` do backend tính theo role + trạng thái để frontend ẩn/disable đúng nút. Backend vẫn validate khi thao tác.

### API: xác nhận đã kiểm tra CCCD qua Instagram

```http
POST /api/v1/orders/{orderId}/identity-verification
Idempotency-Key: <uuid>
```

```json
{
  "verified": true
}
```

Response:

```json
{
  "data": {
    "verified": true,
    "verifiedBy": { "id": "user_uuid", "name": "Minh Lan" },
    "verifiedAt": "2026-09-21T21:10:00+07:00",
    "orderStatus": "CONFIRMED"
  }
}
```

Không có field số CCCD hoặc ảnh CCCD.

### API: hủy order

```http
POST /api/v1/orders/{orderId}/cancel
Idempotency-Key: <uuid>
```

```json
{
  "reason": "Khách hủy trước ngày giao",
  "paymentDecision": "REVIEW_SEPARATELY"
}
```

Response:

```json
{
  "data": {
    "orderId": "order_uuid",
    "status": "CANCELLED",
    "cancellationReason": "Khách hủy trước ngày giao",
    "inventoryReleased": true,
    "requiresPaymentResolution": true
  }
}
```

Chỉ hủy trước khi order hoàn tất; nếu đã `RENTING`, manager phải xử lý qua return/settlement thay vì cancel. Hoàn cọc do hủy dùng payment `CANCELLATION_REFUND` riêng.

Không cung cấp `POST /orders`; đây là chủ ý để staff không tạo đơn tay.

---

## 11. Module Payments — cọc, thu thêm và hoàn tiền

### Mô tả

Lưu từng giao dịch tiền dưới dạng ledger. Không sửa số dư trực tiếp trên order.

### API: ghi nhận khoản cọc còn lại

```http
POST /api/v1/reservations/{reservationId}/payments
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "type": "TARGET_DEPOSIT",
  "amount": 500000,
  "method": "BANK_TRANSFER",
  "transactionRef": "VCB-260922-0018",
  "proofPath": "payments/2026/09/proof_uuid.jpg",
  "paidAt": "2026-09-22T09:15:00+07:00",
  "confirmNow": true,
  "note": "Khách chuyển phần cọc còn lại"
}
```

Response:

```json
{
  "data": {
    "paymentId": "payment_uuid",
    "status": "CONFIRMED",
    "depositConfirmed": 600000,
    "depositRemaining": 0,
    "orderStatus": "PENDING_VERIFICATION"
  }
}
```

Nếu role/policy không cho `confirmNow`, backend tạo `RECORDED`; manager dùng endpoint confirm.

### API: xác nhận hoặc void payment

```http
POST /api/v1/payments/{paymentId}/confirm
POST /api/v1/payments/{paymentId}/void
```

Void request:

```json
{
  "reason": "Nhập nhầm mã giao dịch"
}
```

Không void payment đã dùng để settle đơn theo thao tác thông thường; trả `409 PAYMENT_ALREADY_SETTLED`.

### API: upload chứng từ

```http
POST /api/v1/uploads/presign
```

```json
{
  "purpose": "PAYMENT_PROOF",
  "fileName": "bill.jpg",
  "contentType": "image/jpeg",
  "size": 248921
}
```

Response:

```json
{
  "data": {
    "uploadUrl": "https://storage.example/signed-upload",
    "objectPath": "payments/2026/09/uuid.jpg",
    "expiresAt": "2026-09-21T21:20:00+07:00"
  }
}
```

Frontend upload thẳng lên private object storage, sau đó chỉ gửi `objectPath` cho API nghiệp vụ.

---

## 12. Module Fulfillment — chuẩn bị, giao và nhận

### Mô tả

Cập nhật các mốc vận hành rõ ràng. Dùng action endpoint thay vì cho frontend PATCH status tùy ý.

### API: bắt đầu chuẩn bị

```http
POST /api/v1/orders/{orderId}/prepare
Idempotency-Key: <uuid>
```

Response: order chuyển `CONFIRMED → PREPARING`.

### API: bắt đầu giao

```http
POST /api/v1/orders/{orderId}/delivery/start
```

```json
{
  "trackingCode": "GHN123456",
  "startedAt": "2026-09-25T12:30:00+07:00"
}
```

### API: xác nhận khách đã nhận

```http
POST /api/v1/orders/{orderId}/delivery/complete
```

```json
{
  "deliveredAt": "2026-09-25T14:05:00+07:00"
}
```

Kết quả: `deliveryStatus = DONE`, order `PREPARING → RENTING`.

### API: bắt đầu nhận đồ trả

```http
POST /api/v1/orders/{orderId}/return-delivery/start
```

```json
{
  "trackingCode": "GHN654321",
  "startedAt": "2026-09-28T13:00:00+07:00"
}
```

### API: xác nhận đã nhận đồ về shop

```http
POST /api/v1/orders/{orderId}/return-delivery/complete
```

```json
{
  "returnedAt": "2026-09-28T16:30:00+07:00"
}
```

Kết quả: `returnDeliveryStatus = DONE`, order `RENTING → INSPECTING`.

Lỗi state trả `409 ORDER_TRANSITION_NOT_ALLOWED` kèm `currentStatus` và `allowedActions`.

---

## 13. Module Return Inspection và Refund

### Mô tả

Staff kiểm tra từng váy/phụ kiện, ghi hư hỏng hoặc mất, hệ thống tính đối soát. Staff gửi manager duyệt; manager có thể duyệt trực tiếp, trả lại hoặc tạo phiên bản điều chỉnh.

### Màn hình: hàng chờ kiểm tra

```http
GET /api/v1/returns?status=INSPECTING,WAITING_APPROVAL&cursor=&limit=20
X-Branch-Id: branch_sg_uuid
```

Response item:

```json
{
  "orderId": "order_uuid",
  "orderNo": "SG-260921-018",
  "customerName": "Ngọc Anh",
  "returnedAt": "2026-09-28T16:30:00+07:00",
  "itemCount": 2,
  "inspectionProgress": { "completed": 1, "total": 2 },
  "refundStatus": "DRAFT"
}
```

### API: cập nhật kiểm tra từng item

```http
PUT /api/v1/orders/{orderId}/items/{orderItemId}/inspection
Idempotency-Key: <uuid>
```

Item tốt:

```json
{
  "condition": "GOOD",
  "actualRentalFee": 440000,
  "processingFee": 0,
  "damageNote": null,
  "damagePhotoPaths": []
}
```

Item hư/mất:

```json
{
  "condition": "DAMAGED",
  "actualRentalFee": 440000,
  "processingFee": 120000,
  "damageNote": "Rách 2 cm ở lai váy, cần may lại.",
  "damagePhotoPaths": [
    "damage/2026/09/photo_1.jpg",
    "damage/2026/09/photo_2.jpg"
  ],
  "inventoryOutcome": "MAINTENANCE"
}
```

Response:

```json
{
  "data": {
    "orderItemId": "order_item_uuid",
    "condition": "DAMAGED",
    "processingFee": 120000,
    "inventoryOutcome": "MAINTENANCE",
    "calculation": {
      "depositConfirmed": 900000,
      "rentalFee": 440000,
      "processingFee": 120000,
      "refundAmount": 340000,
      "additionalCollection": 0
    }
  }
}
```

Quy tắc:

- `GOOD`: processing fee phải bằng 0.
- `DAMAGED`: cần note; ảnh bắt buộc theo policy, manager có thể override.
- `MISSING`: inventory outcome bắt buộc `LOST`; phí mặc định từ replacement value nhưng manager được chỉnh.
- Backend tính tổng từ toàn bộ item; không nhận tổng tiền từ client.

### API: tạo/cập nhật phiếu đối soát draft

```http
POST /api/v1/orders/{orderId}/refunds
Idempotency-Key: <uuid>
```

```json
{
  "adjustmentReason": null
}
```

Response:

```json
{
  "data": {
    "refundId": "refund_uuid",
    "version": 1,
    "status": "DRAFT",
    "depositAmount": 900000,
    "rentalFee": 440000,
    "processingFee": 120000,
    "refundAmount": 340000,
    "additionalCollection": 0,
    "items": []
  }
}
```

Backend tạo `itemsSnapshot` từ order items; không nhận snapshot tùy ý từ frontend.

### API: staff gửi manager duyệt

```http
POST /api/v1/refunds/{refundId}/submit
Idempotency-Key: <uuid>
```

Response: refund `DRAFT → SUBMITTED`, lưu `submittedBy`.

### API: manager trả lại kiểm tra

```http
POST /api/v1/refunds/{refundId}/return-for-review
```

```json
{
  "reason": "Ảnh chưa thể hiện rõ phần rách ở lai váy"
}
```

Trong schema tối giản chưa có cột riêng cho review reason; backend có thể trả thông báo realtime. Nếu cần lưu lịch sử bắt buộc, phải bổ sung audit/review table sau khi chốt yêu cầu.

### API: manager duyệt

```http
POST /api/v1/refunds/{refundId}/approve
Idempotency-Key: <uuid>
```

```json
{
  "expectedVersion": 1
}
```

Response:

```json
{
  "data": {
    "refundId": "refund_uuid",
    "version": 1,
    "status": "APPROVED",
    "refundAmount": 340000,
    "additionalCollection": 0,
    "approvedBy": { "id": "manager_uuid", "name": "Quỳnh Anh" },
    "approvedAt": "2026-09-28T17:10:00+07:00",
    "receiptReady": true
  }
}
```

`expectedVersion` chống manager duyệt trên dữ liệu cũ. Nếu draft đã thay đổi, trả `409 REFUND_VERSION_CONFLICT`.

### API: điều chỉnh sau khi đã duyệt nhưng chưa settle

```http
POST /api/v1/refunds/{approvedRefundId}/revisions
Idempotency-Key: <uuid>
```

```json
{
  "reason": "Đối chiếu lại: miễn phí may 50.000đ"
}
```

Response tạo refund version mới `DRAFT`; bản cũ chỉ thành `SUPERSEDED` sau khi version mới được approve.

### API: xác nhận hoàn/thu thêm/đối soát 0đ

```http
POST /api/v1/refunds/{refundId}/settle
Idempotency-Key: <uuid>
```

Hoàn tiền:

```json
{
  "method": "BANK_TRANSFER",
  "transactionRef": "VCB-REF-8821",
  "proofPath": "payments/2026/09/refund_uuid.jpg",
  "paidAt": "2026-09-28T17:20:00+07:00"
}
```

Response:

```json
{
  "data": {
    "settlementType": "REFUND",
    "amount": 340000,
    "paymentId": "payment_uuid",
    "orderStatus": "COMPLETED",
    "settledAt": "2026-09-28T17:20:00+07:00",
    "inventory": [
      {
        "inventoryItemId": "inventory_uuid",
        "status": "MAINTENANCE",
        "cleaningUntil": null
      }
    ]
  }
}
```

Nếu cần thu thêm, backend yêu cầu payment `ADDITIONAL_COLLECTION` confirmed đủ số tiền trước khi settle. Nếu hoàn 0đ và không thu thêm, không tạo payment 0đ; chỉ set `settledBy/settledAt`.

### API: ảnh tổng kết gửi khách

```http
GET /api/v1/refunds/{refundId}/receipt.png
```

- Chỉ refund `APPROVED` và không có revision đang mở.
- Render từ `items_snapshot`, totals và trạng thái settled.
- Response `Content-Type: image/png`.
- Không lưu ảnh PNG thành bảng riêng.

---

## 14. Module Catalog, Inventory và bảng giá chi nhánh

### Mô tả

Catalog sản phẩm dùng chung; từng mã vật lý và bảng giá thuộc chi nhánh. Manager chỉnh giá Sài Gòn không ảnh hưởng Hà Nội hoặc đơn cũ.

### API: danh sách sản phẩm tại branch

```http
GET /api/v1/products?query=Afrodille&category=DRESS&active=true&cursor=&limit=20
X-Branch-Id: branch_sg_uuid
```

Response item:

```json
{
  "id": "product_uuid",
  "code": "AF-GAM",
  "name": "Afrodille gấm",
  "category": "DRESS",
  "coverImagePath": "products/af-gam/cover.jpg",
  "sizes": ["S", "L"],
  "activeInventoryCount": 11,
  "availableNowCount": 7,
  "priceFrom": 260000,
  "packageCodes": ["1D", "2D", "3D"]
}
```

### API: chi tiết sản phẩm theo branch

```http
GET /api/v1/products/{productId}
X-Branch-Id: branch_sg_uuid
```

Response:

```json
{
  "data": {
    "id": "product_uuid",
    "code": "AF-GAM",
    "name": "Afrodille gấm",
    "category": "DRESS",
    "color": "Đỏ gấm",
    "material": "Gấm hoa",
    "description": "...",
    "imagePaths": ["products/af-gam/cover.jpg"],
    "isActive": true,
    "variants": [
      {
        "id": "variant_uuid",
        "size": "S",
        "measurements": "84 × 64–66 × 88",
        "replacementValue": 1200000,
        "prices": [
          { "packageCode": "1D", "label": "1 ngày", "price": 260000 },
          { "packageCode": "2D", "label": "2 ngày", "price": 360000 },
          { "packageCode": "3D", "label": "3 ngày", "price": 440000 }
        ],
        "inventorySummary": {
          "total": 7,
          "usable": 6,
          "maintenance": 1,
          "lost": 0,
          "retired": 0
        },
        "inventoryItems": [
          { "id": "inventory_uuid", "assetCode": "HN-AF-GAM-S-01", "status": "USABLE", "cleaningHours": 12, "cleaningUntil": null }
        ]
      }
    ]
  }
}
```

### API: tạo sản phẩm và thiết lập ban đầu tại branch

Staff và manager đều được tạo. Backend lấy chi nhánh từ `X-Branch-Id` đã qua kiểm tra `user_branches`; giá thuê và mã vật lý trong request chỉ được ghi vào chi nhánh đó. Product/variant là catalog mẫu dùng chung, còn tồn kho và giá là dữ liệu theo chi nhánh.

```http
POST /api/v1/products
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "code": "AF-GAM",
  "name": "Afrodille gấm",
  "category": "DRESS",
  "color": "Đỏ gấm",
  "material": "Gấm hoa",
  "description": "Váy dạ hội",
  "imagePaths": ["products/af-gam/cover.jpg"],
  "variants": [
    {
      "size": "S",
      "measurements": "84 × 64–66 × 88",
      "replacementValue": 1200000,
      "prices": [
        { "packageCode": "1D", "price": 260000 },
        { "packageCode": "2D", "price": 360000 },
        { "packageCode": "3D", "price": 440000 }
      ],
      "inventoryItems": [
        { "assetCode": "SG-AF-GAM-S-01", "cleaningHours": 12 },
        { "assetCode": "SG-AF-GAM-S-02", "cleaningHours": 12 }
      ]
    }
  ]
}
```

Response `201`: product detail cùng branch. Nếu product code đã tồn tại toàn hệ thống, trả `409 PRODUCT_CODE_EXISTS` và hướng frontend mở sản phẩm hiện có để thêm giá/kho SG, không tạo bản sao product.

### API: sửa metadata sản phẩm

```http
PATCH /api/v1/products/{productId}
```

```json
{
  "name": "Afrodille gấm đỏ",
  "category": "DRESS",
  "color": "Đỏ gấm",
  "material": "Gấm hoa",
  "description": "Mô tả mới",
  "imagePaths": ["products/af-gam/new-cover.jpg"],
  "isActive": true
}
```

Metadata dùng chung toàn hệ thống nên chỉ manager cấp toàn hệ thống được sửa. UI "xóa" sản phẩm gửi cùng contract với `isActive: false` (archive); không xóa cứng vì sản phẩm có thể đã xuất hiện trong lịch sử thuê. Manager có thể khôi phục bằng `isActive: true`.

### API: thêm variant/size cho sản phẩm hiện có

```http
POST /api/v1/products/{productId}/variants
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "size": "L",
  "measurements": "92 × 72–74 × 96",
  "replacementValue": 1400000,
  "prices": [
    { "packageCode": "1D", "price": 280000 },
    { "packageCode": "2D", "price": 390000 },
    { "packageCode": "3D", "price": 470000 }
  ],
  "inventoryItems": [
    { "assetCode": "SG-AF-GAM-L-01", "cleaningHours": 12 }
  ]
}
```

Response `201` trả variant cùng giá/mã vừa tạo. Unique `(productId, size)` ngăn tạo trùng size.

### API: cập nhật bảng giá một variant tại branch

```http
PUT /api/v1/product-variants/{variantId}/rental-prices
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "prices": [
    { "packageCode": "1D", "price": 270000 },
    { "packageCode": "2D", "price": 370000 },
    { "packageCode": "3D", "price": 450000 }
  ]
}
```

Response:

```json
{
  "data": {
    "variantId": "variant_uuid",
    "branchId": "branch_sg_uuid",
    "prices": [
      { "packageCode": "1D", "label": "1 ngày", "price": 270000 },
      { "packageCode": "2D", "label": "2 ngày", "price": 370000 },
      { "packageCode": "3D", "label": "3 ngày", "price": 450000 }
    ],
    "effectiveForNewReservationsOnly": true
  }
}
```

PUT thay toàn bộ bộ giá tại branch. Xóa package khỏi array nghĩa ngừng bán package đó cho reservation mới; snapshot đơn cũ không đổi. Backend bắt buộc còn `1D` vì dùng làm giá cơ sở ngày thêm.

### API: thêm mã vật lý vào branch

```http
POST /api/v1/product-variants/{variantId}/inventory-items
X-Branch-Id: branch_sg_uuid
Idempotency-Key: <uuid>
```

```json
{
  "items": [
    { "assetCode": "SG-AF-GAM-S-08", "cleaningHours": 12 },
    { "assetCode": "SG-AF-GAM-S-09", "cleaningHours": 12 }
  ]
}
```

### API: cập nhật trạng thái mã vật lý

```http
PATCH /api/v1/inventory-items/{inventoryItemId}
X-Branch-Id: branch_sg_uuid
```

```json
{
  "status": "MAINTENANCE",
  "cleaningHours": 24
}
```

Không cho chuyển `LOST/RETIRED → USABLE` với staff. Không cho maintenance nếu gây xung đột đơn tương lai mà chưa có manager override.

Điều chuyển kho chưa thuộc MVP. Khi cần, thêm bảng và endpoint `POST /inventory-transfers`; không PATCH `branch_id` âm thầm vì sẽ mất lịch sử chuyển kho.

---

## 15. Module Branch và quản lý nhân viên

### Mô tả

Manager quản lý danh sách chi nhánh và cấp quyền nhân viên. Bảng giá/kho vẫn quản lý ở module catalog.

### API: danh sách chi nhánh

```http
GET /api/v1/branches
```

Staff chỉ nhận branch được cấp; manager toàn hệ thống nhận tất cả.

Response item:

```json
{
  "id": "branch_sg_uuid",
  "code": "SG",
  "name": "Sài Gòn · Quận 3",
  "address": "112 Võ Văn Tần, Quận 3",
  "isActive": true
}
```

### API: tạo/sửa chi nhánh

```http
POST /api/v1/branches
PATCH /api/v1/branches/{branchId}
```

Create input:

```json
{
  "code": "SG",
  "name": "Sài Gòn · Quận 3",
  "address": "112 Võ Văn Tần, Quận 3"
}
```

Không deactivate branch nếu còn reservation/order chưa hoàn tất.

### API: danh sách nhân viên

```http
GET /api/v1/users?query=Minh%20Lan&active=true&cursor=&limit=20
```

Response item:

```json
{
  "id": "user_uuid",
  "name": "Minh Lan",
  "email": "staff.hanoi@aurarental.vn",
  "role": "STAFF",
  "isActive": true,
  "branches": [
    { "id": "branch_hn_uuid", "code": "HN", "name": "Hà Nội · Nguyễn Trãi" }
  ]
}
```

### API: gán chi nhánh cho nhân viên

```http
PUT /api/v1/users/{userId}/branches
```

```json
{
  "branchIds": ["branch_hn_uuid"]
}
```

PUT thay toàn bộ quyền branch. Không cho manager tự xóa branch cuối cùng của chính mình nếu không còn manager khác quản trị hệ thống.

---

## 16. Module Settings

### Mô tả

Chứa chính sách dùng chung toàn hệ thống. Bảng giá là dữ liệu theo chi nhánh nên không nằm trong settings.

### API: đọc cấu hình

```http
GET /api/v1/settings
```

```json
{
  "data": {
    "slotDepositAmount": 100000,
    "defaultCleaningHours": 12,
    "extraDayRate": 0.1,
    "supportedPackageCodes": ["12H", "1D", "2D", "3D"]
  }
}
```

`extraDayRate` hiện được backend config và snapshot vào reservation item; schema hiện chưa lưu field này trong `settings`. Nếu manager cần chỉnh trực tiếp trên UI, phải bổ sung cột trước khi triển khai endpoint update.

### API: cập nhật cấu hình

```http
PUT /api/v1/settings
```

```json
{
  "slotDepositAmount": 100000,
  "defaultCleaningHours": 12,
  "extraDayRate": 0.1
}
```

Manager only. Giá trị mới chỉ áp dụng dữ liệu tạo sau, không sửa snapshot cũ.

---

## 17. Module Reports

### Mô tả

Báo cáo vận hành theo chi nhánh hoặc tổng hợp nhiều chi nhánh đối với manager. Không tạo bảng báo cáo ở MVP.

### API: báo cáo tổng hợp

```http
GET /api/v1/reports/summary?from=2026-09-01&to=2026-09-30
X-Branch-Id: branch_sg_uuid
```

Response:

```json
{
  "data": {
    "period": { "from": "2026-09-01", "to": "2026-09-30" },
    "branch": { "id": "branch_sg_uuid", "code": "SG", "name": "Sài Gòn · Quận 3" },
    "orders": {
      "created": 124,
      "completed": 103,
      "cancelled": 6,
      "active": 15
    },
    "money": {
      "depositConfirmed": 84200000,
      "rentalFee": 36100000,
      "processingFee": 2400000,
      "refundPaid": 45700000,
      "additionalCollection": 600000
    },
    "inventory": {
      "usable": 87,
      "maintenance": 4,
      "lost": 1,
      "utilizationRate": 0.68
    }
  }
}
```

Manager nhiều branch có thể gọi thêm:

```http
GET /api/v1/reports/branches?from=2026-09-01&to=2026-09-30
```

Response là một dòng summary cho mỗi branch được phép xem.

---

## 18. API cho notification

MVP chưa có bảng notification persisted. Hai loại thông báo:

- Counter/task trên dashboard được tính từ `GET /dashboard/today`.
- Sự kiện realtime như khách vừa submit form có thể phát qua SignalR.

Event mẫu:

```json
{
  "type": "ORDER_CREATED",
  "branchId": "branch_sg_uuid",
  "occurredAt": "2026-09-21T20:50:00+07:00",
  "data": {
    "orderId": "order_uuid",
    "orderNo": "SG-260921-018",
    "customerName": "Ngọc Anh"
  }
}
```

Không thiết kế `GET /notifications` cho đến khi quyết định cần lưu đã đọc/chưa đọc qua nhiều thiết bị.

---

## 19. Ma trận quyền rút gọn

| Hành động | Staff | Manager |
|---|---:|---:|
| Xem availability/giá tại branch được cấp | Có | Có |
| Tạo reservation sau khi nhận tiền | Có | Có |
| Tạo order thủ công | Không | Không |
| Ghi nhận cọc/giao/nhận | Có | Có |
| Check CCCD qua Instagram | Có | Có |
| Nhập inspection | Có | Có |
| Submit refund | Có | Có |
| Approve/revise/settle refund | Không | Có |
| Tạo/sửa product, variant, giá | Không | Có |
| Thêm/retire mã vật lý | Theo policy | Có |
| Quản lý branch và user access | Không | Có |
| Xem báo cáo tổng hợp nhiều branch | Không | Có |

## 20. Các validation bắt buộc ở backend

### Branch

- User phải có quyền trên branch của request.
- Inventory item, giá, reservation và order phải cùng branch.
- Không tin `branchId` nằm trong body public/customer form.

### Availability

- Kiểm tra reservation `ACTIVE` và order chưa `COMPLETED/CANCELLED` giao thời gian.
- Tính cả `cleaning_until` và cleaning buffer snapshot.
- `MAINTENANCE`, `RETIRED`, `LOST` không available.

### Money

- Giá lấy từ `branch_rental_prices` và snapshot khi tạo reservation/order.
- Mọi amount không âm; payment amount luôn dương.
- Chỉ `CONFIRMED` payment được tính số dư.
- Không hoàn hai lần, không settle bằng refund version cũ.

### State transition

- Không dùng API generic `PATCH status` cho order/refund/payment.
- Mỗi action endpoint kiểm tra trạng thái hiện tại và role.
- Response conflict phải trả current state và allowed actions để frontend refresh.

### Security

- Không lưu CCCD.
- Proof/damage photo nằm private storage; API chỉ trả signed download URL ngắn hạn cho user có quyền.
- Mask phone ở list; chỉ detail có quyền mới thấy đầy đủ.
- Rate-limit login, public form và OTP.
- Log tối thiểu request ID, actor ID, branch ID và action tài chính ở application log. Nếu cần audit bất biến trong UI, bổ sung bảng audit riêng ở phase sau.

## 21. Các quyết định/schema gap cần chốt trước khi code

Tài liệu API mô tả đầy đủ hành vi mong muốn, nhưng schema tối giản hiện tại còn vài điểm cần xác nhận:

| Vấn đề | Hiện tại | Đề xuất |
|---|---|---|
| Lý do hủy reservation | `reservations` chưa có `cancellation_reason` | Thêm cột nếu cần xem lại lý do; nếu không, bỏ reason khỏi API cancel. Khuyến nghị thêm. |
| Tỷ lệ ngày thuê thêm | `settings.extra_day_rate` là nguồn cấu hình, `reservation_items.extra_day_rate` là snapshot | Manager chỉnh settings chỉ ảnh hưởng reservation tạo sau. |
| Lý do manager trả refund về staff | `refunds` chưa có review note/audit table | Nếu cần lưu lịch sử, bổ sung audit/refund review table; nếu MVP không cần lịch sử, chỉ gửi realtime message. |
| Định nghĩa package | DB chỉ lưu `package_code` và giá | Giữ backend constants `12H/1D/2D/3D` cho MVP. Chỉ thêm bảng package khi cần tên/duration tùy biến. |
| Timeline thay đổi trạng thái | Schema chỉ giữ trạng thái hiện tại và một số timestamp | Nếu bắt buộc audit đầy đủ, thêm bảng audit event ở phase sau. |
| Điều chuyển kho | `inventory_items` chỉ giữ branch hiện tại | Chưa cho chuyển kho trong MVP; khi cần phải thêm `inventory_transfers`. |

Các gap này không ngăn build phần đọc/search mockup, nhưng phải chốt trước khi implement API write liên quan để tránh đổi contract giữa chừng.

## 22. Thứ tự triển khai đề xuất

### Phase 1 — nền tảng và catalog

1. Local auth `POST /auth/login` + `GET /me`.
2. Branch authorization middleware.
3. Branch/user management tối thiểu.
4. Product, variant, inventory item và branch price.

### Phase 2 — tư vấn và giữ lịch

1. Availability search.
2. Customer search/create.
3. Quote.
4. Reservation + initial payment + OTP/link.

### Phase 3 — order và vận hành

1. Public form submit idempotent.
2. Order list/detail.
3. Cọc còn lại và CCCD checklist.
4. Chuẩn bị, giao và nhận.

### Phase 4 — trả đồ và tiền

1. Inspection từng item và upload ảnh.
2. Refund draft/submit/approve/revision.
3. Refund/additional collection settlement.
4. PNG receipt.

### Phase 5 — dashboard và báo cáo

1. Dashboard computed query.
2. SignalR event cơ bản.
3. Báo cáo theo branch và toàn hệ thống.

## 23. Những phần cố ý chưa làm trong MVP

- Điều chuyển mã vật lý giữa chi nhánh có lịch sử.
- Đồng bộ ngân hàng tự động.
- Đồng bộ hãng vận chuyển.
- Thanh toán online từ customer form.
- Notification inbox persisted/read state.
- Hoàn từng phần nhiều lần cho cùng đơn.
- Audit log bất biến hiển thị trong UI.
- Giá có ngày hiệu lực tương lai hoặc lịch sử bảng giá riêng.
- Tự động gợi ý giá bằng AI.

Các phần này chỉ nên thêm khi có nghiệp vụ thật; schema hiện tại đủ cho hai chi nhánh, giá/gói khác nhau và luồng thuê end-to-end.
