# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files (context is ./src, so paths are relative to src/)
COPY ./ECommerce.API/ECommerce.API.csproj ./ECommerce.API/
COPY ./ECommerce.Business/ECommerce.Business.csproj ./ECommerce.Business/
COPY ./ECommerce.Core/ECommerce.Core.csproj ./ECommerce.Core/
COPY ./ECommerce.Data/ECommerce.Data.csproj ./ECommerce.Data/
COPY ./ECommerce.Entities/ECommerce.Entities.csproj ./ECommerce.Entities/
COPY ./ECommerce.Infrastructure/ECommerce.Infrastructure.csproj ./ECommerce.Infrastructure/

# Restore
RUN dotnet restore ./ECommerce.API/ECommerce.API.csproj

# Copy everything
COPY . .

# Build
WORKDIR /src/ECommerce.API
RUN dotnet publish -c Release -o /app/publish

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Compiled app files
COPY --from=build /app/publish .

# ⚠️ NOT: uploads klasörü VOLUME ile mount edilecek (docker-compose.prod.yml)
# COPY uploads yapmıyoruz çünkü volume mount bunu override eder
# Görseller admin panel üzerinden yüklenecek ve host'taki ./uploads/ klasöründe saklanacak
# Bu sayede container yeniden başlatıldığında görseller kaybolmaz

EXPOSE 5000
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
