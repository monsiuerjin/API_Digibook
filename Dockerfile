# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["API_DigiBook.csproj", "./"]
RUN dotnet restore "API_DigiBook.csproj"

# Copy everything else and build the project
COPY . .
RUN dotnet build "API_DigiBook.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "API_DigiBook.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port (Render will override with $PORT anyway)
EXPOSE 80
EXPOSE 443

# Environment variables for Render
# This makes ASP.NET Core listen to the port provided by Render's $PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-80}

# Fix inotify limit error by using polling and disabling reload on change
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

# Entry point for the application
ENTRYPOINT ["dotnet", "API_DigiBook.dll"]
