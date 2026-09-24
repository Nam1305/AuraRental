BEGIN;
SET LOCAL search_path = aura;

ALTER TABLE customers ADD COLUMN IF NOT EXISTS tiktok_handle text;

COMMIT;
