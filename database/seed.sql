-- Development-only mock data for Aura Rental.
-- Seed rows use stable integer IDs and UPSERT, so this file can be run repeatedly.
-- Existing non-seed rows are not deleted.

BEGIN;
SET LOCAL search_path = aura;

-- Branches and users ---------------------------------------------------------
INSERT INTO branches (id, code, name, address, is_active) VALUES
(1, 'HN', 'Aura Rental Hà Nội', '25 Thái Hà, Đống Đa, Hà Nội', true),
(2, 'SG', 'Aura Rental Sài Gòn', '112 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh', true)
ON CONFLICT (id) DO UPDATE SET code = EXCLUDED.code, name = EXCLUDED.name,
address = EXCLUDED.address, is_active = EXCLUDED.is_active;

INSERT INTO users (id, username, password_hash, name, email, role, is_active) VALUES
(1, 'manager', 'AQAAAAIACSfAAAAAENzFhqVDNxRTks+ElzTMfsq0EDSS9G6wiHUcKi84MNmIamJ1kpFwVQkzZlCQaF7DQA==', 'Mai Anh - Quản lý', 'manager@aurarental.local', 'MANAGER', true),
(2, 'hanoi', 'AQAAAAIACSfAAAAAEIEdXF3upWPbQMCuVeAwhzkHJ6HNedPn2tB3Z4huXt0UspeLhT/euP6VmqZMIW4fmw==', 'Thu Hà - Hà Nội', 'hanoi.staff@aurarental.local', 'STAFF', true),
(3, 'saigon', 'AQAAAAIACSfAAAAAEGEIipZTKpWsHW3axevnuZwWVmZZ4I/SnNZHBtQi5VZYOqAu36XTtLFF0B/7fBjOdw==', 'Minh Châu - Sài Gòn', 'saigon.staff@aurarental.local', 'STAFF', true)
ON CONFLICT (id) DO UPDATE SET username = EXCLUDED.username,
password_hash = EXCLUDED.password_hash, name = EXCLUDED.name, email = EXCLUDED.email,
role = EXCLUDED.role, is_active = EXCLUDED.is_active;

INSERT INTO user_branches (user_id, branch_id) VALUES
(1, 1),
(1, 2),
(2, 1),
(3, 2)
ON CONFLICT (user_id, branch_id) DO NOTHING;

-- Customers ------------------------------------------------------------------
INSERT INTO customers (id, name, phone, instagram_handle, tiktok_handle, address) VALUES
(1, 'Nguyễn Ngọc Lan', '+84901234001', '@lan.nguyen', '@lan.nguyen', 'Cầu Giấy, Hà Nội'),
(2, 'Trần Minh Thư', '+84901234002', '@minhthu.daily', '@minhthu.daily', 'Hai Bà Trưng, Hà Nội'),
(3, 'Lê Hoàng Yến', '+84901234003', '@hoangyen.le', null, 'Ba Đình, Hà Nội'),
(4, 'Phạm Khánh Linh', '+84901234004', '@khanhlinh.pham', '@khanhlinh', 'Quận 3, TP. Hồ Chí Minh'),
(5, 'Vũ Gia Hân', '+84901234005', '@giahan.vu', null, 'Quận 7, TP. Hồ Chí Minh')
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, phone = EXCLUDED.phone,
instagram_handle = EXCLUDED.instagram_handle, tiktok_handle = EXCLUDED.tiktok_handle, address = EXCLUDED.address;

