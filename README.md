# ScvmBot

A Discord bot for tabletop RPG character generation with built-in support for **MÖRK BORG** and a modular architecture for adding game systems. Handles character creation, multi-character generation, and PDF export.

## Features

### Core Discord Integration
- **Slash command framework** — `/generate` command with game-module routing; each game module owns its command definitions, option parsing, and output behavior
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
- **Multi-character generation** — generate up to 4 characters in a single command, delivered as a downloadable ZIP file
- **200+ reference data entries** — Armour, weapons, spells, items, names, descriptions, and vignettes in versioned JSON files

### Engineering
- **.NET 10** with nullable reference types enabled throughout
- **Six-project solution** — `ScvmBot.Bot` (Discord host), `ScvmBot.Cli` (CLI host for local generation), `ScvmBot.Modules` (shared module contracts and abstractions), `ScvmBot.Modules.MorkBorg` (MÖRK BORG module adapter — command definitions, option parsing, renderers), `ScvmBot.Games.MorkBorg` (game logic), `ScvmBot.Games.MorkBorg.Pdf` (PDF rendering)
- **Modular game system architecture** — game modules implement `IModuleRegistration` in an assembly named `ScvmBot.Modules.*` and are wired into the host via project references; only assemblies matching this prefix are discovered — adding a new game means adding projects to the solution and referencing them from the host — designed for in-project expansion, not external plugins
- **450 tests** across four test projects — character generation logic, equipment flow, PDF mapping, option parsing, command handling, and multi-character generation
- **Fail-fast module initialization** — each `IModuleRegistration` loads required data during `InitializeAsync()`; missing files abort startup with a non-zero exit code
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
   cp src/ScvmBot.Bot/appsettings.example.json src/ScvmBot.Bot/appsettings.json
   ```
   Edit `src/ScvmBot.Bot/appsettings.json`:
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
   dotnet run --project src/ScvmBot.Bot
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
├── src/
│   ├── ScvmBot.Bot/                      # Discord host — composition root
│   │   ├── Services/
│   │   │   ├── BotService.cs              # Discord lifecycle management
│   │   │   ├── CommandRegistrar.cs        # Slash command registration with Discord API
│   │   │   ├── GenerateCommandHandler.cs  # /generate command routing
│   │   │   ├── GenerationDeliveryService.cs # DM delivery and in-channel confirmation
│   │   │   ├── ResponseCardBuilder.cs     # Discord embed formatting
│   │   │   └── Commands/
│   │   │       ├── ISlashCommand.cs        # Slash command interface
│   │   │       ├── ISlashCommandContext.cs # Testable abstraction over SocketSlashCommand
│   │   │       ├── SocketSlashCommandContext.cs # Runtime adapter (excluded from coverage)
│   │   │       └── HelloCommand.cs
│   │   ├── Program.cs                     # Module discovery & entry point
│   │   ├── Dockerfile
│   │   └── appsettings.example.json
│   │
│   ├── ScvmBot.Cli/                       # CLI host for local generation
│   │   └── Program.cs
│   │
│   ├── ScvmBot.Modules/                   # Shared module contracts and abstractions
│   │   ├── IModuleRegistration.cs          # Discovery contract — async init + DI registration
│   │   ├── IGameModule.cs                 # Module contract — commands, generation, rendering
│   │   ├── GenerateResult.cs              # GenerateResult / GenerationBatch<T>
│   │   ├── IResultRenderer.cs             # Renderer interface
│   │   ├── RendererRegistry.cs            # Selects renderer by result type + format
│   │   ├── CommandDefinition.cs            # SubCommandDefinition, CommandOptionDefinition, CommandChoice
│   │   ├── RenderOutput.cs                # CardOutput / FileOutput discriminated union
│   │   ├── OutputFormat.cs                # Card, File
│   │   └── CharacterZipBuilder.cs         # ZIP archive creation for multi-character PDFs
│   │
│   ├── ScvmBot.Modules.MorkBorg/          # MÖRK BORG module adapter layer
│   │   ├── MorkBorgModule.cs              # Implements IGameModule
│   │   ├── MorkBorgModuleRegistration.cs  # IModuleRegistration — loads data, registers services
│   │   ├── MorkBorgCommandDefinition.cs   # Slash command option tree
│   │   ├── MorkBorgGenerateOptionParser.cs
│   │   ├── MorkBorgCharacterEmbedRenderer.cs
│   │   └── MorkBorgCharacterPdfRenderer.cs
│   │
│   ├── ScvmBot.Games.MorkBorg/            # MÖRK BORG game logic
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
│   │       ├── ReferenceDataService.cs    # Static factory; loads all data at startup
│   │       └── ReferenceDataModels.cs
│   │
│   └── ScvmBot.Games.MorkBorg.Pdf/        # PDF rendering (iText7)
│       ├── MorkBorgPdfRenderer.cs
│       ├── PdfCharacterSheetExtensions.cs
│       ├── CharacterSheetMapper.cs
│       └── CharacterSheetData.cs
│
└── tests/
    ├── ScvmBot.Bot.Tests/                 # Command handling, multi-character generation, response cards, architecture
    ├── ScvmBot.Cli.Tests/                 # CLI integration tests
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
| `/generate morkborg character count:<1-4>` | Generate multiple characters in one command |
| `/hello` | Verify bot is online |

Characters are delivered via DM. In-channel replies confirm delivery. Guild-channel invocations prompt the user to check their DMs; DM invocations deliver inline.

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

1. Create a game logic project under `src/` (e.g., `src/ScvmBot.Games.YourSystem/`) and a module adapter project (e.g., `src/ScvmBot.Modules.YourSystem/`)
2. Implement `IGameModule` in the module adapter:
   ```csharp
   public class YourGameModule : IGameModule
   {
       public string Name => "Your Game";
       public string CommandKey => "yourgame";  // becomes /generate yourgame

       public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = ...;
       public Task<GenerateResult> HandleGenerateCommandAsync(
           string subCommand,
           IReadOnlyDictionary<string, object?> options,
           CancellationToken ct = default) { ... }
   }
   ```
3. Implement `IModuleRegistration` in the module adapter (see `MorkBorgModuleRegistration` for the full pattern):
   ```csharp
   public sealed class YourGameModuleRegistration : IModuleRegistration
   {
       public async Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration)
       {
           // Load reference data; throw if missing — the host treats exceptions as fatal.
           var data = await LoadYourDataAsync(configuration);

           return services =>
           {
               services.AddSingleton<IGameModule>(new YourGameModule(data));
               services.AddSingleton<IResultRenderer, YourEmbedRenderer>();
           };
       }
   }
   ```
4. Add a project reference from `ScvmBot.Bot` (and/or `ScvmBot.Cli`) to your module adapter project. The host discovers `IModuleRegistration` implementations from referenced assemblies at startup.

The `/generate` dispatcher routes to modules by their `CommandKey`. Duplicate keys are rejected at startup with a clear error message.

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| Discord.Net | 3.19.1 | Discord API |
| iText7 | 9.5.0 | PDF form filling |
| itext7.bouncy-castle-adapter | 9.5.0 | iText7 cryptography runtime requirement |
| Newtonsoft.Json | 13.0.4 | Version pin only — not the active serializer (reference data uses `System.Text.Json`); declared explicitly to prevent an older, potentially vulnerable version being selected via Discord.Net's transitive dependency |
| Microsoft.Extensions.Hosting | 10.0.5 | DI / hosted service |
| Microsoft.Extensions.Logging | 10.0.5 | Structured logging |

## MÖRK BORG Attribution

ScvmBot is an independent production by Christopher Martin and is not affiliated with Ockult Örtmästare Games or Stockholm Kartell. It is published under the [MÖRK BORG Third Party License](https://morkborg.com/license/).

MÖRK BORG is © 2019 Ockult Örtmästare Games and Stockholm Kartell.

See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for full third-party licence details.

## Licence

[MIT](LICENSE) © 2025 Christopher Martin

Third-party content is licensed separately — see [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

