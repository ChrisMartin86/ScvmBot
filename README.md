# ScvmBot

A Discord bot for tabletop RPG character generation with built-in support for **MÖRK BORG** and a modular architecture for adding game systems. Handles character creation, multi-character generation, and PDF export.

## Features

### Discord Integration
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
- **Multi-character generation** — Generate up to 4 characters in a single command, delivered as a downloadable ZIP file
- **200+ reference data entries** — Armour, weapons, spells, items, names, descriptions, and vignettes in versioned JSON files

### Engineering
- **.NET 10** with nullable reference types enabled throughout
- **Six-project solution** — `ScvmBot.Bot` (Discord host), `ScvmBot.Cli` (CLI host), `ScvmBot.Modules` (shared contracts), `ScvmBot.Modules.MorkBorg` (module adapter), `ScvmBot.Games.MorkBorg` (game logic), `ScvmBot.Games.MorkBorg.Pdf` (PDF rendering)
- **Modular game system architecture** — Game modules implement `IModuleRegistration` in an assembly named `ScvmBot.Modules.*` and are discovered automatically at startup via the dependency graph. Adding a game means adding projects and a project reference from the host — no configuration files or plugin manifests
- **459 tests** across four test projects covering generation logic, equipment flow, PDF mapping, option parsing, command handling, multi-character generation, and architectural constraints
- **Fail-fast module initialization** — Each `IModuleRegistration` loads required data during `InitializeAsync()`; missing files abort startup with a non-zero exit code
- **Testable command layer** — `ISlashCommandContext` interface decouples command handlers from sealed Discord.Net types, enabling full unit test coverage without Discord infrastructure
- **CLI host** — `scvmbot-cli` provides local character generation, file rendering, and benchmarking through the same module pipeline, with no Discord dependency
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

   ScvmBot uses .NET's standard configuration pipeline. Every setting can come from `appsettings.json`, environment variables, or command-line arguments. Environment variables take precedence over file values.

   For local development, copy and edit the example settings file:
   ```bash
   cp src/ScvmBot.Bot/appsettings.example.json src/ScvmBot.Bot/appsettings.json
   ```

3. **Run the bot**
   ```bash
   dotnet run --project src/ScvmBot.Bot
   ```

See the [Getting Started](https://scvmbot.com/getting-started) guide for the full configuration reference, including every available setting and its environment variable equivalent.

### Docker

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
```

The compose file maps convenience shell variables (like `DISCORD_TOKEN`) to the app's actual configuration keys (like `Discord__Token`). See `docker-compose.yml` for the full mapping.

## Commands

| Command | Description |
|---|---|
| `/generate morkborg character` | Generate a single MÖRK BORG character |
| `/generate morkborg character class:<name>` | Generate with a specific class (or `None` for classless) |
| `/generate morkborg character roll-method:4d6-drop-lowest` | Use heroic ability rolling (classless only) |
| `/generate morkborg character count:<1-4>` | Generate multiple characters in one command |
| `/hello` | Verify bot is online |

Characters are delivered via DM. In-channel replies confirm delivery.

## Implementing a New Game System

Adding a game system requires two projects, one required interface, and two optional ones. The MÖRK BORG module is the reference implementation — every pattern below has a working example in the codebase.

### Overview

| Concern | Required | Interface / Type | Example |
|---|---|---|---|
| Module registration | Yes | `IModuleRegistration` | `MorkBorgModuleRegistration` |
| Command handling | Yes | `IGameModule` | `MorkBorgModule` |
| Card rendering | Yes | `IResultRenderer` (Card) | `MorkBorgCharacterEmbedRenderer` |
| File rendering | No | `IResultRenderer` (File) | `MorkBorgCharacterPdfRenderer` |

### Step 1: Create the Projects

Create two projects under `src/`:

- **`ScvmBot.Games.YourGame/`** — Game logic, models, data loading. No dependency on `ScvmBot.Modules`. This project is pure game code.
- **`ScvmBot.Modules.YourGame/`** — Module adapter that bridges your game logic to the bot framework. References both `ScvmBot.Modules` and `ScvmBot.Games.YourGame`.

The `ScvmBot.Modules.` prefix is required — `ModuleBootstrapper` scans the dependency graph for assemblies matching this prefix and ignores everything else.

Add a project reference from `ScvmBot.Bot` (and `ScvmBot.Cli` if desired) to `ScvmBot.Modules.YourGame`. This is the only wiring step — no registration files, no manifest.

### Step 2: Implement `IModuleRegistration`

This is the entry point the host calls at startup. It must have a **public parameterless constructor**.

```csharp
public sealed class YourGameModuleRegistration : IModuleRegistration
{
    public async Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration)
    {
        // Navigate to your config section. Convention: Modules:{ModuleName}
        var dataPath = configuration["Modules:YourGame:DataPath"]
                    ?? configuration["Modules:DataPath"];

        // Load reference data. Throw on failure — the host treats exceptions as
        // fatal startup errors and exits with a non-zero code.
        var data = await YourDataService.LoadAsync(dataPath);

        // Return a registration callback. The host calls this once to populate DI.
        return services =>
        {
            services.AddSingleton(data);
            services.AddSingleton<IGameModule, YourGameModule>();
            services.AddSingleton<IResultRenderer, YourCardRenderer>();
            // Optional: file renderer for PDF/image export
            // services.AddSingleton<IResultRenderer, YourPdfRenderer>();
        };
    }
}
```

Key rules:
- The async portion (`InitializeAsync`) runs before DI is built. Load data, validate files, and fail fast here.
- The returned `Action<IServiceCollection>` registers everything the module needs.
- Register exactly one `IGameModule` implementation. Register one or more `IResultRenderer` implementations.

### Step 3: Implement `IGameModule`

This is the runtime contract the bot uses to build commands and dispatch generation requests.

```csharp
public sealed class YourGameModule : IGameModule
{
    private readonly YourCharacterGenerator _generator;

