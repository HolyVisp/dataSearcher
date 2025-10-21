# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/Shared/Records/Records.csproj backend/Shared/Records/
COPY backend/DataIngestionService/DataIngestionService.csproj backend/DataIngestionService/
RUN dotnet restore backend/DataIngestionService/DataIngestionService.csproj

COPY backend/Shared/Records/ backend/Shared/Records/
COPY backend/DataIngestionService/ backend/DataIngestionService/
RUN dotnet publish backend/DataIngestionService/DataIngestionService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DataIngestionService.dll"]
