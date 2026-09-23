# Aura Rental backend

Backend là modular monolith .NET 10, tổ chức theo cách của `backend_folder_guide`:

```text
AuraRental.Domain/       Entity và enum nghiệp vụ, không phụ thuộc EF/HTTP
AuraRental.Service/      DTO, use case, interface, repository và DbContext
AuraRental.WebAPI/       Controller, authentication và middleware HTTP
```

Luồng phụ thuộc chỉ đi theo chiều `WebAPI -> Service -> Domain`. Controller không query database và repository không quyết định nghiệp vụ.

## Phần đã triển khai

- Map toàn bộ bảng nghiệp vụ và bảng idempotency trong `database/schema.sql` sang entity và `AuraRentalDbContext`.
- Đăng nhập nội bộ bằng email/username + password hash; backend phát JWT có `sub = users.id`.
- Request context theo user và middleware kiểm tra `X-Branch-Id` với `user_branches`.
- Response/error envelope, request ID và exception middleware.
- `GET /health` và `GET /api/v1/health`: public, luôn trả `200` khi API process hoạt động, không truy cập database; dùng được cho Render health check/cron ping.
- `GET /api/v1/me`, `GET /api/v1/branches`.
- `GET /api/v1/products`, `GET /api/v1/products/{id}`.
- `GET /api/v1/availability` có kiểm tra reservation và order trùng lịch.
- Customer search/create/update và lịch sử thuê theo các branch user được truy cập.
- Quote, reservation, payment ban đầu, đổi lịch/mã, hủy và cấp lại OTP.
- Public rental form có rate limit, verify token/OTP và chỉ tự sinh một order cho mỗi reservation.
- Order list/detail, checklist CCCD Instagram, cọc bổ sung, payment confirm/void và fulfillment.
- Return inspection theo từng item, refund version, submit/review/approve/revision/settlement.
- Catalog write: product, variant, bảng giá branch, mã vật lý và trạng thái kho.
- Quản lý branch, quyền branch của nhân viên và settings.
- Dashboard ngày, report theo branch/toàn hệ thống và SignalR `OrderCreated` theo group branch.
- Idempotency bền vững cho write API: response được mã hóa bằng Data Protection trước khi lưu 24 giờ.

Integration còn lại cần adapter hạ tầng trước khi mở production là render `GET /refunds/{id}/receipt.png`. Upload ảnh dùng `POST /api/v1/uploads/presign`: backend chỉ ký URL PUT ngắn hạn, còn trình duyệt upload trực tiếp vào Cloudflare R2 và lưu `objectPath` vào API nghiệp vụ.

## Quy ước code

- Tên C# dùng PascalCase; interface có tiền tố `I`; field/private local dùng camelCase.
- File-scoped namespace, nullable reference types và warnings-as-errors luôn bật.
- Một public type nghiệp vụ chính trên mỗi file; DTO liên quan chặt có thể nhóm cùng file.
- Method async nhận `CancellationToken` ở tham số cuối.
- API dùng enum dạng `UPPER_SNAKE_CASE`; enum trong C# vẫn dùng PascalCase.
- Tiền dùng `decimal`, không dùng `double`; thời gian dùng `DateTimeOffset` UTC trong backend.
- Mọi query theo nghiệp vụ chi nhánh đọc `IRequestContext.BranchId`, không nhận `branchId` từ body.
- Controller chỉ bind HTTP và gọi use case. Validation/state transition nằm trong use case; EF query nằm trong repository.
- Endpoint đổi tiền/trạng thái phải là action cụ thể, không có generic `PATCH status`.
- Write endpoint có `[Idempotent]` bắt buộc header `Idempotency-Key`; cùng key khác payload trả `409`.

## Chạy local

Không có migration tự chạy. Tạo database bằng `../database/schema.sql`, sau đó cấu hình connection string và khóa ký JWT bằng environment variable hoặc user-secrets.

```bash
dotnet restore AuraRental.slnx
dotnet build AuraRental.slnx
dotnet run --project AuraRental.WebAPI
```

Các key cấu hình chính:

```text
ConnectionStrings__DefaultConnection
Authentication__Issuer
Authentication__Audience
Authentication__SigningKey        # secret tối thiểu 32 bytes
Authentication__AccessTokenMinutes
Cors__Origins__0
Security__TokenPepper
Frontend__BaseUrl
R2__AccountId
R2__AccessKeyId
R2__SecretAccessKey
R2__BucketName
R2__Endpoint                         # optional; mặc định https://<AccountId>.r2.cloudflarestorage.com
R2__UploadUrlLifetimeSeconds         # optional; mặc định 300, từ 60 đến 3600
```

Không đưa secret production vào `appsettings.json`. `SigningKey` hiện tại chỉ dùng local và phải được override bằng secret ngoài source khi deploy.

## Thêm module mới

1. Thêm request/response model vào `DTOs/<Module>`.
2. Khai báo hành vi tại `Interface/UseCase` và persistence tại `Interface/Persistence`.
3. Viết use case trước, repository sau; đăng ký cả hai ở `ServiceCollectionExtensions`.
4. Controller kế thừa `ApiControllerBase`; thêm `[RequireBranch]` nếu dữ liệu thuộc chi nhánh.
5. Write API liên quan tiền/giữ lịch phải có transaction, row lock và `[Idempotent]` trước khi mở cho frontend.
