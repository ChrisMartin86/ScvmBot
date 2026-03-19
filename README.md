# ScvmBot

A Discord bot for tabletop RPG character generation with built-in support for **MÖRK BORG** and an extensible plugin architecture for other game systems. Handles complex character creation workflows, party generation, PDF export, and guild-specific settings.

## Features

### Core Discord Integration
- **Slash command framework** — `/generate` command with automatic game system routing
- **DM delivery** — Generated characters sent via DM with in-channel confirmation
- **Guild settings** — Per-guild configuration and customization
- **Ephemeral responses** — Clean Discord UI with private confirmations

### Character Generation
- **MÖRK BORG system** — Full implementation of the MÖRK BORG character generation rules
  - Ability score rolling and character class assignment
  - Equipment and inventory generation
  - Vignette (backstory) generation
  - Omens and scroll mechanics
  - HP and alignment determination
- **Comprehensive game rules** — 100+ reference data entries (armor, weapons, spells, items, names, etc.)
- **PDF support** — Export characters to fillable PDF character sheets
- **Party generation** — Create multiple characters in one command, bundled in a downloadable ZIP file
- **Pluggable architecture** — Implement `IGameSystem` to add new TTRPG systems

### Development & Testing
- **.NET 10.0** — Modern C# with nullable reference types
- **Comprehensive test suite** — 30+ test classes covering character generation logic, party building, PDF generation, and option parsing
- **Structured logging** — Microsoft.Extensions.Logging integration
- **Docker ready** — Dockerfile and docker-compose.yml for containerized deployment

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- Discord bot token (see [Discord Developer Portal](https://discord.com/developers/applications))
- (Optional) Configured PDF template for character sheets

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/ChrisMartin86/ScvmBot.git
   cd ScvmBot
   ```

2. **Configure Discord bot settings**
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

   **Configuration guide:**
   - `Token`: Your bot's Discord token (required)
   - `GuildId`: Optional. Leave empty for global command registration, or enter a Discord server ID (as a string) for guild-specific registration
     - **Empty or `"0"`**: Commands register globally across Discord (takes ~1 hour to propagate)
     - **Valid ID**: Commands register only in that guild (takes ~15 seconds)

3. **Run the bot**
   ```bash
   dotnet run --project bot
   ```

### Docker Deployment

```bash
docker-compose up --build
```

## Project Structure

```
ScvmBot/
├── bot/
│   ├── Data/
│   │   └── MorkBorg/
│   │       ├── armor.json         # Armor reference data
│   │       ├── classes.json       # Character classes
│   │       ├── weapons.json       # Weapons and gear
│   │       ├── spells.json        # Scroll/spell mechanics
│   │       ├── items.json         # Miscellaneous items
│   │       ├── names.json         # Character name generation
│   │       ├── descriptions.json  # Vignette descriptions
│   │       ├── vignettes.json     # Backstory templates
│   │       ├── character_sheet.pdf # PDF template
│   │       └── DATA_REFERENCE.md  # Data documentation
│   ├── Games/
│   │   ├── IGameSystem.cs         # Game system plugin interface
│   │   ├── CharacterGenerationResult.cs
│   │   └── MorkBorg/              # MÖRK BORG implementation
│   │       ├── CharacterGenerator.cs
│   │       ├── CharacterCardBuilder.cs
│   │       ├── ReferenceDataService.cs
│   │       ├── VignetteGenerator.cs
│   │       ├── MorkBorgCommandDefinition.cs
│   │       └── ...
│   ├── Models/
│   │   ├── ICharacter.cs          # Character interface
│   │   ├── GuildSettings.cs
│   │   └── MorkBorg/
│   │       └── MorkBorgCharacter.cs
│   ├── Services/
│   │   ├── BotService.cs          # Discord lifecycle management
│   │   ├── GenerateCommandHandler.cs  # Command routing
│   │   ├── ResponseCardBuilder.cs     # Discord embed formatting
│   │   ├── PartyZipBuilder.cs         # ZIP archive creation
│   │   └── Commands/
│   │       ├── ISlashCommand.cs       # Command plugin interface
│   │       └── HelloCommand.cs        # Example command
│   ├── Program.cs                 # DI configuration & entry point
│   ├── Dockerfile
│   └── appsettings.example.json
├── tests/
│   ├── ScvmBot.Bot.Tests/         # Framework and integration tests
│   └── ScvmBot.Games.MorkBorg.Tests/  # Game logic unit tests
├── docker-compose.yml
├── ScvmBot.sln
└── LICENSE
```

## Commands

- `/generate morkborg character` — Generate a single MÖRK BORG character
- `/generate morkborg party [count]` — Generate a party of characters (1-10)
- `/hello` — Test bot connectivity

Characters are sent via DM. In-channel responses confirm delivery and provide download links for generated files.

## Adding a New Game System

1. Create a new directory under `bot/Games/YourSystem/`
2. Implement `IGameSystem` interface:
   ```csharp
   public class YourGameSystem : IGameSystem
   {
       public string Name => "Your Game";
       public string CommandKey => "yourgame";
       public bool SupportsPdf => true;
       
       public SlashCommandOptionBuilder BuildCommandGroupOptions() { ... }
       public Task<GenerateResult> HandleGenerateCommandAsync(...) { ... }
   }
   ```
3. Register in `Program.cs`:
   ```csharp
   services.AddSingleton<IGameSystem, YourGameSystem>();
   ```
4. The system will automatically appear under `/generate yourgame`

## Development

### Running Tests
```bash
dotnet test
```

### Build Solution
```bash
dotnet build ScvmBot.sln
```

### Project Dependencies
- **Discord.Net** (3.19.1) — Discord API integration
- **iText7** (9.5.0) — PDF generation
- **Microsoft.Extensions.*** (10.0.5) — Dependency injection, configuration, logging, hosting

## MÖRK BORG Attribution

ScvmBot is an independent production by Christopher Martin and is not affiliated
with Ockult Örtmästare Games or Stockholm Kartell. It is published under the
[MÖRK BORG Third Party License](https://morkborg.com/license/).

MÖRK BORG is © 2019 Ockult Örtmästare Games and Stockholm Kartell.

See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for full details.

## License

[MIT](LICENSE) © 2025 Christopher Martin

Third-party content is licensed separately — see [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).
