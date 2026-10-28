# Dados de teste para o ambiente local

Script SQL que cria usuários, acompanhamentos e planos prontos no banco **local**, para testar a API sem passar por cadastro, confirmação de e-mail e convite.

> ⚠️ **Só para o banco local.** Nunca rode este script no staging nem em produção: todos os usuários têm a mesma senha, que está escrita neste arquivo.

## Antes de rodar

1. O banco local precisa estar com todas as migrations aplicadas:

   ```bash
   dotnet ef database update --project src/NogVita.Infrastructure --startup-project src/NogVita.Api
   ```

2. Os e-mails usam o domínio **`.test`**, reservado pela RFC 2606 para testes. Ele nunca existe na internet, então nenhum e-mail sai para uma pessoa de verdade, mesmo se algum fluxo tentar enviar. No ambiente local, os e-mails aparecem no Mailpit (`http://localhost:8025`).

## Como rodar

Com o PostgreSQL rodando no Docker (o contêiner `nogvita-postgres` do README), salve o bloco SQL abaixo num arquivo, por exemplo `seed-local.sql`, **em UTF-8**, e rode:

```powershell
docker cp seed-local.sql nogvita-postgres:/tmp/seed-local.sql
docker exec nogvita-postgres psql -U nogvita -d nogvita -v ON_ERROR_STOP=1 -f /tmp/seed-local.sql
```

> Prefira o `docker cp` a mandar o arquivo por pipe (`Get-Content ... | docker exec -i ...`). No Windows PowerShell, o pipe para programas externos pode converter o texto para ASCII, e os acentos chegam ao banco como `?` ("Caf? da manh?").

Também dá para colar o bloco SQL direto num cliente gráfico (DBeaver, pgAdmin ou a extensão de PostgreSQL do VS Code), conectado ao `localhost:5432`.

O script roda numa transação e pode ser executado mais de uma vez: o que já existe é ignorado (`ON CONFLICT DO NOTHING`).

## Os usuários

**Senha de todos:** `senha-local-nogvita-123`

| E-mail | Papel | Situação | Para testar |
|---|---|---|---|
| `admin.teste@nogvita.test` | Admin | Ativo | Painel do administrador, pré-cadastro de nutricionista, ativar e desativar contas e alimentos |
| `nutri.ana@nogvita.test` | Nutricionista | Ativa, CRN-3 10001 | Acompanha a Carla, tem uma solicitação pendente do Diego, planos e refeições |
| `nutri.bruno@nogvita.test` | Nutricionista | Ativo, CRN-8 20002 | Já acompanhou a Carla (encerrado). Sem pacientes hoje: rotas de paciente respondem `404` |
| `nutri.convite@nogvita.test` | Nutricionista | **Convite pendente** (sem senha) | Não consegue entrar. Teste o reenvio de convite pelo admin, ou o cancelamento do pré-cadastro (que apaga a conta: rode o seed de novo para recriá-la) |
| `paciente.carla@nogvita.test` | Paciente | Ativa, acompanhada pela Ana | Plano atual com 4 refeições, histórico com o plano do Bruno, rotina preenchida |
| `paciente.diego@nogvita.test` | Paciente | Ativo, sem acompanhamento | Solicitação pendente para a Ana, rotina de **turno da noite** (acorda 14h, dorme 6h) |
| `paciente.elisa@nogvita.test` | Paciente | **E-mail não confirmado** | Login com senha certa responde `403`. Teste o reenvio da confirmação |
| `paciente.fabio@nogvita.test` | Paciente | **Desativado pelo admin** | Login responde `401`, e a redefinição de senha não envia link |

O administrador criado pelo `AdminSeeder` (configurado nos User Secrets) continua existindo. O `admin.teste` é só um segundo admin, com senha conhecida.

## O que cada cenário já traz

- **Carla e Ana:** acompanhamento ativo. Plano **ativo** "Plano de hipertrofia" (início em 01/10/2026) com café da manhã, lanche, almoço e jantar, todos com horário sugerido, e um **rascunho** "Plano de manutenção" (início em 01/11/2026), que a Carla não vê.
- **Carla e Bruno:** acompanhamento **encerrado** e um plano **finalizado** dele (agosto a setembro de 2026). A Carla vê esse plano no histórico, e o Bruno não acessa mais.
- **Diego:** solicitação **pendente** para a Ana, com mensagem. A Ana pode aceitar ou recusar, e o Diego pode cancelar.

## O script

