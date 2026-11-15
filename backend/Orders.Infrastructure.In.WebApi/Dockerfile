FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: el restore queda en caché mientras no cambien las dependencias
COPY Directory.Build.props ./
COPY Orders.Domain/Orders.Domain.csproj Orders.Domain/
COPY Orders.Application/Orders.Application.csproj Orders.Application/
COPY Orders.Infrastructure.Out.SqlServer/Orders.Infrastructure.Out.SqlServer.csproj Orders.Infrastructure.Out.SqlServer/
COPY Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj Orders.Infrastructure.In.WebApi/
RUN dotnet restore Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj

COPY . .
RUN dotnet publish Orders.Infrastructure.In.WebApi/Orders.Infrastructure.In.WebApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Orders.Infrastructure.In.WebApi.dll"]
