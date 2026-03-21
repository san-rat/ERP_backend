# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj first (better caching)
COPY ProductService.csproj ./
RUN dotnet restore

# Copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Container Apps expects app to listen on 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]