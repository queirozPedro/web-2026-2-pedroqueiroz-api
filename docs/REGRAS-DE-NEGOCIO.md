# Cumbuca: regras de negócio

Sistema de gestão e análise do Restaurante Universitário (RU) da UFERSA, trabalho de
Desenvolvimento de Aplicações Web (Prof. Walber José Adriano Silva). O objetivo não é só
CRUD: é transformar produção, consumo, pagamentos e feedbacks em indicadores que ajudem a
decidir **quanto produzir** e **onde estão as perdas**.

Este documento descreve as regras como estão implementadas. Ao mudar uma regra no código,
atualize-o junto.

- API (.NET 8, DDD + CQRS): este repositório, `cumbuca-api`
- Front (Angular): repositório `cumbuca-web`
- Infra AWS (Learner Lab): [`aws/DEPLOY.md`](../aws/DEPLOY.md)

---

## 1. Perfis e acesso

O login é feito pelo **Cognito (Hosted UI)**, num único User Pool, de duas formas. O sistema
não guarda senha (ela fica no Cognito). O front envia o **ID token** do Cognito
(`Authorization: Bearer`), e a API valida o emissor (User Pool) e a audiência (App client).

| Perfil | Como é definido | Pode (front) | Pode (API) |
|---|---|---|---|
| **Funcionário** (administrador) | login com **e-mail e senha** (conta criada em `POST /api/cadastro`) | dashboard, Administração (funcionários, alunos, restaurante) | dashboard, calendário, cardápio, produção, caixa, consumos, estudantes, funcionários (consulta), dados do restaurante, feedbacks (consultar e mudar status), auditoria |
| **Estudante** | login com **Google** e e-mail cujo domínio é `ufersa.edu.br` ou termina em `.ufersa.edu.br` | tela de feedback | ver o calendário/cardápio, **enviar feedback**, ver os próprios feedbacks |
| **Público externo** | não faz login | — | só aparece como consumo registrado pelo funcionário no balcão |
| Google com outro domínio | — | tela "sem acesso" | autenticado, mas sem perfil: **403** em tudo |

- **A forma de login decide o perfil**, não o e-mail: o ID token de quem entrou pelo Google traz a
  claim `identities` com `providerName: "Google"` (reserva: `cognito:username` começa com
  `google_`); quem não traz é usuário do próprio User Pool, ou seja, funcionário. O perfil é
  recalculado a cada requisição (`UsuarioClaimsTransformation` → `RegistrarAcessoCommand`).
- **O funcionário não avalia a comida.** Só o perfil estudante cria feedback.
- **E-mail único no sistema inteiro**: um e-mail tem uma conta só, por uma forma de login. O
  cadastro com um e-mail já usado devolve 409; um login com um e-mail que já pertence à outra
  forma de login fica sem perfil (motivo `email-em-uso`).
- No **primeiro acesso** pelo Google a API cria o `Usuario` com o **cadastro incompleto**. O
  funcionário já nasce completo (informa tudo no cadastro).
- **Cadastro incompleto não acessa nada**: as policies exigem a claim
  `cumbuca_cadastro_completo = true`. Só respondem `GET /api/usuarios/me` e
  `PUT /api/usuarios/me/cadastro`. O front manda para a tela "Complete seu cadastro".
- O estudante é ligado ao seu cadastro do RU (`Estudante.UsuarioId`) **pelo e-mail institucional**.
  Se o e-mail não estiver cadastrado como estudante, ele faz login, mas não consegue enviar
  feedback (409).

### Cadastro de funcionário (`POST /api/cadastro`, sem token)
Nome completo, e-mail, data de nascimento e senha, todos obrigatórios.
- **Nome completo**: só letras (com acento), espaço, hífen e apóstrofo, com pelo menos **2
  palavras de 3 letras ou mais** ("Ana de Souza" passa; "Li Wang" não). Até 150 caracteres.
- **E-mail**: formato válido (`algo@dominio.tld`), até 150 caracteres. **Sem confirmação** por
  enquanto: a conta já nasce com o e-mail verificado.
