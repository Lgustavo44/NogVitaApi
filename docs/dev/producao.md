# Produção

O que muda entre o staging e a produção, e a lista de conferência antes de colocar a API no ar para usuários reais. A imagem é a mesma (o mesmo `Dockerfile` da Render, do staging e do `docker-compose`); o que muda é o ambiente (`ASPNETCORE_ENVIRONMENT=Production`) e a configuração.

## O que muda em relação ao staging

| Assunto | Staging | Produção |
|---|---|---|
| Ambiente | `Staging` | `Production` |
| Swagger e `/openapi/v1.json` | ligados | desligados (um anônimo recebe `401`, como em qualquer endereço inexistente) |
| Dados | só de teste | dados reais de saúde (LGPD) |
| `AllowedHosts` | `nogvitaapi.onrender.com` | o domínio da API, obrigatório |
| CORS | só o front (`https://nogvita.vercel.app`) | só o front, só com `https://` |
| Domínio | `*.onrender.com` (compartilhado) | um domínio próprio, para o HSTS poder crescer |
| Plano da Render | gratuito (dorme depois de 15 minutos) | pago, sem dormir |

Em `Production`, a API **se recusa a subir** se a configuração estiver insegura ou incompleta (`ProductionSettingsGuard`), e a mensagem diz o que falta: `AllowedHosts` aberto, CORS vazio ou com `http://`, `Frontend:BaseUrl` sem `https://`, ou o e-mail ainda no SMTP local. Um teste de integração confere essa recusa.

## Configuração

O `appsettings.Production.json` traz só o que não é segredo nem depende do domínio (o e-mail pela Brevo e os níveis de log). O resto entra por variável de ambiente no painel da hospedagem, com `__` no lugar do `:`:

| Variável | O que é |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__NogVita` | a conexão com o Postgres de produção (com `SSL Mode=Require`) |
| `Jwt__SecretKey` | 32 bytes aleatórios em base64, **diferentes** dos do staging |
| `AllowedHosts` | o domínio da API, por exemplo `api.nogvita.com.br` |
| `Cors__AllowedOrigins__0` | o endereço do front, com `https://` |
| `Frontend__BaseUrl` | o endereço do front, com `https://` (os links dos e-mails) |
| `Email__ApiKey` e `Email__FromAddress` | a chave da Brevo e o remetente, de um domínio verificado |
| `AdminSeed__Name`, `__Email`, `__Cpf`, `__Password` | o administrador inicial (troque a senha depois do primeiro acesso) |

Não defina `Database__MigrateOnStartup`: ele fica `false`, e a migration é um passo do deploy (abaixo). Também não defina `RateLimiting__Enabled` nem `OpenFoodFacts__ImportOnStartup`: os dois ficam ligados, como devem.

## Lista de conferência

### Infraestrutura

- [ ] O plano da hospedagem não dorme, e tem memória para a importação do Open Food Facts na primeira subida.
- [ ] A checagem de saúde da plataforma aponta para **`/health/ready`** (o banco incluído). O `/health/live` serve para um "o processo está vivo?" que não deve reiniciar o container quando só o banco caiu.
- [ ] O domínio próprio tem o certificado TLS emitido pela plataforma, e o HTTP redireciona para o HTTPS (a API responde `308`, e manda o HSTS de 30 dias).
- [ ] O banco tem **backup automático** e o restauro foi testado pelo menos uma vez.
- [ ] O banco e a API ficam na mesma região, e o banco não aceita conexões públicas sem TLS.

### Deploy

- [ ] As migrations são aplicadas **antes** do código novo subir (`dotnet ef database update` contra o banco de produção). Código novo com banco velho quebra o login (como na coluna `deactivated_at_utc` da Fase 21).
- [ ] O CI do commit está verde (unitários, integração e resultados sem teste).
- [ ] Depois do deploy: `/health/ready` responde `200`, o login funciona, e um e-mail de teste chega.

### Configuração e segurança

- [ ] Todas as variáveis da tabela acima estão definidas; a API sobe sem a mensagem "Configuração de produção incompleta".
- [ ] Os segredos de produção (JWT, banco, Brevo, senha do admin) são diferentes dos do staging, e não estão em nenhum arquivo do repositório.
- [ ] O domínio do remetente está verificado na Brevo (SPF e DKIM), para os e-mails não caírem no spam.
- [ ] O Swagger não abre na produção.
- [ ] Os logs não têm dados pessoais (só Ids; os eventos `9100 AdminAudit` mostram as ações do admin).

### LGPD

- [ ] A [Política de Privacidade](../legal/politica-de-privacidade.md) e os [Termos de Uso](../legal/termos-de-uso.md) estão publicados no front, com a data de atualização certa.
- [ ] A tabela de operadores da política lista os serviços reais da produção (hospedagem, banco, e-mail) e onde ficam os dados (transferência internacional).
- [ ] O encarregado (o contato da seção 1 da política) responde os pedidos dos titulares; o prazo prometido é de 15 dias.
- [ ] Existe um procedimento para pedidos de exclusão e de portabilidade (hoje manuais, pelo e-mail do encarregado).
- [ ] Existe um plano para incidentes: quem avalia, e como avisar os titulares e a ANPD (art. 48).
- [ ] Nenhum dado do staging (de teste) foi copiado para a produção, e nenhum dado real vai para o staging.

### Licenças

- [ ] A citação da TACO e do Open Food Facts aparece para o usuário (o campo `attribution` e o rodapé do PDF).
- [ ] O `off_brasil.csv` continua público sob a ODbL, com o `SOURCE-OFF.md`.
- [ ] O QuestPDF segue dentro da Community License (faturamento anual abaixo de US$ 1 milhão). Acima disso, é preciso a licença paga **antes** de passar do limite.
- [ ] A fonte Roboto segue acompanhada do `OFL.txt`.

As licenças estão descritas no [README](../../README.md#licenças-de-terceiros).