-- Catalog --------------------------------------------------------------------
INSERT INTO products (id, branch_id, code, name, category, color, material, description, image_paths, is_active) VALUES
(1, 1, 'AURORA-RED', 'Váy Aurora Đỏ', 'EVENING_DRESS', 'Đỏ ruby', 'Satin', 'Váy dạ hội satin, dáng corset.', ARRAY['/mock/products/aurora-red-1.jpg'], true),
(2, 1, 'LUNA-WHITE', 'Váy Luna Trắng', 'PARTY_DRESS', 'Trắng kem', 'Organza', 'Váy ngắn dự tiệc, tay phồng.', ARRAY['/mock/products/luna-white-1.jpg'], true),
(3, 1, 'CELESTE-BLUE', 'Áo dài Celeste', 'AO_DAI', 'Xanh pastel', 'Lụa', 'Áo dài lụa thêu hoa thủ công.', ARRAY['/mock/products/celeste-blue-1.jpg'], true),
(4, 1, 'NOIR-GOWN', 'Váy Noir Đen', 'EVENING_DRESS', 'Đen', 'Nhung', 'Váy nhung dài dự tiệc tối.', ARRAY['/mock/products/noir-gown-1.jpg'], true),
(11, 2, 'AURORA-RED', 'Váy Aurora Đỏ', 'EVENING_DRESS', 'Đỏ ruby', 'Satin', 'Váy dạ hội satin, dáng corset.', ARRAY['/mock/products/aurora-red-1.jpg'], true),
(12, 2, 'LUNA-WHITE', 'Váy Luna Trắng', 'PARTY_DRESS', 'Trắng kem', 'Organza', 'Váy ngắn dự tiệc, tay phồng.', ARRAY['/mock/products/luna-white-1.jpg'], true),
(13, 2, 'CELESTE-BLUE', 'Áo dài Celeste', 'AO_DAI', 'Xanh pastel', 'Lụa', 'Áo dài lụa thêu hoa thủ công.', ARRAY['/mock/products/celeste-blue-1.jpg'], true),
(14, 2, 'NOIR-GOWN', 'Váy Noir Đen', 'EVENING_DRESS', 'Đen', 'Nhung', 'Váy nhung dài dự tiệc tối.', ARRAY['/mock/products/noir-gown-1.jpg'], true)
ON CONFLICT (id) DO UPDATE SET branch_id = EXCLUDED.branch_id, code = EXCLUDED.code, name = EXCLUDED.name,
category = EXCLUDED.category, color = EXCLUDED.color, material = EXCLUDED.material,
description = EXCLUDED.description, image_paths = EXCLUDED.image_paths, is_active = EXCLUDED.is_active;

INSERT INTO product_variants (id, product_id, size, measurements, replacement_value, is_active) VALUES
(1, 1, 'S', 'Ngực 82-86, eo 62-66', 4500000, true),
(2, 1, 'M', 'Ngực 86-90, eo 66-70', 4500000, true),
(3, 2, 'M', 'Ngực 84-90, eo 64-70', 2800000, true),
(4, 2, 'L', 'Ngực 90-96, eo 70-76', 2800000, true),
(5, 3, 'S', 'Ngực 80-86, eo 60-66', 3200000, true),
(6, 3, 'M', 'Ngực 86-92, eo 66-72', 3200000, true),
(7, 4, 'M', 'Ngực 84-90, eo 64-70', 5200000, true),
(8, 4, 'L', 'Ngực 90-96, eo 70-76', 5200000, true),
(11, 11, 'S', 'Ngực 82-86, eo 62-66', 4500000, true),
(12, 11, 'M', 'Ngực 86-90, eo 66-70', 4500000, true),
(13, 12, 'M', 'Ngực 84-90, eo 64-70', 2800000, true),
(14, 12, 'L', 'Ngực 90-96, eo 70-76', 2800000, true),
(15, 13, 'S', 'Ngực 80-86, eo 60-66', 3200000, true),
(16, 13, 'M', 'Ngực 86-92, eo 66-72', 3200000, true),
(17, 14, 'M', 'Ngực 84-90, eo 64-70', 5200000, true),
(18, 14, 'L', 'Ngực 90-96, eo 70-76', 5200000, true)
ON CONFLICT (id) DO UPDATE SET product_id = EXCLUDED.product_id, size = EXCLUDED.size,
measurements = EXCLUDED.measurements, replacement_value = EXCLUDED.replacement_value,
is_active = EXCLUDED.is_active;

-- Each branch has independent 1D/2D/3D prices.
WITH base_prices (branch_id, variant_id, one_day_price) AS (VALUES
(1, 1, 650000::numeric),
(1, 2, 650000),
(1, 3, 420000),
(1, 4, 420000),
(1, 5, 480000),
(1, 6, 480000),
(1, 7, 780000),
(1, 8, 780000),
(2, 11, 720000),
(2, 12, 720000),
(2, 13, 460000),
(2, 14, 460000),
(2, 15, 520000),
(2, 16, 520000),
(2, 17, 850000),
(2, 18, 850000)
), packages (package_code, multiplier) AS (VALUES
('1D', 1.00::numeric), ('2D', 1.65::numeric), ('3D', 2.20::numeric)
)
INSERT INTO branch_rental_prices (branch_id, variant_id, package_code, price)
SELECT base_prices.branch_id, base_prices.variant_id, packages.package_code,
       round(base_prices.one_day_price * packages.multiplier, -3)
