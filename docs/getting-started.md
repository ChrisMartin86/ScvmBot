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
    "GuildIds": []
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
| `GuildIds` | No | Leave empty for global registration, or specify one or more Discord server IDs for guild-specific registration |
| `SyncCommands` | No | Set to `true` to auto-sync commands on startup (default: `false`) |

**Guild vs. Global Registration:**
- **Global** (empty `GuildIds`): Commands are registered across all Discord servers. Changes take ~1 hour to propagate.
- **Guild-specific** (one or more IDs in `GuildIds`): Commands register only in those servers. Changes propagate in ~15 seconds. Useful for testing and development.

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

**Required:** `DISCORD_TOKEN` must be set before running. The simplest way is a `.env` file in the repository root:

```
DISCORD_TOKEN=your_token_here
```

All other settings are optional:

| `.env` key | Notes |
|---|---|
| `DISCORD_TOKEN` | **Required.** Your bot's Discord token |
| `BOT_SYNC_COMMANDS` | Set to `true` to register commands on startup (default: `true`) |
| `Discord__GuildIds__0` | Guild ID for guild-scoped registration (optional; repeat with `__1`, `__2`, … for multiple guilds) |
| `LOG_LEVEL_DEFAULT` | Logging level (default: `Information`) |

**How guild-scoped registration works:** variables in `.env` are injected directly into the container via `env_file:` in `docker-compose.yml`. `Discord__GuildIds__0=<id>` maps to `Discord:GuildIds[0]` in .NET's configuration system.

Example `.env` for guild-scoped registration:

```
DISCORD_TOKEN=your_token_here
BOT_SYNC_COMMANDS=true
Discord__GuildIds__0=123456789012345678
Discord__GuildIds__1=987654321098765432
```

Leave all `Discord__GuildIds__*` entries out to use global registration.

---

## Commands

Once the bot is running and added to your Discord server:

| Command | Description |
| ------- | ----------- |
| `/generate morkborg character` | Generate a single MÖRK BORG character |
| `/generate morkborg character class:<name>` | Generate with a specific class (or `None` for classless) |
| `/generate morkborg character roll-method:4d6-drop-lowest` | Heroic ability rolling (classless only) |
| `/generate morkborg party` | Generate a party of 4 characters |
| `/generate morkborg party size:<1-4>` | Generate a party of a specific size |
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

The bot uses a modular architecture with explicit host registration. To add a new TTRPG system:

1. Create a game logic project under `games/YourSystem/`
2. Create a module adapter project under `modules/ScvmBot.Modules.YourSystem/`
3. Implement the `IGameModule` interface
4. Register explicitly in `Program.cs`

The new system will appear under `/generate yoursystem`. See the MÖRK BORG implementation across `games/ScvmBot.Games.MorkBorg/` and `modules/ScvmBot.Modules.MorkBorg/` as a reference.
