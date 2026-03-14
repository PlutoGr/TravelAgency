FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Identity/TravelAgency.Identity.API/TravelAgency.Identity.API.csproj", "src/Services/Identity/TravelAgency.Identity.API/"]
COPY ["src/Services/Identity/TravelAgency.Identity.Application/TravelAgency.Identity.Application.csproj", "src/Services/Identity/TravelAgency.Identity.Application/"]
COPY ["src/Services/Identity/TravelAgency.Identity.Domain/TravelAgency.Identity.Domain.csproj", "src/Services/Identity/TravelAgency.Identity.Domain/"]
COPY ["src/Services/Identity/TravelAgency.Identity.Infrastructure/TravelAgency.Identity.Infrastructure.csproj", "src/Services/Identity/TravelAgency.Identity.Infrastructure/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Services/Identity/TravelAgency.Identity.API/TravelAgency.Identity.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Identity/TravelAgency.Identity.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Identity.API.dll"]