FROM base_prices CROSS JOIN packages
ON CONFLICT (branch_id, variant_id, package_code) DO UPDATE SET price = EXCLUDED.price;

-- Inventory includes free, reserved, renting, maintenance and lost items. --
INSERT INTO inventory_items (id, variant_id, branch_id, asset_code, status) VALUES
(1, 1, 1, 'HN-AUR-S-01', 'USABLE'),
(2, 1, 1, 'HN-AUR-S-02', 'USABLE'),
(3, 2, 1, 'HN-AUR-M-01', 'USABLE'),
(4, 3, 1, 'HN-LUN-M-01', 'USABLE'),
(5, 4, 1, 'HN-LUN-L-01', 'USABLE'),
(6, 5, 1, 'HN-CEL-S-01', 'MAINTENANCE'),
(7, 3, 1, 'HN-LUN-M-02', 'USABLE'),
(8, 7, 1, 'HN-NOI-M-01', 'USABLE'),
(9, 8, 1, 'HN-NOI-L-01', 'LOST'),
(10, 14, 2, 'SG-LUN-L-01', 'USABLE'),
(11, 11, 2, 'SG-AUR-S-01', 'USABLE'),
(12, 16, 2, 'SG-CEL-M-01', 'MAINTENANCE'),
(13, 1, 1, 'HN-AUR-S-03', 'USABLE'),
(14, 6, 1, 'HN-CEL-M-02', 'USABLE'),
(15, 7, 1, 'HN-NOI-M-02', 'USABLE'),
(16, 12, 2, 'SG-AUR-M-01', 'USABLE')
ON CONFLICT (id) DO UPDATE SET variant_id = EXCLUDED.variant_id, branch_id = EXCLUDED.branch_id,
asset_code = EXCLUDED.asset_code, status = EXCLUDED.status;

-- Reservations. Public mock form: token mock-hn-reservation-token / OTP 123456.
INSERT INTO reservations (
    id, reservation_no, customer_id, branch_id, status, rental_start_at, rental_end_at,
    deposit_plan, deposit_required, otp_hash, form_token_hash,
    otp_expires_at, otp_used_at, cancellation_reason, created_by, created_at) VALUES