- **Data de nascimento**: entre 01/01/1900 e hoje (UTC-3).
- **Senha**: a política padrão do User Pool (mínimo 8, maiúscula, minúscula, número e símbolo).
- A API cria a conta no Cognito (`AdminCreateUser` sem envio de convite +
  `AdminSetUserPassword` definitiva) e o `Usuario` funcionário no tenant padrão. Se a gravação
  no banco falhar, apaga a conta do Cognito.

### Permissões por rota

| Rota | Acesso |
|---|---|
| `POST /api/cadastro` | anônimo |
| `GET /api/usuarios/me` | qualquer token válido (mesmo sem perfil) |
| `PUT /api/usuarios/me/cadastro` | com perfil, mesmo com o cadastro incompleto |
| `GET /profile`, `PUT /profile` | funcionário ou estudante (rotas próprias no API Gateway) |
| `GET /api/restaurante` | qualquer usuário logado (o front aplica as cores do restaurante) |
| `PUT /api/restaurante`, `PUT/DELETE /api/restaurante/foto` | funcionário |
| `GET /api/funcionarios`, `GET /api/alunos` | funcionário |
| `GET /api/dashboard` | funcionário |
| `GET /api/calendario`, `/dia/{data}`, `/{id}` | qualquer usuário logado |
| `POST/PUT/DELETE /api/calendario...`, `PUT .../cardapio`, `.../producao`, `.../caixa` | funcionário |
| `/api/estudantes...` | funcionário |
| `/api/consumos...` | funcionário |
| `GET /api/feedbacks`, `GET /api/feedbacks/{id}`, `PATCH /api/feedbacks/{id}/status`, `GET /api/estudantes/{id}/feedbacks` | funcionário |
| `POST /api/feedbacks`, `GET /api/feedbacks/meus` | estudante |
| `GET /api/categorias-feedback` | qualquer usuário logado |
| `GET /api/auditoria` | funcionário |

Todas as rotas de dados também exigem o cadastro concluído.

Respostas: 400 (validação, com os campos), 401 (sem token ou token inválido), 403 (perfil sem
permissão ou cadastro incompleto), 404 (não encontrado), 409 (conflito com o estado atual dos
dados).

### Tenant / restaurante (`Tenant`, tabelas `Tenants` e `HorariosFuncionamento`)
O Restaurante Universitário atendido. Por enquanto existe **um só**, gravado pela migration
(`HasData`): Id 1, "Restaurante Universitário: Ufersa - Campus Pau dos Ferros", responsável
"Pedro Andrade" (provisório), Pau dos Ferros/RN. **Todo usuário novo entra nele**
(`Tenant.PadraoId`); a divisão entre tenants fica para depois. O funcionário edita tudo em
**Administração > Restaurante** (`PUT /api/restaurante`).

| Campo | Regra |
|---|---|
| Nome | obrigatório, até 150 |
| Responsável | obrigatório, até 150 |
| Endereço | opcional, até 200 |
| Cidade | obrigatória, até 100 |
| UF | obrigatória, uma das 27 siglas (gravada em maiúsculas) |
| Cor principal | `#RRGGBB` (gravada em minúsculas): fundo do menu lateral |
| Cor secundária | `#RRGGBB`: item ativo do menu, botões e links |
| Foto | opcional, no S3 |
| Horário de funcionamento | por dia da semana, ver abaixo |

- **Cores**: as duas ficam sob texto branco no app, então precisam de **contraste de pelo menos
  3:1 com o branco** (cálculo da WCAG, `Tenant.ContrasteComBranco`). As cores do seed são as do
  tema original (`#1b4535` e `#c2571e`). O front grava as cores nas variáveis CSS
  `--color-sidebar` e `--color-accent` (e deriva `--color-accent-soft`).
- **Horário de funcionamento** (`HorarioFuncionamento`, owned, chave `TenantId + DiaSemana +
  TurnoId`): para cada dia da semana (0 = domingo ... 6 = sábado), no máximo **um almoço e uma
  janta**, cada um com abertura e fechamento (fechamento depois da abertura); no mesmo dia o
  **almoço termina antes de a janta abrir**. **Dia sem horário = fechado.** O PUT manda a semana
  inteira; a lista é atualizada no lugar (o que saiu é removido). O seed traz um horário
  **provisório**: segunda a sexta, almoço 11:00–13:30 e janta 17:00–19:00. Por enquanto é só
  informativo: o turno do calendário continua com o próprio horário.
