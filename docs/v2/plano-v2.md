# NogVita: o plano da V2

> **Status:** proposta para discussão. O E4 está concluído. **Atualizado em 03/11/2026** com a pesquisa com nutricionistas (seção 1.1) e com a conformidade legal (seção 7). **Atualizado em 04/11/2026** com a sequência por prioridade (seção 5) e a Cloudflare gratuita (D-V2-21).
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
| E4 | **Imagens** ✅ concluído | A foto do nutricionista e as fotos de evolução do paciente (opcionais) |
| E5 | **Agenda e teleconsulta** | Marcar a consulta e fazer a videochamada pelo NogVita |
| E6 | **Os essenciais que faltam** | O que os épicos acima pedem para fazerem sentido (o diário alimentar, as medidas corporais, as notas da consulta...) |

### A regra "tudo gratuito" e o preço dela

A V2 continua sem custo. Isso tem limites que **precisam estar na cabeça de quem desenvolve**:

| Serviço | O que usamos | O limite gratuito | O que isso muda no projeto |
|---|---|---|---|
| Render | A API | O serviço dorme após 15 min sem uso; a primeira requisição leva ~1 min | **Nada roda em segundo plano com confiança.** Um lembrete "às 9h" não dispara se a API estiver dormindo |
| Supabase | O banco + **as fotos (novo)** | Banco de 500 MB, 1 GB de arquivos, **pausa após 7 dias sem uso** | As fotos precisam ser comprimidas. O projeto não pode ficar uma semana parado |
| Brevo | Os e-mails | 300 e-mails por dia | Os lembretes de consulta contam nesse limite |
| **Cloudflare Realtime** | **A videochamada (plano A, D-V2-01)** | 1.000 GB de saída por mês, somando o SFU e o TURN; depois, US$ 0,05/GB | Uma consulta de 50 min em 720p gasta ~1,1 GB (estimativa): **~900 consultas por mês** |
| **Cloudflare Workers (Cron Triggers)** | **Os horários das notificações (E6.4)** | 5 crons por conta; 10 ms de CPU por execução | O cron só **chama** a API, que faz o trabalho. Cada chamada acorda a Render (veja o E6.4) |
| **Cloudflare Turnstile** | **Antirrobô no cadastro, no login e na recuperação de senha** | Gratuito, sem limite de verificações (até 20 widgets) | A API confere o token no servidor, com uma chave secreta guardada na Render |
| Cloudflare R2 | **Plano B das fotos**, se o 1 GB do Supabase encher | 10 GB por mês e saída gratuita | Só uma implementação nova do `IFileStorage` (D-V2-09): nenhum caso de uso muda |
| Daily.co | A videochamada (plano B) | ~10.000 minutos-participante por mês | ~100 consultas de 50 min com 2 pessoas por mês |

> **Sobre a Cloudflare:** um domínio próprio (para usar o DNS e o proxy da Cloudflare na frente da API) **não** é gratuito: um `.com.br` custa por ano. Sem ele, a API continua em `onrender.com` e o front na Vercel, e tudo acima funciona do mesmo jeito.

> Todos os números vêm de pesquisas feitas em outubro de 2026 (veja as Fontes). **Confira na página oficial de cada serviço antes de depender deles.**

### 1.1. O que a pesquisa com nutricionistas mudou (03/11/2026)

Base: a apresentação "NogVita para nutricionistas" e a conversa com a nutricionista Júlia Ávila.

