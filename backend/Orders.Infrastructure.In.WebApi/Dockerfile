# Contexto de build: backend (ver docker-compose.yml)

# ── Build ──────────────────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Primero solo los .csproj: la capa del restore se cachea mientras no cambien las dependencias
COPY Directory.Build.props ./
COPY OrderFlow.Contracts/OrderFlow.Contracts.csproj OrderFlow.Contracts/
COPY Orders.Domain/Orders.Domain.csproj Orders.Domain/
COPY Orders.Application/Orders.Application.csproj Orders.Application/
COPY Orders.Infrastructure.Out.SqlServer/Orders.Infrastructure.Out.SqlServer.csproj Orders.Infrastructure.Out.SqlServer/
COPY Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj Orders.Infrastructure.In.WebApi/
RUN dotnet restore Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj -a $TARGETARCH

COPY OrderFlow.Contracts/ OrderFlow.Contracts/
COPY Orders.Domain/ Orders.Domain/
COPY Orders.Application/ Orders.Application/
COPY Orders.Infrastructure.Out.SqlServer/ Orders.Infrastructure.Out.SqlServer/
COPY Orders.Infrastructure.In.WebApi/ Orders.Infrastructure.In.WebApi/
RUN dotnet publish Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj \
    -c Release -a $TARGETARCH --self-contained false -o /app --no-restore -p:UseAppHost=false
RUN rm -f /app/libmsalruntime.so

# ── Runtime ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Orders.Infrastructure.In.WebApi.dll"]
