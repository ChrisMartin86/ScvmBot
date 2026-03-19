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
    "Token": "<YOUR_DISCORD_BOT_TOKEN>"
  }
}
```

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
