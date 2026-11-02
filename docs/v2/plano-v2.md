# NogVita: o plano da V2

> **Status:** proposta para discussão. Nada aqui está implementado.
> **Para quem:** Gustavo e Yuri, a dupla que vai desenvolver a V2. Lê-se em uns 15 minutos.
> **Regra de ouro:** uma coisa de cada vez. Um épico só começa quando o anterior estiver em produção.

---

## 1. A visão da V2

A V1 entregou o essencial: o paciente encontra um nutricionista, recebe um plano alimentar calculado em gramas, exporta em PDF e acompanha o peso. Tudo isso na API (.NET 10, PostgreSQL, JWT) e no front Angular publicado.

A V2 deixa o NogVita **mais próximo da rotina real** de um acompanhamento nutricional:

| # | Épico | Em uma frase |
|---|---|---|
| E1 | **Porções** | "2 ovos" e "1 pão francês", e não 100 g e 50 g |
| E2 | **Substituições no plano** | "100 g de frango **ou** 100 g de patinho" |
| E3 | **Atividade física e água** | "30 min de musculação + 30 min de esteira" vira um gasto estimado, e a água ganha uma meta e um registro diário |
| E4 | **Imagens** | A foto do nutricionista e as fotos de evolução do paciente (opcionais) |
| E5 | **Agenda e teleconsulta** | Marcar a consulta e fazer a videochamada pelo NogVita |
| E6 | **Os essenciais que faltam** | O que os épicos acima pedem para fazerem sentido (o diário alimentar, as medidas corporais, as notas da consulta...) |

### A regra "tudo gratuito" e o preço dela

A V2 continua sem custo. Isso tem limites que **precisam estar na cabeça de quem desenvolve**:

| Serviço | O que usamos | O limite gratuito | O que isso muda no projeto |
|---|---|---|---|
| Render | A API | O serviço dorme após 15 min sem uso; a primeira requisição leva ~1 min | **Nada roda em segundo plano com confiança.** Um lembrete "às 9h" não dispara se a API estiver dormindo |
| Supabase | O banco + **as fotos (novo)** | Banco de 500 MB, 1 GB de arquivos, **pausa após 7 dias sem uso** | As fotos precisam ser comprimidas. O projeto não pode ficar uma semana parado |
| Brevo | Os e-mails | 300 e-mails por dia | Os lembretes de consulta contam nesse limite |
| Daily.co | **A videochamada (novo)** | ~10.000 minutos-participante por mês | ~100 consultas de 50 min com 2 pessoas por mês |

> Todos os números vêm de pesquisas feitas em outubro de 2026 (veja as Fontes). **Confira na página oficial de cada serviço antes de depender deles.**

---

## 2. Como trabalhar em dupla

Na V1, o desenvolvimento foi de uma pessoa só, direto na `main`. Com dois, isso quebra rápido. **O Sprint 0 é combinar isto:**

1. **Uma branch por tarefa** (`feat/porcoes-dominio`), e a `main` só recebe código por **Pull Request**.
2. **A integração contínua precisa passar** antes do merge (ela já existe: o estilo, o build e os 637 testes).
3. **Revisão cruzada:** quem não escreveu revisa. A revisão usa a classificação CRITICAL, HIGH, MEDIUM, LOW e SUGGESTION.
4. **⚠️ Uma migration por PR, e só uma pessoa mexendo em migration por vez.** O EF Core guarda o estado do banco num arquivo (`NogVitaDbContextModelSnapshot.cs`). Duas branches que criam migrations em paralelo geram um conflito nesse arquivo, que **não se resolve com merge**: a segunda pessoa apaga a migration dela, atualiza a branch e gera a migration de novo.
5. **A migration vai para o Supabase sozinha:** o staging aplica as migrations ao subir (`Database:MigrateOnStartup`). Se ela falhar, o deploy na Render falha e a versão anterior continua no ar: confira o log do deploy.
6. **Commits:** o tipo do Conventional Commits em inglês e minúsculo, a descrição em português (`feat: porções dos alimentos`).

**Definition of Done de uma tarefa:**
- [ ] Os testes de domínio, de caso de uso e (quando toca o banco) de integração foram escritos
- [ ] Os endpoints aparecem documentados no OpenAPI (um teste já confere isso)
- [ ] `docs/API.md` e `CHANGELOG.md` atualizados
- [ ] O deploy do staging subiu e o `/health/ready` responde `200` (a migration foi aplicada, se houver)
- [ ] Testado no staging, **só com dados fictícios** (são dados de saúde e a LGPD se aplica)

