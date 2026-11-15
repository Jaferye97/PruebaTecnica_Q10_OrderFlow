using Orders.Infrastructure.Out.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServerPersistence(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "OrderFlow API");

// Verifica la conexión a SQL Server (health check registrado en AddSqlServerPersistence)
app.MapHealthChecks("/health");

app.Run();