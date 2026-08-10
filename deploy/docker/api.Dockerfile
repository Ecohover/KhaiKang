ARG APP_VERSION=0.0.0-dev
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VERSION Directory.Build.props Directory.Packages.props ./
COPY backend/NuGet.config backend/
COPY backend/src backend/src
COPY contract contract

RUN dotnet restore backend/src/KhaiKang.Api/KhaiKang.Api.csproj --configfile backend/NuGet.config
RUN dotnet publish backend/src/KhaiKang.Api/KhaiKang.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG APP_VERSION
WORKDIR /app

LABEL org.opencontainers.image.version=$APP_VERSION

USER root
RUN mkdir -p /var/lib/khaikang/data-protection \
    && chown -R $APP_UID:$APP_UID /var/lib/khaikang

COPY --from=build /app/publish ./

USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KhaiKang.Api.dll"]