**A regra de compatibilidade do contrato** (`docs/dev/versionamento.md`): **acrescentar** um campo na resposta não quebra o front. **Renomear, remover ou mudar o tipo** de um campo quebra, e exige o `/api/v2`. Todos os épicos abaixo foram pensados para serem **só acréscimos**.

---

## 3. Os épicos

Tamanhos: **P** (até uma semana para um dev), **M** (1 a 2 semanas), **G** (mais de 2 semanas, divida em partes).

### E1. Porções (P–M) ⭐ o primeiro

**O problema:** o nutricionista pensa em "2 ovos" e o paciente come "1 pão francês". O NogVita só fala em gramas.

**A proposta:**
- Um alimento pode ter **porções**: `FoodPortion` (o nome, como "unidade", "fatia" ou "colher de sopa", e os gramas de uma porção).
- No plano, o nutricionista escolhe **"2 × unidade"** em vez de digitar 100 g.
- **O cálculo não muda:** o `MealFood` continua guardando o `QuantityInGrams` (2 × 50 g = 100 g), e o `NutritionCalculator` continua igual. A porção é só uma forma de **entrar e de mostrar** a quantidade.
- **Snapshot da porção:** o `MealFood` guarda o nome da porção, os gramas de uma porção e a quantidade, como já faz com o `FoodSnapshot`. Se a tabela de porções mudar, os planos antigos continuam iguais.
- **Exibição:** "2 unidades (100 g)". O arroz, que não tem porção escolhida, continua em "120 g".
- **Bebidas:** a mesma ideia resolve o "1 copo de 200 ml" (uma porção em ml, com a densidade para converter em g).

**De onde vêm os gramas:** a **Tabela de Medidas Referidas para os Alimentos Consumidos no Brasil**, do IBGE (POF), que relaciona as medidas caseiras aos gramas. É um dado público; confira a licença e cite a fonte, como já fazemos com a TACO. O nutricionista também pode criar uma porção.

**Onde encaixa:** `Domain/NutritionPlans/MealFood.cs`, `FoodSnapshot.cs`, `Meal.AddFood`; o seed como o `TacoFoodSeeder`.

**Decisões em aberto:**
| Decisão | Recomendação |
|---|---|
| A versão da tabela do IBGE | A 2017-2018 (revisada), se o arquivo estiver disponível |
| O nutricionista pode criar uma porção? | Sim, valendo só para os planos dele (evita uma porção errada para todos) |

---

### E2. Substituições no plano (M)

**O problema:** um plano real dá opções ("frango **ou** patinho"). Hoje, cada refeição é uma lista fixa.

**A proposta:**
- Um item da refeição pode ter **alternativas**. Exemplo: o item principal é "100 g de peito de frango" e as alternativas são "100 g de patinho moído" e "2 ovos".
- **Os totais da refeição e do plano usam o item principal.** Cada alternativa mostra **a diferença** em relação a ele ("+12 kcal, −3 g de proteína"), para o nutricionista montar substituições equivalentes.
- O PDF e a tela do paciente mostram "ou".

**O modelo:** o `MealFood` ganha um `SubstituteForId` opcional (aponta para o item principal). Os limites atuais continuam valendo (20 itens por refeição, contando as alternativas; no máximo 3 alternativas por item).

**O contrato:** cada item ganha um campo `substitutes: []`. É um acréscimo, sem quebra.

**Depende de:** E1 (as alternativas também usam as porções).

---

### E3. Atividade física e água (M–G)

**O problema:** o nutricionista precisa saber o quanto o paciente gasta, e a hidratação faz parte do acompanhamento.

**A proposta:**

**Atividade física:**
- O paciente registra: a modalidade, os minutos e a intensidade ("musculação, 30 min, moderada" + "esteira, caminhada, 30 min").
- **O gasto estimado = MET × peso (kg) × horas.** O MET vem do **Compêndio de Atividades Físicas para Adultos (2024)**. Por exemplo, a musculação com vários exercícios (8 a 15 repetições) vale 3,5 METs. O peso é o último registro de peso (o `WeightRecord`, da V1).
- Uma lista **curada** de umas 30 modalidades comuns, e não as 1.114 do Compêndio.

**Água:**
- O registro diário com botões rápidos (+250 ml, +500 ml).
- **A meta de água é definida pelo nutricionista.** O sistema mostra uma **sugestão**, sempre identificada como estimativa (D-V2-02).

