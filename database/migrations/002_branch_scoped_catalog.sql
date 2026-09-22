-- Convert the legacy shared catalog into independent catalogs per branch.
-- Products/variants that have inventory or prices at more than one branch are cloned;
-- inventory and rental prices are moved to the corresponding cloned variant.
BEGIN;
SET LOCAL search_path = aura;

ALTER TABLE products ADD COLUMN branch_id uuid REFERENCES branches (id);
ALTER TABLE products DROP CONSTRAINT products_code_key;

CREATE TEMP TABLE product_branch_map ON COMMIT DROP AS
WITH product_branches AS (
    SELECT DISTINCT variant.product_id, item.branch_id
    FROM product_variants variant
    JOIN inventory_items item ON item.variant_id = variant.id
    UNION
    SELECT DISTINCT variant.product_id, price.branch_id
    FROM product_variants variant
    JOIN branch_rental_prices price ON price.variant_id = variant.id
    UNION
    SELECT product.id, (
        SELECT branch.id FROM branches branch WHERE branch.is_active ORDER BY branch.id LIMIT 1
    )
    FROM products product
    WHERE NOT EXISTS (
        SELECT 1
        FROM product_variants variant
        LEFT JOIN inventory_items item ON item.variant_id = variant.id
        LEFT JOIN branch_rental_prices price ON price.variant_id = variant.id
        WHERE variant.product_id = product.id
          AND (item.id IS NOT NULL OR price.id IS NOT NULL)
    )
), ranked AS (
    SELECT product_id, branch_id,
           row_number() OVER (PARTITION BY product_id ORDER BY branch_id) AS branch_rank
    FROM product_branches
)
SELECT product_id AS source_product_id,
       branch_id,
       branch_rank = 1 AS keeps_source_product,
       CASE WHEN branch_rank = 1 THEN product_id ELSE gen_random_uuid() END AS target_product_id
FROM ranked;

UPDATE products product
SET branch_id = map.branch_id
FROM product_branch_map map
WHERE map.source_product_id = product.id
  AND map.keeps_source_product;

INSERT INTO products (id, branch_id, code, name, category, color, material, description, image_paths, is_active)
SELECT map.target_product_id, map.branch_id, product.code, product.name, product.category,
       product.color, product.material, product.description, product.image_paths, product.is_active
FROM product_branch_map map
JOIN products product ON product.id = map.source_product_id
WHERE NOT map.keeps_source_product;

CREATE TEMP TABLE variant_branch_map ON COMMIT DROP AS
SELECT variant.id AS source_variant_id,
       map.branch_id,
       CASE WHEN map.keeps_source_product THEN variant.id ELSE gen_random_uuid() END AS target_variant_id,
       map.target_product_id,
       map.keeps_source_product
FROM product_variants variant
JOIN product_branch_map map ON map.source_product_id = variant.product_id;

INSERT INTO product_variants (id, product_id, size, measurements, replacement_value, is_active)
SELECT map.target_variant_id, map.target_product_id, variant.size, variant.measurements,
       variant.replacement_value, variant.is_active
FROM variant_branch_map map
JOIN product_variants variant ON variant.id = map.source_variant_id
WHERE NOT map.keeps_source_product;

UPDATE inventory_items item
SET variant_id = map.target_variant_id
FROM variant_branch_map map
WHERE item.variant_id = map.source_variant_id
  AND item.branch_id = map.branch_id
  AND NOT map.keeps_source_product;

UPDATE branch_rental_prices price
SET variant_id = map.target_variant_id
FROM variant_branch_map map
WHERE price.variant_id = map.source_variant_id
  AND price.branch_id = map.branch_id
  AND NOT map.keeps_source_product;

ALTER TABLE products ALTER COLUMN branch_id SET NOT NULL;
ALTER TABLE products ADD CONSTRAINT products_branch_code_key UNIQUE (branch_id, code);
CREATE INDEX products_branch_name_idx ON products (branch_id, name);

COMMIT;
