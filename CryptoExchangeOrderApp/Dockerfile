# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj files and restore dependencies
COPY CryptoExchangeOrderApp.Api/*.csproj ./CryptoExchangeOrderApp.Api/
COPY CryptoExchangeOrderApp/*.csproj ./CryptoExchangeOrderApp/
COPY Domain/*.csproj ./Domain/
COPY ExchangesData/*.csproj ./ExchangesData/
COPY Application/*.csproj ./Application/
COPY Infrastructure/*.csproj ./Infrastructure/
RUN dotnet restore ./CryptoExchangeOrderApp.Api/CryptoExchangeOrderApp.Api.csproj

# Copy the entire project and build
COPY . ./
RUN dotnet publish ./CryptoExchangeOrderApp.Api/CryptoExchangeOrderApp.Api.csproj -c Release -o out

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose port 80 for the application
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "CryptoExchangeOrderApp.Api.dll"]