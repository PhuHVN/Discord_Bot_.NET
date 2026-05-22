# MyDiscordApp

A Discord bot application built with .NET 8, designed to handle interactions, manage seeds/memes, and provide AI-powered responses using Gemini API.

## Project Structure

```
MyDiscordApp/
├── MyDiscordApp.Api/          # REST API layer
├── MyDiscordApp.Application/  # Business logic & services
├── MyDiscordApp.Bot/          # Discord bot implementation
├── MyDiscordApp.Domain/       # Domain models
└── MyDiscordApp.Infrastructure/ # External services & data access
```

## Features

- **Discord Bot**: Automated responses and interactions with Discord servers
- **REST API**: API endpoints for external integrations
- **AI Integration**: Gemini API integration for intelligent responses
- **Seed Management**: Manage and notify about seeds
- **Meme Service**: Meme handling and sharing functionality
- **Idle Messages**: Automated messages during idle periods

## Tech Stack

- **.NET 8**
- **C#**
- **Discord.Net** (for Discord integration)
- **Gemini API** (for AI responses)

## Getting Started

### Prerequisites

- .NET 8 SDK
- Discord Bot Token
- Gemini API Key

### Installation

1. Clone the repository
```bash
git clone <repository-url>
cd MyDiscordApp
```

2. Configure application settings
   - Update `appsettings.json` in `MyDiscordApp.Bot` and `MyDiscordApp.Api`
   - Add your Discord Bot Token and Gemini API Key

3. Build the project
```bash
dotnet build
```

4. Run the application
```bash
# Run Discord Bot
dotnet run --project MyDiscordApp.Bot

# Or run the API
dotnet run --project MyDiscordApp.Api
```

## Configuration

Application settings are managed through `appsettings.json` files:
- `MyDiscordApp.Bot/appsettings.json` - Bot configuration
- `MyDiscordApp.Api/appsettings.json` - API configuration

For development, use `appsettings.Development.json`

## Services

### Application Layer
- **IGeminiService**: AI response generation
- **IHandleTalkService**: Talk handling logic
- **IMemeService**: Meme management
- **ISeedService**: Seed management
- **ITalkService**: General talk service

### Infrastructure
- **GeminiService**: External Gemini API integration
- **MemeService**: Meme data management
- **SeedService**: Seed data management

### Bot Services
- **GeneralModule**: General Discord commands and interactions
- **IdleMessageService**: Idle state message handling
- **SeedNotifierService**: Seed notifications

## Development

### Project Dependencies
- MyDiscordApp.Api → MyDiscordApp.Application
- MyDiscordApp.Bot → MyDiscordApp.Application
- MyDiscordApp.Application → MyDiscordApp.Domain, MyDiscordApp.Infrastructure
- MyDiscordApp.Infrastructure → MyDiscordApp.Domain

### Adding New Features

1. Add interfaces in `MyDiscordApp.Application/Interface/`
2. Implement in appropriate service layer
3. Register in `DependencyInjection.cs`
4. Use in Bot or API layers

## License

This project is part of FPTU's LET_ME_WIN program.
