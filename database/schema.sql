-- Minimal table design for a NEW PostgreSQL database.
-- No triggers, stored functions, exclusion constraints or generated columns.
-- Roles, transitions, calculations and concurrency checks belong to .NET.
BEGIN;
CREATE SCHEMA IF NOT EXISTS aura;
SET LOCAL search_path = aura;

CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    name text NOT NULL,
    email text NOT NULL UNIQUE,
    role text NOT NULL,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE branches (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code text NOT NULL UNIQUE,
    name text NOT NULL,
    address text NOT NULL,
    is_active boolean NOT NULL DEFAULT true
);

-- A user may operate more than one branch; role remains on users.
CREATE TABLE user_branches (
    user_id uuid NOT NULL REFERENCES users (id),
    branch_id uuid NOT NULL REFERENCES branches (id),
    PRIMARY KEY (user_id, branch_id)
);

CREATE TABLE customers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL,
    -- Backend stores phone in one normalized format before insert/update.
    phone text NOT NULL UNIQUE,
    instagram_handle text,
    address text
);

CREATE TABLE products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id uuid NOT NULL REFERENCES branches (id),
    code text NOT NULL,
    name text NOT NULL,
    category text NOT NULL,
    color text,
    material text,
    description text,
    image_paths text[] NOT NULL DEFAULT '{}',
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (branch_id, code)
);

CREATE TABLE product_variants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products (id),
    size text NOT NULL,
    measurements text,
    replacement_value numeric(14,0) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (product_id, size)
);

CREATE TABLE inventory_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    variant_id uuid NOT NULL REFERENCES product_variants (id),
    branch_id uuid NOT NULL REFERENCES branches (id),
    asset_code text NOT NULL UNIQUE,
    status text NOT NULL DEFAULT 'USABLE'
);

-- A package exists at a branch exactly when it has a price row.
-- This keeps each branch's 1D/2D/3D prices independent.
CREATE TABLE branch_rental_prices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id uuid NOT NULL REFERENCES branches (id),
    variant_id uuid NOT NULL REFERENCES product_variants (id),
    package_code text NOT NULL,
    price numeric(14,0) NOT NULL,
    UNIQUE (branch_id, variant_id, package_code)
);

-- Exactly one settings row, id = 1, maintained by the backend.
CREATE TABLE settings (
    id integer PRIMARY KEY DEFAULT 1,
    slot_deposit_amount numeric(14,0) NOT NULL DEFAULT 100000,
    extra_day_rate numeric(6,4) NOT NULL DEFAULT 0.1000
);

INSERT INTO settings DEFAULT VALUES;

CREATE TABLE reservations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_no text NOT NULL UNIQUE,
    customer_id uuid NOT NULL REFERENCES customers (id),
    branch_id uuid NOT NULL REFERENCES branches (id),
    status text NOT NULL DEFAULT 'ACTIVE',
    rental_start_at timestamptz NOT NULL,
    rental_end_at timestamptz NOT NULL,
    deposit_plan text NOT NULL,
    deposit_required numeric(14,0) NOT NULL,
    otp_hash text,
    form_token_hash text UNIQUE,
    otp_expires_at timestamptz,
    otp_used_at timestamptz,
    cancellation_reason text,
    created_by uuid NOT NULL REFERENCES users (id),
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE reservation_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id uuid NOT NULL REFERENCES reservations (id),
    inventory_item_id uuid NOT NULL REFERENCES inventory_items (id),
    package_code text NOT NULL,
    replacement_value numeric(14,0) NOT NULL,
    rental_price numeric(14,0) NOT NULL,
    one_day_price numeric(14,0) NOT NULL,
    extra_day_rate numeric(6,4) NOT NULL,
    UNIQUE (reservation_id, inventory_item_id)
);

