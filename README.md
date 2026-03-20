# ScvmBot

A Discord bot for tabletop RPG character generation with built-in support for **MÖRK BORG** and an extensible plugin architecture for other game systems. Handles character creation, party generation, and PDF export.

## Features

### Core Discord Integration
- **Slash command framework** — `/generate` command with automatic game system routing; new systems register themselves via DI with no changes required to the dispatcher
- **DM delivery** — Characters sent via DM; in-channel confirmation keeps channels clean
- **Guild-scoped commands** — Command registration can be targeted to specific guilds for near-instant propagation (~15 seconds); omit guild IDs for global registration (~1 hour)
- **Ephemeral responses** — Private follow-ups visible only to the requesting user

### MÖRK BORG Character Generation
- Full implementation of the MÖRK BORG ruleset:
  - Ability score rolling (3d6 standard; 4d6 drop-lowest heroic mode for classless characters)
  - Character class assignment (6 official classes + classless)
  - Equipment, inventory, and starting container generation
  - Vignette (backstory) generation
  - Omens, scrolls, and HP determination
- **PDF export** — Generates a filled PDF character sheet using an official-layout template
- **Party generation** — 1–4 characters in a single command, delivered as a downloadable ZIP file
- **100+ reference data entries** — Armour, weapons, spells, items, names, descriptions, and vignettes in versioned JSON files

