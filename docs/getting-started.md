---
layout: default
title: Getting Started
---

# Getting Started

ScvmBot is a self-hosted Discord bot. You run it on your own machine or server — there is no public hosted instance at this time.

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- A Discord bot token (from the [Discord Developer Portal](https://discord.com/developers/applications))
- (Optional) Docker, if you prefer containerized deployment

---

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/ChrisMartin86/ScvmBot.git
cd ScvmBot
```

### 2. Configure your Discord bot settings

```bash
cp bot/appsettings.example.json bot/appsettings.json
```

Edit `bot/appsettings.json` with your Discord bot token:

```json
{
  "Discord": {
    "Token": "<YOUR_DISCORD_BOT_TOKEN>",
    "GuildId": ""
  },
  "Bot": {
    "SyncCommands": false
  }
}
```

**Configuration options:**

| Setting | Required | Notes |
|---------|----------|-------|
| `Token` | Yes | Your bot's Discord token from the [Discord Developer Portal](https://discord.com/developers/applications) |
| `GuildId` | No | Leave empty for global registration, or specify a Discord server ID for guild-specific registration |
| `SyncCommands` | No | Set to `true` to auto-sync commands on startup (default: `false`) |

**Guild vs. Global Registration:**
- **Global** (empty `GuildId`): Commands are registered across all Discord servers. Changes take ~1 hour to propagate.
- **Guild-specific** (valid `GuildId`): Commands register only in that server. Changes propagate in ~15 seconds. Useful for testing and development.

### 3. Run the bot

```bash
dotnet run --project bot
```

---

## Docker Deployment

If you prefer Docker:

```bash
docker-compose up --build
```

The included `docker-compose.yml` and `Dockerfile` handle the build and runtime environment.

**Environment variables** you can configure:

| Variable | Required | Notes |
|----------|----------|-------|
| `DISCORD_TOKEN` | Yes | Your bot's Discord token |
| `DISCORD_GUILD_ID` | No | Leave empty for global registration, or specify a Discord server ID for guild-specific registration |
| `BOT_SYNC_COMMANDS` | No | Set to `true` to auto-sync commands (default: `true`) |
| `LOG_LEVEL_DEFAULT` | No | Logging level (default: `Information`) |

Example with environment variables:
```bash
DISCORD_TOKEN=your_token_here DISCORD_GUILD_ID=123456789 docker-compose up --build
```

Or create a `.env` file:
```bash
DISCORD_TOKEN=your_token_here
DISCORD_GUILD_ID=123456789
BOT_SYNC_COMMANDS=true
```

---

## Commands

Once the bot is running and added to your Discord server:

| Command | Description |
| ------- | ----------- |
| `/generate morkborg character` | Generate a single MÖRK BORG character |
| `/generate morkborg party [count]` | Generate a party of characters (1–10) |
| `/hello` | Test bot connectivity |

Characters are sent to your DMs. In-channel responses confirm delivery and provide download links for generated files.

---

## Reporting Issues

Found a bug or have a feature request?

- [Open an issue on GitHub](https://github.com/ChrisMartin86/ScvmBot/issues)
- Email: [chris@scvmbot.com](mailto:chris@scvmbot.com)

---

## Contributing

ScvmBot is open source under the MIT license. The project welcomes contributions — check the [GitHub repository](https://github.com/ChrisMartin86/ScvmBot) for details.

### Adding a new game system

The bot uses a plugin architecture. To add a new TTRPG system:

1. Create a directory under `bot/Games/YourSystem/`
2. Implement the `IGameSystem` interface
3. Register it in `Program.cs`

The new system will automatically appear under `/generate yoursystem`. See the MÖRK BORG implementation in `bot/Games/MorkBorg/` as a reference.
