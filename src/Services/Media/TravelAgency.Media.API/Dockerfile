FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Media/TravelAgency.Media.API/TravelAgency.Media.API.csproj", "src/Services/Media/TravelAgency.Media.API/"]
COPY ["src/Services/Media/TravelAgency.Media.Application/TravelAgency.Media.Application.csproj", "src/Services/Media/TravelAgency.Media.Application/"]
COPY ["src/Services/Media/TravelAgency.Media.Domain/TravelAgency.Media.Domain.csproj", "src/Services/Media/TravelAgency.Media.Domain/"]
COPY ["src/Services/Media/TravelAgency.Media.Infrastructure/TravelAgency.Media.Infrastructure.csproj", "src/Services/Media/TravelAgency.Media.Infrastructure/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Services/Media/TravelAgency.Media.API/TravelAgency.Media.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Media/TravelAgency.Media.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Media.API.dll"]