- **Foto**: `PUT /api/restaurante/foto` (multipart, campo `foto`), JPG, PNG ou WebP até 2 MB, no
  mesmo bucket privado das fotos de perfil (`Perfil:Bucket`), em `tenants/{id}/foto-{data}.{ext}`;
  a anterior é apagada depois que a nova está gravada, e a nova é apagada se a gravação no banco
  falhar. `DELETE /api/restaurante/foto` remove. O GET devolve um link temporário (1 hora).
- Alterar o restaurante avisa os fronts pelo WebSocket (`restaurante-atualizado`), que recarregam
  nome e cores.
- **Usuários e alunos do restaurante**: não são coleções do `Tenant` (agregados se ligam só por
  Id). Cada `Usuario` e `Estudante` tem o `TenantId`; as listas de Administração são
  `GET /api/funcionarios` (usuários com perfil funcionário do tenant) e `GET /api/alunos`.
- **Lista de alunos** (`GET /api/alunos`): junta o **cadastro do RU** (`Estudante`, feito pelo
  funcionário, com matrícula) e a **conta** (`Usuario` com perfil estudante, criada no primeiro
  login com Google). Uma linha por aluno: os dois ligados (`Estudante.UsuarioId` ou, antes do
  próximo login, o mesmo e-mail), só o cadastro (ainda não entrou) ou **só a conta** (entrou,
  mas ainda não tem cadastro no RU: matrícula, curso, vínculo e subsídio nulos; não envia
  feedback nem consome como estudante até ser cadastrado).

- `CalendarioRefeicoes`, `Estudantes`, `RegistrosConsumo`, `Feedbacks`, `Usuarios` e
  `HistoricoAlteracoes` têm a coluna `TenantId` (FK Restrict). As partes owned (cardápio,
  produção, caixa, avaliações) seguem o dono. `CategoriasFeedback` é global.
- **Toda consulta filtra pelo tenant do usuário logado**: filtro global do EF
  (`HasQueryFilter`) nos repositórios e `"TenantId" = @tenantId` em todo SQL das queries Dapper.
  O tenant da requisição é definido no `RegistrarAcessoCommand` (`TenantAtual`, scoped).
- Ao gravar, o `SaveEntitiesAsync` preenche o `TenantId` dos registros novos.
- Únicos **por tenant**: turno (`TenantId + Data + TurnoId`), matrícula e e-mail do estudante.
  Únicos **no sistema todo**: e-mail e `IdExterno` do usuário.

---

## 2. Entidades e regras

Os horários e datas "de hoje" usam o fuso do RU, **UTC-3** fixo (Pau dos Ferros/RN não tem
horário de verão). Ver `Commands/HoraLocal.cs`. A Lambda roda em UTC, então nunca use
`DateTime.Now` para isso.

### Calendário de refeições (`CalendarioRefeicao`, tabela `CalendarioRefeicoes`)
Um **turno** (`almoco` ou `janta`) servido em uma **data**, com horário de abertura e fechamento.
É a âncora de tudo: cardápio, produção, caixa e consumos pertencem a um turno.
- **Só um almoço e uma janta por data** (índice único `Data + TurnoId`; repetir dá 409).
- O fechamento precisa ser **depois** da abertura.
- **Não pode ser removido** se já tiver produção, caixa fechado ou consumos (409). Remover leva o
  cardápio junto.

### Cardápio (`Cardapio`, tabela `Cardapios`, 1 para 1 com o turno)
Itens (texto livre), categoria nutricional (texto livre: "Tradicional", "Vegetariano"...),
**preço subsidiado** (estudante) e **preço integral** (externo).
- Preços maiores que zero, com 2 casas decimais.
- O subsidiado não pode passar do integral.
- Sem cardápio, o turno **não aceita consumo nem fechamento de caixa** (409).

