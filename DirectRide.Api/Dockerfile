FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["DirectRide.Api/DirectRide.Api.csproj", "DirectRide.Api/"]
RUN dotnet restore "DirectRide.Api/DirectRide.Api.csproj"

COPY . .
WORKDIR "/src/DirectRide.Api"
RUN dotnet publish "DirectRide.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DirectRide.Api.dll"]