**O gasto do dia:**
- A taxa metabólica basal estimada (a fórmula de Mifflin-St Jeor, usando o peso, a altura, a idade e o sexo do perfil) + as atividades do dia.
- **Tudo é uma estimativa** e aparece assim na tela. **O nutricionista confirma ou ajusta** as metas (D-V2-02).
- O "balanço do dia" (o que comeu menos o que gastou) **só existe com o E6.1** (o diário alimentar). Sem ele, a comparação é com o plano, e não com o que foi comido.

**Onde encaixa:** a `PatientRoutine` (a frequência de treino da V1), o `WeightRecord`, o `CareAccessService` (o nutricionista vê os registros do paciente que acompanha).

---

### E4. Imagens (M)

**A proposta:**
- **A foto do nutricionista:** pública, aparece na busca de nutricionistas.
- **A foto de perfil do paciente:** opcional e privada. Sem foto, o front mostra o avatar padrão. Quem é paciente e nutricionista tem duas fotos separadas.
- **As fotos de evolução do paciente:** opcionais, privadas, até 3 por data (frente, lado, costas), enviadas só pelo paciente e vistas só por ele e pelo nutricionista com vínculo ativo.

**Onde guardar:** o **Supabase Storage** (D-V2-03): o bucket público `nutritionist-photos` e o privado `patient-photos`, organizado por paciente (`{patientId}/profile/` e `{patientId}/progress/`).

**⚠️ As fotos de evolução são dado sensível de saúde (LGPD).** O mínimo obrigatório:
1. **Bucket privado.** A API gera uma **URL assinada de curta duração** (alguns minutos) só para quem pode ver, conferido pelo `CareAccessService`.
2. **Consentimento explícito** antes do primeiro envio, e a política de privacidade atualizada.
3. **Apagar de verdade:** apagar a foto remove o arquivo, e não só o registro no banco.
4. **Validar o arquivo pelos bytes** (JPEG, PNG, WebP), e não pela extensão; ter um limite de tamanho.
5. **Recodificar a imagem** para remover os metadados EXIF. **Uma foto de celular pode trazer a localização GPS da casa do paciente.** Recodificar também comprime, e isso economiza o 1 GB gratuito.
6. A chave de serviço do Supabase fica **só na API** (variável da Render), nunca no front.

**A biblioteca de imagem:** escolha conferindo a licença (o SkiaSharp é MIT; o ImageSharp tem uma licença dividida, com condições).

---

### E5. Agenda e teleconsulta (G), o maior épico

**A agenda:**
- O nutricionista define a **disponibilidade semanal** (por exemplo, "terças, das 14h às 18h, consultas de 50 min").
- O paciente **só agenda com o nutricionista que o acompanha** (o vínculo ativo da V1).
- **Sem agendamento duplo:** um índice único parcial (o nutricionista + o horário, só para os agendamentos não cancelados). É o mesmo padrão dos índices da V1, e duas pessoas clicando ao mesmo tempo recebem um `409`.
- Cancelar e remarcar, com uma antecedência mínima configurável.
- **O lembrete sem servidor acordado:** o e-mail de confirmação leva um **arquivo `.ics`** (um convite de calendário) **com um alarme**. O calendário do celular lembra a pessoa, e a API não precisa estar acordada. Isso contorna o sono da Render.
- O fuso horário: a V1 usa o horário de Brasília fixo, o que basta enquanto os usuários forem do Brasil. O fuso por paciente continua no backlog.

**A videochamada (D-V2-01):**
- **Plano A: o vídeo dentro do NogVita, com o Daily.co**, **se o cadastro não pedir cartão de crédito**. As fontes divergem sobre isso, então **criar a conta e confirmar é uma tarefa do Sprint 0**.
- **Plano B, se pedir o cartão:** a agenda no NogVita + **um link externo** (uma sala do Jitsi gerada pela API, ou o link do Google Meet que o nutricionista cadastrou), aberto em outra aba.
- **A arquitetura que deixa os dois planos trocáveis:** uma interface `IVideoRoomProvider` na camada Application, com duas implementações (a do Daily e a do "link externo"). Os casos de uso não sabem qual está em uso.
- **Segurança:** a chave da API do Daily fica só no servidor. As salas são **privadas**, e a API emite um **token de entrada** só para o paciente e o nutricionista daquele agendamento, válido só perto do horário.
- **LGPD:** a chamada passa por um serviço no exterior, então a transferência internacional entra na política de privacidade. **Nada de gravação.**