| O que ela disse | O que muda no plano |
|---|---|
| **O paciente não deve ver carboidrato e proteína.** As kcal podem aparecer, mas os macros podem virar gatilho em quem tem histórico de anorexia ou obesidade, e levam o paciente a questionar a conduta | **Item novo e prioritário: E6.7.** Hoje o paciente vê os macros (o `shared/plan/meal-card.html` é o mesmo para os dois perfis, e o PDF sempre mostra Prot./Carb./Gord.). **É o achado mais importante da conversa** |
| As notificações são essenciais (consulta, horário das refeições, água) | O E6.4 sobe de prioridade, com a solução que funciona com a Render dormindo (veja o E6.4) |
| Anamnese que salva o questionário de cada um, e que o paciente responde antes, no atendimento online | O E6.6 sobe de prioridade e vira um questionário-modelo que o nutricionista monta. A Res. CFN 594/2017 já exige a anamnese na primeira consulta |
| Pode entrar qualquer dado, desde que algumas coisas fiquem visíveis só para ela | As notas do E6.3 são **privadas do nutricionista** por padrão |
| Na clínica, a recepção cuida da agenda e do pagamento, e os estagiários das medidas | **E6.8:** um papel "recepção" sem acesso a dado clínico. O estagiário só com a autorização prévia do paciente (Res. CFN 760/2023, art. 9º, XI). Fica para depois das clínicas |
| Lembrete para o paciente refazer as fotos | **E6.10:** um lembrete periódico, que complementa o E4 |
| Quem entrega o plano na hora precisa de agilidade | **E6.9:** modelos de plano |
| Assistente virtual (o feedback da semana, o pagamento, a remarcação), mas o WhatsApp dela continua com os pacientes | Só para tarefas administrativas. Pelo novo Código de Ética (2026), a IA **não substitui** o nutricionista no contato com o paciente, e o uso de IA precisa ser informado. O feedback da semana encaixa no check-in do E6.1 |
| Usa o Dietbox (plano pago), inclusive a parte Academy | É a referência de concorrente. Os cursos ficam fora do escopo |

**⚠️ O tamanho da amostra:** a própria Júlia ainda não atende todos os dias. Antes de reordenar os sprints de vez, ouvir **mais 3 ou 4 nutricionistas**, de preferência alguém que atende **só online** e alguém que trabalha em **clínica**.

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
- [ ] Se a tarefa cria, muda ou compartilha um dado pessoal: o `docs/legal/registro-de-operacoes.md` e a política de privacidade atualizados (e, se mudar um consentimento, a versão do termo sobe)

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

**⚠️ O Código de Ética (Res. CFN 599/2018, reforçada pelo código de 2026) proíbe divulgar resultados de pacientes**, como fotos de "antes e depois", dados corporais e exames. As fotos de evolução continuam privadas, **sem nenhuma forma de compartilhar** e sem aparecer em perfil público. Nenhuma feature futura pode mudar isso.

**Falta (da pesquisa):** o lembrete periódico para refazer as fotos (E6.10).

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
- **Plano A: o vídeo dentro do NogVita, com o Cloudflare Realtime** (o SFU e o TURN da Cloudflare). O limite gratuito (~900 consultas por mês) é bem maior que o do Daily. O preço disso: o Realtime é de **baixo nível**, e o front monta a chamada com WebRTC. **No Sprint 0, uma prova de conceito de 1 dia** (uma chamada entre dois navegadores) e a conferência de que a conta não pede cartão.
- **Plano B: o Daily.co**, se a prova de conceito do Realtime ficar pesada demais para a dupla. O SDK pronto é mais simples, mas o limite é de ~100 consultas por mês, e também precisa conferir o cartão.
- **Plano C: a agenda no NogVita + um link externo** (uma sala do Jitsi gerada pela API, ou o link do Google Meet que o nutricionista cadastrou), aberto em outra aba.
- **A arquitetura que deixa os três planos trocáveis:** uma interface `IVideoRoomProvider` na camada Application, com uma implementação por plano. Os casos de uso não sabem qual está em uso.
- **Segurança:** a chave do provedor fica só no servidor (variável da Render). As salas são **privadas**, e a API emite as credenciais de entrada só para o paciente e o nutricionista daquele agendamento, válidas só perto do horário.
- **LGPD:** a chamada passa por um serviço no exterior, então a transferência internacional entra na política de privacidade. **Nada de gravação.**

**⚠️ As regras do CFN (Resolução nº 760/2023, que regulamenta a telenutrição e revogou a 666/2020):**
- O nutricionista precisa estar no cadastro **e-Nutricionista** antes do primeiro atendimento (art. 5º). O admin registra isso junto com a conferência do CRN.
- O paciente precisa **aceitar o Termo de Consentimento (TCLE)** da telenutrição antes da primeira teleconsulta, e o comprovante fica guardado (art. 9º, VI). O NogVita guarda o aceite com a data e a versão, no mesmo padrão do termo dos dados de saúde.
- **Nada de gravação:** o conteúdo com dados pessoais "não poderá ser gravado nem compartilhado [...] por ambas as partes" (art. 9º, VII). Avisar isso no TCLE.
- A tela da consulta mostra o nome, a profissão e o CRN do nutricionista (art. 9º, V).
- O atendimento presencial continua sendo o padrão de referência (art. 6º).
- Promoções e sorteios como propaganda seguem vedados, inclusive na telenutrição (art. 12, §1º). Importa se o NogVita tiver cobrança.
- O Daily entra na política de privacidade como operador no exterior, e no aceite da transferência internacional (a versão do termo sobe).
- Confira o texto vigente no site do CFN antes de implementar.

