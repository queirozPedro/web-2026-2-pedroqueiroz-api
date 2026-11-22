using System.Security.Claims;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string CorsFront = "front";

var builder = WebApplication.CreateBuilder(args);

// Connection string do RDS em dev (Manage User Secrets). Registro explícito porque,
// quando o EF executa a API (Add-Migration / Update-Database), o carregamento
// automático dos User Secrets não acontece e a API cairia no localhost.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// No Lambda, as requisições da HTTP API (API Gateway) viram requisições do ASP.NET.
// Fora do Lambda (Visual Studio, local) esta linha não faz nada.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    // Botão "Authorize" do Swagger: cole o id_token do Cognito para testar as rotas protegidas.
    var esquema = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "ID token do Cognito (id_token), sem o prefixo Bearer.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    swagger.AddSecurityDefinition("Bearer", esquema);
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = [] });
});

// Autenticação: o front manda o ID token do Cognito (Google institucional ou e-mail e
// senha). A API valida emissor e audiência e, a cada requisição, registra o acesso,
// define o tenant e acrescenta o perfil: Google é estudante, e-mail e senha é funcionário.
var autenticacao = builder.Configuration.GetSection(AutenticacaoOptions.Secao);
builder.Services.Configure<AutenticacaoOptions>(autenticacao);
var opcoesAutenticacao = autenticacao.Get<AutenticacaoOptions>() ?? new AutenticacaoOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = opcoesAutenticacao.Authority;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = opcoesAutenticacao.Authority,
            // Só o ID token tem "aud" = client id (o access token do Cognito não tem).
            ValidAudience = opcoesAutenticacao.ClientId,
            NameClaimType = "email",
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Sem cadastro concluído (nome completo e data de nascimento), nenhuma rota de dados
    // responde: só as de /api/usuarios/me, que usam as policies Logado e ComPerfil.
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(Perfis.Funcionario, Perfis.Estudante)
        .RequireClaim(Perfis.ClaimCadastroCompleto, "true")
        .Build();
    options.AddPolicy(Perfis.Funcionario, policy => policy
        .RequireRole(Perfis.Funcionario)
        .RequireClaim(Perfis.ClaimCadastroCompleto, "true"));
    options.AddPolicy(Perfis.Estudante, policy => policy
        .RequireRole(Perfis.Estudante)
        .RequireClaim(Perfis.ClaimCadastroCompleto, "true"));
    options.AddPolicy(Perfis.ComPerfil, policy => policy.RequireRole(Perfis.Funcionario, Perfis.Estudante));
    options.AddPolicy(Perfis.Logado, policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsTransformation, UsuarioClaimsTransformation>();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

// Tenant da requisição: definido no RegistrarAcessoCommand, lido pelo EF e pelas queries.
builder.Services.AddScoped<TenantAtual>();
builder.Services.AddScoped<ITenantAtual>(sp => sp.GetRequiredService<TenantAtual>());

// Cadastro de funcionário: cria a conta de e-mail e senha no User Pool (credenciais da
// AWS pela cadeia padrão: LabRole na Lambda, arquivo de credenciais no Visual Studio).
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(opcoesAutenticacao.Regiao)));
builder.Services.AddScoped<IProvedorIdentidade, CognitoProvedorIdentidade>();

builder.Services.AddInfrastructure(builder.Configuration);

// Commands: MediatR + validação no pipeline.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddScoped<IValidator<AtualizarStatusFeedbackCommand>, AtualizarStatusFeedbackCommandValidator>();
builder.Services.AddScoped<IValidator<CriarCalendarioRefeicaoCommand>, CriarCalendarioRefeicaoCommandValidator>();
builder.Services.AddScoped<IValidator<AtualizarCalendarioRefeicaoCommand>, AtualizarCalendarioRefeicaoCommandValidator>();
builder.Services.AddScoped<IValidator<RemoverCalendarioRefeicaoCommand>, RemoverCalendarioRefeicaoCommandValidator>();
builder.Services.AddScoped<IValidator<DefinirCardapioCommand>, DefinirCardapioCommandValidator>();
builder.Services.AddScoped<IValidator<RegistrarProducaoCommand>, RegistrarProducaoCommandValidator>();
builder.Services.AddScoped<IValidator<FecharCaixaCommand>, FecharCaixaCommandValidator>();
builder.Services.AddScoped<IValidator<CadastrarEstudanteCommand>, CadastrarEstudanteCommandValidator>();
builder.Services.AddScoped<IValidator<AlterarVinculoEstudanteCommand>, AlterarVinculoEstudanteCommandValidator>();
builder.Services.AddScoped<IValidator<RegistrarConsumoCommand>, RegistrarConsumoCommandValidator>();
builder.Services.AddScoped<IValidator<CancelarConsumoCommand>, CancelarConsumoCommandValidator>();
builder.Services.AddScoped<IValidator<EnviarFeedbackCommand>, EnviarFeedbackCommandValidator>();
builder.Services.AddScoped<IValidator<CadastrarFuncionarioCommand>, CadastrarFuncionarioCommandValidator>();
builder.Services.AddScoped<IValidator<ConcluirCadastroCommand>, ConcluirCadastroCommandValidator>();

// Queries: leitura direta (Dapper), sempre filtrada pelo tenant da requisição.
var connectionString = builder.Configuration.GetConnectionString(DependencyInjection.ConnectionStringName)!;
builder.Services.AddScoped<IFeedbackQueries>(sp => new FeedbackQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<ICalendarioQueries>(sp => new CalendarioQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<IEstudanteQueries>(sp => new EstudanteQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<IConsumoQueries>(sp => new ConsumoQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<IDashboardQueries>(sp => new DashboardQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<IAuditoriaQueries>(sp => new AuditoriaQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));
builder.Services.AddScoped<IUsuarioQueries>(sp => new UsuarioQueries(connectionString, sp.GetRequiredService<ITenantAtual>()));

// Avisa o front, pela WebSocket API, quando algum command muda os dados.
builder.Services.Configure<TempoRealOptions>(builder.Configuration.GetSection(TempoRealOptions.Secao));
builder.Services.AddSingleton<INotificadorTempoReal, NotificadorWebSocket>();

var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsFront, policy => policy.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsFront);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
