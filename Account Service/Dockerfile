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
COPY ["Account Service.sln", "./"]
COPY ["Account Service/Account Service.csproj", "Account Service/"]

# Восстанавливаем зависимости — указываем полный путь к .csproj
RUN dotnet restore "Account Service/Account Service.csproj"

# Копируем всё остальное
COPY . .

# Переходим в папку проекта и собираем
WORKDIR "/src/Account Service"
RUN dotnet build "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Этап публикации ---
FROM build AS publish
WORKDIR /src/Account Service
RUN dotnet publish "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Финальный образ ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Account Service.dll"]