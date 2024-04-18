FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5080
ENV ASPNETCORE_URLS=http://*:5080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS cache
WORKDIR /src
COPY ["KeyStoreApi/KeyStoreApi.csproj", "KeyStoreApi/"]
RUN dotnet restore "KeyStoreApi/KeyStoreApi.csproj"

FROM cache AS test
WORKDIR /src
COPY ["KeyStoreApi.Tests/KeyStoreApi.Tests.csproj", "KeyStoreApi.Tests/"]
RUN dotnet restore "KeyStoreApi.Tests/KeyStoreApi.Tests.csproj"
COPY . .
CMD ["dotnet", "test"]

FROM cache AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
WORKDIR "/src/KeyStoreApi"
RUN dotnet build "KeyStoreApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KeyStoreApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS runner
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeyStoreApi.dll"]
