# Build context = repo root (Host project references ../../Modules and ../../Shared and ../Domain)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Domain/ Domain/
COPY src/ src/

RUN dotnet publish src/Host/RealEstate.Host/RealEstate.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RealEstate.Host.dll"]