### Produção (`Producao`, tabela `Producoes`, 1 para 1 com o turno)
Quantidade de refeições **produzidas** no turno (≥ 0) e observações.
- **O servido não é digitado aqui**: é a contagem dos registros de consumo do turno.
- Excedente = produzido − servido (nunca negativo), calculado na leitura.

### Fechamento de caixa (`FechamentoCaixa`, tabela `FechamentosCaixa`, 1 para 1 com o turno)
"Pagamentos/Caixa" da especificação: valor **contado em dinheiro** e valor **recebido via Pix**
(≥ 0) informados pelo funcionário ao fechar o turno.
- Exige cardápio cadastrado.
- Real arrecadado = dinheiro + Pix.
- Pode ser corrigido (o PUT substitui os valores).

### Estudante (`Estudante`, tabela `Estudantes`)
Nome, e-mail institucional (`@alunos.ufersa.edu.br`, gravado em minúsculas), matrícula, curso,
vínculo e tipo de subsídio.
- **Matrícula e e-mail são únicos** (409).
- Começa com vínculo **ativo**. Vínculo `inativo` (formatura, trancamento...) não consome como
  estudante nem envia feedback.
- Tipo de subsídio: `padrao` paga o preço subsidiado; `isento` não paga (por exemplo, auxílio
  alimentação).
- Nome e e-mail ficam no estudante porque o cadastro acontece antes do primeiro login dele.

### Registro de consumo (`RegistroConsumo`, tabela `RegistrosConsumo`)
Uma refeição servida no ponto de venda. É a base dos indicadores.
- **Estudante**: precisa existir e estar ativo. É identificado por `qrcode` (simulado) ou
  `matricula` (padrão). Paga o **preço subsidiado** ou **R$ 0** se for isento; nesse caso não há
  forma de pagamento.
- **Externo**: sem cadastro, identificação sempre `balcao`, paga o **preço integral**. A forma de
  pagamento é obrigatória.
- **O valor pago nunca vem do cliente**: é calculado pelo cardápio do turno conforme o tipo.
- Forma de pagamento: `dinheiro` ou `pix`, obrigatória quando há valor a pagar.
- O **horário precisa estar dentro do funcionamento do turno** (409). Se não for informado, vale o
  horário atual (UTC-3).
- A data é a do turno; o registro guarda só o horário.
- Cancelar (DELETE) apaga o registro (lançamento feito por engano).

### Feedback e avaliações (`Feedback`, tabelas `Feedbacks` e `Avaliacoes`)
Enviado pelo **estudante logado** com vínculo ativo: um assunto (categoria de feedback), um
comentário (até 2000 caracteres) e uma **nota de 1 a 5 para cada uma das 7 categorias de
avaliação**: qualidade, sabor, temperatura, variedade, atendimento, limpeza e estrutura.
- **As 7 notas são obrigatórias** (o dashboard calcula a média de cada categoria sobre todos os
  feedbacks).
- A data é a de hoje (UTC-3). O status começa em `novo`.
- O funcionário muda o status: `novo` → `em_analise` → `resolvido` (qualquer ordem). Mudar o
  status gera o evento de domínio `StatusFeedbackAlteradoDomainEvent`.
- O estudante vem do token, nunca do corpo da requisição.

### Categorias de feedback (`CategoriaFeedback`, tabela `CategoriasFeedback`)
Assuntos fixos, gravados pela migration (`HasData`): 1 Cardápio, 2 Filas e espera,
3 Atendimento, 4 Higiene, 5 Estrutura, 6 Preço. Os nomes precisam bater com o tipo
`CategoriaFeedback` do front (`dashboard.models.ts`). Não há tela nem rota para editá-las.

### Usuário (`Usuario`, tabela `Usuarios`)
Pertence a um tenant. Campos: `IdExterno` (o `sub` do Cognito, único), e-mail do login (único
no sistema, minúsculas), **nome completo** e **data de nascimento** (nulos até o cadastro ser
concluído), perfil, data de criação e `CadastroConcluidoEm` (UTC; nulo = cadastro incompleto).
- Funcionário: criado pelo cadastro, já completo.
- Estudante: criado no primeiro login com Google, incompleto; conclui em
  `PUT /api/usuarios/me/cadastro` (mesmas regras de nome e data do cadastro). O e-mail é sempre
  o do login.
