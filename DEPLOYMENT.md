# Deploy Aura Rental

Kiến trúc production:

```text
Browser
  └─ Vercel (React/Vite)
       └─ Render (ASP.NET Core API)
            ├─ Supabase PostgreSQL
            └─ Cloudflare R2 (ảnh sản phẩm và ảnh hư hại)

cron-job.org ── GET /health mỗi 10 phút ──> Render
```

## 0. Phân tách môi trường

Không dùng chung database, bucket hoặc secret giữa development và production.

| Thành phần | Development | Production |
|---|---|---|
| Frontend | `http://localhost:5173` | Vercel production URL/custom domain |
| Backend | `http://localhost:5000` | Render `onrender.com`/custom domain |
| Database | PostgreSQL Docker local | Supabase project production |
| Object storage | Chưa nối | R2 bucket production |
| JWT signing key/token pepper | Giá trị local | Hai secret production độc lập |

Nếu cần staging lâu dài, tạo Supabase project, R2 bucket và Render service riêng; không trỏ Vercel Preview vào production backend.

## 1. Chuẩn bị repository

Push repository lên GitHub/GitLab. Các file deploy đã có:

- `backend/Dockerfile`: build và chạy .NET 10 trên Render, bind vào `$PORT`.
- `backend/.dockerignore`: loại build artifact khỏi Docker context.
- `frontend/vercel.json`: fallback mọi frontend route về `index.html` cho SPA.
- `database/schema.sql`: DDL nguồn; backend không tự chạy migration.
- `database/seed.sql`: chỉ dành cho demo/development.

## 2. Supabase PostgreSQL

### 2.1 Tạo database

1. Tạo Supabase project, ưu tiên region gần người dùng/backend.
2. Lưu database password trong password manager.
3. Mở **SQL Editor**, copy toàn bộ `database/schema.sql`, chạy một lần.
4. Kiểm tra schema `aura` và các bảng đã xuất hiện.

Không chạy `seed.sql` trên production thật vì file chứa mock order và ba tài khoản có password development đã biết. Với môi trường demo tạm thời có thể chạy seed, nhưng không dùng các tài khoản đó cho dữ liệu khách thật.

Hiện dự án chưa có luồng bootstrap tài khoản manager/password reset production. Trước khi nhận dữ liệu thật cần bổ sung cơ chế tạo manager đầu tiên hoặc công cụ sinh password hash; không nên dùng `Manager@123` ngoài demo.

### 2.2 Lấy connection string cho Render

Render là backend chạy lâu dài. Chọn **Connect → Session pooler** trong Supabase, không chọn Transaction pooler. Session pooler dùng IPv4 và hỗ trợ hành vi session/prepared statement bình thường; copy chính xác host và username từ dashboard.

Đổi thông tin dashboard sang Npgsql format:

```text
Host=aws-<index>-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<PROJECT_REF>;Password=<DB_PASSWORD>;SSL Mode=Require;Maximum Pool Size=5;Minimum Pool Size=0;Timeout=15;Command Timeout=30
```

Không đặt connection string này trong Git, Vercel hoặc biến `VITE_*`.

Tài liệu: https://supabase.com/docs/guides/database/connecting-to-postgres

## 3. Cloudflare R2

### 3.1 Tạo tài nguyên

1. Cloudflare Dashboard → R2 → tạo bucket, ví dụ `aura-rental-production`.
2. Giữ bucket private cho ảnh hư hại và chứng từ thanh toán.
3. Tạo R2 API token giới hạn đúng bucket với quyền Object Read & Write.
4. Lưu `Account ID`, `Access Key ID`, `Secret Access Key`, bucket name trong Render secrets — tuyệt đối không đưa secret vào Vercel.

Endpoint S3-compatible:

