# Build stage — context must be the repository root so all projects are visible
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy project files first so restore is cached as a separate layer
COPY ScvmBot.sln ./
COPY bot/ScvmBot.Bot.csproj bot/
COPY games/ScvmBot.Games.MorkBorg/ScvmBot.Games.MorkBorg.csproj games/ScvmBot.Games.MorkBorg/
COPY games/ScvmBot.Games.MorkBorg.Pdf/ScvmBot.Games.MorkBorg.Pdf.csproj games/ScvmBot.Games.MorkBorg.Pdf/

RUN dotnet restore bot/ScvmBot.Bot.csproj

# Copy source and publish
COPY bot/ bot/
COPY games/ games/
RUN dotnet publish bot/ScvmBot.Bot.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble
WORKDIR /app

# Run as the built-in non-root user from the .NET base image
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ScvmBot.Bot.dll"]
