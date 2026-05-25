# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app

# Copy project files
COPY ["MyDiscordApp.Api/MyDiscordApp.Api.csproj", "MyDiscordApp.Api/"]
COPY ["MyDiscordApp.Application/MyDiscordApp.Application.csproj", "MyDiscordApp.Application/"]
COPY ["MyDiscordApp.Bot/MyDiscordApp.Bot.csproj", "MyDiscordApp.Bot/"]
COPY ["MyDiscordApp.Domain/MyDiscordApp.Domain.csproj", "MyDiscordApp.Domain/"]
COPY ["MyDiscordApp.Infrastructure/MyDiscordApp.Infrastructure.csproj", "MyDiscordApp.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MyDiscordApp.Bot/MyDiscordApp.Bot.csproj"

# Copy all source files
COPY . .

# Build the application
RUN dotnet build "MyDiscordApp.Bot/MyDiscordApp.Bot.csproj" -c Release -o /app/build

# Publish stage
FROM builder AS publish
RUN dotnet publish "MyDiscordApp.Bot/MyDiscordApp.Bot.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Copy appsettings files explicitly
COPY MyDiscordApp.Bot/appsettings*.json ./

# Set environment variables
ENV DOTNET_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "MyDiscordApp.Bot.dll"]
