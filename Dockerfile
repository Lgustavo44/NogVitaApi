FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json nuget.config ./
COPY src/NogVita.Domain/NogVita.Domain.csproj src/NogVita.Domain/
COPY src/NogVita.Application/NogVita.Application.csproj src/NogVita.Application/
COPY src/NogVita.Infrastructure/NogVita.Infrastructure.csproj src/NogVita.Infrastructure/
COPY src/NogVita.Api/NogVita.Api.csproj src/NogVita.Api/
RUN dotnet restore src/NogVita.Api/NogVita.Api.csproj

COPY src/ src/
RUN dotnet publish src/NogVita.Api/NogVita.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "NogVita.Api.dll"]