**Divida em partes:** (a) a disponibilidade e o agendamento, (b) cancelar, remarcar e o `.ics`, (c) o vídeo.

---

### E6. Os essenciais que faltam

Os épicos acima ficam incompletos sem estes itens. A coluna **Prioridade** vem da pesquisa com nutricionistas (seção 1.1); confirmem com as próximas entrevistas.

| # | Item | Prioridade | Por que é essencial |
|---|---|---|---|
| E6.7 | **O que o paciente vê** (P): o nutricionista escolhe, por plano ou por paciente, se o paciente vê as kcal e se vê os macros | **1** (o S1, sozinho) | Os macros podem ser gatilho para quem tem histórico de transtorno alimentar. Vale para a tela do paciente, o PDF que ele baixa ou recebe por e-mail, o "balanço do dia" do E3 e o diário do E6.1. O PDF do nutricionista continua completo. **⚠️ Esconder na API, e não só no front:** se a resposta trouxer os macros e o front só esconder, qualquer um vê pelo F12. Os campos do paciente passam a poder ser `null`, com um `macrosVisible`; é uma mudança de tipo, registrada no CHANGELOG |
| E6.6 | **Anamnese** configurável: um questionário-modelo que o nutricionista monta e o paciente responde **antes da consulta** | **2** | É o primeiro passo de qualquer consulta (a Res. CFN 594/2017 a exige na primeira), alimenta as substituições (E2: "não sugerir lactose") e agiliza o atendimento online |
| E6.3 | **Notas da consulta**, privadas do nutricionista | **3** | Uma consulta (E5) sem registro não serve. ⚠️ Veja abaixo a guarda de 20 anos |
| E6.4 | **Notificações**: consulta, horário das refeições, água, nova solicitação | **4** | O e-mail é limitado (300/dia) e se perde. **Como funciona com a Render dormindo:** a consulta usa o `.ics` com alarme (E5); as refeições e a água usam **Web Push**, disparado por um **Cron Trigger da Cloudflare** que chama um endpoint da API (o que também acorda a API). ⚠️ O endpoint exige um **segredo no cabeçalho** (sem isso, quem achar a URL dispara notificações para todos). ⚠️ O cron só roda **das 6h às 22h**: acordar a Render 24 h por dia gasta ~730 das 750 horas gratuitas do mês. ⚠️ No **iPhone**, o Web Push só funciona com o NogVita **instalado na tela de início** (PWA); conferir a regra atual antes de implementar |
| E6.1 | **Diário alimentar / check-in de adesão** ("segui o almoço? sim, parcial, não"), com o feedback da semana | **5** | Sem saber o que foi comido, não existe o "balanço do dia" do E3. **Respeita o E6.7** |
| E6.2 | **Medidas corporais** (cintura, quadril, % de gordura) | — | O peso sozinho engana. As medidas e as fotos (E4) contam a evolução de verdade |
| E6.5 | **Excluir e exportar a conta (LGPD)** | — | Com as fotos e as consultas, o volume de dado sensível cresce. ⚠️ Não pode apagar as notas da consulta (abaixo) |
| E6.9 | **Modelos de plano** | — | Quem entrega o plano na hora precisa de agilidade |
| E6.10 | **Lembrete para refazer as fotos** de evolução | — | Pedido da pesquisa; usa a mesma infraestrutura do E6.4 |
| E6.8 | **Papel "recepção"** (agenda e pagamento, sem dado clínico) | Depois das clínicas | O estagiário só entra com a autorização prévia do paciente (Res. CFN 760/2023, art. 9º, XI) |

**⚠️ As notas da consulta (E6.3) e a exclusão da conta (E6.5) entram em conflito. Decidir no desenho:**
- A Res. CFN 594/2017, art. 3º, VI, "b", manda guardar o prontuário eletrônico de forma permanente, "podendo ser eliminado 20 anos após o último registro". A Lei 13.787/2018, art. 6º, fixa o mesmo mínimo de 20 anos. A LGPD permite guardar por obrigação legal (art. 16, I).
- **Excluir a conta não pode apagar as notas:** elas ficam guardadas, bloqueadas e fora do uso normal.
- **O nutricionista continua lendo as próprias notas depois que o vínculo termina.** Isso é uma exceção ao D-V2-06 (e à mesma regra do peso), só para as notas.
- A Res. 594 pede conteúdo mínimo (art. 4º: anamnese, antropometria, diagnóstico e prescrição com data e horário) e "no mínimo dois níveis de garantia de segurança" na identificação (art. 8º). Isso é pesado para um projeto gratuito: **até lá, chamar as notas de "anotações de apoio", e não de prontuário.**

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

