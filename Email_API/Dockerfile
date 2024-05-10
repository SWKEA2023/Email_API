FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /source

COPY . .

# Restore as distinct layers
RUN dotnet restore "./Email_API.csproj" --disable-parallel

# Build and publish a release
RUN dotnet publish "./Email_API.csproj" -c release -o /app --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app ./
ENTRYPOINT ["dotnet", "Email_API.dll"]