(1, 'RSV-HN-MOCK-ACTIVE', 1, 1, 'ACTIVE', now() + interval '2 days', now() + interval '3 days', 'FIXED', 100000, '1C199484AE4C6D64E11D98686293D8BEBF730EF8ED758A3CB9CE5E8832B8F0F5', '330FB5FDED6CABE6FD6EADCD16A816D2AE2DE9E89860D50662216D5C4FE0B6B4', now() + interval '24 hours', null, null, 2, now() - interval '1 hour'),
(2, 'RSV-HN-MOCK-SLOT', 2, 1, 'ACTIVE', now() + interval '4 days', now() + interval '5 days', 'FIXED', 100000, null, null, null, null, null, 2, now() - interval '1 day'),
(3, 'RSV-HN-MOCK-PREPARING', 3, 1, 'CONVERTED_TO_ORDER', (timezone('Asia/Ho_Chi_Minh', now())::date + time '18:00') AT TIME ZONE 'Asia/Ho_Chi_Minh', ((timezone('Asia/Ho_Chi_Minh', now())::date + time '18:00') AT TIME ZONE 'Asia/Ho_Chi_Minh') + interval '1 day', 'FULL', 2800000, null, null, null, now() - interval '1 day', null, 2, now() - interval '2 days'),
(4, 'RSV-HN-MOCK-RENTING', 1, 1, 'CONVERTED_TO_ORDER', ((timezone('Asia/Ho_Chi_Minh', now())::date + time '20:00') AT TIME ZONE 'Asia/Ho_Chi_Minh') - interval '1 day', (timezone('Asia/Ho_Chi_Minh', now())::date + time '20:00') AT TIME ZONE 'Asia/Ho_Chi_Minh', 'FULL', 2800000, null, null, null, now() - interval '3 days', null, 2, now() - interval '4 days'),
(5, 'RSV-HN-MOCK-INSPECTING', 2, 1, 'CONVERTED_TO_ORDER', now() - interval '4 days', now() - interval '3 days', 'FULL', 3200000, null, null, null, now() - interval '6 days', null, 2, now() - interval '7 days'),
(6, 'RSV-SG-MOCK-COMPLETED', 4, 2, 'CONVERTED_TO_ORDER', now() - interval '20 days', now() - interval '18 days', 'FULL', 2800000, null, null, null, now() - interval '22 days', null, 3, now() - interval '23 days'),
(7, 'RSV-HN-MOCK-PENDING', 3, 1, 'CONVERTED_TO_ORDER', now() + interval '1 day', now() + interval '2 days', 'FULL', 2800000, null, null, null, now() - interval '30 minutes', null, 2, now() - interval '2 hours'),
(8, 'RSV-HN-MOCK-CONFIRMED', 1, 1, 'CONVERTED_TO_ORDER', now() + interval '3 days', now() + interval '4 days', 'FULL', 4500000, null, null, null, now() - interval '1 day', null, 2, now() - interval '2 days'),
(9, 'RSV-HN-MOCK-VERIFY-ID', 2, 1, 'CONVERTED_TO_ORDER', now() + interval '4 days', now() + interval '5 days', 'FIFTY_WITH_ID', 1600000, null, null, null, now() - interval '1 day', null, 2, now() - interval '2 days'),
(10, 'RSV-HN-MOCK-TO-INSPECT', 3, 1, 'CONVERTED_TO_ORDER', now() - interval '3 days', now() - interval '2 days', 'FULL', 5200000, null, null, null, now() - interval '5 days', null, 2, now() - interval '6 days'),
(11, 'RSV-SG-MOCK-ACTIVE', 5, 2, 'ACTIVE', now() + interval '3 days', now() + interval '4 days', 'FULL', 4500000, null, null, null, null, null, 3, now() - interval '30 minutes')
ON CONFLICT (id) DO UPDATE SET reservation_no = EXCLUDED.reservation_no,
customer_id = EXCLUDED.customer_id, branch_id = EXCLUDED.branch_id, status = EXCLUDED.status,
rental_start_at = EXCLUDED.rental_start_at, rental_end_at = EXCLUDED.rental_end_at,
deposit_plan = EXCLUDED.deposit_plan, deposit_required = EXCLUDED.deposit_required,
otp_hash = EXCLUDED.otp_hash,
form_token_hash = EXCLUDED.form_token_hash, otp_expires_at = EXCLUDED.otp_expires_at,
otp_used_at = EXCLUDED.otp_used_at, cancellation_reason = EXCLUDED.cancellation_reason,
created_by = EXCLUDED.created_by, created_at = EXCLUDED.created_at;

INSERT INTO reservation_items (id, reservation_id, inventory_item_id, package_code,
replacement_value, rental_price, one_day_price, extra_day_rate) VALUES
(1, 1, 2, '1D', 4500000, 650000, 650000, 0.1000),
(2, 2, 3, '1D', 4500000, 650000, 650000, 0.1000),
(3, 3, 4, '1D', 2800000, 420000, 420000, 0.1000),
(4, 4, 5, '1D', 2800000, 420000, 420000, 0.1000),
(5, 5, 6, '1D', 3200000, 480000, 480000, 0.1000),
(6, 6, 10, '2D', 2800000, 759000, 460000, 0.1000),
(7, 7, 7, '1D', 2800000, 420000, 420000, 0.1000),
(8, 8, 13, '1D', 4500000, 650000, 650000, 0.1000),
(9, 9, 14, '1D', 3200000, 480000, 480000, 0.1000),
(10, 10, 15, '1D', 5200000, 780000, 780000, 0.1000),
(11, 11, 16, '1D', 4500000, 720000, 720000, 0.1000)
ON CONFLICT (id) DO UPDATE SET reservation_id = EXCLUDED.reservation_id,
inventory_item_id = EXCLUDED.inventory_item_id, package_code = EXCLUDED.package_code,
replacement_value = EXCLUDED.replacement_value, rental_price = EXCLUDED.rental_price,
one_day_price = EXCLUDED.one_day_price, extra_day_rate = EXCLUDED.extra_day_rate;

