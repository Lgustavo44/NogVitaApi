# Dados da TACO

**Arquivo:** `taco_composicao.csv` (597 alimentos, valores por 100 g de parte comestível)

**Fonte original:** NEPA – UNICAMP. *Tabela Brasileira de Composição de Alimentos – TACO*. 4. ed. rev. e ampl. Campinas: NEPA-UNICAMP, 2011. Disponível em: <https://nepa.unicamp.br/categoria/taco/>

**Conversão para CSV:** repositório [brolesi/taco](https://github.com/brolesi/taco), commit `b800c910dfc9d2e8e6bab45dd42e5b8f5167b466`, baixado em 10/10/2026. Gerado por script (`scripts/process_taco.py`) a partir da planilha oficial (`data/raw/taco/Taco_4a_edicao_2011.xls`). Caminho no repositório: `data/processed/taco/taco_composicao.csv`.

- Hash Git do arquivo (blob): `7f263e4f42370dc37b3731cc5a144befb5ebf8e8`, idêntico ao do repositório nesse commit. Para conferir: `git hash-object taco_composicao.csv`.
- O arquivo não muda no `brolesi/taco` desde o commit `c057d5468c269c96cd80248fa5348f3d65c90a63` (28/08/2026).

**Conferência:** 10 alimentos de grupos diferentes conferidos contra a planilha oficial do NEPA (`Taco-4a-Edicao.xlsx`, aba `CMVCol taco3`) em 10/10/2026: 90 de 90 valores iguais.

- Alimentos: 3 (arroz tipo 1 cozido), 89 (batata-doce crua), 124 (fécula de mandioca), 174 (atemoia crua), 213 (suco de laranja-lima), 262 (manteiga sem sal), 324 (caldo de carne em tablete), 445 (toucinho frito), 493 (açúcar mascavo) e 561 (feijão carioca cozido).
- Nutrientes: energia (kcal), proteína, lipídeos, carboidrato, fibra, cálcio, ferro, sódio e vitamina C.
- A amostra incluiu os marcadores `Tr` (8 ocorrências), `NA` (3) e células vazias (3), todos convertidos como descrito abaixo.
- Atenção a quem repetir a conferência: na planilha, há uma coluna extra (N) entre o magnésio e o manganês. A partir dela, as letras das colunas da planilha ficam uma posição à frente da ordem do CSV (o sódio está na coluna R, e não na Q).

**Marcadores da TACO** (definições das notas de rodapé da planilha oficial):

- `Tr` (traço): quantidade presente, mas muito pequena para ser expressa. A TACO adota traço para valores arredondados que caem entre 0 e 0,5 (inteiros), entre 0 e 0,05 (uma casa decimal) ou entre 0 e 0,005 (duas casas decimais), e para valores abaixo dos limites de quantificação. No CSV: `1e-05`. Na importação: 0.
- `NA`: não aplicável (o nutriente não se aplica àquele alimento). No CSV: vazio. Na importação: `null`.
- `*`: as análises estão sendo reavaliadas (o valor não foi publicado). No CSV: vazio. Na importação: `null`.
- Célula vazia: análise não solicitada (o nutriente não foi medido). No CSV: vazio. Na importação: `null`.

`null` significa "sem informação", e não zero: um nutriente `null` não deve entrar como 0 em somas de um plano alimentar.

**Carboidratos negativos:** a TACO calcula o carboidrato por diferença, e 4 alimentos têm médias levemente negativas na planilha original: 288 (corimba crua, −0,027 g), 322 (tucunaré cru, −0,045 g), 337 (capa de contra-filé grelhada, −0,007 g) e 400 (fígado de frango cru, −0,023 g). Na importação, eles viram 0: o `TacoCsvReader.ParseNutrient` trata qualquer valor abaixo de `TraceThreshold` (0,0001) como traço, o que inclui os negativos. Isso é necessário porque o domínio (`NutrientsPer100g`) recusa valores negativos. Se a regra do traço mudar, esses 4 alimentos precisam continuar convertidos para 0, ou a importação da TACO falha.

**Citação obrigatória:** a reprodução dos dados da TACO exige a citação da fonte original (NEPA – UNICAMP), conforme a referência acima.
