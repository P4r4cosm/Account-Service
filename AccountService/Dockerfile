# --- Этап базового образа ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# --- Этап сборки ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем SLN и CSPROJ, сохраняя структуру
COPY ["AccountService.sln", "./"]
COPY ["AccountService/AccountService.csproj", "AccountService/"]

# Восстанавливаем зависимости — указываем полный путь к .csproj
RUN dotnet restore "AccountService/AccountService.csproj"

# Копируем всё остальное
COPY . .

# Переходим в папку проекта и собираем
WORKDIR "/src/AccountService"
RUN dotnet build "AccountService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Этап публикации ---
FROM build AS publish
WORKDIR /src/AccountService
RUN dotnet publish "AccountService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Финальный образ ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]