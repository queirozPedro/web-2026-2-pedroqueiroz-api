# Deploy na AWS (Learner Lab)

Arquitetura:

```
Front (Amplify) ──HTTPS──▶ HTTP API (API Gateway) ──▶ Lambda CumbucaApi (.NET 8)
      ▲                                                       │
      │ wss://                                                │ broadcast após commands
      └──────────── WebSocket API (API Gateway) ◀─────────────┘
                              │
                              ▼
                Lambda CumbucaWebSocket (Node.js) ──▶ DynamoDB CumbucaConexoes
```

Tudo em **us-east-1**. No Learner Lab não dá para criar perfis do IAM: as duas Lambdas usam a **LabRole**.

---

## 1. DynamoDB: tabela de conexões

DynamoDB → **Criar tabela**

- Nome: `CumbucaConexoes`
- Chave de partição: `connectionId`, tipo **String**
- O resto no padrão → **Criar tabela**

## 2. Lambda do WebSocket

Lambda → **Criar função** → *Criar do zero*

- Nome: `CumbucaWebSocket`
- Tempo de execução: **Node.js** (20.x ou mais novo)
- **Configurações adicionais → Perfil de execução → Usar um perfil existente → `LabRole`**
- **Criar função**

Depois de criada:

1. Aba **Código**: abra o `index.mjs`, apague tudo, cole o conteúdo de [`websocket/index.mjs`](websocket/index.mjs) → **Deploy**.
2. **Configuração → Variáveis de ambiente → Editar** → `TABELA_CONEXOES` = `CumbucaConexoes`.

## 3. WebSocket API

API Gateway → **Criar API** → **WebSocket API** → *Criar*

- Nome: `CumbucaWebSocket`
- Expressão de seleção de rota: `request.body.action` (fica `$request.body.action`)
- Rotas: marque **$connect**, **$disconnect**, **$default** e adicione a rota personalizada **broadcast**
- Integrações: todas **Lambda** → `CumbucaWebSocket`
- Estágio: `prod`, com implantação automática → **Criar e implantar**

Em **Estágios → prod**, copie a **URL do WebSocket** (`wss://xxxx.execute-api.us-east-1.amazonaws.com/prod`).

> Toda alteração nas rotas só vale depois de **Implantar API** de novo.

Teste rápido (dois terminais):

```bash
npx wscat -c wss://xxxx.execute-api.us-east-1.amazonaws.com/prod
```

Em um deles envie `{"action":"broadcast","mensagem":{"tipo":"teste"}}`. Os dois devem receber `{"tipo":"teste"}`.

## 4. Lambda da CumbucaApi (.NET)

Gerar o pacote (na raiz do `cumbuca-api`):

```bash
dotnet publish src/CumbucaSaas.API -c Release -o publish
```

Compacte **o conteúdo** da pasta `publish` (os arquivos na raiz do zip, não a pasta):

```powershell
Compress-Archive -Path publish\* -DestinationPath cumbuca-api.zip -Force
```

Lambda → **Criar função** → *Criar do zero*

