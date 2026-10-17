# Produtos brasileiros do Open Food Facts

**Arquivo:** `off_brasil.csv`, 22.728 produtos, valores por 100 g (ou 100 ml).

**Origem:** Open Food Facts (<https://world.openfoodfacts.org>), arquivo `food.parquet` do dataset `openfoodfacts/product-database` no Hugging Face (7.953.611.557 bytes), baixado em 17/10/2026.

**Licença:** este arquivo é uma base derivada do Open Food Facts e é disponibilizado sob a **Open Database License (ODbL) 1.0**: <https://opendatacommons.org/licenses/odbl/1-0/>. Os conteúdos individuais estão sob a Database Contents License: <https://opendatacommons.org/licenses/dbcl/1-0/>.

**Filtro aplicado (DuckDB 1.5.6):** apenas produtos com `en:brazil` em `countries_tags`, código de barras válido (8, 12, 13 ou 14 dígitos), nome, energia, proteína, carboidrato e gordura informados, dentro dos limites físicos do domínio, com a soma dos macronutrientes até 100 g, e com a energia coerente com os fatores de Atwater (diferença de até 20% ou 20 kcal). Um produto por código de barras.

## Os números do filtro

| Etapa | Produtos |
|---|---|
| Produtos do Brasil no `food.parquet` | 37.060 |
| Com os 4 nutrientes principais (energia, proteína, carboidrato e gordura) | 25.960 |
| Excluídos por energia incoerente com os macronutrientes (D150) | 2.845 |
| **No arquivo, depois de todos os filtros** | **22.728** |

Dos 22.728, 8.619 não informam a fibra, 7.355 não informam o sódio e 3.295 não têm marca. Eles entram assim mesmo: o total de um nutriente que falta aparece como incompleto (`isComplete: false`), e nunca como zero.

## Como reproduzir

Com o `food.parquet` na mesma pasta, dentro do DuckDB (`duckdb off-brasil.duckdb`):

```sql
-- 1. Os produtos do Brasil, só com as colunas usadas.
CREATE OR REPLACE TABLE br AS
SELECT code, product_name, brands, nutriments
FROM 'food.parquet'
WHERE list_contains(countries_tags, 'en:brazil');

-- 2. As listas "achatadas": o nome em português (ou o principal), a primeira marca e os nutrientes por 100 g.
CREATE OR REPLACE VIEW br_flat AS
SELECT
    code,
    trim(coalesce(
        list_filter(product_name, lambda p: p.lang = 'pt')[1]."text",
        list_filter(product_name, lambda p: p.lang = 'main')[1]."text"
    )) AS name,
    nullif(trim(split_part(brands, ',', 1)), '') AS brand,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'energy-kcal')[1]."100g" AS DECIMAL(9,4)) AS energy_kcal,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'proteins')[1]."100g" AS DECIMAL(9,4)) AS protein,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'carbohydrates')[1]."100g" AS DECIMAL(9,4)) AS carbohydrate,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'fat')[1]."100g" AS DECIMAL(9,4)) AS fat,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'fiber')[1]."100g" AS DECIMAL(9,4)) AS fiber,
    TRY_CAST(list_filter(nutriments, lambda n: n."name" = 'sodium')[1]."100g" * 1000 AS DECIMAL(9,4)) AS sodium_mg
FROM br;

-- 3. Materializada: no DuckDB 1.5.6, o QUALIFY row_number() direto sobre a visão acima
--    (com as lambdas e os TRY_CAST) dá um erro interno ("Failed to cast expression to type").
CREATE OR REPLACE TABLE br_flat_t AS SELECT * FROM br_flat;

-- 4. Todas as regras do domínio, para nenhuma linha ser recusada no meio da importação.
CREATE OR REPLACE VIEW br_import AS
SELECT *
FROM br_flat_t
WHERE regexp_full_match(code, '(\d{8}|\d{12}|\d{13}|\d{14})')         -- Barcode.IsValid
  AND name IS NOT NULL AND name <> '' AND length(name) <= 200           -- Food.NameMaxLength
  AND (brand IS NULL OR length(brand) <= 100)                           -- Food.BrandMaxLength
  AND energy_kcal BETWEEN 0 AND 900                                     -- D118
  AND protein BETWEEN 0 AND 100
  AND carbohydrate BETWEEN 0 AND 100
  AND fat BETWEEN 0 AND 100
  AND protein + carbohydrate + fat <= 100                               -- D149
  AND (fiber IS NULL OR fiber BETWEEN 0 AND 100)
  AND (sodium_mg IS NULL OR sodium_mg BETWEEN 0 AND 40000)              -- NutrientsPer100g.MaxSodiumMg
  AND abs(energy_kcal - (4 * protein + 4 * carbohydrate + 9 * fat))    -- D150 (Atwater)
      <= greatest(20, 0.2 * greatest(energy_kcal, 4 * protein + 4 * carbohydrate + 9 * fat))
QUALIFY row_number() OVER (PARTITION BY code ORDER BY name) = 1;      -- ux_food_barcode

-- 5. O arquivo, sempre na mesma ordem (um diff mostra só os produtos que mudaram).
COPY (
    SELECT code, name, brand, energy_kcal, protein, carbohydrate, fat, fiber, sodium_mg
    FROM br_import
    ORDER BY code
) TO 'off_brasil.csv' (HEADER, DELIMITER ',');
```

**A coerência da energia (D150):** a energia declarada pode se afastar da calculada pelos fatores de Atwater (4 kcal/g de proteína e de carboidrato, 9 kcal/g de gordura) em até 20% do maior dos dois valores, ou em até 20 kcal, o que for maior. As 20 kcal protegem os produtos de pouca energia: um refrigerante zero com 1 kcal declarada e 0 calculada está certo. A regra vale só para esta importação em massa, e não para o domínio: o álcool (7 kcal/g), os polióis e as fibras têm outros fatores, e na importação de um produto só o nutricionista confere a prévia.

## Limitações conhecidas

- **Dados da comunidade:** os filtros garantem que os valores são possíveis e coerentes entre si, mas não que estão corretos. Um cadastro errado que respeite todas as regras passa (por exemplo, o código `00000022`, "Farandole de madeleine", com a marca "Coca-Cola").
- **Os nomes variam na escrita:** alguns vêm em caixa alta e abreviados ("BISC COOKIE NEGRESCO 60G NESTLE"), e os produtos sem nome em português usam o nome principal, às vezes em outro idioma.
- **Um produto por embalagem:** a mesma bebida em embalagens diferentes tem códigos diferentes, e por isso aparece mais de uma vez. Isso é esperado.

## Atualizar a base

Baixe um `food.parquet` novo e rode o SQL acima. Como o arquivo sai ordenado pelo código, o `git diff` mostra só os produtos que entraram, saíram ou mudaram. A importação (D152) só insere os códigos que ainda não existem: um produto que já está no banco **não é atualizado** por um arquivo novo.
