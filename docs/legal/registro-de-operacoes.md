# Registro das operações de tratamento de dados pessoais

> **Base:** LGPD, art. 37: "O controlador e o operador devem manter registro das operações de tratamento de dados pessoais que realizarem".
> **Última revisão:** 03/11/2026 · **Documento interno.** Revise a cada feature que crie, mude ou compartilhe um dado pessoal (é um item da Definition of Done da V2).
> **Aviso:** isto não é um parecer jurídico. O NogVita é um projeto de estudo.

## Os agentes

| Papel | Quem | Observação |
|---|---|---|
| Controlador | Luiz Gustavo Nogueira de Lima | Também é o encarregado (art. 41) · gustadev44@gmail.com |
| Operadores | Render (API), Vercel (site), Supabase (banco e arquivos), Brevo (e-mails) | Tratam os dados em nome do controlador. Nenhum deles recebe dados para uso próprio |
| Em aberto | O nutricionista | Pela Res. CFN 594/2017, art. 3º, V, ele guarda o próprio prontuário. Quando entrarem as notas da consulta (E6.3) ou clínicas, avaliar se ele é controlador (art. 5º, VI) e o NogVita, operador (art. 39). Veja o `docs/v2/plano-v2.md` |

## As operações

| # | Dados | Titular | Finalidade | Base legal | Quem acessa | Compartilhado com | Retenção |
|---|---|---|---|---|---|---|---|
| 1 | Nome, e-mail, CPF, *hash* da senha | Todos | Criar a conta, login, evitar contas duplicadas | Contrato (art. 7º, V) | O próprio titular e os administradores. O nutricionista **não** vê o CPF nem o e-mail | Render, Supabase | Enquanto a conta existir |
| 2 | Data de nascimento, sexo biológico, altura, objetivo, rotina | Paciente | Acompanhamento nutricional | **Consentimento específico** (art. 11, I), com a versão e a data guardadas | O paciente e o nutricionista com vínculo ativo (a idade, e não a data) | Render, Supabase | Enquanto a conta existir |
| 3 | Registros de peso e IMC calculado | Paciente | Acompanhar a evolução | Consentimento (art. 11, I) | O paciente e o nutricionista com vínculo ativo | Render, Supabase | Até serem apagados ou a conta ser excluída |
| 4 | Planos alimentares, refeições, metas | Paciente (o plano é dele) | Prescrição do nutricionista | Consentimento (art. 11, I) | O paciente (ativos e finalizados) e o nutricionista que criou, com vínculo ativo | Render, Supabase; Brevo, se o paciente pedir o PDF por e-mail | Enquanto a conta existir |
| 5 | Fotos de evolução (data, ângulo, arquivo sem metadados) | Paciente | Acompanhar a evolução | **Consentimento próprio** (art. 11, I), com a versão e a data guardadas | O paciente e o nutricionista com vínculo ativo, por URL assinada de curta duração | Render, Supabase Storage (bucket privado) | Até serem apagadas, o consentimento ser retirado ou a conta ser excluída |
| 6 | Foto de perfil do paciente | Paciente | Identificação | Contrato (art. 7º, V) | O paciente, os nutricionistas com pedido pendente e o nutricionista com vínculo ativo | Render, Supabase Storage (privado) | Até ser trocada, apagada ou a conta ser excluída |
| 7 | Nome, CRN, apresentação e foto do nutricionista | Nutricionista | Ser encontrado pelos pacientes | Contrato (art. 7º, V) | Qualquer usuário conectado. A foto fica num bucket público | Render, Supabase | Enquanto a conta existir |
| 8 | Solicitações e vínculos de acompanhamento (com a mensagem) | Paciente e nutricionista | Acompanhamento | Contrato (art. 7º, V) | As duas partes | Render, Supabase; Brevo (o aviso de encerramento) | Mantidos como histórico |
| 9 | Registro de emissão do PDF (código, data, nome e CRN do nutricionista, SHA-256) | Nutricionista | Autenticidade do documento (Res. CFN 760/2023, art. 10) | Legítimo interesse (art. 7º, IX) | **Público**, pelo código. Sem dado do paciente | Render, Supabase | Enquanto o plano existir |
| 10 | Aceites (dados de saúde, transferência internacional, fotos): versão e data | Paciente | Provar o consentimento (art. 8º, §2º) | Obrigação legal (art. 7º, II) | O paciente | Render, Supabase | Enquanto a conta existir |
| 11 | *Hash* dos tokens (sessão, confirmação, convite, redefinição de senha) | Todos | Segurança da conta | Contrato (art. 7º, V) | Ninguém (só o *hash*) | Render, Supabase | Até expirar ou ser revogado |
| 12 | Endereço IP | Todos | Limitar tentativas (*rate limit*) | Legítimo interesse (art. 7º, IX) | Ninguém: fica só na memória, durante a requisição | Render | Não é guardado |
| 13 | Registro de auditoria do administrador (só identificadores) | Administradores e usuários afetados | Rastrear ações administrativas | Legítimo interesse (art. 7º, IX) | O controlador, pelos *logs* | Render | Pela retenção dos *logs* da Render |

## A transferência internacional

| Dados | Hipótese (art. 33) |
|---|---|
| Dados de saúde (operações 2 a 5) | VIII: consentimento específico e em destaque, num aceite separado |
| Cadastro e o resto (operações 1, 6 a 13) | IX, combinado com o art. 7º, V: necessária à execução do contrato |

**Para diminuir a transferência:** confirmar a região do projeto no Supabase. Se for São Paulo, o banco e as fotos ficam no Brasil, e sobram no exterior só a Render, a Vercel e a Brevo.
