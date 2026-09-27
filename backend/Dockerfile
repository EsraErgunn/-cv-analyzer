# Tüm servisler için ortak Dockerfile. Hangi servisin derleneceği SERVICE build argümanıyla seçilir:
#   docker build -f backend/Dockerfile --build-arg SERVICE=UploadService .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG SERVICE
WORKDIR /src

# Önce sadece proje dosyaları kopyalanır; restore katmanı kod değişikliklerinde cache'ten gelir.
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/${SERVICE}/${SERVICE}.csproj backend/${SERVICE}/
RUN dotnet restore backend/${SERVICE}/${SERVICE}.csproj

COPY backend/Shared/ backend/Shared/
COPY backend/${SERVICE}/ backend/${SERVICE}/
RUN dotnet publish backend/${SERVICE}/${SERVICE}.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
ARG SERVICE
ENV ASPNETCORE_URLS=http://+:8080 \
    SERVICE_DLL=${SERVICE}.dll
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$SERVICE_DLL\""]
