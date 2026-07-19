# Use the official .NET 10 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY EStore/*.csproj ./EStore/
RUN dotnet restore EStore/EStore.csproj

# Copy everything else and build the app
COPY . ./
RUN dotnet publish EStore/EStore.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose container port (ASP.NET Core 10 default)
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "EStore.dll"]
