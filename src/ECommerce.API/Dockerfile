FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore first for better caching
COPY ./ECommerce.API/*.csproj ./ECommerce.API/
COPY ../ECommerce.Business/*.csproj ../ECommerce.Business/
COPY ../ECommerce.Core/*.csproj ../ECommerce.Core/
COPY ../ECommerce.Data/*.csproj ../ECommerce.Data/
COPY ../ECommerce.Infrastructure/*.csproj ../ECommerce.Infrastructure/
COPY ../ECommerce.Entities/*.csproj ../ECommerce.Entities/

RUN dotnet restore "ECommerce.API/ECommerce.API.csproj"

# Copy the rest of the sources
COPY . .

RUN dotnet publish "ECommerce.API/ECommerce.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