- Contas anteriores a esta regra ficaram com o cadastro incompleto e passam pela mesma tela.

### Perfil do usuário (`GET/PUT /profile`, DynamoDB + S3)
Tela "Meu perfil" no front (`/perfil`), para funcionário e estudante.
- `PUT /profile` (multipart/form-data): nome completo e data de nascimento (mesmas regras do
  cadastro) e, opcional, a **foto** (JPG, PNG ou WebP, até 2 MB).
- A foto vai para o bucket **privado** do S3 (`Perfil:Bucket`), em `perfis/{sub}/foto-{data}.{ext}`;
  a anterior é apagada depois que a nova é gravada. A tela recebe um link temporário (1 hora).
- O perfil vai para a tabela do DynamoDB `Perfil:Tabela` (`pedroqueiroz-message-2026.2`), um
  item por usuário: `id` (sub do Cognito, chave de partição), `tenantId`, `email`, `nome`,
  `dataNascimento`, `perfil`, `fotoChave`, `atualizadoEm`.
- Nome e data de nascimento também são gravados no `Usuario` (Postgres), para o app mostrar o
  mesmo nome em todo lugar. Se a gravação falhar, a foto nova é apagada do bucket.
- `GET /profile` lê o item do DynamoDB; quem ainda não salvou o perfil vê os dados do cadastro,
  sem foto.

### Auditoria (`HistoricoAlteracao`, tabela `HistoricoAlteracoes`)
**Toda gravação feita pelo `SaveEntitiesAsync`** gera uma linha por registro afetado: tabela,
ação (`criacao`, `edicao`, `exclusao`), usuário logado (ou nulo), data/hora em UTC e os valores
antes e depois em JSON (`jsonb`). É gravada na **mesma transação** dos dados. Só leitura, em
`GET /api/auditoria`.

---

## 3. Indicadores do dashboard

`GET /api/dashboard` devolve dados brutos: uma linha por turno que **já teve movimento**
(produção, consumo ou caixa) e os feedbacks. Os KPIs, gráficos e filtros são calculados no
front (`cumbuca-web/src/app/pages/dashboard/dashboard.utils.ts`).

| Campo | Regra |
|---|---|
| `produzido` | Produção do turno (0 enquanto não registrada) |
| `servido` | quantidade de consumos do turno |
| `excedente` | produzido − servido, mínimo 0 |
| `estudantes` / `externos` | consumos por tipo de cliente |
| `estimado` | soma do valor pago dos consumos (clientes × preço esperado do cardápio) |
| `dinheiro` / `pix` | valores do **fechamento de caixa**; com o caixa ainda aberto, a soma dos consumos pagos em cada forma |
| `real` | dinheiro + pix |
| `fluxoPorHora` | consumos na 1ª, 2ª e 3ª hora contadas da abertura do turno (o que passar entra na 3ª) |

Regras do front:
- **Alerta de excesso**: excedente acima de **10%** do produzido no turno.
- **Alerta de arrecadação**: arrecadado mais de **2%** abaixo do estimado.
- **Nota baixa**: média de categoria abaixo de **3,5**.
- **Estimativa de produção** para uma data: média servida nas **últimas 4 ocorrências do mesmo dia
  da semana** e turno, mais **5%** de margem, arredondada para cima de 5 em 5.
- Período sem turnos com movimento: aviso "Não existem dados para o período selecionado".
- O dashboard **recarrega sozinho** quando a API avisa pelo WebSocket (`dashboard-atualizado`),
  depois de qualquer command que muda dados.

---

## 4. Arquitetura e padrão de código (API)