-- Orders cover dashboard, fulfillment, return and customer-history screens. --
INSERT INTO orders (id, order_no, customer_id, reservation_id, branch_id, status,
customer_name, customer_phone, delivery_address, delivery_status, return_delivery_status,
delivery_tracking_code, return_tracking_code, identity_verified_by, identity_verified_at,
delivered_at, returned_at, settled_by, settled_at, cancellation_reason, created_at) VALUES
(3, 'ORD-HN-MOCK-PREPARING', 3, 3, 1, 'PREPARING', 'Lê Hoàng Yến', '+84901234003', 'Ba Đình, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, 2, now() - interval '1 day', null, null, null, null, null, now() - interval '2 days'),
(4, 'ORD-HN-MOCK-RENTING', 1, 4, 1, 'RENTING', 'Nguyễn Ngọc Lan', '+84901234001', 'Cầu Giấy, Hà Nội', 'COMPLETED', 'NOT_STARTED', 'GHN-HN-MOCK-004', null, 2, now() - interval '3 days', now() - interval '1 day', null, null, null, null, now() - interval '4 days'),
(5, 'ORD-HN-MOCK-INSPECTING', 2, 5, 1, 'INSPECTING', 'Trần Minh Thư', '+84901234002', 'Hai Bà Trưng, Hà Nội', 'COMPLETED', 'COMPLETED', 'GHN-HN-MOCK-005', 'RTN-HN-MOCK-005', 2, now() - interval '6 days', now() - interval '4 days', now() - interval '2 hours', null, null, null, now() - interval '7 days'),
(6, 'ORD-SG-MOCK-COMPLETED', 4, 6, 2, 'COMPLETED', 'Phạm Khánh Linh', '+84901234004', 'Quận 3, TP. Hồ Chí Minh', 'COMPLETED', 'COMPLETED', 'GHN-SG-MOCK-006', 'RTN-SG-MOCK-006', 3, now() - interval '22 days', now() - interval '20 days', now() - interval '17 days', 1, now() - interval '17 days', null, now() - interval '23 days'),
(7, 'ORD-HN-MOCK-PENDING', 3, 7, 1, 'PENDING_DEPOSIT', 'Lê Hoàng Yến', '+84901234003', 'Ba Đình, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, null, null, null, null, null, null, null, now() - interval '2 hours'),
(8, 'ORD-HN-MOCK-CONFIRMED', 1, 8, 1, 'CONFIRMED', 'Nguyễn Ngọc Lan', '+84901234001', 'Cầu Giấy, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, null, null, null, null, null, null, null, now() - interval '2 days'),
(9, 'ORD-HN-MOCK-VERIFY-ID', 2, 9, 1, 'PENDING_VERIFICATION', 'Trần Minh Thư', '+84901234002', 'Hai Bà Trưng, Hà Nội', 'NOT_STARTED', 'NOT_STARTED', null, null, null, null, null, null, null, null, null, now() - interval '2 days'),
(10, 'ORD-HN-MOCK-TO-INSPECT', 3, 10, 1, 'INSPECTING', 'Lê Hoàng Yến', '+84901234003', 'Ba Đình, Hà Nội', 'COMPLETED', 'DONE', 'GHN-HN-MOCK-010', 'RTN-HN-MOCK-010', 2, now() - interval '5 days', now() - interval '3 days', now() - interval '1 hour', null, null, null, now() - interval '6 days')
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
(3, 3, 4, 'Váy Luna Trắng', 'M', 'HN-LUN-M-01', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}'),
(4, 4, 5, 'Váy Luna Trắng', 'L', 'HN-LUN-L-01', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}'),
(5, 5, 6, 'Áo dài Celeste', 'S', 'HN-CEL-S-01', '1D', 3200000, 480000, 480000, 0.1000, 480000, 'DAMAGED', 250000, 'Rách đường may bên hông, cần sửa trước khi cho thuê lại.', ARRAY['/mock/damage/ord-hn-005-1.jpg']),
(6, 6, 10, 'Váy Luna Trắng', 'L', 'SG-LUN-L-01', '2D', 2800000, 759000, 460000, 0.1000, 759000, 'GOOD', 0, null, '{}'),
(7, 7, 7, 'Váy Luna Trắng', 'M', 'HN-LUN-M-02', '1D', 2800000, 420000, 420000, 0.1000, null, null, 0, null, '{}'),
(8, 8, 13, 'Váy Aurora Đỏ', 'S', 'HN-AUR-S-03', '1D', 4500000, 650000, 650000, 0.1000, null, null, 0, null, '{}'),
(9, 9, 14, 'Áo dài Celeste', 'M', 'HN-CEL-M-02', '1D', 3200000, 480000, 480000, 0.1000, null, null, 0, null, '{}'),
(10, 10, 15, 'Váy Noir Đen', 'M', 'HN-NOI-M-02', '1D', 5200000, 780000, 780000, 0.1000, null, null, 0, null, '{}')
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
(5, 5, 1, 'SUBMITTED', 3200000, 480000, 250000, 2470000, '[{"orderItemId":5,"productName":"Áo dài Celeste","size":"S","assetCode":"HN-CEL-S-01","condition":"DAMAGED","actualRentalFee":480000,"processingFee":250000,"damageNote":"Rách đường may bên hông","damagePhotoPaths":["/mock/damage/ord-hn-005-1.jpg"]}]', 'Chờ manager duyệt phí sửa váy', 2, 2, null, null, now() - interval '1 hour'),
(6, 6, 1, 'APPROVED', 2800000, 759000, 0, 2041000, '[{"orderItemId":6,"productName":"Váy Luna Trắng","size":"L","assetCode":"SG-LUN-L-01","condition":"GOOD","actualRentalFee":759000,"processingFee":0,"damagePhotoPaths":[]}]', null, 3, 3, 1, now() - interval '17 days', now() - interval '17 days')
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
(1, 1, null, 'SLOT_DEPOSIT', 100000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-001', null, 2, 2, now() - interval '1 hour', 'Cọc giữ lịch qua Instagram'),
(3, 3, null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-003', null, 2, 2, now() - interval '2 days', null),
(4, 4, null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-004', null, 2, 2, now() - interval '4 days', null),
(5, 5, null, 'TARGET_DEPOSIT', 3200000, 'CASH', 'CONFIRMED', 'MOCK-HN-DEP-005', null, 2, 2, now() - interval '7 days', null),
(6, 6, null, 'TARGET_DEPOSIT', 2800000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-SG-DEP-006', null, 3, 3, now() - interval '23 days', null),
(106, 6, 6, 'REFUND', 2041000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-SG-REF-006', null, 1, 1, now() - interval '17 days', 'Hoàn cọc sau đối soát'),
(8, 8, null, 'TARGET_DEPOSIT', 4500000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-008', null, 2, 2, now() - interval '2 days', 'Đủ cọc, sẵn sàng chuẩn bị'),
(9, 9, null, 'TARGET_DEPOSIT', 1600000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-HN-DEP-009', null, 2, 2, now() - interval '2 days', 'Cọc 50%, chờ đối chiếu CCCD'),
(10, 10, null, 'TARGET_DEPOSIT', 5200000, 'CASH', 'CONFIRMED', 'MOCK-HN-DEP-010', null, 2, 2, now() - interval '6 days', 'Chờ staff kiểm hàng trả'),
(11, 11, null, 'SLOT_DEPOSIT', 100000, 'BANK_TRANSFER', 'CONFIRMED', 'MOCK-SG-DEP-011', null, 3, 3, now() - interval '30 minutes', 'Cọc giữ lịch Sài Gòn')
ON CONFLICT (id) DO UPDATE SET reservation_id = EXCLUDED.reservation_id,
refund_id = EXCLUDED.refund_id, type = EXCLUDED.type, amount = EXCLUDED.amount,
method = EXCLUDED.method, status = EXCLUDED.status, transaction_ref = EXCLUDED.transaction_ref,
proof_path = EXCLUDED.proof_path, recorded_by = EXCLUDED.recorded_by,
confirmed_by = EXCLUDED.confirmed_by, paid_at = EXCLUDED.paid_at, note = EXCLUDED.note;

SELECT setval('users_id_seq', (SELECT max(id) FROM users), true);
SELECT setval('branches_id_seq', (SELECT max(id) FROM branches), true);
SELECT setval('customers_id_seq', (SELECT max(id) FROM customers), true);
SELECT setval('products_id_seq', (SELECT max(id) FROM products), true);
SELECT setval('product_variants_id_seq', (SELECT max(id) FROM product_variants), true);
SELECT setval('inventory_items_id_seq', (SELECT max(id) FROM inventory_items), true);
SELECT setval('branch_rental_prices_id_seq', (SELECT max(id) FROM branch_rental_prices), true);
SELECT setval('reservations_id_seq', (SELECT max(id) FROM reservations), true);
SELECT setval('reservation_items_id_seq', (SELECT max(id) FROM reservation_items), true);
SELECT setval('orders_id_seq', (SELECT max(id) FROM orders), true);
SELECT setval('order_items_id_seq', (SELECT max(id) FROM order_items), true);
SELECT setval('refunds_id_seq', (SELECT max(id) FROM refunds), true);
SELECT setval('payments_id_seq', (SELECT max(id) FROM payments), true);

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