    public YourGameModule(YourCharacterGenerator generator, YourDataService data)
    {
        _generator = generator;
        // Build subcommand definitions. These become /generate yourgame <subcommand>.
        SubCommands = YourCommandDefinition.BuildSubCommands(data);
    }

    public string Name => "Your Game";       // Display name shown in the command description
    public string CommandKey => "yourgame";  // Becomes /generate yourgame — must be unique

    public IReadOnlyList<SubCommandDefinition> SubCommands { get; }

    public Task<GenerateResult> HandleGenerateCommandAsync(
        string subCommand,
        IReadOnlyDictionary<string, object?> options,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!string.Equals(subCommand, "character", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unknown subcommand '{subCommand}'.");

        var character = _generator.Generate(options);

        return Task.FromResult<GenerateResult>(
            new GenerationBatch<YourCharacter>(new[] { character }));
    }
}
```

#### Command Definitions

Define your command structure using the transport-agnostic record types. These are mapped to Discord slash command options by `DiscordCommandAdapter` and to CLI arguments by the CLI host — your module never touches Discord types.

```csharp
public static class YourCommandDefinition
{
    public static IReadOnlyList<SubCommandDefinition> BuildSubCommands(YourDataService data)
    {
        return new[]
        {
            new SubCommandDefinition("character", "Generate a random character", new CommandOptionDefinition[]
            {
                new("difficulty",
                    "Starting difficulty level",
                    CommandOptionType.String, Required: false,
                    Choices: new[] { new CommandChoice("Easy", "easy"), new CommandChoice("Hard", "hard") }),

                new("count",
                    "Number of characters to generate",
                    CommandOptionType.Integer, Required: false, MinValue: 1,
                    Role: CommandOptionRole.GenerationCount)
            })
        };
    }
}
```

`CommandOptionRole.GenerationCount` tells transport hosts this option controls batch size. The Discord host uses it to cap the value at its transport-specific maximum (currently 4) so the Discord UI enforces the limit before the command reaches the handler.

#### Returning Results

All generation methods must return a `GenerationBatch<TCharacter>` where `TCharacter` is your game-specific model type. The batch must contain at least one character — the constructor throws `ArgumentException` on empty lists.

For multi-character support, parse the `count` option and generate multiple characters:

```csharp
var count = ParseCount(options); // your parser; default to 1 if missing
var characters = Enumerable.Range(0, count).Select(_ => _generator.Generate()).ToList();
var groupName = count > 1 ? GenerateGroupName(characters) : null;

return new GenerationBatch<YourCharacter>(characters.AsReadOnly(), groupName);
```

### Step 4: Implement Renderers

Renderers convert a `GenerateResult` into output the host can deliver. Each renderer declares what result type and output format it handles.

#### Card Renderer (Required)

Every module must register at least one card renderer. The card output is what appears as a Discord embed or CLI text output.

```csharp
public sealed class YourCardRenderer : IResultRenderer
{
    public Type ResultType => typeof(GenerationBatch<YourCharacter>);
    public OutputFormat Format => OutputFormat.Card;

    public bool CanRender(GenerateResult result) =>
        result is GenerationBatch<YourCharacter>;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not GenerationBatch<YourCharacter> batch)
            throw new InvalidOperationException($"Cannot render {result.GetType().Name}.");

        var character = batch.Characters[0];
        return new CardOutput(
            Title: character.Name,
            Description: $"Level {character.Level} — HP {character.HitPoints}",
            Color: new CardColor(100, 150, 200),
            Fields: new[]
            {
                new CardField("Abilities", FormatAbilities(character)),
                new CardField("Equipment", FormatEquipment(character))
            });
    }
}
```

For multi-character results, check `batch.Characters.Count` and return either a detailed single-character card or a roster summary — see `MorkBorgCharacterEmbedRenderer.BuildRosterCard` for the pattern.

#### File Renderer (Optional)

If your game has PDF export, image generation, or any downloadable file output, add a file renderer. When a file renderer is registered, the bot automatically attaches the file alongside the card embed.

```csharp
public sealed class YourPdfRenderer : IResultRenderer
{
    public Type ResultType => typeof(GenerationBatch<YourCharacter>);
    public OutputFormat Format => OutputFormat.File;

