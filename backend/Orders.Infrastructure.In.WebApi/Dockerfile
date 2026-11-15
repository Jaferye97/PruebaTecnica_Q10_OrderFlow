# Contexto de build: backend (ver docker-compose.yml)

# ── Build ──────────────────────────────────────────────
# BUILDPLATFORM: el SDK corre nativo y compila para la arquitectura destino (TARGETARCH)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Primero solo los .csproj: la capa del restore se cachea mientras no cambien las dependencias
COPY Directory.Build.props ./
COPY OrderFlow.Contracts/OrderFlow.Contracts.csproj OrderFlow.Contracts/
COPY Orders.Domain/Orders.Domain.csproj Orders.Domain/
COPY Orders.Application/Orders.Application.csproj Orders.Application/
COPY Orders.Infrastructure.Out.SqlServer/Orders.Infrastructure.Out.SqlServer.csproj Orders.Infrastructure.Out.SqlServer/
COPY Orders.Infrastructure.Out.RabbitMq/Orders.Infrastructure.Out.RabbitMq.csproj Orders.Infrastructure.Out.RabbitMq/
COPY Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj Orders.Infrastructure.In.WebApi/
# -a: restore/publish para un RID concreto; sin él se copian los nativos de win/osx (~30 MB)
RUN dotnet restore Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj -a $TARGETARCH

COPY OrderFlow.Contracts/ OrderFlow.Contracts/
COPY Orders.Domain/ Orders.Domain/
COPY Orders.Application/ Orders.Application/
COPY Orders.Infrastructure.Out.SqlServer/ Orders.Infrastructure.Out.SqlServer/
COPY Orders.Infrastructure.Out.RabbitMq/ Orders.Infrastructure.Out.RabbitMq/
COPY Orders.Infrastructure.In.WebApi/ Orders.Infrastructure.In.WebApi/
RUN dotnet publish Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj \
    -c Release -a $TARGETARCH --self-contained false -o /app --no-restore -p:UseAppHost=false
# libmsalruntime.so (36 MB) llega por SqlClient → Azure.Identity → MSAL.Broker. Solo lo carga
# la autenticación con broker de Entra ID; con login SQL nunca se usa. Si algún día se migra a
# Managed Identity / Entra ID, quitar esta línea.
RUN rm -f /app/libmsalruntime.so

# ── Runtime ────────────────────────────────────────────
# chiseled-extra = chiseled + ICU y tzdata. Microsoft.Data.SqlClient no soporta el modo de
# globalización invariante de la chiseled base. Sin shell, sin gestor de paquetes, no root (UID 1654)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Orders.Infrastructure.In.WebApi.dll"]