```
src/CumbucaSaas.Domain/Aggregates/XxxAggregate/   entidades, Enumerations, IXxxRepository
src/CumbucaSaas.Infrastructure/EntityConfigurations/   mapeamento EF (um por agregado)
src/CumbucaSaas.Infrastructure/Repositories/      repositórios (+ registro em DependencyInjection.cs)
src/CumbucaSaas.Infrastructure/CumbucaDbContext.cs DbSet + ApplyConfiguration explícito + auditoria
src/CumbucaSaas.API/Commands/XxxCommand.cs        record Command + Validator + Handler (MediatR + FluentResults)
src/CumbucaSaas.API/Queries/XxxQueries.cs          leitura com Dapper + view models
src/CumbucaSaas.API/Controllers/XxxController.cs   Result -> HTTP via this.ParaResposta(...)
src/CumbucaSaas.API/Program.cs                    registro de validators, queries, auth
```

- **Escrita** pelo domínio (EF Core + `SaveEntitiesAsync`); **leitura** direta com Dapper (SQL com
  nomes entre aspas, ex.: `"CalendarioRefeicoes"`).
- **Enumerations** (`Turno`, `StatusFeedback`, `TipoCliente`...) são gravadas pelo **Id**
  (colunas `XxxId`) e trafegam na API pelo **Name** (`"almoco"`, `"em_analise"`...).
- Agregados se referenciam **só por Id**, com chave estrangeira `Restrict` (sem navegação).
  Partes 1 para 1 de um agregado (cardápio, produção, caixa) são `OwnsOne` com tabela própria.
- Erros: `NaoEncontradoError` (404), `ConflitoError` (409), `ValidacaoError` (400).
  `DomainException` é a guarda do domínio; quando a regra depende do estado, o handler a converte
  em `ConflitoError`.
- Commands que mudam dados chamam `INotificadorTempoReal.NotificarAsync("dashboard-atualizado")`
  **depois** de gravar.
- Datas em parâmetros Dapper vão como texto ISO com `CAST(@x AS date)`; `date` volta como
  `DateTime` e `time` como `TimeSpan`.
- Cada validator novo precisa de uma linha `AddScoped<IValidator<...>>` no `Program.cs`.

### Migrations (Visual Studio, Package Manager Console)

```powershell
Add-Migration NomeDaMudanca -Project CumbucaSaas.Infrastructure -StartupProject CumbucaSaas.API -OutputDir Migrations
Update-Database -Project CumbucaSaas.Infrastructure -StartupProject CumbucaSaas.API
```

Há **um só banco** (RDS PostgreSQL `cumbuca-db`) para local e AWS: revise a migration antes do
`Update-Database`. Nunca edite uma migration já aplicada; crie outra. A connection string local
fica nos **User Secrets** (`ConnectionStrings:CumbucaDb`), e na Lambda em
`ConnectionStrings__CumbucaDb`. **Nunca em arquivo versionado** (inclusive o
`launchSettings.json`).

---

## 5. Pendências conhecidas

- **Telas de cadastro no front**: calendário/cardápio, produção, caixa, ponto de venda (consumo),
  estudantes, envio de feedback pelo estudante, gestão de status. Hoje só pelo Swagger.
- **Relatórios CSV/PDF** com filtro por período (previstos na especificação, com S3).
- **Deploy da versão atual na Lambda**: gerar o pacote de novo (`aws/DEPLOY.md`, passo 4). A
  LabRole precisa poder chamar `cognito-idp:AdminCreateUser`, `AdminSetUserPassword` e
  `AdminDeleteUser` (cadastro de funcionário).
- **Confirmação de e-mail** no cadastro de funcionário (hoje a conta nasce verificada) e
  restrição de quem pode criar conta de funcionário (hoje qualquer pessoa cria).
- **Divisão entre tenants**: escolher/criar o tenant do usuário (hoje todos entram no tenant 1).
- **Cardápios do restaurante**: tela e regras próprias por tenant (hoje o cardápio é por turno do
  calendário, que já pertence ao tenant).
- **Usar o horário de funcionamento** do restaurante como padrão ao criar turnos no calendário (e
  impedir turno em dia fechado).
- Cadastro de estudante e ações sobre funcionários/alunos na Administração (hoje só consulta).
- No front, o estudante só tem a tela de feedback (o cardápio do dia ainda não tem tela).