**A sequência por prioridade (revisada em 04/11/2026).** Cada linha só começa quando a anterior estiver em produção.

| # | Sprint | Entrega | Tamanho | Por que nesta posição |
|---|---|---|---|---|
| ✅ | — | **E4. Imagens** e o consentimento dos dados de saúde + o código verificador do PDF (D-V2-18, D-V2-19) | — | Feito |
| 1 | **Já** | **O front da conformidade legal**: os dois aceites no cadastro, o aceite dos pacientes antigos, a página `/verificar-documento/{code}` e o aviso da política nova | P | A API já exige isso, e o staging tem dados de saúde. Sem o front, os pacientes antigos ficam sem o aceite |
| 2 | **S0** | **O processo da dupla e as contas**: a proteção da `main` (sem `push --force`), o PR obrigatório, o Yuri no projeto; a conta da Cloudflare (sem cartão?) e a prova de conceito de 1 dia do Realtime (decide o vídeo do E5); confirmar a região do Supabase; baixar a tabela do IBGE (E1); **marcar as entrevistas com mais 3 ou 4 nutricionistas** | P | Sem isso, dois devs na mesma `main` repetem o problema do histórico reescrito |
| 3 | **S1** | **E6.7. O que o paciente vê** (os macros escondidos **na API**) + **o Turnstile** no cadastro, no login e na recuperação de senha | P + P | O E6.7 é o achado mais importante da pesquisa, e é segurança do paciente. O Turnstile é uma proteção rápida e gratuita: cabe na coluna do segundo dev |
| 4 | **S2** | **E1. Porções** | P–M | Pequeno, isolado e o pedido original da V2 |
| 5 | **S3** | **E6.6. Anamnese** configurável | M | A prioridade 2 da pesquisa, e exigida pelo CFN na primeira consulta. Alimenta o E2 ("não sugerir lactose") |
| 6 | **S4** | **E2. Substituições** | M | Depende do E1 (as alternativas usam as porções) e aproveita a anamnese |
| 7 | **S5** | **E6.4. Notificações** (Web Push + o Cron Trigger da Cloudflare) + **E6.10**, o lembrete das fotos | M | A prioridade 4 da pesquisa. Vem antes da agenda porque o E5 usa a mesma estrutura de notificação |
| 8 | **S6–S7** | **E5. Agenda e teleconsulta** (o vídeo pelo plano escolhido no S0) | G | O maior épico, para quando a dupla já estiver afinada |
| 9 | **S8** | **E6.3. Notas da consulta**, privadas, e **E6.5. Excluir e exportar a conta**, desenhados **juntos** (a guarda de 20 anos) | M | As notas ficam ligadas à consulta do E5, e as duas regras entram em conflito: desenhar separado obriga a refazer |
| 10 | **S9** | **E6.1. Diário / check-in de adesão** | M | A base do "balanço do dia" do E3. Respeita o E6.7 |
| 11 | **S10** | **E3. Atividade física e água** | M–G | O balanço do dia só faz sentido com o diário (E6.1) |
| 12 | depois | **E6.2** (medidas), **E6.9** (modelos de plano), **E6.8** (recepção, só com clínicas), o **R2** se o 1 GB das fotos encher | — | Sem pressão da pesquisa; a ordem fica para as próximas entrevistas |

**Quem faz o quê (decidido em 04/11/2026):**

| Gustavo | Yuri | Os dois juntos | A definir |
|---|---|---|---|
| E4. Imagens ✅ · E2. Substituições · E6.6. Anamnese · E6.3 + E6.5. Notas e exclusão da conta · E6.1. Diário | E1. Porções · E6.4. Notificações (+ E6.10) · E3. Atividade e água | E5. Agenda e teleconsulta | E6.7 + Turnstile (S1) · E6.2 · E6.9 · E6.8 |

