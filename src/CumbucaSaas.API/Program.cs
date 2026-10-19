using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using CumbucaSaas.Infrastructure;
using FluentValidation;

const string CorsFront = "front";

var builder = WebApplication.CreateBuilder(args);

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
