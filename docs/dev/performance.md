# Performance

A revisão de performance da Fase 27: o que foi medido, o que mudou e o que ficou como estava, sempre com o número que justificou a decisão. A regra foi medir antes de mudar, e medir de novo depois.

## Como foi medido

- **Banco:** o Postgres 17 de desenvolvimento, com o volume real: 23.325 alimentos (597 da TACO e 22.728 do Open Food Facts), 7,5 MB na `tb_food`.
- **Consultas:** o SQL exato que o EF gera (pelo log `Microsoft.EntityFrameworkCore.Database.Command`), analisado com `EXPLAIN (ANALYZE, BUFFERS)`. O experimento do índice rodou numa transação desfeita com `ROLLBACK`, antes da migration.
- **Requisições:** o tempo total pelo `curl -w "%{time_total}"`, seis vezes seguidas. A primeira chamada aparece separada, porque inclui a compilação JIT e o aquecimento.
- **Como na Render:** a mesma imagem do `Dockerfile`, num container com `--cpus=0.1 --memory=512m` (os limites do plano gratuito), ligado ao mesmo banco.
- **O plano grande:** montado pela API com 10 refeições de 20 alimentos (200 itens, o máximo permitido).

## O que mudou

### D191: a busca de alimentos com um índice de trigramas

| Medição | Antes | Depois |
|---|---|---|
| Plano da consulta | `Seq Scan` (lê as 23.325 linhas) | `Bitmap Index Scan on ix_food_name_trgm` |
| Contagem de "arroz" no banco | 23,0 ms | 1,5 ms |
| Página de "arroz" no banco | 24,4 ms | 1,8 ms |
| "biscoito chocolate recheado" no banco | — | 0,4 ms |
| Busca pela API, a 0,1 CPU | 0,26 a 0,30 s | 0,21 a 0,24 s |

Cada busca faz duas consultas (a contagem e a página). Localmente, elas eram cerca de 80% do tempo da requisição. O índice ocupa 1,8 MB e é criado em cerca de 0,2 s.

- **Um índice comum não serve:** ele só ajuda o "começa com". A busca é por "contém" (`ILIKE '%arroz%'`), e quem cobre isso é o índice GIN de trigramas (`pg_trgm`).
- **O Postgres só indexa funções imutáveis:** o `unaccent` é `STABLE`. A migration cria o `public.f_unaccent`, a mesma função declarada `IMMUTABLE`, chamando o `unaccent` pelo schema e pelo dicionário explícitos. O schema é descoberto na hora: no Supabase, as extensões costumam ficar no `extensions`.
- **O índice só é usado com a mesma expressão:** a busca chama o `NogVitaDbFunctions.Unaccent`, mapeado no EF para o `public.f_unaccent`. Com o `EF.Functions.Unaccent` de antes, o índice existiria, mas o Postgres continuaria lendo a tabela inteira. Um teste unitário confere o SQL gerado, e um teste de integração confere que o índice serve para a consulta.

### A relevância da busca

Antes, a ordem era só alfabética. Em "arroz", o primeiro alimento da TACO aparecia na **posição 75 de 502**, e as primeiras páginas eram de produtos industrializados (inclusive dez "Arroz" sem detalhe nenhum). Agora a ordem é:

1. o nome que começa com a primeira palavra buscada;
2. a TACO, que é a referência para montar um plano;
3. o nome mais curto, que costuma ser o mais genérico;
4. a ordem alfabética, e o Id para a paginação ser estável.

Em "arroz", os sete arrozes da TACO vêm primeiro. O custo extra da ordenação é desprezível: ela roda só sobre as linhas que o índice já filtrou.

### D192: a importação do Open Food Facts sem repetir trabalho

| A 0,1 CPU, da API atendendo até a importação terminar | Tempo |
|---|---|
| Antes, a cada vez que a API acordava | 5,2 s |
| Agora, na primeira subida (faz a passada e grava a marca) | 6,7 s |
| Agora, nas subidas seguintes | **1,3 s** |

Antes, a cada subida, a importação buscava os 22.728 códigos no banco (4 ms) e relia o CSV inteiro só para descobrir que não havia nada novo. Com 0,1 de CPU, eram 5,2 s disputando o processador com a primeira requisição do front, justamente a que acordou a API.

Agora, ao terminar, ela grava na `tb_seed_run` o hash SHA-256 do CSV. O mesmo hash quer dizer "nada a fazer". O que sobra (1,3 s) é o próprio cálculo do hash dos 2 MB do arquivo com 0,1 de CPU.

## D193: o que ficou como estava

### O plano carregado inteiro para mudar um status

| Operação | Plano vazio | Plano com 200 itens |
|---|---|---|
| Definir ou remover a meta (local) | 17 a 24 ms | 22 a 25 ms |
| Ativar (local, uma vez) | — | 51 ms, em 6 consultas de 1 a 2 ms |
| Definir a meta (0,1 CPU) | — | 0,24 a 0,55 s |
| Ver o detalhe (0,1 CPU, como base) | — | 0,25 a 0,36 s |

Carregar os 200 itens custa uns 5 ms a mais, e a 0,1 CPU a operação fica no mesmo patamar do detalhe do plano. Carregar só o cabeçalho do plano exigiria um caminho paralelo ao agregado, e a regra de mudar o status ficaria fora dele, por 5 ms que ninguém percebe. **Não mudar.** Vale reabrir se o limite de 10 refeições ou de 20 alimentos crescer muito.

### O PDF gerado durante a requisição

| PDF do plano de 200 itens (80 KB) | Primeira geração | Depois |
|---|---|---|
| Local | 754 ms | 92 a 124 ms |
| A 0,1 CPU | 4,25 s | 0,86 a 1,45 s |

A primeira geração carrega as fontes do QuestPDF. O PDF é pedido pela pessoa, com um clique, e esperar de 1 a 1,5 s por um arquivo é aceitável. Gerar em segundo plano exigiria uma fila, um lugar para guardar o arquivo e uma tela de "pronto, pode baixar". **Não mudar.** Vale reabrir se a produção mostrar gerações acima de 3 s, ou se o envio por e-mail passar a ser em massa.

### A contagem separada da página

Cada busca faz duas consultas: a contagem (para `totalItems` e `totalPages`) e a página. Com o índice, cada uma leva cerca de 1,5 ms. Juntar as duas numa consulta só (com `count(*) OVER ()`) economizaria 1,5 ms e mudaria o mecanismo de paginação que todas as listas da API usam. **Não mudar.**

## Para medir de novo

```bash
# O SQL do EF, no log da API
Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information

# Os tempos de uma rota, seis vezes
for i in 1 2 3 4 5 6; do curl -s -o /dev/null -w "%{time_total} " -H "Authorization: Bearer $TOKEN" "$API/api/v1/foods?search=arroz"; done

# Como no plano gratuito da Render
docker run --cpus=0.1 --memory=512m ... nogvita-api
```
