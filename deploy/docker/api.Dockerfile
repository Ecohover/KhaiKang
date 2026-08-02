FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY backend/NuGet.config backend/
COPY backend/KhaiKang.Backend.slnx backend/
COPY backend/src backend/src
COPY contract contract

RUN dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
RUN dotnet publish backend/src/KhaiKang.Api/KhaiKang.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER root
RUN mkdir -p /var/lib/khaikang/data-protection \
    && chown -R $APP_UID:$APP_UID /var/lib/khaikang

COPY --from=build /app/publish ./

USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KhaiKang.Api.dll"]
