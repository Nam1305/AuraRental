BEGIN;
SET LOCAL search_path = aura;

-- A hold can be created as soon as the item and payment are confirmed. The customer
-- is linked later, when they submit the public rental form.
ALTER TABLE reservations ALTER COLUMN customer_id DROP NOT NULL;

COMMIT;
