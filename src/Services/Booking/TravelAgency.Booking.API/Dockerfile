FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Booking/TravelAgency.Booking.API/TravelAgency.Booking.API.csproj", "src/Services/Booking/TravelAgency.Booking.API/"]
COPY ["src/Services/Booking/TravelAgency.Booking.Application/TravelAgency.Booking.Application.csproj", "src/Services/Booking/TravelAgency.Booking.Application/"]
COPY ["src/Services/Booking/TravelAgency.Booking.Domain/TravelAgency.Booking.Domain.csproj", "src/Services/Booking/TravelAgency.Booking.Domain/"]
COPY ["src/Services/Booking/TravelAgency.Booking.Infrastructure/TravelAgency.Booking.Infrastructure.csproj", "src/Services/Booking/TravelAgency.Booking.Infrastructure/"]
COPY ["src/Shared/TravelAgency.Shared.Contracts/TravelAgency.Shared.Contracts.csproj", "src/Shared/TravelAgency.Shared.Contracts/"]
RUN dotnet restore "src/Services/Booking/TravelAgency.Booking.API/TravelAgency.Booking.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Booking/TravelAgency.Booking.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelAgency.Booking.API.dll"]