    public bool CanRender(GenerateResult result) =>
        result is GenerationBatch<YourCharacter> && PdfTemplateExists();

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not GenerationBatch<YourCharacter> batch)
            throw new InvalidOperationException($"Cannot render {result.GetType().Name}.");

        if (batch.Characters.Count == 1)
        {
            var pdf = RenderPdf(batch.Characters[0]);
            return new FileOutput(pdf, $"{batch.Characters[0].Name}.pdf");
        }

        // Multiple characters: render each as a PDF, bundle into a ZIP
        var memberPdfs = batch.Characters
            .Select(c => (c.Name, PdfBytes: RenderPdf(c)))
            .ToList();
        var zipBytes = CharacterZipBuilder.CreateZip(memberPdfs);
        var zipName = CharacterZipBuilder.GenerateZipFileName(
            batch.GroupName ?? "characters");
        return new FileOutput(zipBytes, zipName);
    }
}
```

`CharacterZipBuilder` handles ZIP creation and filename sanitization — use it instead of rolling your own.

#### Renderer Rules

- Each `(ResultType, OutputFormat)` pair must have exactly one renderer. `RendererRegistry` validates this at startup.
- `CanRender` is a runtime guard — return `false` if a required resource is unavailable (e.g. missing PDF template) and the host will skip file rendering gracefully.

### Step 5: Add Tests

Create a test project `tests/ScvmBot.Games.YourGame.Tests/` for game logic tests and optionally `tests/ScvmBot.Modules.YourGame.Tests/` for module adapter tests. The shared test infrastructure in `ScvmBot.Tests.Shared` provides helpers like `SharedTestInfrastructure.GetRepositoryRoot()` for locating data files.

Test the module through the same pipeline the hosts use:

```csharp
[Fact]
public async Task Generate_ReturnsCharacterWithRequiredFields()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Modules:YourGame:DataPath"] = "/path/to/data"
        })
        .Build();
    var register = await new YourGameModuleRegistration().InitializeAsync(config);

    var services = new ServiceCollection();
    register(services);
    services.AddSingleton<RendererRegistry>();
    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    var provider = services.BuildServiceProvider();

    var module = provider.GetRequiredService<IGameModule>();
    var result = await module.HandleGenerateCommandAsync("character", new Dictionary<string, object?>());

    var batch = Assert.IsType<GenerationBatch<YourCharacter>>(result);
    Assert.False(string.IsNullOrWhiteSpace(batch.Characters[0].Name));
}
```

### Architecture Summary

```
Host (Bot or CLI)
  │
  ├── ModuleBootstrapper.DiscoverAndInitializeAsync()
  │     └── Scans for ScvmBot.Modules.* assemblies
  │     └── Calls IModuleRegistration.InitializeAsync() on each
  │     └── Returns DI registration callbacks
  │
  ├── DI Container
  │     ├── IGameModule instances (one per game system)
  │     ├── IResultRenderer instances (card + optional file per module)
  │     └── RendererRegistry (selects renderer by result type + format)
  │
  └── /generate command
        ├── Routes to IGameModule by CommandKey
        ├── Module returns GenerationBatch<T>
        ├── RendererRegistry.RenderCard() → CardOutput → Discord embed
        └── RendererRegistry.TryRenderFile() → FileOutput? → attachment
```

## Running Tests

```bash
dotnet test
```

Individual test projects:

```bash
dotnet test tests/ScvmBot.Bot.Tests
dotnet test tests/ScvmBot.Games.MorkBorg.Tests
dotnet test tests/ScvmBot.Games.MorkBorg.Pdf.Tests
dotnet test tests/ScvmBot.Cli.Tests
```

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| Discord.Net | 3.19.1 | Discord API |
| iText7 | 9.5.0 | PDF form filling |
| itext7.bouncy-castle-adapter | 9.5.0 | iText7 cryptography runtime requirement |
| Newtonsoft.Json | 13.0.4 | Version pin — prevents vulnerable transitive version via Discord.Net |
| Microsoft.Extensions.Hosting | 10.0.5 | DI / hosted service |
| Microsoft.Extensions.Logging | 10.0.5 | Structured logging |

## Artist Credit

Character sheet artwork used in the PDF export is by **Thomas Kolvenbag**.

The artwork is **not** covered by the MIT licence and is not included in the open-source distribution. All rights to the artwork remain with the artist.

## Licence

[MIT](LICENSE) © 2025 Christopher Martin

### MÖRK BORG Attribution

ScvmBot is an independent production by Christopher Martin and is not affiliated with Ockult Örtmästare Games or Stockholm Kartell. It is published under the [MÖRK BORG Third Party License](https://morkborg.com/license/).

MÖRK BORG is © 2019 Ockult Örtmästare Games and Stockholm Kartell.

See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for all third-party licence details.

