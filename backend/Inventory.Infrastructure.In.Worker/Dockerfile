# Contexto de build: backend (ver docker-compose.yml)

# ── Build ──────────────────────────────────────────────
# BUILDPLATFORM: el SDK corre nativo y compila para la arquitectura destino (TARGETARCH)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Primero solo los .csproj: la capa del restore se cachea mientras no cambien las dependencias
COPY Directory.Build.props ./
COPY OrderFlow.Contracts/OrderFlow.Contracts.csproj OrderFlow.Contracts/
COPY Inventory.Domain/Inventory.Domain.csproj Inventory.Domain/
COPY Inventory.Application/Inventory.Application.csproj Inventory.Application/
COPY Inventory.Infrastructure.Out.SqlServer/Inventory.Infrastructure.Out.SqlServer.csproj Inventory.Infrastructure.Out.SqlServer/
COPY Inventory.Infrastructure.Out.RabbitMq/Inventory.Infrastructure.Out.RabbitMq.csproj Inventory.Infrastructure.Out.RabbitMq/
COPY Inventory.Infrastructure.In.Worker/Inventory.Infrastructure.In.Worker.csproj Inventory.Infrastructure.In.Worker/
# -a: restore/publish para un RID concreto; sin él se copian los nativos de win/osx (~30 MB)
RUN dotnet restore Inventory.Infrastructure.In.Worker/Inventory.Infrastructure.In.Worker.csproj -a $TARGETARCH

COPY OrderFlow.Contracts/ OrderFlow.Contracts/
COPY Inventory.Domain/ Inventory.Domain/
COPY Inventory.Application/ Inventory.Application/
COPY Inventory.Infrastructure.Out.SqlServer/ Inventory.Infrastructure.Out.SqlServer/
COPY Inventory.Infrastructure.Out.RabbitMq/ Inventory.Infrastructure.Out.RabbitMq/
COPY Inventory.Infrastructure.In.Worker/ Inventory.Infrastructure.In.Worker/
RUN dotnet publish Inventory.Infrastructure.In.Worker/Inventory.Infrastructure.In.Worker.csproj \
    -c Release -a $TARGETARCH --self-contained false -o /app --no-restore -p:UseAppHost=false
# libmsalruntime.so (36 MB) llega por SqlClient → Azure.Identity → MSAL.Broker. Solo lo carga
# la autenticación con broker de Entra ID; con login SQL nunca se usa. Si algún día se migra a
# Managed Identity / Entra ID, quitar esta línea.
RUN rm -f /app/libmsalruntime.so

# ── Runtime ────────────────────────────────────────────
# Worker: basta el runtime de .NET (sin ASP.NET). -extra por ICU, que SqlClient necesita. Sin shell y no root
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID

ENTRYPOINT ["dotnet", "Inventory.Infrastructure.In.Worker.dll"]
