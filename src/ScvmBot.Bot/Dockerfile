# Build stage — context must be the repository root so all projects are visible
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy project files first so restore is cached as a separate layer
COPY ScvmBot.sln ./
COPY src/ScvmBot.Bot/ScvmBot.Bot.csproj src/ScvmBot.Bot/
COPY src/ScvmBot.Games.MorkBorg/ScvmBot.Games.MorkBorg.csproj src/ScvmBot.Games.MorkBorg/
COPY src/ScvmBot.Modules/ScvmBot.Modules.csproj src/ScvmBot.Modules/
COPY src/ScvmBot.Modules.MorkBorg/ScvmBot.Modules.MorkBorg.csproj src/ScvmBot.Modules.MorkBorg/
COPY src/ScvmBot.Games.MorkBorg.Pdf/ScvmBot.Games.MorkBorg.Pdf.csproj src/ScvmBot.Games.MorkBorg.Pdf/

RUN dotnet restore src/ScvmBot.Bot/ScvmBot.Bot.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/ScvmBot.Bot/ScvmBot.Bot.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble
WORKDIR /app

# Run as the built-in non-root user from the .NET base image
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ScvmBot.Bot.dll"]