```sql
BEGIN;

-- Senha de todos: senha-local-nogvita-123 (hash gerado pelo IdentityPasswordHasher do projeto)

-- Usuários
INSERT INTO tb_user (id, name, email, cpf, password_hash, is_active, is_admin, email_confirmed_at_utc, created_at, updated_at) VALUES
  ('11111111-0000-4000-8000-000000000001', 'Admin Teste',     'admin.teste@nogvita.test',     '10020030088', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', true,  true,  now(), now(), NULL),
  ('11111111-0000-4000-8000-000000000002', 'Ana Nutricionista', 'nutri.ana@nogvita.test',     '10020030169', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', true,  false, now(), now(), NULL),
  ('11111111-0000-4000-8000-000000000003', 'Bruno Nutricionista', 'nutri.bruno@nogvita.test', '10020030240', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', true,  false, now(), now(), NULL),
  ('11111111-0000-4000-8000-000000000004', 'Nutri Convite Pendente', 'nutri.convite@nogvita.test', '10020030320', NULL,                                                                                     false, false, NULL,  now(), NULL),
  ('11111111-0000-4000-8000-000000000005', 'Carla Paciente',  'paciente.carla@nogvita.test',  '10020030401', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', true,  false, now(), now(), NULL),
  ('11111111-0000-4000-8000-000000000006', 'Diego Paciente',  'paciente.diego@nogvita.test',  '10020030592', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', true,  false, now(), now(), NULL),
  ('11111111-0000-4000-8000-000000000007', 'Elisa Paciente',  'paciente.elisa@nogvita.test',  '10020030673', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', false, false, NULL,  now(), NULL),
  ('11111111-0000-4000-8000-000000000008', 'Fabio Paciente',  'paciente.fabio@nogvita.test',  '10020030754', 'AQAAAAIAAYagAAAAELjZKgJP5xeWyqUqYPx+qWFiG317+22Q8F3IFyaxL5jC4xsH4du1N7L9C0j0SY91qg==', false, false, now(), now(), NULL)
ON CONFLICT DO NOTHING;

-- Perfis de nutricionista (o do convite pendente fica inativo até o aceite)
INSERT INTO tb_nutritionist_profile (id, user_id, crn_region, crn_number, bio, is_active, created_at, updated_at) VALUES
  ('33333333-0000-4000-8000-000000000002', '11111111-0000-4000-8000-000000000002', 3, '10001', 'Nutrição esportiva e hipertrofia.', true,  now(), NULL),
  ('33333333-0000-4000-8000-000000000003', '11111111-0000-4000-8000-000000000003', 8, '20002', 'Emagrecimento e reeducação alimentar.', true, now(), NULL),
  ('33333333-0000-4000-8000-000000000004', '11111111-0000-4000-8000-000000000004', 5, '30003', NULL,                                   false, now(), NULL)
ON CONFLICT DO NOTHING;

-- Perfis de paciente (todos maiores de 18 anos)
INSERT INTO tb_patient_profile (id, user_id, birth_date, biological_sex, height_in_cm, goal, wake_up_time, bed_time, training_time, created_at, updated_at) VALUES
  ('22222222-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000005', '1995-03-10', 'Female', 165, 'MuscleGain',  '06:00', '22:30', '18:00', now(), NULL),
  ('22222222-0000-4000-8000-000000000006', '11111111-0000-4000-8000-000000000006', '1990-07-22', 'Male',   180, 'WeightLoss',  '14:00', '06:00', NULL,    now(), NULL),
  ('22222222-0000-4000-8000-000000000007', '11111111-0000-4000-8000-000000000007', '2000-01-15', 'Female', 158, 'Maintenance', NULL,    NULL,    NULL,    now(), NULL),
  ('22222222-0000-4000-8000-000000000008', '11111111-0000-4000-8000-000000000008', '1988-11-30', 'Male',   175, 'WeightLoss',  NULL,    NULL,    NULL,    now(), NULL)
ON CONFLICT DO NOTHING;

-- Acompanhamentos: Carla com a Ana (ativo) e com o Bruno (encerrado pela Carla)
INSERT INTO tb_care_relationship (id, patient_id, nutritionist_id, started_at_utc, ended_at_utc, ended_by_user_id, created_at, updated_at) VALUES
  ('44444444-0000-4000-8000-000000000001', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000003', '2026-08-01 12:00:00+00', '2026-09-15 12:00:00+00', '11111111-0000-4000-8000-000000000005', '2026-08-01 12:00:00+00', '2026-09-15 12:00:00+00'),
  ('44444444-0000-4000-8000-000000000002', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000002', '2026-09-20 12:00:00+00', NULL,                     NULL,                                   '2026-09-20 12:00:00+00', NULL)
ON CONFLICT DO NOTHING;

-- Solicitações: as duas da Carla (aceitas) e a do Diego para a Ana (pendente)
INSERT INTO tb_nutritionist_request (id, patient_id, nutritionist_id, status, message, responded_at_utc, created_at, updated_at) VALUES
  ('55555555-0000-4000-8000-000000000001', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000003', 'Accepted', 'Quero perder peso antes do verão.', '2026-08-01 12:00:00+00', '2026-07-30 12:00:00+00', '2026-08-01 12:00:00+00'),
  ('55555555-0000-4000-8000-000000000002', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000002', 'Accepted', 'Agora quero ganhar massa muscular.', '2026-09-20 12:00:00+00', '2026-09-18 12:00:00+00', '2026-09-20 12:00:00+00'),
  ('55555555-0000-4000-8000-000000000003', '11111111-0000-4000-8000-000000000006', '11111111-0000-4000-8000-000000000002', 'Pending',  'Trabalho à noite e quero emagrecer.', NULL,                    now(),                    NULL)
ON CONFLICT DO NOTHING;

-- Planos da Carla: o finalizado do Bruno, o ativo da Ana e um rascunho da Ana
INSERT INTO tb_nutrition_plan (id, patient_id, nutritionist_id, name, description, start_date, end_date, status, created_at, updated_at) VALUES
  ('66666666-0000-4000-8000-000000000001', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000003', 'Plano de emagrecimento', 'Déficit calórico leve.',                         '2026-08-01', '2026-09-15', 'Finished',  '2026-08-01 12:00:00+00', '2026-09-15 12:00:00+00'),
  ('66666666-0000-4000-8000-000000000002', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000002', 'Plano de hipertrofia',   'Foco em proteína distribuída ao longo do dia.', '2026-10-01', NULL,         'Active',    '2026-09-25 12:00:00+00', '2026-10-01 12:00:00+00'),
  ('66666666-0000-4000-8000-000000000003', '11111111-0000-4000-8000-000000000005', '11111111-0000-4000-8000-000000000002', 'Plano de manutenção',    NULL,                                            '2026-11-01', NULL,         'Draft',     now(),                    NULL)
ON CONFLICT DO NOTHING;

-- Refeições do plano ativo (ordem sequencial 1, 2, 3...)
INSERT INTO tb_meal (id, nutrition_plan_id, name, notes, suggested_time, meal_order, created_at, updated_at) VALUES
  ('77777777-0000-4000-8000-000000000001', '66666666-0000-4000-8000-000000000002', 'Café da manhã',   'Em jejum, beber um copo de água antes.', '06:30', 1, now(), NULL),
  ('77777777-0000-4000-8000-000000000002', '66666666-0000-4000-8000-000000000002', 'Lanche da manhã', NULL,                                     '10:00', 2, now(), NULL),
  ('77777777-0000-4000-8000-000000000003', '66666666-0000-4000-8000-000000000002', 'Almoço',          NULL,                                     '13:00', 3, now(), NULL),
  ('77777777-0000-4000-8000-000000000004', '66666666-0000-4000-8000-000000000002', 'Jantar pós-treino', 'Até 1 hora depois do treino.',         '19:30', 4, now(), NULL),
  ('77777777-0000-4000-8000-000000000005', '66666666-0000-4000-8000-000000000001', 'Café da manhã',   NULL,                                     '07:00', 1, '2026-08-01 12:00:00+00', NULL),
  ('77777777-0000-4000-8000-000000000006', '66666666-0000-4000-8000-000000000001', 'Almoço',          NULL,                                     '12:30', 2, '2026-08-01 12:00:00+00', NULL)
ON CONFLICT DO NOTHING;

COMMIT;
```