**⚠️ As regras do CFN (Resolução nº 760/2023, que regulamenta a telenutrição e revogou a 666/2020):**
- O nutricionista precisa estar no cadastro **e-Nutricionista** para fazer teleconsulta.
- O paciente precisa **aceitar o Termo de Consentimento** da telenutrição antes da primeira teleconsulta. O NogVita guarda esse aceite, com a data e a versão.
- Confira o texto vigente no site do CFN antes de implementar.

**Divida em partes:** (a) a disponibilidade e o agendamento, (b) cancelar, remarcar e o `.ics`, (c) o vídeo.

---

### E6. Os essenciais que faltam (propostas para discutir)

Os épicos acima ficam incompletos sem estes itens. Priorizem juntos:

| # | Item | Por que é essencial |
|---|---|---|
| E6.1 | **Diário alimentar / check-in de adesão** ("segui o almoço? sim, parcial, não") | Sem saber o que foi comido, não existe o "balanço do dia" do E3, e o nutricionista não vê a adesão ao plano |
| E6.2 | **Medidas corporais** (cintura, quadril, % de gordura) | O peso sozinho engana. As medidas e as fotos (E4) contam a evolução de verdade |
| E6.3 | **Notas da consulta** (o prontuário ligado ao agendamento) | Uma consulta (E5) sem registro não serve. O CFN exige a guarda do prontuário |
| E6.4 | **Notificações no app** ("nova solicitação", "consulta amanhã") | O e-mail é limitado (300/dia) e se perde. Já estava no backlog |
| E6.5 | **Excluir e exportar a conta (LGPD)** | Com as fotos e as consultas, o volume de dado sensível cresce. Já estava no backlog |
| E6.6 | **Anamnese** (um questionário inicial: alergias, restrições, histórico) | É o primeiro passo de qualquer consulta, e alimenta as substituições (E2): "não sugerir lactose" |

---

## 4. O backlog da V2 que já existia

Itens anotados durante a V1, para encaixar quando fizer sentido:

- **Segurança:** a autenticação em dois fatores (obrigatória para admin e nutricionista); invalidar os tokens de acesso já emitidos após a troca de senha; um registro de auditoria em tabela (hoje só nos logs); a checagem automática de pacotes vulneráveis (Dependabot).
- **E-mails:** uma fila de envio com novas tentativas (a outbox). **Fica mais importante com o E5.**
- **Produto:** as avaliações de nutricionistas; a troca de e-mail; a ordem dos alimentos dentro da refeição; atualizar o snapshot de um item do plano.
- **Alimentos:** os alimentos personalizados do nutricionista; a TBCA (pedir permissão à USP); a busca do Open Food Facts por nome; a leitura do código de barras pela câmera; as gorduras saturadas, os açúcares e os micronutrientes.
- **Técnico:** a concorrência otimista nos planos (dois nutricionistas editando ao mesmo tempo); o fuso horário por paciente.

---

## 5. Os sprints sugeridos

Sprints de 2 semanas. **Um épico por vez**, e dentro dele as duas pessoas pegam partes diferentes. **Gustavo e Yuri decidem juntos quem fica com a coluna "Dev A" e quem fica com a "Dev B"** em cada sprint. Vale alternar, para os dois passarem pelo domínio e pelos endpoints.

| Sprint | Entrega | Dev A | Dev B |
|---|---|---|---|
| S0 | O processo da dupla | As regras da seção 2, a proteção da `main` no GitHub | **Criar a conta do Daily e confirmar se pede cartão** (decide o E5); baixar e avaliar a tabela do IBGE (E1) |
| S1 | **E1. Porções** | O domínio, a migration e o seed do IBGE | Os casos de uso, os endpoints e o PDF |
| S2 | **E2. Substituições** | O domínio e a migration | Os totais, a diferença, as respostas e o PDF |
| S3 | **E4. Imagens** | O armazenamento e a recodificação (EXIF) | Os endpoints, a URL assinada, o consentimento |
| S4 | **E3. Atividade e água** | O diário de atividades e os METs | A água, a TMB e as sugestões de meta |
| S5–S6 | **E5. Agenda e teleconsulta** | A disponibilidade, o agendamento e o `.ics` | O `IVideoRoomProvider`, o termo do CFN e o vídeo |
| S7 | **E6** (o que a dupla priorizar) | — | — |

**Por que essa ordem:** o E1 é pequeno, isolado e de alto valor (um bom primeiro épico para a dupla treinar o processo). O E2 depende do E1. O E4 vem antes do E3 porque é independente e ensina o armazenamento de arquivos. O E5 é o maior, e fica para quando a dupla já estiver afinada.

**O front Angular** acompanha cada épico. Combinem quem faz as telas de cada sprint.

---

## 6. As decisões

