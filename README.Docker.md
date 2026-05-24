# Docker Setup Guide

## Prerequisites

- Docker
- Docker Compose

## Quick Start

1. **Create environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` file with your credentials:**
   ```bash
   DISCORD_TOKEN=your_discord_bot_token_here
   DISCORD_GUILD_ID=your_guild_id_here
   DISCORD_BOT_SEED_CHANNEL_ID=your_seed_channel_id_here
   DISCORD_CHAT_CHANNEL_ID=your_chat_channel_id_here
   GEMINI_TOKEN=your_gemini_token_here
   ```

3. **Start the services:**
   ```bash
   docker-compose up -d
   ```

## Service Details

### Lavalink Service
- **Image:** lavalink/lavalink:latest
- **Port:** 2333
- **Health Check:** Automatically waits for Lavalink to be ready
- **Volume:** `./Lavalink/application.yml` - Configuration file

### Bot Service
- **Container:** discord-bot
- **Port:** Internal use only (communicates with Lavalink)
- **Dependencies:** Waits for Lavalink to be healthy
- **Environment Variables:** Loaded from `.env` file

## Commands

### View logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f bot
docker-compose logs -f lavalink
```

### Stop services
```bash
docker-compose down
```

### Stop and remove volumes
```bash
docker-compose down -v
```

### Rebuild images
```bash
docker-compose build --no-cache
```

### Restart services
```bash
docker-compose restart
```

## Network

Both services communicate through a Docker network named `discord-network`. The bot connects to Lavalink using the hostname `lavalink:2333`.

## Lavalink Configuration

Edit `./Lavalink/application.yml` to modify:
- Server port
- Password
- Plugins
- Audio sources (YouTube, SoundCloud, etc.)

## Troubleshooting

### Bot can't connect to Lavalink
- Ensure Lavalink is healthy: `docker-compose logs lavalink`
- Check if both services are in the same network: `docker network ls`

### Port already in use
- Modify the port mapping in `docker-compose.yml` (change `2333:2333` to `xxxx:2333`)

### Build fails
- Clear docker cache: `docker-compose build --no-cache`
- Check .NET SDK compatibility with Dockerfile