Com cada um no seu épico, **as duas trilhas andam em paralelo**, mas três regras continuam valendo:
- **Uma migration por vez** (seção 2): antes de gerar uma, avise o outro.
- **O E2 do Gustavo depende do E1 do Yuri**: as alternativas usam as porções. Enquanto o E1 não estiver na `main`, o Gustavo faz a anamnese (E6.6).
- **O E3 do Yuri depende do E6.1 do Gustavo**: o balanço do dia precisa do diário. Enquanto o E6.1 não estiver na `main`, o Yuri faz as notificações (E6.4).

**Por que essa ordem:** a pesquisa com a nutricionista mudou o que vale mais. O que protege o paciente (os aceites legais, os macros escondidos) vem primeiro. Depois vêm os épicos pequenos que treinam a dupla (as porções, a anamnese). Os grandes (a agenda, o diário, a atividade) ficam para o fim. **Depois das entrevistas do S0, revisem as linhas de 5 em diante.**

**O front Angular** acompanha cada épico. Combinem quem faz as telas de cada sprint.

---

## 6. As decisões

### Já tomadas

| ID | Decisão |
|---|---|
| D-V2-01 | O vídeo **dentro do app**: plano A o **Cloudflare Realtime**, plano B o Daily, plano C **a agenda + um link externo** (Jitsi ou Meet). Os três atrás da interface `IVideoRoomProvider`; a escolha sai da prova de conceito do S0 (revisada em 04/11/2026) |
| D-V2-21 | **A Cloudflare gratuita** cobre as peças que a Render dormindo não cobre: o vídeo (Realtime), os horários das notificações (Cron Triggers) e o antirrobô (Turnstile). O R2 fica como plano B das fotos. Sem domínio próprio, por enquanto (não é gratuito) |
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
| D-V2-18 | O cadastro de paciente exige **dois aceites separados**, os dados de saúde (LGPD, art. 11, I) e a transferência internacional (art. 33, VIII), com a versão e a data guardadas. Os pacientes antigos aceitam pelo `POST /patients/me/health-data-consent` |
| D-V2-19 | Cada PDF de plano leva o CRN, a data e a hora da emissão e um **código verificador**, conferível em `GET /plan-documents/{code}`, sem nenhum dado do paciente (Res. CFN 760/2023, art. 10). A assinatura avançada fica com o nutricionista (o assinador do gov.br) |
| D-V2-20 | As fotos de evolução nunca são compartilháveis nem públicas (Código de Ética do CFN) |

### Em aberto (decidir no começo de cada épico)

| Épico | Decisão | Recomendação |
|---|---|---|
| E1 | A versão da tabela do IBGE | A 2017-2018 |
| E1 | O nutricionista cria porções? | Sim, só para os planos dele |
| E2 | O máximo de alternativas por item | 3 |
| E3 | Quantas modalidades na lista | ~30 comuns, curadas do Compêndio |
| E4 | A biblioteca de imagem | Conferir a licença; SkiaSharp (MIT) é a primeira candidata |
| E5 | A antecedência mínima para cancelar | 24 h, configurável pelo nutricionista |
| E6 | Quais itens entram, e em que ordem | A ordem da tabela do E6 (da pesquisa), confirmada com mais 3 ou 4 nutricionistas |
| E6.7 | O padrão da visibilidade | Mostrar as kcal e esconder os macros, com o nutricionista podendo mudar por paciente |
| E6.3 | Quem é o controlador das notas | Pela Res. CFN 594, art. 3º, V, o nutricionista guarda o próprio prontuário, o que o aproxima do papel de controlador (LGPD, art. 5º, VI) e coloca o NogVita como operador (art. 39). **Crítico se entrarem clínicas.** Ouvir alguém de direito digital ou de saúde |

---

## 7. A conformidade legal (revisada em 03/11/2026)

**Feito:**
- O consentimento dos dados de saúde e o da transferência internacional, separados, com a versão e a data (D-V2-18).
- A política de privacidade e os termos de uso atualizados (as fotos, o Storage, a hipótese certa do art. 33 e o aviso de mudança), e os PDFs deles refeitos (no front, `npm run legal:pdf`).
- O PDF do plano com o CRN, a data e a hora e o código verificador (D-V2-19).
- O [registro das operações](../legal/registro-de-operacoes.md) (art. 37) e o [relatório de impacto](../legal/relatorio-de-impacto.md) (art. 38).

**Falta:**
- **O front** pedir os dois aceites no cadastro, e pedir o aceite aos pacientes antigos (`GET /patients/me/health-data-consent` → `accepted: false`); a página `/verificar-documento/{code}`; e o aviso, com destaque, da mudança da política.
- Confirmar a região do Supabase (se for São Paulo, a transferência internacional diminui).
- No E5: o e-Nutricionista, o TCLE, sem gravação, o Daily na política.
- No E6.3 e no E6.5: a guarda das notas por 20 anos.