### Já tomadas

| ID | Decisão |
|---|---|
| D-V2-01 | O vídeo **dentro do app (Daily)**, se o cadastro não pedir cartão; senão, **a agenda + um link externo** (Jitsi ou Meet). Os dois atrás da interface `IVideoRoomProvider` |
| D-V2-02 | As metas (a água, o gasto): **o sistema sugere, marcado como estimativa; o nutricionista confirma** |
| D-V2-03 | As fotos no **Supabase Storage**: um bucket público (o perfil do nutricionista) e um privado com URL assinada (as fotos do paciente) |
| D-V2-04 | Só o paciente envia e apaga as fotos de evolução; o nutricionista só vê |
| D-V2-05 | Até 3 fotos de evolução por data, uma por ângulo (frente, lado, costas); reenviar o mesmo ângulo substitui |
| D-V2-06 | Sem vínculo ativo, o nutricionista perde o acesso às fotos; elas ficam com o paciente |
| D-V2-07 | Toda imagem é recodificada em WebP (qualidade 80; 1600 px na evolução, 512 px no perfil), sem metadados; upload de até 5 MB e 40 megapixels |
| D-V2-08 | O processamento usa o SkiaSharp (MIT) com o pacote nativo NoDependencies |
| D-V2-09 | O Supabase Storage é acessado por um HttpClient próprio, sem SDK |
| D-V2-10 | O caminho do arquivo é gerado pela API; o nome enviado pelo usuário nunca é usado |
| D-V2-11 | A foto de perfil do paciente é opcional e privada: só a veem ele, os nutricionistas com pedido pendente dele e quem o acompanha |
| D-V2-12 | Um bucket privado `patient-photos`, organizado por paciente, para apagar a conta inteira de uma vez (LGPD) |

### Em aberto (decidir no começo de cada épico)

| Épico | Decisão | Recomendação |
|---|---|---|
| E1 | A versão da tabela do IBGE | A 2017-2018 |
| E1 | O nutricionista cria porções? | Sim, só para os planos dele |
| E2 | O máximo de alternativas por item | 3 |
| E3 | Quantas modalidades na lista | ~30 comuns, curadas do Compêndio |
| E4 | A biblioteca de imagem | Conferir a licença; SkiaSharp (MIT) é a primeira candidata |
| E5 | A antecedência mínima para cancelar | 24 h, configurável pelo nutricionista |
| E6 | Quais itens entram, e em que ordem | O E6.1 (diário/adesão) e o E6.3 (notas da consulta) primeiro |

---

## 7. Fontes (pesquisadas em outubro de 2026; confira antes de usar)

- CFN, Resolução nº 760/2023 (telenutrição): [cfn.org.br](https://cfn.org.br/cfn-publica-resolucao-que-regulamenta-a-telenutricao/), [Machado Meyer](https://www.machadomeyer.com.br/pt/inteligencia-juridica/publicacoes-ij/life-sciences-e-saude/cfn-atualiza-regras-de-telenutricao), [LegisMap](https://legismap.com.br/leis-e-normas/resolucao-cfn-n-760-de-22-10-2023)
- IBGE, Tabela de Medidas Referidas (POF): [biblioteca do IBGE](https://biblioteca.ibge.gov.br/visualizacao/livros/liv50002.pdf), [Revista de Nutrição (a revisão 2017-2018)](https://scielo.br/j/rn/a/g9C8cp87zgmdMXYtc4rMKQC/?lang=en)
- Compêndio de Atividades Físicas para Adultos, 2024: [pacompendium.com](https://pacompendium.com/wp-content/uploads/2024/01/2024-adult-compendium_1_2024.pdf), [PubMed](https://pubmed.ncbi.nlm.nih.gov/38242596/)
- Daily.co (o plano gratuito): [blog do Daily](https://www.daily.co/blog/announcing-our-new-pricing/), [CostBench](https://costbench.com/software/video-conferencing/daily-co/free-plan)
- Jitsi / JaaS: [JaaS](https://jitsi.org/jaas/), [FAQ da 8x8](https://developer.8x8.com/jaas/docs/faq), [o limite do meet.jit.si embutido](https://jitsi.org/blog/jitsi-moodle-with-a-dash-of-jaas/)
- Supabase (o plano gratuito): [guia de preços 2026](https://www.jetadmin.io/blog/supabase-pricing-2026-guide-to-plans-limits-and-real-world-costs/)
- Cloudflare R2 (a alternativa avaliada para as fotos): [cloudflare.com](https://cloudflare.com/en-ca/products/r2)
