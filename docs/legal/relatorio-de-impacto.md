# Relatório de impacto à proteção de dados pessoais (RIPD)

> **Base:** LGPD, art. 38. A ANPD pode exigir este relatório, "inclusive de dados sensíveis". Pelo parágrafo único, ele contém no mínimo: os tipos de dados coletados, a metodologia de coleta e de garantia da segurança, e a análise das medidas, salvaguardas e mecanismos de mitigação de risco.
> **Última revisão:** 03/11/2026 · **Documento interno.** Revise a cada épico que trate um dado de saúde novo (o E5, o E6.1, o E6.2, o E6.3 e o E6.6 do `docs/v2/plano-v2.md`).
> **Aviso:** isto não é um parecer jurídico. O NogVita é um projeto de estudo, sem fins comerciais.

## 1. O tratamento

O NogVita conecta pacientes e nutricionistas. O nutricionista monta o plano alimentar do paciente e acompanha o peso e, se o paciente quiser, as fotos de evolução. **O risco principal é o vazamento ou o acesso indevido a dados de saúde**, que são dados sensíveis (art. 5º, II).

| | |
|---|---|
| Controlador | Luiz Gustavo Nogueira de Lima (também encarregado) |
| Titulares | Pacientes (maiores de 18 anos), nutricionistas e administradores |
| Volume | Pequeno: um projeto de estudo, só com dados fictícios no staging |
| Operações | Veja o [registro das operações](registro-de-operacoes.md) |

## 2. Os tipos de dados

- **Cadastrais:** nome, e-mail, CPF e *hash* da senha.
- **Sensíveis (saúde):** data de nascimento, sexo biológico, altura, objetivo, rotina, peso, planos alimentares e fotos de evolução corporal.
- **Profissionais:** CRN, apresentação e foto do nutricionista (públicos para os usuários conectados).
- **Técnicos:** *hash* dos tokens, datas, registros de consentimento e de emissão dos PDFs. O IP não é guardado.

## 3. Como os dados são coletados

- **Só o titular informa os próprios dados**, pelos formulários do site. A exceção é o nutricionista, que registra o plano e pode registrar o peso medido na consulta.
- **O consentimento vem antes:** os dados de saúde e a transferência internacional têm dois aceites separados no cadastro, e as fotos de evolução têm um aceite próprio antes do primeiro envio. A versão e a data de cada aceite ficam guardadas (art. 8º, §2º).
- **Minimização:** o nutricionista vê a idade, e não a data de nascimento, e nunca vê o CPF nem o e-mail. Fotos são opcionais. O IP fica só na memória.
- **Menores de 18 anos** são recusados no cadastro.

## 4. Como a segurança é garantida

| Camada | Medida |
|---|---|
| Transporte | HTTPS em tudo, HSTS e redirecionamento do HTTP |
| Autenticação | Senha com PBKDF2 (100 mil iterações) e política do NIST (15+ caracteres e lista de bloqueio); JWT de 15 minutos; *refresh token* com rotação e detecção de reuso |
| Autorização | Seguro por padrão (toda rota exige login); papéis; o acesso aos próprios dados sai do token, nunca de um Id enviado; o nutricionista só vê pacientes com vínculo ativo (e recebe `404` fora disso) |
| Dados guardados | Tokens só como *hash*; fotos num bucket privado, recodificadas sem metadados (EXIF/GPS) e mostradas só por URL assinada de curta duração; a chave do armazenamento só no servidor |
| Entrada | Validação de todos os campos; limite de tamanho; *rate limit* nas rotas públicas e nos envios |
| Respostas | Mensagens que não revelam se um e-mail ou CPF tem conta; erros sem detalhe interno; cabeçalhos de segurança |
| *Logs* | Sem dado pessoal, só identificadores |
| Documentos | Cada PDF de plano tem um código verificador e o registro da emissão (sem dado do paciente) |
| Processo | Integração contínua com os testes; revisão de código por PR; staging só com dados fictícios |

## 5. Os riscos e as medidas

Probabilidade e impacto: **B** (baixo), **M** (médio), **A** (alto).

| # | Risco | Prob. | Impacto | Medidas | Risco residual |
|---|---|---|---|---|---|
| R1 | Vazamento do banco (dados de saúde) | B | A | Credenciais só em variáveis do servidor; senhas e tokens em *hash*; o fornecedor cifra o disco | **M**. Falta cifrar os campos sensíveis na aplicação; avaliar quando houver dados reais |
| R2 | Acesso de um nutricionista a um paciente que não é dele | B | A | A checagem do vínculo em todas as rotas, com testes de integração para cada uma | B |
| R3 | Exposição das fotos de evolução | B | A | Bucket privado; URL assinada de minutos; consentimento próprio; sem compartilhamento | B |
| R4 | Localização da casa do paciente numa foto | M | A | Recodificação que remove o EXIF | B |
| R5 | Tomada de conta (senha roubada) | M | A | Política de senha; *rate limit*; rotação do *refresh token* | **M**. Falta a autenticação em dois fatores (está no backlog da V2) |
| R6 | Transferência internacional sem base legal | M | M | Consentimento específico do art. 33, VIII (saúde) e o contrato (cadastro) | B. Confirmar a região do Supabase |
| R7 | Consentimento sem prova | B | M | A versão e a data guardadas a cada aceite | B |
| R8 | Os pacientes antigos sem o aceite novo | A | M | `GET /patients/me/health-data-consent` diz quem precisa aceitar | **M** até o front pedir o aceite a eles |
| R9 | Apagar o prontuário quando a conta é excluída (E6.3 e E6.5) | — | A | Ainda não existe: o desenho exige a guarda por 20 anos (Res. CFN 594/2017 e Lei 13.787/2018) | Tratar no desenho do E6.3 |
| R10 | Indisponibilidade (o plano gratuito dorme e pausa) | A | B | Os dados ficam guardados; o serviço volta sozinho | B (aceito, por ser um projeto de estudo) |

## 6. Pendências

1. **O front pedir o aceite** aos pacientes que se cadastraram antes do termo (R8).
2. **Confirmar a região do Supabase** e, se for possível, mover para São Paulo (R6).
3. **A autenticação em dois fatores**, pelo menos para o nutricionista e o administrador (R5).
4. **Definir o papel do nutricionista** (controlador ou não) antes das notas da consulta e das clínicas.
5. Antes de qualquer uso com **dados reais**: revisão por alguém de direito digital ou de saúde, e um plano de resposta a incidentes (art. 48).