- Nome: `CumbucaApi`
- Tempo de execução: **.NET 8 (C#/F#/PowerShell)**
- Perfil de execução: **Usar um perfil existente → `LabRole`**
- **Criar função**

Depois de criada:

1. **Código → Fazer upload de → arquivo .zip** → `cumbuca-api.zip`.
2. **Configurações do tempo de execução → Editar** → Manipulador: `CumbucaSaas.API`.
3. **Configuração → Configuração geral → Editar** → Memória `1024` MB, Tempo limite `30` s.
4. **Configuração → Variáveis de ambiente → Editar**:

   | Chave | Valor |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `Cors__Origens__0` | URL do Amplify, ex.: `https://staging.d1abc.amplifyapp.com` (sem barra no fim) |
   | `TempoReal__WebSocketUrl` | URL do WebSocket do passo 3 |

## 5. HTTP API (REST)

API Gateway → **Criar API** → **HTTP API** → *Criar*

- Integração: **Lambda** → `CumbucaApi`
- Nome: `CumbucaHttp`
- Rota: método **ANY**, caminho `/{proxy+}`
- Estágio: `$default` com implantação automática → **Criar**

**Não configure CORS no API Gateway**: quem responde o CORS é a própria API (`Cors__Origens__0`).

Copie a **URL de invocação** e teste no navegador: `https://yyyy.execute-api.us-east-1.amazonaws.com/api/dashboard` deve devolver o JSON.

## 6. Front (Amplify)

No `cumbuca-web`, edite `src/environments/environment.ts`:

```ts
apiUrl: 'https://yyyy.execute-api.us-east-1.amazonaws.com', // passo 5, sem barra no fim
wsUrl: 'wss://xxxx.execute-api.us-east-1.amazonaws.com/prod'  // passo 3
```

Faça push na branch conectada ao Amplify; ele roda o `ng build` (configuração de produção) sozinho.

No **Cognito → App client → Login pages**, confirme que a URL do Amplify (com `/` no fim) está nas URLs de retorno e de logout.

### Cognito: login com e-mail e senha (funcionários)

Estudantes entram pelo Google; funcionários, com e-mail e senha de contas que a própria API cria
(`POST /api/cadastro`). No User Pool:

1. **Fazer login** → o pool do Cumbuca usa **nome de usuário + e-mail** (e-mail como alias). A
   API cria cada funcionário com um nome de usuário gerado (`func_...`) e o e-mail verificado; a
   pessoa entra digitando o e-mail. (Essa opção só é escolhida na criação do pool.)
2. **Sign-up** → desligue o **auto-cadastro** (self-registration): só a API cria funcionários.
3. **Authentication methods → Password policy**: a API e o front seguem a política padrão
   (8 caracteres, maiúscula, minúscula, número e símbolo). Se mudar, ajuste `RegrasUsuario.cs`
   e `validacao.ts`.
4. **App client → Login pages → Identity providers**: marque **Cognito user pool** junto com o
   **Google**.

### Perfil do usuário (S3 + DynamoDB + rota /profile)

1. **S3 → Criar bucket**: nome `pedroqueiroz-cumbuca-perfil` (ou outro, e configure
   `Perfil__Bucket` na Lambda), região `us-east-1`, **Bloquear todo o acesso público** marcado.
   As fotos são lidas por links temporários que a API gera.
2. **DynamoDB → Criar tabela**: nome `pedroqueiroz-message-2026.2`, chave de partição `id`
   (**String**), sem chave de classificação, capacidade **sob demanda**.
3. **API Gateway → `CumbucaHTTP` → Rotas → Criar**: `GET /profile` e `PUT /profile`, cada uma
   com a integração da Lambda `CumbucaApi` (a mesma do `ANY /{proxy+}`). O preflight de CORS
   (`OPTIONS /profile`) continua indo para o `ANY /{proxy+}`; se o navegador acusar CORS no PUT,
   crie também `OPTIONS /profile` com a mesma integração.
4. Na Lambda e no Visual Studio, a API usa a `LabRole` / as credenciais do lab para o S3 e o
   DynamoDB, como no Cognito.

A API chama `AdminCreateUser`, `AdminSetUserPassword` e `AdminDeleteUser`:

- **Na Lambda**: a `LabRole` precisa dessas permissões do `cognito-idp`.
- **No Visual Studio**: a API usa as credenciais da AWS do computador. Copie as do Learner Lab
  (*AWS Details → AWS CLI → Show*) para `%USERPROFILE%\.aws\credentials`. Elas vencem a cada
  sessão do lab.

## 7. Testar tudo

1. Abra o front no Amplify e entre no dashboard: os dados vêm da API e aparece o selo **Ao vivo**.
2. Num terminal, conecte com o `wscat` e envie
   `{"action":"broadcast","mensagem":{"tipo":"dashboard-atualizado"}}`.
   O dashboard recarrega sozinho (dá para ver o novo `GET /api/dashboard` na aba Rede do navegador).

Quando o banco existir, o `PATCH /api/feedbacks/{id}/status` faz esse broadcast automaticamente.

## Se der erro

| Sintoma | Onde olhar / causa comum |
|---|---|
| WebSocket recusa a conexão (500) | CloudWatch → `/aws/lambda/CumbucaWebSocket`. Quase sempre `TABELA_CONEXOES` errada, tabela com outro nome ou função sem a LabRole. |
| `/api/dashboard` devolve 500 | CloudWatch → `/aws/lambda/CumbucaApi`. Confira o manipulador `CumbucaSaas.API` e se o zip tem os arquivos na raiz. |
| Erro de CORS no navegador | `Cors__Origens__0` diferente da URL do Amplify (https, sem barra no fim). |
| Primeira chamada demora uns segundos | Cold start do .NET no Lambda; as seguintes são rápidas. |
