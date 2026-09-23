-- Deposit deadlines and the resulting overdue-reservation workflow are no longer part of the model.
BEGIN;
SET LOCAL search_path = aura;

-- Keep historical holds active when the former computed status is removed.
UPDATE reservations SET status = 'ACTIVE' WHERE status = 'OVERDUE';

ALTER TABLE reservations DROP COLUMN IF EXISTS deposit_deadline_at;

COMMIT;
