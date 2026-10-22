
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ClientManagement.Api.csproj ./

RUN dotnet restore ClientManagement.Api.csproj

COPY . ./

RUN dotnet publish ClientManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Run 
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "ClientManagement.Api.dll"]