## Ids fixos

Os Ids seguem um padrão, para facilitar as chamadas no `.http` e no Swagger:

| Prefixo | Tabela | Exemplo |
|---|---|---|
| `11111111-…` | Usuários (o final é o número da tabela de usuários) | Carla: `11111111-0000-4000-8000-000000000005` |
| `44444444-…` | Acompanhamentos | Carla e Ana: `44444444-0000-4000-8000-000000000002` |
| `55555555-…` | Solicitações | Pendente do Diego: `55555555-0000-4000-8000-000000000003` |
| `66666666-…` | Planos | Plano ativo da Carla: `66666666-0000-4000-8000-000000000002` |
| `77777777-…` | Refeições | Café da manhã do plano ativo: `77777777-0000-4000-8000-000000000001` |

## Para limpar

Apaga só os dados deste script, identificados pelo domínio `@nogvita.test`. O resto do banco não é tocado.

```sql
BEGIN;

CREATE TEMP TABLE seed_users ON COMMIT DROP AS
  SELECT id FROM tb_user WHERE email LIKE '%@nogvita.test';

DELETE FROM tb_meal WHERE nutrition_plan_id IN (
  SELECT id FROM tb_nutrition_plan
  WHERE patient_id IN (SELECT id FROM seed_users) OR nutritionist_id IN (SELECT id FROM seed_users));
DELETE FROM tb_nutrition_plan WHERE patient_id IN (SELECT id FROM seed_users) OR nutritionist_id IN (SELECT id FROM seed_users);
DELETE FROM tb_care_relationship WHERE patient_id IN (SELECT id FROM seed_users) OR nutritionist_id IN (SELECT id FROM seed_users);
DELETE FROM tb_nutritionist_request WHERE patient_id IN (SELECT id FROM seed_users) OR nutritionist_id IN (SELECT id FROM seed_users);
DELETE FROM tb_refresh_token WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_email_confirmation_token WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_password_reset_token WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_nutritionist_invitation WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_patient_profile WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_nutritionist_profile WHERE user_id IN (SELECT id FROM seed_users);
DELETE FROM tb_user WHERE id IN (SELECT id FROM seed_users);

COMMIT;
```

Depois de limpar, rode o script de novo para voltar ao estado inicial, por exemplo depois de um teste que encerrou o acompanhamento da Carla ou aceitou a solicitação do Diego.
