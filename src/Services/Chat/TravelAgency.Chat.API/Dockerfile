FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Chat/TravelAgency.Chat.API/TravelAgency.Chat.API.csproj", "src/Services/Chat/TravelAgency.Chat.API/"]
COPY ["src/Services/Chat/TravelAgency.Chat.Application/TravelAgency.Chat.Application.csproj", "src/Services/Chat/TravelAgency.Chat.Application/"]
COPY ["src/Services/Chat/TravelAgency.Chat.Domain/TravelAgency.Chat.Domain.csproj", "src/Services/Chat/TravelAgency.Chat.Domain/"]
COPY ["src/Services/Chat/TravelAgency.Chat.Infrastructure/TravelAgency.Chat.Infrastructure.csproj", "src/Services/Chat/TravelAgency.Chat.Infrastructure/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Services/Chat/TravelAgency.Chat.API/TravelAgency.Chat.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Chat/TravelAgency.Chat.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Chat.API.dll"]
