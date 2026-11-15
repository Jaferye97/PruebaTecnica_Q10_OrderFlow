using Inventory.Application;
using Inventory.Infrastructure.In.Worker.Messaging;
using Inventory.Infrastructure.Out.SqlServer;

var builder = Host.CreateApplicationBuilder(args);

// Reloj del sistema: los handlers dependen de TimeProvider (en tests se reemplaza)
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddInventoryApplication();
builder.Services.AddSqlServerPersistence(builder.Configuration); // fail-fast si falta ConnectionStrings:InventoryDb
builder.Services.AddRabbitMqMessaging(builder.Configuration);    // ValidateOnStart de RabbitMq__*

builder.Services.AddHostedService<OrderCreatedConsumer>();

var host = builder.Build();
host.Run();
