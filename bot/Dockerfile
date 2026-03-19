# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble
WORKDIR /app

# Run as the built-in non-root user from the .NET base image
USER app

# Copy published app
COPY --from=build /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "ScvmBot.Bot.dll"]
