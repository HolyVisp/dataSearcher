# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/Shared/Records/Records.csproj backend/Shared/Records/
COPY backend/SearchService/SearchService.csproj backend/SearchService/
RUN dotnet restore backend/SearchService/SearchService.csproj

COPY backend/Shared/Records/ backend/Shared/Records/
COPY backend/SearchService/ backend/SearchService/
RUN dotnet publish backend/SearchService/SearchService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "SearchService.dll"]
