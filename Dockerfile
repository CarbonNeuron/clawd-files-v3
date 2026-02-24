# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/ClawdFiles.Domain/ClawdFiles.Domain.csproj src/ClawdFiles.Domain/
COPY src/ClawdFiles.Application/ClawdFiles.Application.csproj src/ClawdFiles.Application/
COPY src/ClawdFiles.Infrastructure/ClawdFiles.Infrastructure.csproj src/ClawdFiles.Infrastructure/
COPY src/ClawdFiles.Web/ClawdFiles.Web.csproj src/ClawdFiles.Web/
RUN dotnet restore src/ClawdFiles.Web/ClawdFiles.Web.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/ClawdFiles.Web/ClawdFiles.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/clawdfiles.db"
ENV Storage__RootPath=/app/data/storage

EXPOSE 8080

ENTRYPOINT ["dotnet", "ClawdFiles.Web.dll"]
