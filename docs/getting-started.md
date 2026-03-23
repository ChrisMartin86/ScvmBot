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

### 2. Configure the bot

ScvmBot uses .NET's standard configuration pipeline. Every setting can come from `appsettings.json`, environment variables, or command-line arguments. **Environment variables take precedence over file values.**

For local development, copy and edit the example settings file:

```bash
cp src/ScvmBot.Bot/appsettings.example.json src/ScvmBot.Bot/appsettings.json
```

### Configuration Reference

| `appsettings.json` key | Environment variable | Required | Description |
|---|---|---|---|
| `Discord:Token` | `Discord__Token` | Yes | Your bot's Discord token from the [Discord Developer Portal](https://discord.com/developers/applications) |
| `Discord:GuildIds` | `Discord__GuildIds__0`, `__1`, … | No | Guild IDs for guild-scoped registration (~15 s). Omit for global registration (~1 hour). |
| `Bot:SyncCommands` | `Bot__SyncCommands` | No | Set `true` on first run or after changing commands to register them with Discord (default: `false`) |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | No | Logging level (default: `Information`) |
| `Logging:LogLevel:Microsoft` | `Logging__LogLevel__Microsoft` | No | Microsoft library log level (default: `Warning`) |
| `Logging:LogLevel:Discord` | `Logging__LogLevel__Discord` | No | Discord.Net log level (default: `Information`) |

**Guild vs. Global Registration:**
- **Global** (empty `GuildIds`): Commands are registered across all Discord servers. Changes take ~1 hour to propagate.
- **Guild-specific** (one or more IDs in `GuildIds`): Commands register only in those servers. Changes propagate in ~15 seconds. Useful for testing and development.

### 3. Run the bot

```bash
dotnet run --project src/ScvmBot.Bot
```

---

## Docker

The Dockerfile produces a standard .NET application image. Provide configuration however your environment supports it — environment variables, mounted config files, orchestrator secrets, etc.

A `docker-compose.yml` is included as an example. To use it from the **repository root**:

```bash
export DISCORD_TOKEN=your_token_here
docker compose up --build
```

The compose file reads an optional `.env` file for additional settings:

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
| `/generate morkborg character count:<1-4>` | Generate multiple characters in one command |
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

The project is designed to be expanded from within the solution itself — not as a drop-in plugin system. To add a new TTRPG system:

1. Create a game logic project under `src/` (e.g., `src/ScvmBot.Games.YourSystem/`)
2. Create a module adapter project under `src/` named **`ScvmBot.Modules.YourSystem`** — this exact prefix is required; only assemblies named `ScvmBot.Modules.*` are discovered at startup
3. Implement the `IGameModule` and `IModuleRegistration` interfaces
4. Add a project reference from `ScvmBot.Bot` (and/or `ScvmBot.Cli`) to the module adapter project

> **Important:** The assembly name must start with `ScvmBot.Modules.`. Assemblies with any other naming pattern will be silently ignored by the module discovery system, even if they implement `IModuleRegistration`.

The host discovers `IModuleRegistration` implementations from referenced assemblies at startup. The new system will appear under `/generate yoursystem`. See the MÖRK BORG implementation across `src/ScvmBot.Games.MorkBorg/` and `src/ScvmBot.Modules.MorkBorg/` as a reference.
