FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Catalog/TravelAgency.Catalog.API/TravelAgency.Catalog.API.csproj", "src/Services/Catalog/TravelAgency.Catalog.API/"]
COPY ["src/Services/Catalog/TravelAgency.Catalog.Application/TravelAgency.Catalog.Application.csproj", "src/Services/Catalog/TravelAgency.Catalog.Application/"]
COPY ["src/Services/Catalog/TravelAgency.Catalog.Domain/TravelAgency.Catalog.Domain.csproj", "src/Services/Catalog/TravelAgency.Catalog.Domain/"]
COPY ["src/Services/Catalog/TravelAgency.Catalog.Infrastructure/TravelAgency.Catalog.Infrastructure.csproj", "src/Services/Catalog/TravelAgency.Catalog.Infrastructure/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Services/Catalog/TravelAgency.Catalog.API/TravelAgency.Catalog.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Catalog/TravelAgency.Catalog.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Catalog.API.dll"]
