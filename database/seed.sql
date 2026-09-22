-- Development-only mock data for Aura Rental.
-- Seed rows use stable UUIDs and UPSERT, so this file can be run repeatedly.
-- Existing non-seed rows are not deleted.

BEGIN;
SET LOCAL search_path = aura;

-- Branches and users ---------------------------------------------------------
INSERT INTO branches (id, code, name, address, is_active) VALUES
('10000000-0000-0000-0000-000000000001', 'HN', 'Aura Rental Hà Nội', '25 Thái Hà, Đống Đa, Hà Nội', true),
('10000000-0000-0000-0000-000000000002', 'SG', 'Aura Rental Sài Gòn', '112 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh', true)
ON CONFLICT (id) DO UPDATE SET code = EXCLUDED.code, name = EXCLUDED.name,
address = EXCLUDED.address, is_active = EXCLUDED.is_active;

INSERT INTO users (id, username, password_hash, name, email, role, is_active) VALUES
('20000000-0000-0000-0000-000000000001', 'manager', 'AQAAAAIACSfAAAAAENzFhqVDNxRTks+ElzTMfsq0EDSS9G6wiHUcKi84MNmIamJ1kpFwVQkzZlCQaF7DQA==', 'Mai Anh - Quản lý', 'manager@aurarental.local', 'MANAGER', true),
('20000000-0000-0000-0000-000000000002', 'hanoi', 'AQAAAAIACSfAAAAAEIEdXF3upWPbQMCuVeAwhzkHJ6HNedPn2tB3Z4huXt0UspeLhT/euP6VmqZMIW4fmw==', 'Thu Hà - Hà Nội', 'hanoi.staff@aurarental.local', 'STAFF', true),
('20000000-0000-0000-0000-000000000003', 'saigon', 'AQAAAAIACSfAAAAAEGEIipZTKpWsHW3axevnuZwWVmZZ4I/SnNZHBtQi5VZYOqAu36XTtLFF0B/7fBjOdw==', 'Minh Châu - Sài Gòn', 'saigon.staff@aurarental.local', 'STAFF', true)
ON CONFLICT (id) DO UPDATE SET username = EXCLUDED.username,
password_hash = EXCLUDED.password_hash, name = EXCLUDED.name, email = EXCLUDED.email,
role = EXCLUDED.role, is_active = EXCLUDED.is_active;

INSERT INTO user_branches (user_id, branch_id) VALUES
('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'),
('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002'),
('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001'),
('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002')
ON CONFLICT (user_id, branch_id) DO NOTHING;

-- Customers ------------------------------------------------------------------
INSERT INTO customers (id, name, phone, instagram_handle, address) VALUES
('30000000-0000-0000-0000-000000000001', 'Nguyễn Ngọc Lan', '+84901234001', '@lan.nguyen', 'Cầu Giấy, Hà Nội'),
('30000000-0000-0000-0000-000000000002', 'Trần Minh Thư', '+84901234002', '@minhthu.daily', 'Hai Bà Trưng, Hà Nội'),
('30000000-0000-0000-0000-000000000003', 'Lê Hoàng Yến', '+84901234003', '@hoangyen.le', 'Ba Đình, Hà Nội'),
('30000000-0000-0000-0000-000000000004', 'Phạm Khánh Linh', '+84901234004', '@khanhlinh.pham', 'Quận 3, TP. Hồ Chí Minh'),
('30000000-0000-0000-0000-000000000005', 'Vũ Gia Hân', '+84901234005', '@giahan.vu', 'Quận 7, TP. Hồ Chí Minh')
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, phone = EXCLUDED.phone,
instagram_handle = EXCLUDED.instagram_handle, address = EXCLUDED.address;

