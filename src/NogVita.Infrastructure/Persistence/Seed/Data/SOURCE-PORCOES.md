# Porções (medidas caseiras) dos alimentos

Dois arquivos, lidos pelo `FoodPortionSeeder` (D-V2-33). As porções são só automáticas: ninguém cria uma porção à mão.

| Arquivo | Para | Linhas | Alimentos |
|---|---|---|---|
| `taco_porcoes.csv` | os alimentos da TACO | 1.318 porções | 378 dos 597 |
| `off_porcoes.csv` | os produtos do Open Food Facts de `off_brasil.csv` | 21.599 porções | 21.599 dos 22.728 |

Os alimentos sem porção continuam com a quantidade em gramas, que vale para todos.

## TACO: a tabela de medidas do IBGE

**Origem:** IBGE, *Pesquisa de Orçamentos Familiares 2008-2009: Tabela de Medidas Referidas para os Alimentos Consumidos no Brasil*. Rio de Janeiro: IBGE, 2011. ISBN 978-85-240-4196-9. Arquivo `liv50000.pdf` (545 páginas), em <https://biblioteca.ibge.gov.br/visualizacao/livros/liv50000.pdf>, baixado em 07/11/2026.

**Licença:** publicação oficial do IBGE, de uso público com a citação da fonte, como fazemos com a TACO.

**Como foi gerado** (uma vez, fora do repositório):

1. **Extração:** o texto das páginas da tabela (a partir da 35) foi lido com o PyMuPDF. Cada registro tem o código e o nome do alimento, o preparo, a medida citada, a medida mais citada, os gramas e a referência ("Arroz cozido - colher de sopa cheia"). Saíram 11.725 registros.
2. **Filtro das medidas:**
   - fora: grama, quilo, litro, mililitro, pacote, embalagem, lata, garrafa e caixa (não são medidas caseiras);
   - fora: as referências de outra medida (a "caneca" de arroz que o IBGE converte em "prato fundo raso"). Só fica o registro em que a primeira palavra da medida aparece na referência;
   - fora: as frações ("1/8 colher") e os percentuais;
   - o nome da porção é o texto da referência depois do " - " ("colher de sopa cheia"), e os gramas são a mediana dos registros com o mesmo nome.
3. **Ligação com a TACO**, pelo nome normalizado (sem acentos, sem pontuação, sem palavras como "de", "com", "tipo"):
   - o primeiro termo do nome da TACO ("arroz") precisa estar no nome do IBGE;
   - as variedades entre parênteses no IBGE ("Feijão (preto, mulatinho, roxo, rosinha, etc.)") contam como alternativas;
   - os termos que mudam o alimento (integral, desnatado, light, diet, zero, lactose, condensado, em pó, mascavo, as cores...) precisam estar nos dois nomes, ou em nenhum;
   - sinônimos: "pão de sal" é o pão francês, e "galinha" é frango;
   - a semelhança precisa ser de pelo menos 0,3 (termos em comum sobre o total);
   - o preparo precisa casar ("cozido" com "Cozido(a)"). Sem preparo no IBGE, a medida vale para o alimento pronto: um alimento **cru** só recebe essas medidas se for verdura ou fruta. Grãos, massas, carnes, peixes e ovos crus não recebem porção.

**Colunas:** `numero_alimento` (TACO), `porcao`, `gramas`, `codigo_ibge` e `alimento_ibge` (para conferir de onde veio).

## Open Food Facts: a porção da embalagem

**Origem:** Open Food Facts, export `en.openfoodfacts.org.products.csv.gz` (1.529.658.234 bytes), em <https://static.openfoodfacts.org/data/en.openfoodfacts.org.products.csv.gz>, baixado em 07/11/2026. O parquet do Hugging Face usado no `off_brasil.csv` limita as leituras remotas, então o export completo foi usado.

**Licença:** Open Database License (ODbL) 1.0, como o `off_brasil.csv` (veja o `SOURCE-OFF.md`).

**Como foi gerado (DuckDB 1.5):**

```sql
CREATE TABLE seed AS SELECT code FROM read_csv('off_brasil.csv', header = true, all_varchar = true);

CREATE TABLE serv AS
SELECT code, TRY_CAST(serving_quantity AS DECIMAL(9,2)) AS grams, trim(serving_size) AS label
FROM read_csv('en.openfoodfacts.org.products.csv.gz', delim = '\t', header = true, quote = '',
              all_varchar = true, ignore_errors = true, strict_mode = false)
WHERE code IN (SELECT code FROM seed);

COPY (
  SELECT code, grams AS serving_grams, left(regexp_replace(coalesce(label, ''), '\s+', ' ', 'g'), 60) AS serving_label
  FROM serv
  WHERE grams > 0 AND grams <= 2000
  QUALIFY row_number() OVER (PARTITION BY code ORDER BY grams) = 1
  ORDER BY code
) TO 'off_porcoes.csv' (HEADER, DELIMITER ',');
```

No seed, a porção se chama "porção da embalagem (rótulo)", por exemplo "porção da embalagem (30g)". Os produtos importados depois, pelo código de barras, ganham a porção na própria importação (`serving_quantity` da API do Open Food Facts).
