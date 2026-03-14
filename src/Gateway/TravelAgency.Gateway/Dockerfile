FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Gateway/TravelAgency.Gateway/TravelAgency.Gateway.csproj", "src/Gateway/TravelAgency.Gateway/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Gateway/TravelAgency.Gateway/TravelAgency.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Gateway/TravelAgency.Gateway"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Gateway.dll"]
