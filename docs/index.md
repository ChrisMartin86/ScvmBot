---
layout: default
title: Home
---

<div class="hero" markdown="1">

# ScvmBot

A Discord bot for tabletop RPG character generation, starting with **MÖRK&nbsp;BORG** and designed to grow from there.

Roll up a character, get it delivered to your DMs, and get back to the table.

<div class="hero-links">
  <a href="https://github.com/ChrisMartin86/ScvmBot" class="btn">View on GitHub</a>
  <a href="{{ site.baseurl }}/getting-started" class="btn btn-outline">Get Started</a>
</div>

</div>

---

## What ScvmBot Does
{: #about}

ScvmBot is a Discord bot that automates tabletop RPG character creation. You run a slash command in your Discord server, and the bot generates a character following the rules of the game system you pick. The result gets sent to your DMs as a formatted card — and optionally as a fillable PDF character sheet.

It is built to be straightforward: no accounts, no web dashboards, no subscriptions. Just a bot that rolls dice and builds characters.

---

## Features
{: #features}

**Character generation**
Full rules-based character creation. Ability scores, equipment, class assignment, backstory — the bot handles the mechanics so you do not have to flip through books mid-session.

**Party generation**
Need a whole group? Generate multiple characters in one command and get them bundled in a downloadable ZIP with individual PDFs.

**Discord-native interaction**
Everything runs through Discord slash commands. Characters are delivered via DM with a clean in-channel confirmation. Responses are ephemeral where it makes sense, keeping your server tidy.

**PDF character sheets**
Generated characters can be exported to fillable PDF character sheets, ready to use at the table or print out.

**Per-guild settings** (optional)
Configure the bot to run in guild-specific mode for a single Discord server, or leave it unconfigured for global Discord-wide command registration. Settings are scoped per deployment instance.

**Open source**
MIT licensed. The code is public, the development is in the open, and contributions are welcome.

**Modular architecture**
The project is designed to be expanded from within the solution itself. New game systems are added as projects in the solution, implementing the `IGameModule` and `IModuleRegistration` interfaces and wiring in via project references. Each game module owns its command definitions, generation logic, and output behavior.

---

## Supported Systems
{: #systems}

### MÖRK BORG

ScvmBot currently supports [MÖRK BORG](https://morkborg.com/), the pitch-black apocalyptic fantasy RPG by Ockult Örtmästare Games and Stockholm Kartell.

The MÖRK BORG implementation includes:

- Ability score rolling and class assignment
- Equipment and inventory generation
- Vignette (backstory) generation
- Omens and scroll mechanics
- HP and alignment determination
- 100+ reference data entries (armor, weapons, spells, items, names, and more)
- PDF character sheet export

ScvmBot is an independent production and is not affiliated with Ockult Örtmästare Games or Stockholm Kartell. It is published under the [MÖRK BORG Third Party License](https://morkborg.com/license/).

### Other Systems

The architecture supports adding new game systems by creating projects within the solution that implement the `IGameModule` and `IModuleRegistration` interfaces. No additional systems are currently implemented, but the project is designed with expansion in mind. If there is a system you would like to see supported, [open an issue on GitHub](https://github.com/ChrisMartin86/ScvmBot/issues).

---

## Getting Started
{: #getting-started}

ScvmBot is a self-hosted bot — you run it on your own infrastructure. There is no public hosted instance at this time.

- **Source code:** [github.com/ChrisMartin86/ScvmBot](https://github.com/ChrisMartin86/ScvmBot)
- **Prerequisites:** .NET 10.0 SDK, a Discord bot token
- **Deployment:** Docker support included via `docker-compose.yml`

For full setup instructions, see the [Getting Started guide]({{ site.baseurl }}/getting-started).

To report bugs or request features, [open an issue on GitHub](https://github.com/ChrisMartin86/ScvmBot/issues).

For questions or general contact: [chris@scvmbot.com](mailto:chris@scvmbot.com)

---

## Project Status
{: #status}

ScvmBot is an early-stage project under active development. It works, but it is not finished.

Things to expect:

- Features may change or be reworked
- New systems and capabilities are planned but not guaranteed on any timeline
- The API surface (commands, options) may evolve
- Feedback, bug reports, and contributions are genuinely welcome

If you run into something broken or have an idea, [open an issue](https://github.com/ChrisMartin86/ScvmBot/issues) or [get in touch](mailto:chris@scvmbot.com).

---

## Legal & Policies
{: #policies}

- [Terms of Service]({{ site.baseurl }}/terms-of-service)
- [Privacy Policy]({{ site.baseurl }}/privacy)
- [License (MIT)]({{ site.baseurl }}/license)
- [Security Policy]({{ site.baseurl }}/security)
- [Third-Party Licenses]({{ site.baseurl }}/third-party-licenses)

---

## Links
{: #links}

- [GitHub Repository](https://github.com/ChrisMartin86/ScvmBot)
- [Contact: chris@scvmbot.com](mailto:chris@scvmbot.com)
