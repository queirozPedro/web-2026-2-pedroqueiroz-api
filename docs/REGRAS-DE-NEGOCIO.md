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

O login é feito pelo **Cognito (Hosted UI) com Google**. O sistema não guarda senha. O front
envia o **ID token** do Cognito (`Authorization: Bearer`), e a API valida o emissor (User Pool)
e a audiência (App client).

| Perfil | Como é definido | Pode |
|---|---|---|
| **Funcionário** (administrador) | e-mail `@ufersa.edu.br` **ou** listado em `Autenticacao:Funcionarios` (configuração) | dashboard, calendário, cardápio, produção, caixa, consumos, estudantes, feedbacks (consultar e mudar status), auditoria |
| **Estudante** | e-mail `@alunos.ufersa.edu.br` | ver o calendário/cardápio, **enviar feedback**, ver os próprios feedbacks |
| **Público externo** | não faz login | só aparece como consumo registrado pelo funcionário no balcão |
| Outro e-mail | qualquer outro domínio | fica autenticado, mas sem perfil: **403** em tudo |

- **O funcionário não avalia a comida.** Só o perfil estudante cria feedback.
- No **primeiro acesso**, a API cria o `Usuario`. O perfil é recalculado a cada requisição, a
  partir do e-mail e da configuração.
- O estudante é ligado ao seu cadastro do RU (`Estudante.UsuarioId`) **pelo e-mail institucional**.
  Se o e-mail não estiver cadastrado como estudante, ele faz login, mas não consegue enviar
  feedback (409).
- `Autenticacao:Funcionarios` serve para testes com contas fora da UFERSA. Fica nos User Secrets
  (local) ou na variável `Autenticacao__Funcionarios__0` da Lambda, **nunca no repositório**.

### Permissões por rota

| Rota | Acesso |
|---|---|
| `GET /api/dashboard` | funcionário |
| `GET /api/calendario`, `/dia/{data}`, `/{id}` | qualquer usuário logado |
| `POST/PUT/DELETE /api/calendario...`, `PUT .../cardapio`, `.../producao`, `.../caixa` | funcionário |
| `/api/estudantes...` | funcionário |
| `/api/consumos...` | funcionário |
| `GET /api/feedbacks`, `GET /api/feedbacks/{id}`, `PATCH /api/feedbacks/{id}/status`, `GET /api/estudantes/{id}/feedbacks` | funcionário |
| `POST /api/feedbacks`, `GET /api/feedbacks/meus` | estudante |
| `GET /api/categorias-feedback` | qualquer usuário logado |
| `GET /api/auditoria` | funcionário |

Respostas: 400 (validação, com os campos), 401 (sem token ou token inválido), 403 (perfil sem
permissão), 404 (não encontrado), 409 (conflito com o estado atual dos dados).

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
Criado no primeiro login: `IdExterno` (o `sub` do Cognito, único), e-mail (único), nome
(`name` do token ou a parte antes do @), perfil e data de criação (UTC).

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
- **Deploy da versão atual na Lambda**: gerar o pacote de novo (`aws/DEPLOY.md`, passo 4) e configurar
  `Autenticacao__Funcionarios__0`. Depois disso, a API na AWS deixa de usar dados fictícios.
- O front ainda não mostra telas por perfil: o estudante logado vê o menu do funcionário e recebe
  "Sem acesso" no dashboard.
