using Amazon.Lambda.AspNetCoreServer.Hosting;
using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Infrastructure;
using FluentValidation;

const string CorsFront = "front";

var builder = WebApplication.CreateBuilder(args);

// No Lambda, as requisições da HTTP API (API Gateway) viram requisições do ASP.NET.
// Fora do Lambda (Visual Studio, local) esta linha não faz nada.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

// Commands: MediatR + validação no pipeline.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddScoped<IValidator<AtualizarStatusFeedbackCommand>, AtualizarStatusFeedbackCommandValidator>();

// Queries: leitura direta (Dapper). O dashboard ainda usa dados fictícios.
var connectionString = builder.Configuration.GetConnectionString(DependencyInjection.ConnectionStringName)!;
builder.Services.AddScoped<IFeedbackQueries>(_ => new FeedbackQueries(connectionString));
builder.Services.AddSingleton<IDashboardQueries, DashboardMockQueries>();

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
app.MapControllers();

app.Run();