-- Catalog --------------------------------------------------------------------
INSERT INTO products (id, code, name, category, color, material, description, image_paths, is_active) VALUES
('40000000-0000-0000-0000-000000000001', 'AURORA-RED', 'Váy Aurora Đỏ', 'EVENING_DRESS', 'Đỏ ruby', 'Satin', 'Váy dạ hội satin, dáng corset.', ARRAY['/mock/products/aurora-red-1.jpg'], true),
('40000000-0000-0000-0000-000000000002', 'LUNA-WHITE', 'Váy Luna Trắng', 'PARTY_DRESS', 'Trắng kem', 'Organza', 'Váy ngắn dự tiệc, tay phồng.', ARRAY['/mock/products/luna-white-1.jpg'], true),
('40000000-0000-0000-0000-000000000003', 'CELESTE-BLUE', 'Áo dài Celeste', 'AO_DAI', 'Xanh pastel', 'Lụa', 'Áo dài lụa thêu hoa thủ công.', ARRAY['/mock/products/celeste-blue-1.jpg'], true),
('40000000-0000-0000-0000-000000000004', 'NOIR-GOWN', 'Váy Noir Đen', 'EVENING_DRESS', 'Đen', 'Nhung', 'Váy nhung dài dự tiệc tối.', ARRAY['/mock/products/noir-gown-1.jpg'], true)
ON CONFLICT (id) DO UPDATE SET code = EXCLUDED.code, name = EXCLUDED.name,
category = EXCLUDED.category, color = EXCLUDED.color, material = EXCLUDED.material,
description = EXCLUDED.description, image_paths = EXCLUDED.image_paths, is_active = EXCLUDED.is_active;