```text
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

### 3.2 CORS cho upload trực tiếp từ browser

Thay domain bằng Vercel production/custom domain:

```json
[
  {
    "AllowedOrigins": [
      "https://aura-rental.vercel.app",
      "http://localhost:5173"
    ],
    "AllowedMethods": ["GET", "PUT", "HEAD"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

Kiến trúc: frontend xin presigned PUT URL từ backend → upload thẳng lên R2 → chỉ gửi `objectPath` vào API nghiệp vụ. `POST /api/v1/uploads/presign` yêu cầu JWT và `X-Branch-Id`, chỉ ký JPG/PNG/WebP tối đa 10 MB, với URL mặc định 5 phút và `Content-Type` đã ký. Access key/secret không rời backend.

**Trạng thái hiện tại:** đã triển khai upload từ máy staff/manager cho ảnh sản phẩm và ảnh bằng chứng hư hỏng. Cần thiết lập bucket, CORS và các biến R2 bên dưới trước khi deploy.

Các biến dự kiến khi adapter được triển khai:

```text
R2__AccountId
R2__AccessKeyId
R2__SecretAccessKey
R2__BucketName
R2__Endpoint                         # tùy chọn, mặc định https://<ACCOUNT_ID>.r2.cloudflarestorage.com
R2__UploadUrlLifetimeSeconds         # tùy chọn, mặc định 300
```

Tài liệu:

- https://developers.cloudflare.com/r2/api/s3/presigned-urls/
- https://developers.cloudflare.com/r2/buckets/cors/

## 4. Deploy backend lên Render

### 4.1 Tạo Web Service

1. Render Dashboard → **New → Web Service** → kết nối repository.
2. Chọn branch production, thường là `main`.
3. Language: **Docker**.
4. Root Directory: `backend`.
5. Dockerfile Path: `Dockerfile`.
6. Docker Build Context: `.`.
7. Chọn plan. Free phù hợp demo, không nên coi là production SLA.

Docker image chạy:

```text
dotnet AuraRental.WebAPI.dll --urls http://0.0.0.0:${PORT:-10000}
```

Không tự đặt `PORT`; Render cấp biến này. Render yêu cầu service bind `0.0.0.0:$PORT`.

### 4.2 Environment variables trên Render

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ConnectionStrings__DefaultConnection=<NPGSQL_SESSION_POOLER_STRING>

Authentication__Issuer=aura-rental-production
Authentication__Audience=aura-rental-api
Authentication__SigningKey=<RANDOM_SECRET_AT_LEAST_32_BYTES>
Authentication__AccessTokenMinutes=480

Security__TokenPepper=<DIFFERENT_RANDOM_SECRET_AT_LEAST_32_BYTES>

Cors__Origins__0=https://<VERCEL_PRODUCTION_DOMAIN>
Frontend__BaseUrl=https://<VERCEL_PRODUCTION_DOMAIN>
```

Sinh hai secret độc lập, ví dụ:

```bash
openssl rand -base64 48
openssl rand -base64 48
```

Không đổi `Authentication__SigningKey` tùy tiện vì JWT hiện tại sẽ hết hiệu lực. Không đổi `Security__TokenPepper` vì link/OTP đang tồn tại sẽ không verify được.

`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` giúp ASP.NET Core hiểu request gốc là HTTPS khi Render terminate TLS ở reverse proxy.

### 4.3 Health check

Render service → Settings → Health Check Path:

```text
/health
```

Hai endpoint public hiện có:

```text
GET https://<BACKEND>.onrender.com/health
GET https://<BACKEND>.onrender.com/api/v1/health
```

Response mong đợi:

```json
{
  "status": "ok",
  "service": "aura-rental-api",
  "timestamp": "2026-09-22T10:00:00Z"
}
```

Endpoint này là liveness check và không query database. Sau deploy, kiểm tra `/health`, sau đó đăng nhập và gọi một API đọc để xác nhận kết nối Supabase.

Tài liệu:

- https://render.com/docs/docker
- https://render.com/docs/monorepo-support
- https://render.com/docs/web-services
- https://render.com/docs/health-checks

## 5. Deploy frontend lên Vercel

1. Vercel → **Add New Project** → import cùng repository.
2. Root Directory: `frontend`.
3. Framework Preset: **Vite**.
4. Build Command: `npm run build`.
5. Output Directory: `dist`.
6. Node.js: 22.x.
7. Thêm environment variable cho Production:

```text
VITE_API_BASE_URL=https://<BACKEND>.onrender.com
```

`VITE_API_BASE_URL` được nhúng vào bundle lúc build nên sau khi thay đổi phải redeploy. Đây là URL public, không phải secret; không bao giờ đặt database password, JWT signing key hoặc R2 secret vào biến `VITE_*`.

`frontend/vercel.json` đã cấu hình SPA fallback để các URL như `/orders`, `/returns` và `/r/<token>` không bị 404 khi refresh.

Sau khi Vercel cấp domain chính xác:

1. Quay lại Render.
2. Cập nhật `Cors__Origins__0` và `Frontend__BaseUrl` bằng origin chính xác, không có dấu `/` cuối.
3. Redeploy/restart backend.
4. Nếu đổi sang custom domain, cập nhật cả Render CORS, R2 CORS và redeploy frontend/backend liên quan.

Tài liệu:

- https://vercel.com/docs/frameworks/frontend/vite
- https://vercel.com/docs/project-configuration/vercel-json
- https://vercel.com/docs/environment-variables

## 6. cron-job.org ping mỗi 10 phút

1. Tạo tài khoản tại `https://cron-job.org`.
2. Tạo cronjob mới.
3. URL:

```text
https://<BACKEND>.onrender.com/health
```

4. Request method: `GET`.
5. Schedule: mỗi 10 phút — phút `0,10,20,30,40,50`, mọi giờ/ngày/tháng/thứ.
6. Không cần header Authorization hoặc request body.
7. Bật job, chạy thử và kiểm tra execution history nhận HTTP 200.
8. Bật failure notification nếu tài khoản hỗ trợ.

Render Free hiện spin down sau 15 phút không có inbound HTTP/WebSocket traffic; ping 10 phút thường ngăn idle spin-down. Tuy nhiên Free service vẫn có giới hạn giờ chạy/bandwidth và Render có thể restart instance, nên cron không thay thế plan production có SLA.

### Lưu ý về Data Protection trên Render

Backend hiện dùng ASP.NET Core Data Protection để mã hóa response idempotency đã lưu trong PostgreSQL. Key ring mặc định nằm trên filesystem của container, trong khi filesystem của Render có thể bị thay thế khi deploy/restart. Hệ quả là instance mới có thể không giải mã được idempotency record do instance cũ tạo ra.

Trước khi chạy production thật hoặc scale nhiều instance, cần lưu Data Protection key ring vào nơi bền vững dùng chung (ví dụ PostgreSQL hoặc object storage phù hợp), bảo vệ key bằng secret riêng và kiểm thử request retry qua một lần restart. Đây không ảnh hưởng tới `/health`, nhưng ảnh hưởng khả năng retry an toàn của các thao tác ghi.

Tài liệu:

- https://docs.cron-job.org/rest-api.html
- https://render.com/docs/free

## 7. Checklist sau deploy

- [ ] `GET /health` trả 200 qua HTTPS.
- [ ] Backend logs không có lỗi kết nối Supabase/SSL.
- [ ] Login frontend hoạt động.
- [ ] `GET /api/v1/me` trả đúng role và branch.
- [ ] Tài khoản Hà Nội không truy cập kho Sài Gòn và ngược lại.
- [ ] Tạo giữ chỗ, nhận thêm cọc và xem đơn chạy được trên database production/staging.
- [ ] Refresh trực tiếp `/orders` trên Vercel không 404.
- [ ] Render CORS chỉ chứa frontend origin cần thiết.
- [ ] cron-job.org history trả 200 mỗi 10 phút.
- [ ] R2 secret chỉ nằm ở backend; frontend chỉ nhận presigned URL tạm thời.
- [ ] Data Protection key ring đã được persist trước khi chạy production thật/scale nhiều instance.
- [ ] Không chạy `seed.sql` hoặc dùng password development với dữ liệu production thật.

## 8. Thứ tự triển khai khuyến nghị

1. Supabase: tạo project và chạy `schema.sql`.
2. Render: deploy backend với connection string và tạm điền domain Vercel dự kiến.
3. Kiểm tra health/backend → Supabase.
4. Vercel: deploy frontend với Render URL.
5. Chốt domain, cập nhật Render CORS + frontend base URL.
6. R2: tạo bucket/token/CORS và đặt các biến `R2__*` cho backend.
7. cron-job.org: bật GET `/health` mỗi 10 phút.
8. Chạy checklist end-to-end trên staging trước production.
