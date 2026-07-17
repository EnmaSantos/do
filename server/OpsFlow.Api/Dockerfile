FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/Directory.Build.props server/
COPY server/OpsFlow.Domain/OpsFlow.Domain.csproj server/OpsFlow.Domain/
COPY server/OpsFlow.Application/OpsFlow.Application.csproj server/OpsFlow.Application/
COPY server/OpsFlow.Infrastructure/OpsFlow.Infrastructure.csproj server/OpsFlow.Infrastructure/
COPY server/OpsFlow.Api/OpsFlow.Api.csproj server/OpsFlow.Api/
RUN dotnet restore server/OpsFlow.Api/OpsFlow.Api.csproj
COPY server/ server/
RUN dotnet publish server/OpsFlow.Api/OpsFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpsFlow.Api.dll"]