CREATE TABLE orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_no text NOT NULL UNIQUE,
    -- Kept on the order so customer purchase/rental history is direct to query.
    -- .NET copies this from reservations.customer_id when converting a reservation.
    customer_id uuid NOT NULL REFERENCES customers (id),
    reservation_id uuid NOT NULL UNIQUE REFERENCES reservations (id),
    -- Preserves the operating branch even if an inventory item is transferred later.
    branch_id uuid NOT NULL REFERENCES branches (id),
    status text NOT NULL,
    customer_name text NOT NULL,
    customer_phone text NOT NULL,
    delivery_address text NOT NULL,
    delivery_status text NOT NULL DEFAULT 'NOT_STARTED',
    return_delivery_status text NOT NULL DEFAULT 'NOT_STARTED',
    delivery_tracking_code text,
    return_tracking_code text,
    identity_verified_by uuid REFERENCES users (id),
    identity_verified_at timestamptz,
    delivered_at timestamptz,
    returned_at timestamptz,
    settled_by uuid REFERENCES users (id),
    settled_at timestamptz,
    cancellation_reason text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id uuid NOT NULL REFERENCES orders (id),
    inventory_item_id uuid NOT NULL REFERENCES inventory_items (id),
    product_name text NOT NULL,
    size text NOT NULL,
    asset_code text NOT NULL,
    package_code text NOT NULL,
    replacement_value numeric(14,0) NOT NULL,
    rental_price numeric(14,0) NOT NULL,
    one_day_price numeric(14,0) NOT NULL,
    extra_day_rate numeric(6,4) NOT NULL,
    actual_rental_fee numeric(14,0),
    condition text,
    processing_fee numeric(14,0) NOT NULL DEFAULT 0,
    damage_note text,
    damage_photo_paths text[] NOT NULL DEFAULT '{}',
    UNIQUE (order_id, inventory_item_id)
);

-- One row per refund calculation/approval version, not per bank transfer.
CREATE TABLE refunds (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id uuid NOT NULL REFERENCES orders (id),
    version integer NOT NULL DEFAULT 1,
    status text NOT NULL DEFAULT 'DRAFT',
    deposit_amount numeric(14,0) NOT NULL,
    rental_fee numeric(14,0) NOT NULL,
    processing_fee numeric(14,0) NOT NULL,
    refund_amount numeric(14,0) NOT NULL,
    items_snapshot jsonb NOT NULL DEFAULT '[]',
    adjustment_reason text,
    created_by uuid NOT NULL REFERENCES users (id),
    submitted_by uuid REFERENCES users (id),
    approved_by uuid REFERENCES users (id),
    approved_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (order_id, version)
);

-- Reuse the same reservation ledger before and after order creation.
CREATE TABLE payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id uuid NOT NULL REFERENCES reservations (id),
    refund_id uuid REFERENCES refunds (id),
    type text NOT NULL,
    amount numeric(14,0) NOT NULL,
    method text NOT NULL,
    status text NOT NULL DEFAULT 'RECORDED',
    transaction_ref text,
    proof_path text,
    recorded_by uuid NOT NULL REFERENCES users (id),
    confirmed_by uuid REFERENCES users (id),
    paid_at timestamptz,
    note text
);

-- Technical request replay protection. Response payload is encrypted by
-- ASP.NET Data Protection and expires after a short retention window.
CREATE TABLE idempotency_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key uuid NOT NULL,
    scope text NOT NULL,
    operation text NOT NULL,
    request_hash text NOT NULL,
    response_status integer,
    response_payload text,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    UNIQUE (scope, operation, idempotency_key)
);

-- Customer history: list orders newest first, then load its items/refund/payment detail.
CREATE INDEX orders_customer_history_idx ON orders (customer_id, created_at DESC);
CREATE INDEX payments_reservation_idx ON payments (reservation_id);
CREATE INDEX inventory_items_branch_status_idx ON inventory_items (branch_id, status);
CREATE INDEX reservations_branch_schedule_idx ON reservations (branch_id, status, rental_start_at);
CREATE INDEX orders_branch_created_idx ON orders (branch_id, created_at DESC);
CREATE INDEX idempotency_records_expiry_idx ON idempotency_records (expires_at);
COMMIT;