---

## 8. Fontes (pesquisadas em outubro e novembro de 2026; confira antes de usar)

- **A pesquisa:** a apresentação "NogVita para nutricionistas" e a conversa com a nutricionista Júlia Ávila (03/11/2026)
- Cloudflare (pesquisado em 04/11/2026): [Realtime, preço do SFU](https://developers.cloudflare.com/realtime/sfu/pricing), [Realtime, visão geral](https://developers.cloudflare.com/realtime/), [limites dos Workers](https://developers.cloudflare.com/workers/about/limits), [o limite de crons do plano gratuito (terceiros)](https://github.com/knowtific/helppuff/wiki/Cloudflare-Free-Plan), [planos do Turnstile](https://developers.cloudflare.com/turnstile/plans/)
- LGPD, Lei 13.709/2018: [normas.leg.br](https://normas.leg.br/?urn=urn:lex:br:federal:lei:2018-08-14;13709) · Lei 13.787/2018 (prontuário eletrônico): [normas.leg.br](https://normas.leg.br/?urn=urn:lex:br:federal:lei:2018-12-27;13787) · Lei 14.063/2020 (assinaturas eletrônicas): [normas.leg.br](https://normas.leg.br/?urn=urn:lex:br:federal:lei:2020-09-23;14063). Conferidas no acervo [ctresb/leis](https://github.com/ctresb/leis), que tem caráter informativo
- CFN: [Res. 594/2017 (prontuário)](https://cfn.org.br/wp-content/uploads/resolucoes/Res_594_2017.htm), [Res. 599/2018 (Código de Ética anterior)](https://cfn.org.br/wp-content/uploads/resolucoes/Res_599_2018.html), [a atualização do Código de Ética (abril de 2026)](https://cfn.org.br/cfn-atualiza-codigo-de-etica-e-reforca-protecao-ao-paciente-e-a-pratica-profissional-na-nutricao/), [a proibição do "antes e depois"](https://cfn.org.br/novo-codigo-de-etica-dos-nutricionistas-proibe-divulgacao-de-fotos-antes-e-depois-de-pacientes/), [Exame: o novo código e a IA](https://exame.com/brasil/o-que-o-novo-codigo-de-nutricionistas-define-sobre-uso-de-ia-e-redes-sociais/)

- CFN, Resolução nº 760/2023 (telenutrição): [cfn.org.br](https://cfn.org.br/cfn-publica-resolucao-que-regulamenta-a-telenutricao/), [Machado Meyer](https://www.machadomeyer.com.br/pt/inteligencia-juridica/publicacoes-ij/life-sciences-e-saude/cfn-atualiza-regras-de-telenutricao), [LegisMap](https://legismap.com.br/leis-e-normas/resolucao-cfn-n-760-de-22-10-2023)
- IBGE, Tabela de Medidas Referidas (POF): [biblioteca do IBGE](https://biblioteca.ibge.gov.br/visualizacao/livros/liv50002.pdf), [Revista de Nutrição (a revisão 2017-2018)](https://scielo.br/j/rn/a/g9C8cp87zgmdMXYtc4rMKQC/?lang=en)
- Compêndio de Atividades Físicas para Adultos, 2024: [pacompendium.com](https://pacompendium.com/wp-content/uploads/2024/01/2024-adult-compendium_1_2024.pdf), [PubMed](https://pubmed.ncbi.nlm.nih.gov/38242596/)
- Daily.co (o plano gratuito): [blog do Daily](https://www.daily.co/blog/announcing-our-new-pricing/), [CostBench](https://costbench.com/software/video-conferencing/daily-co/free-plan)
- Jitsi / JaaS: [JaaS](https://jitsi.org/jaas/), [FAQ da 8x8](https://developer.8x8.com/jaas/docs/faq), [o limite do meet.jit.si embutido](https://jitsi.org/blog/jitsi-moodle-with-a-dash-of-jaas/)
- Supabase (o plano gratuito): [guia de preços 2026](https://www.jetadmin.io/blog/supabase-pricing-2026-guide-to-plans-limits-and-real-world-costs/)
- Cloudflare R2 (a alternativa avaliada para as fotos): [cloudflare.com](https://cloudflare.com/en-ca/products/r2)