### Engineering
- **.NET 10** with nullable reference types enabled throughout
- **Three-project library split** — `ScvmBot.Games.MorkBorg` (pure game logic), `ScvmBot.Games.MorkBorg.Pdf` (PDF rendering), `ScvmBot.Bot` (Discord host)
- **420 tests** across four test projects — character generation logic, equipment flow, PDF mapping, option parsing, command handling, and party building
- **Static factory pattern** — `MorkBorgReferenceDataService.CreateAsync()` atomically loads all required data at startup; missing files fail fast
- **Testable command layer** — `ISlashCommandContext` interface decouples command handlers from the sealed Discord.Net type, enabling full unit test coverage
- **Structured logging** — `Microsoft.Extensions.Logging` integration throughout
- **Docker ready** — `Dockerfile` and `docker-compose.yml` included

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Discord bot token — create one at the [Discord Developer Portal](https://discord.com/developers/applications)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/ChrisMartin86/ScvmBot.git
   cd ScvmBot
   ```

2. **Configure the bot**
   ```bash
   cp bot/appsettings.example.json bot/appsettings.json
   ```
   Edit `bot/appsettings.json`:
   ```json
   {
     "Discord": {
       "Token": "<YOUR_DISCORD_BOT_TOKEN_HERE>",
       "GuildIds": []
     },
     "Bot": {
       "SyncCommands": false
     }
   }
   ```

   | Setting | Description |
   |---|---|
   | `Token` | Your bot's Discord token (required) |
   | `GuildIds` | Leave empty `[]` for global registration (~1 hour to propagate), or list specific server IDs for instant guild-scoped registration (~15 seconds) |
   | `SyncCommands` | Set `true` on first run or after adding/changing commands to register them with Discord |

3. **Run the bot**
   ```bash
   dotnet run --project bot
   ```

### Docker

Set the required environment variable and start the container from the **repository root**:

```bash
export DISCORD_TOKEN=your_token_here
docker-compose up --build
```

Or with optional settings:

```bash
export DISCORD_TOKEN=your_token_here
export BOT_SYNC_COMMANDS=true          # register commands on this startup
docker-compose up --build
```

For guild-scoped command registration, add the target guild IDs to a `.env` file in the repository root alongside `DISCORD_TOKEN`:

```
DISCORD_TOKEN=your_token_here
Discord__GuildIds__0=123456789012345678
Discord__GuildIds__1=987654321098765432
```

`docker-compose.yml` uses `env_file:` to inject all `.env` entries directly into the container. `Discord__GuildIds__N` maps to the `Discord:GuildIds` array via .NET's double-underscore environment variable convention. Leave all `Discord__GuildIds__*` entries out for global registration.

The build context is the repository root so the multi-project solution resolves correctly.

## Project Structure

```
ScvmBot/
├── bot/                                   # Discord host — DI entry point
│   ├── Games/
│   │   ├── IGameSystem.cs                 # Plugin interface
│   │   ├── IGamePdfSupport.cs             # Optional PDF support interface
│   │   ├── CharacterGenerationResult.cs
│   │   └── MorkBorg/                      # MÖRK BORG bot adapter
│   │       ├── MorkBorgGameSystem.cs      # Implements IGameSystem + IGamePdfSupport
│   │       ├── MorkBorgServiceExtensions.cs # DI registration for MÖRK BORG services
│   │       ├── MorkBorgCommandDefinition.cs
│   │       ├── MorkBorgGenerateOptionParser.cs
│   │       ├── MorkBorgPartyOptionParser.cs
│   │       ├── CharacterCardBuilder.cs
│   │       └── PartyEmbedBuilder.cs
│   ├── Services/
│   │   ├── BotService.cs                  # Discord lifecycle management
│   │   ├── CommandRegistrar.cs            # Slash command registration with Discord API
│   │   ├── GenerateCommandHandler.cs      # /generate command routing
│   │   ├── GenerationDeliveryService.cs   # DM delivery and in-channel confirmation
│   │   ├── ResponseCardBuilder.cs         # Discord embed formatting
│   │   ├── PartyZipBuilder.cs             # ZIP archive creation
│   │   └── Commands/
│   │       ├── ISlashCommand.cs           # Slash command plugin interface
│   │       ├── ISlashCommandContext.cs    # Testable abstraction over SocketSlashCommand
│   │       ├── SocketSlashCommandContext.cs # Runtime adapter (excluded from coverage)
│   │       └── HelloCommand.cs
│   ├── Program.cs                         # DI wiring & entry point
│   ├── Dockerfile
│   └── appsettings.example.json
│
├── games/
│   ├── ScvmBot.Games.MorkBorg/            # Pure MÖRK BORG game logic (no Discord dependency)
│   │   ├── Data/
│   │   │   ├── armor.json
│   │   │   ├── classes.json
│   │   │   ├── descriptions.json
│   │   │   ├── items.json
│   │   │   ├── names.json
│   │   │   ├── spells.json
│   │   │   ├── vignettes.json
│   │   │   ├── weapons.json
│   │   │   └── DATA_REFERENCE.md          # Full data schema documentation
│   │   ├── Generation/
│   │   │   ├── CharacterGenerator.cs
│   │   │   ├── MorkBorgConstants.cs       # Shared string constants (tokens, modes, types)
│   │   │   ├── ScrollResolver.cs
│   │   │   ├── StartingGearTable.cs
│   │   │   ├── WeaponResolver.cs
│   │   │   └── ...
│   │   ├── Models/
│   │   └── Reference/
│   │       ├── ReferenceDataService.cs          # Static factory; loads all data at startup
│   │       └── ReferenceDataModels.cs
│   └── ScvmBot.Games.MorkBorg.Pdf/        # PDF rendering (iText7)
│       ├── MorkBorgPdfRenderer.cs
│       ├── PdfCharacterSheetExtensions.cs
│       ├── CharacterSheetMapper.cs
│       └── CharacterSheetData.cs
│
└── tests/
    ├── ScvmBot.Bot.Tests/                 # Command handling, party building, response cards
    ├── ScvmBot.Games.MorkBorg.Tests/      # Character generation, equipment flow, data integrity
    ├── ScvmBot.Games.MorkBorg.Pdf.Tests/  # PDF field mapping
    └── ScvmBot.Tests.Shared/              # Shared test helpers (DeterministicRandom, temp dirs)
```

## Commands

| Command | Description |
|---|---|
| `/generate morkborg character` | Generate a single MÖRK BORG character |
| `/generate morkborg character class:<name>` | Generate with a specific class (or `None` for classless) |
| `/generate morkborg character roll-method:4d6-drop-lowest` | Use heroic ability rolling (classless only) |
| `/generate morkborg party` | Generate a party of 4 characters |
| `/generate morkborg party size:<1-4>` | Generate a party of a specific size |
| `/hello` | Verify bot is online |

Characters and parties are delivered via DM. In-channel replies confirm delivery. Guild-channel invocations prompt the user to check their DMs; DM invocations deliver inline.

## Running Tests

```bash
dotnet test
```

Individual test projects:

```bash
dotnet test tests/ScvmBot.Bot.Tests
dotnet test tests/ScvmBot.Games.MorkBorg.Tests
dotnet test tests/ScvmBot.Games.MorkBorg.Pdf.Tests
```

## Adding a New Game System

1. Create a project under `games/` or add a folder under `bot/Games/YourSystem/`
2. Implement `IGameSystem` (and optionally `IGamePdfSupport`):
   ```csharp
   public class YourGameSystem : IGameSystem
   {
       public string Name => "Your Game";
       public string CommandKey => "yourgame";  // becomes /generate yourgame
   
       public SlashCommandOptionBuilder BuildCommandGroupOptions() { ... }
       public Task<GenerateResult> HandleGenerateCommandAsync(
           IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
           CancellationToken ct = default) { ... }
   }
   ```
3. Register in `Program.cs`:
   ```csharp
   services.AddSingleton<IGameSystem, YourGameSystem>();
   ```

The `/generate` dispatcher picks it up automatically — no other code changes required.

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| Discord.Net | 3.19.1 | Discord API |
| iText7 | 9.5.0 | PDF form filling |
| itext7.bouncy-castle-adapter | 9.5.0 | iText7 cryptography runtime requirement |
| Newtonsoft.Json | 13.0.3 | Version pin only — not the active serializer (reference data uses `System.Text.Json`); declared explicitly to prevent an older, potentially vulnerable version being selected via Discord.Net's transitive dependency |
| Microsoft.Extensions.Hosting | 10.0.5 | DI / hosted service |
| Microsoft.Extensions.Logging | 10.0.5 | Structured logging |

## MÖRK BORG Attribution

ScvmBot is an independent production by Christopher Martin and is not affiliated with Ockult Örtmästare Games or Stockholm Kartell. It is published under the [MÖRK BORG Third Party License](https://morkborg.com/license/).

MÖRK BORG is © 2019 Ockult Örtmästare Games and Stockholm Kartell.

See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for full third-party licence details.

## Licence

[MIT](LICENSE) © 2025 Christopher Martin

Third-party content is licensed separately — see [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

