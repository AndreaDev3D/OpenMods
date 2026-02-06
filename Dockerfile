# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
# This is done separately to leverage Docker cache
COPY ["OpenMods.Server/OpenMods.Server.csproj", "OpenMods.Server/"]
RUN dotnet restore "OpenMods.Server/OpenMods.Server.csproj"

# Copy the rest of the source code
COPY . .

# Build the project
WORKDIR "/src/OpenMods.Server"
RUN dotnet build "OpenMods.Server.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "OpenMods.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Enable running in container flag
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Default port, can be overridden by Koyeb/Docker
ENV PORT=8000
EXPOSE 8000

# Copy the published output from the publish stage
COPY --from=publish /app/publish .

# Create keys directory for Data Protection and set ownership
# Note: In .NET 8+, the default user is 'app'
USER root
RUN mkdir -p /app/keys && chown -R 1654:1654 /app/keys
USER app

# Set the entry point for the container
ENTRYPOINT ["dotnet", "OpenMods.Server.dll"]
