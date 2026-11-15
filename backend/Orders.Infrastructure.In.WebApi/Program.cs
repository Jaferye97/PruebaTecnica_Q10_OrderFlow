using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Orders.Application;
using Orders.Infrastructure.In.WebApi.Endpoints;
using Orders.Infrastructure.In.WebApi.ErrorHandling;
using Orders.Infrastructure.Out.SqlServer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Reloj del sistema: los handlers dependen de TimeProvider (en tests se reemplaza)
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOrdersApplication();
builder.Services.AddSqlServerPersistence(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Errores no controlados -> ProblemDetails (BadHttpRequestException -> 400, resto -> 500)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Fuera de Development los fallos de binding devuelven 400 sin cuerpo.
// Así siempre lanzan BadHttpRequestException y pasan por GlobalExceptionHandler.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // /scalar
}

// Sin Code First (fase 03c): el esquema y los datos los crea db-init con db\init\*.sql

app.MapOrdersEndpoints();
app.MapProductsEndpoints();

// live: el proceso responde (sin checks). ready: además la BD está accesible.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(Orders.Infrastructure.Out.SqlServer.DependencyInjection.ReadyTag)
});

app.Run();
