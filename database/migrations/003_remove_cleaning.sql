-- Cleaning buffers are no longer part of the rental model.
BEGIN;
SET LOCAL search_path = aura;

ALTER TABLE inventory_items
    DROP COLUMN cleaning_hours,
    DROP COLUMN cleaning_until;

ALTER TABLE settings
    DROP COLUMN default_cleaning_hours;

COMMIT;
