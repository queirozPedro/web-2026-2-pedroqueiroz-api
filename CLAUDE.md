# cumbuca-api

API do Cumbuca (gestão do Restaurante Universitário da UFERSA): .NET 8, DDD + CQRS
(MediatR, FluentValidation, FluentResults), EF Core + Npgsql para escrita, Dapper para
leitura, PostgreSQL no RDS. Roda no AWS Lambda atrás de uma HTTP API (API Gateway).

**Antes de mudar qualquer regra, leia [`docs/REGRAS-DE-NEGOCIO.md`](docs/REGRAS-DE-NEGOCIO.md)**:
perfis e permissões, regras de cada entidade, fórmulas do dashboard, padrão de código e
pendências. Ao mudar uma regra, atualize o documento no mesmo commit.

- Deploy na AWS (Learner Lab, LabRole): [`aws/DEPLOY.md`](aws/DEPLOY.md)
- Front: repositório `cumbuca-web` (Angular), pasta irmã deste repositório

## Como trabalhar aqui

- A API é aberta e executada pelo **Visual Studio** (perfil `https`, https://localhost:7048).
- Migrations são geradas e aplicadas pelo **Package Manager Console** (comandos no documento de
  regras, seção 4). O banco é um só para local e AWS.
- Segredos (connection string, e-mails de funcionário de teste) ficam nos **User Secrets** ou nas
  variáveis da Lambda, nunca em arquivo versionado.
- Branch de trabalho: `staging`. A `main` recebe o merge.
