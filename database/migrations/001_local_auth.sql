-- Converts an existing Aura Rental database from external auth_subject mapping
-- to local username/email + password authentication.
BEGIN;
SET LOCAL search_path = aura;

ALTER TABLE users ADD COLUMN IF NOT EXISTS username text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash text;

UPDATE users
SET username = CASE email
    WHEN 'manager@aurarental.local' THEN 'manager'
    WHEN 'hanoi.staff@aurarental.local' THEN 'hanoi'
    WHEN 'saigon.staff@aurarental.local' THEN 'saigon'
    ELSE 'user-' || id::text
END
WHERE username IS NULL;

-- Non-seed accounts cannot authenticate until a manager assigns a real hash.
UPDATE users SET password_hash = 'RESET_REQUIRED' WHERE password_hash IS NULL;

ALTER TABLE users ALTER COLUMN username SET NOT NULL;
ALTER TABLE users ALTER COLUMN password_hash SET NOT NULL;
ALTER TABLE users DROP COLUMN IF EXISTS auth_subject;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'aura.users'::regclass AND conname = 'users_username_key'
    ) THEN
        ALTER TABLE users ADD CONSTRAINT users_username_key UNIQUE (username);
    END IF;
END $$;

COMMIT;