INSERT INTO product_variants (id, product_id, size, measurements, replacement_value, is_active) VALUES
('41000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'S', 'Ngực 82-86, eo 62-66', 4500000, true),
('41000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000001', 'M', 'Ngực 86-90, eo 66-70', 4500000, true),
('41000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000002', 'M', 'Ngực 84-90, eo 64-70', 2800000, true),
('41000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000002', 'L', 'Ngực 90-96, eo 70-76', 2800000, true),
('41000000-0000-0000-0000-000000000005', '40000000-0000-0000-0000-000000000003', 'S', 'Ngực 80-86, eo 60-66', 3200000, true),
('41000000-0000-0000-0000-000000000006', '40000000-0000-0000-0000-000000000003', 'M', 'Ngực 86-92, eo 66-72', 3200000, true),
('41000000-0000-0000-0000-000000000007', '40000000-0000-0000-0000-000000000004', 'M', 'Ngực 84-90, eo 64-70', 5200000, true),
('41000000-0000-0000-0000-000000000008', '40000000-0000-0000-0000-000000000004', 'L', 'Ngực 90-96, eo 70-76', 5200000, true)
ON CONFLICT (id) DO UPDATE SET product_id = EXCLUDED.product_id, size = EXCLUDED.size,
measurements = EXCLUDED.measurements, replacement_value = EXCLUDED.replacement_value,
is_active = EXCLUDED.is_active;

-- Each branch has independent 1D/2D/3D prices.
WITH base_prices (branch_id, variant_id, one_day_price) AS (VALUES
('10000000-0000-0000-0000-000000000001'::uuid, '41000000-0000-0000-0000-000000000001'::uuid, 650000::numeric),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000002', 650000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000003', 420000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000004', 420000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000005', 480000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000006', 480000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000007', 780000),
('10000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000008', 780000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000001', 720000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000002', 720000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000003', 460000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000004', 460000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000005', 520000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000006', 520000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000007', 850000),
('10000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000008', 850000)
), packages (package_code, multiplier) AS (VALUES
('1D', 1.00::numeric), ('2D', 1.65::numeric), ('3D', 2.20::numeric)
)
INSERT INTO branch_rental_prices (id, branch_id, variant_id, package_code, price)
SELECT md5(base_prices.branch_id::text || base_prices.variant_id::text || packages.package_code)::uuid,
       base_prices.branch_id, base_prices.variant_id, packages.package_code,
       round(base_prices.one_day_price * packages.multiplier, -3)
FROM base_prices CROSS JOIN packages
ON CONFLICT (branch_id, variant_id, package_code) DO UPDATE SET price = EXCLUDED.price;

-- Inventory includes free, reserved, renting, cleaning, maintenance and lost. --
INSERT INTO inventory_items (id, variant_id, branch_id, asset_code, status, cleaning_hours, cleaning_until) VALUES
('50000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'HN-AUR-S-01', 'USABLE', 12, null),
('50000000-0000-0000-0000-000000000002', '41000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'HN-AUR-S-02', 'USABLE', 12, null),
('50000000-0000-0000-0000-000000000003', '41000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'HN-AUR-M-01', 'USABLE', 12, null),
('50000000-0000-0000-0000-000000000004', '41000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'HN-LUN-M-01', 'USABLE', 8, null),
('50000000-0000-0000-0000-000000000005', '41000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', 'HN-LUN-L-01', 'USABLE', 8, null),
('50000000-0000-0000-0000-000000000006', '41000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000001', 'HN-CEL-S-01', 'MAINTENANCE', 12, null),
('50000000-0000-0000-0000-000000000007', '41000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'HN-LUN-M-02', 'USABLE', 8, null),
('50000000-0000-0000-0000-000000000008', '41000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000001', 'HN-NOI-M-01', 'USABLE', 24, now() + interval '6 hours'),
('50000000-0000-0000-0000-000000000009', '41000000-0000-0000-0000-000000000008', '10000000-0000-0000-0000-000000000001', 'HN-NOI-L-01', 'LOST', 24, null),
('50000000-0000-0000-0000-000000000010', '41000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', 'SG-LUN-L-01', 'USABLE', 8, now() - interval '2 days'),
('50000000-0000-0000-0000-000000000011', '41000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 'SG-AUR-S-01', 'USABLE', 12, null),
('50000000-0000-0000-0000-000000000012', '41000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000002', 'SG-CEL-M-01', 'MAINTENANCE', 12, null)
ON CONFLICT (id) DO UPDATE SET variant_id = EXCLUDED.variant_id, branch_id = EXCLUDED.branch_id,
asset_code = EXCLUDED.asset_code, status = EXCLUDED.status,
cleaning_hours = EXCLUDED.cleaning_hours, cleaning_until = EXCLUDED.cleaning_until;

-- Reservations. Public mock form: token mock-hn-reservation-token / OTP 123456.
INSERT INTO reservations (
    id, reservation_no, customer_id, branch_id, status, rental_start_at, rental_end_at,
    deposit_plan, deposit_required, deposit_deadline_at, otp_hash, form_token_hash,
    otp_expires_at, otp_used_at, cancellation_reason, created_by, created_at) VALUES
('70000000-0000-0000-0000-000000000001', 'RSV-HN-MOCK-ACTIVE', '30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'ACTIVE', now() + interval '2 days', now() + interval '3 days', 'FIXED', 100000, now() + interval '4 hours', '1C199484AE4C6D64E11D98686293D8BEBF730EF8ED758A3CB9CE5E8832B8F0F5', '330FB5FDED6CABE6FD6EADCD16A816D2AE2DE9E89860D50662216D5C4FE0B6B4', now() + interval '24 hours', null, null, '20000000-0000-0000-0000-000000000002', now() - interval '1 hour'),
('70000000-0000-0000-0000-000000000002', 'RSV-HN-MOCK-OVERDUE', '30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'ACTIVE', now() + interval '4 days', now() + interval '5 days', 'FIXED', 100000, now() - interval '3 hours', null, null, null, null, null, '20000000-0000-0000-0000-000000000002', now() - interval '1 day'),
('70000000-0000-0000-0000-000000000003', 'RSV-HN-MOCK-PREPARING', '30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'CONVERTED_TO_ORDER', (timezone('Asia/Ho_Chi_Minh', now())::date + time '18:00') AT TIME ZONE 'Asia/Ho_Chi_Minh', ((timezone('Asia/Ho_Chi_Minh', now())::date + time '18:00') AT TIME ZONE 'Asia/Ho_Chi_Minh') + interval '1 day', 'FULL', 2800000, now() - interval '1 day', null, null, null, now() - interval '1 day', null, '20000000-0000-0000-0000-000000000002', now() - interval '2 days'),
('70000000-0000-0000-0000-000000000004', 'RSV-HN-MOCK-RENTING', '30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'CONVERTED_TO_ORDER', ((timezone('Asia/Ho_Chi_Minh', now())::date + time '20:00') AT TIME ZONE 'Asia/Ho_Chi_Minh') - interval '1 day', (timezone('Asia/Ho_Chi_Minh', now())::date + time '20:00') AT TIME ZONE 'Asia/Ho_Chi_Minh', 'FULL', 2800000, now() - interval '3 days', null, null, null, now() - interval '3 days', null, '20000000-0000-0000-0000-000000000002', now() - interval '4 days'),
('70000000-0000-0000-0000-000000000005', 'RSV-HN-MOCK-INSPECTING', '30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'CONVERTED_TO_ORDER', now() - interval '4 days', now() - interval '3 days', 'FULL', 3200000, now() - interval '6 days', null, null, null, now() - interval '6 days', null, '20000000-0000-0000-0000-000000000002', now() - interval '7 days'),
('70000000-0000-0000-0000-000000000006', 'RSV-SG-MOCK-COMPLETED', '30000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000002', 'CONVERTED_TO_ORDER', now() - interval '20 days', now() - interval '18 days', 'FULL', 2800000, now() - interval '22 days', null, null, null, now() - interval '22 days', null, '20000000-0000-0000-0000-000000000003', now() - interval '23 days'),
('70000000-0000-0000-0000-000000000007', 'RSV-HN-MOCK-PENDING', '30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'CONVERTED_TO_ORDER', now() + interval '1 day', now() + interval '2 days', 'FULL', 2800000, now() + interval '2 hours', null, null, null, now() - interval '30 minutes', null, '20000000-0000-0000-0000-000000000002', now() - interval '2 hours')
ON CONFLICT (id) DO UPDATE SET reservation_no = EXCLUDED.reservation_no,
customer_id = EXCLUDED.customer_id, branch_id = EXCLUDED.branch_id, status = EXCLUDED.status,
rental_start_at = EXCLUDED.rental_start_at, rental_end_at = EXCLUDED.rental_end_at,
deposit_plan = EXCLUDED.deposit_plan, deposit_required = EXCLUDED.deposit_required,
deposit_deadline_at = EXCLUDED.deposit_deadline_at, otp_hash = EXCLUDED.otp_hash,
form_token_hash = EXCLUDED.form_token_hash, otp_expires_at = EXCLUDED.otp_expires_at,
otp_used_at = EXCLUDED.otp_used_at, cancellation_reason = EXCLUDED.cancellation_reason,
created_by = EXCLUDED.created_by, created_at = EXCLUDED.created_at;

INSERT INTO reservation_items (id, reservation_id, inventory_item_id, package_code,
replacement_value, rental_price, one_day_price, extra_day_rate) VALUES
('c0000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000002', '1D', 4500000, 650000, 650000, 0.1000),
('c0000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000003', '1D', 4500000, 650000, 650000, 0.1000),
('c0000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000004', '1D', 2800000, 420000, 420000, 0.1000),
('c0000000-0000-0000-0000-000000000004', '70000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000005', '1D', 2800000, 420000, 420000, 0.1000),
('c0000000-0000-0000-0000-000000000005', '70000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000006', '1D', 3200000, 480000, 480000, 0.1000),
('c0000000-0000-0000-0000-000000000006', '70000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000010', '2D', 2800000, 759000, 460000, 0.1000),
('c0000000-0000-0000-0000-000000000007', '70000000-0000-0000-0000-000000000007', '50000000-0000-0000-0000-000000000007', '1D', 2800000, 420000, 420000, 0.1000)
ON CONFLICT (id) DO UPDATE SET reservation_id = EXCLUDED.reservation_id,
inventory_item_id = EXCLUDED.inventory_item_id, package_code = EXCLUDED.package_code,
replacement_value = EXCLUDED.replacement_value, rental_price = EXCLUDED.rental_price,
one_day_price = EXCLUDED.one_day_price, extra_day_rate = EXCLUDED.extra_day_rate;

-- Orders cover dashboard, fulfillment, return and customer-history screens. --
INSERT INTO orders (id, order_no, customer_id, reservation_id, branch_id, status,
customer_name, customer_phone, delivery_address, delivery_status, return_delivery_status,
delivery_tracking_code, return_tracking_code, identity_verified_by, identity_verified_at,
delivered_at, returned_at, settled_by, settled_at, cancellation_reason, created_at) VALUES
('80000000-0000-0000-0000-000000000003', 'ORD-HN-MOCK-PREPARING', '30000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'PREPARING', 'Lê Hoàng Yến', '+84901234003', 'Ba Đình, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, '20000000-0000-0000-0000-000000000002', now() - interval '1 day', null, null, null, null, null, now() - interval '2 days'),
('80000000-0000-0000-0000-000000000004', 'ORD-HN-MOCK-RENTING', '30000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', 'RENTING', 'Nguyễn Ngọc Lan', '+84901234001', 'Cầu Giấy, Hà Nội', 'COMPLETED', 'NOT_STARTED', 'GHN-HN-MOCK-004', null, '20000000-0000-0000-0000-000000000002', now() - interval '3 days', now() - interval '1 day', null, null, null, null, now() - interval '4 days'),
('80000000-0000-0000-0000-000000000005', 'ORD-HN-MOCK-INSPECTING', '30000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000001', 'INSPECTING', 'Trần Minh Thư', '+84901234002', 'Hai Bà Trưng, Hà Nội', 'COMPLETED', 'COMPLETED', 'GHN-HN-MOCK-005', 'RTN-HN-MOCK-005', '20000000-0000-0000-0000-000000000002', now() - interval '6 days', now() - interval '4 days', now() - interval '2 hours', null, null, null, now() - interval '7 days'),
('80000000-0000-0000-0000-000000000006', 'ORD-SG-MOCK-COMPLETED', '30000000-0000-0000-0000-000000000004', '70000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000002', 'COMPLETED', 'Phạm Khánh Linh', '+84901234004', 'Quận 3, TP. Hồ Chí Minh', 'COMPLETED', 'COMPLETED', 'GHN-SG-MOCK-006', 'RTN-SG-MOCK-006', '20000000-0000-0000-0000-000000000003', now() - interval '22 days', now() - interval '20 days', now() - interval '17 days', '20000000-0000-0000-0000-000000000001', now() - interval '17 days', null, now() - interval '23 days'),
('80000000-0000-0000-0000-000000000007', 'ORD-HN-MOCK-PENDING', '30000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000001', 'PENDING_DEPOSIT', 'Lê Hoàng Yến', '+84901234003', 'Ba Đình, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, null, null, null, null, null, null, null, now() - interval '2 hours')
ON CONFLICT (id) DO UPDATE SET order_no = EXCLUDED.order_no, customer_id = EXCLUDED.customer_id,
reservation_id = EXCLUDED.reservation_id, branch_id = EXCLUDED.branch_id, status = EXCLUDED.status,
customer_name = EXCLUDED.customer_name, customer_phone = EXCLUDED.customer_phone,
delivery_address = EXCLUDED.delivery_address, delivery_status = EXCLUDED.delivery_status,
return_delivery_status = EXCLUDED.return_delivery_status, delivery_tracking_code = EXCLUDED.delivery_tracking_code,
return_tracking_code = EXCLUDED.return_tracking_code, identity_verified_by = EXCLUDED.identity_verified_by,
identity_verified_at = EXCLUDED.identity_verified_at, delivered_at = EXCLUDED.delivered_at,
returned_at = EXCLUDED.returned_at, settled_by = EXCLUDED.settled_by,
settled_at = EXCLUDED.settled_at, cancellation_reason = EXCLUDED.cancellation_reason,
created_at = EXCLUDED.created_at;

INSERT INTO order_items (id, order_id, inventory_item_id, product_name, size, asset_code,
package_code, replacement_value, rental_price, one_day_price, extra_day_rate,
actual_rental_fee, condition, processing_fee, damage_note, damage_photo_paths) VALUES
('b0000000-0000-0000-0000-000000000003', '80000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000004', 'Váy Luna Trắng', 'M', 'HN-LUN-M-01', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}'),
('b0000000-0000-0000-0000-000000000004', '80000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000005', 'Váy Luna Trắng', 'L', 'HN-LUN-L-01', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}'),
('b0000000-0000-0000-0000-000000000005', '80000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000006', 'Áo dài Celeste', 'S', 'HN-CEL-S-01', '1D', 3200000, 480000, 480000, 0.1000, 480000, 'DAMAGED', 250000, 'Rách đường may bên hông, cần sửa trước khi cho thuê lại.', ARRAY['/mock/damage/ord-hn-005-1.jpg']),
('b0000000-0000-0000-0000-000000000006', '80000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000010', 'Váy Luna Trắng', 'L', 'SG-LUN-L-01', '2D', 2800000, 759000, 460000, 0.1000, 759000, 'GOOD', 0, null, '{}'),
('b0000000-0000-0000-0000-000000000007', '80000000-0000-0000-0000-000000000007', '50000000-0000-0000-0000-000000000007', 'Váy Luna Trắng', 'M', 'HN-LUN-M-02', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}')
ON CONFLICT (id) DO UPDATE SET order_id = EXCLUDED.order_id,
inventory_item_id = EXCLUDED.inventory_item_id, product_name = EXCLUDED.product_name,
size = EXCLUDED.size, asset_code = EXCLUDED.asset_code, package_code = EXCLUDED.package_code,
replacement_value = EXCLUDED.replacement_value, rental_price = EXCLUDED.rental_price,
one_day_price = EXCLUDED.one_day_price, extra_day_rate = EXCLUDED.extra_day_rate,
actual_rental_fee = EXCLUDED.actual_rental_fee, condition = EXCLUDED.condition,
processing_fee = EXCLUDED.processing_fee, damage_note = EXCLUDED.damage_note,
damage_photo_paths = EXCLUDED.damage_photo_paths;

-- Refund awaiting manager approval and one completed historical refund. -------
INSERT INTO refunds (id, order_id, version, status, deposit_amount, rental_fee,
processing_fee, refund_amount, items_snapshot, adjustment_reason, created_by,
submitted_by, approved_by, approved_at, created_at) VALUES
('90000000-0000-0000-0000-000000000005', '80000000-0000-0000-0000-000000000005', 1, 'SUBMITTED', 3200000, 480000, 250000, 2470000, '[{"orderItemId":"b0000000-0000-0000-0000-000000000005","productName":"Áo dài Celeste","size":"S","assetCode":"HN-CEL-S-01","condition":"DAMAGED","actualRentalFee":480000,"processingFee":250000,"damageNote":"Rách đường may bên hông","damagePhotoPaths":["/mock/damage/ord-hn-005-1.jpg"]}]', 'Chờ manager duyệt phí sửa váy', '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', null, null, now() - interval '1 hour'),
('90000000-0000-0000-0000-000000000006', '80000000-0000-0000-0000-000000000006', 1, 'APPROVED', 2800000, 759000, 0, 2041000, '[{"orderItemId":"b0000000-0000-0000-0000-000000000006","productName":"Váy Luna Trắng","size":"L","assetCode":"SG-LUN-L-01","condition":"GOOD","actualRentalFee":759000,"processingFee":0,"damagePhotoPaths":[]}]', null, '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001', now() - interval '17 days', now() - interval '17 days')
ON CONFLICT (id) DO UPDATE SET order_id = EXCLUDED.order_id, version = EXCLUDED.version,
status = EXCLUDED.status, deposit_amount = EXCLUDED.deposit_amount,
rental_fee = EXCLUDED.rental_fee, processing_fee = EXCLUDED.processing_fee,
refund_amount = EXCLUDED.refund_amount, items_snapshot = EXCLUDED.items_snapshot,
adjustment_reason = EXCLUDED.adjustment_reason, created_by = EXCLUDED.created_by,
submitted_by = EXCLUDED.submitted_by, approved_by = EXCLUDED.approved_by,
approved_at = EXCLUDED.approved_at, created_at = EXCLUDED.created_at;

-- Confirmed deposits and a completed refund for reports/history. -------------
INSERT INTO payments (id, reservation_id, refund_id, type, amount, method, status,
transaction_ref, proof_path, recorded_by, confirmed_by, paid_at, note) VALUES
('a0000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', null, 'SLOT_DEPOSIT', 100000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-001', null, '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', now() - interval '1 hour', 'Cọc giữ lịch qua Instagram'),
('a0000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000003', null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-003', null, '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', now() - interval '2 days', null),
('a0000000-0000-0000-0000-000000000004', '70000000-0000-0000-0000-000000000004', null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-004', null, '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', now() - interval '4 days', null),
('a0000000-0000-0000-0000-000000000005', '70000000-0000-0000-0000-000000000005', null, 'TARGET_DEPOSIT', 3200000, 'CASH', 'CONFIRMED', 'MOCK-HN-DEP-005', null, '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', now() - interval '7 days', null),
('a0000000-0000-0000-0000-000000000006', '70000000-0000-0000-0000-000000000006', null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-SG-DEP-006', null, '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000003', now() - interval '23 days', null),
('a0000000-0000-0000-0000-000000000106', '70000000-0000-0000-0000-000000000006', '90000000-0000-0000-0000-000000000006', 'REFUND', 2041000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-SG-REF-006', null, '20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', now() - interval '17 days', 'Hoàn cọc sau đối soát')
ON CONFLICT (id) DO UPDATE SET reservation_id = EXCLUDED.reservation_id,
refund_id = EXCLUDED.refund_id, type = EXCLUDED.type, amount = EXCLUDED.amount,
method = EXCLUDED.method, status = EXCLUDED.status, transaction_ref = EXCLUDED.transaction_ref,
proof_path = EXCLUDED.proof_path, recorded_by = EXCLUDED.recorded_by,
confirmed_by = EXCLUDED.confirmed_by, paid_at = EXCLUDED.paid_at, note = EXCLUDED.note;

COMMIT;

SELECT 'branches' AS entity, count(*) AS row_count FROM aura.branches
UNION ALL SELECT 'users', count(*) FROM aura.users
UNION ALL SELECT 'customers', count(*) FROM aura.customers
UNION ALL SELECT 'products', count(*) FROM aura.products
UNION ALL SELECT 'inventory_items', count(*) FROM aura.inventory_items
UNION ALL SELECT 'reservations', count(*) FROM aura.reservations
UNION ALL SELECT 'orders', count(*) FROM aura.orders
UNION ALL SELECT 'refunds', count(*) FROM aura.refunds
UNION ALL SELECT 'payments', count(*) FROM aura.payments
ORDER BY entity;
