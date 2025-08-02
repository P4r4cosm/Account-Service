# --- Этап 1: Базовый образ для конечного приложения ---
# Используем официальный образ ASP.NET 9.0, как требуется в задании.
# Он содержит только среду выполнения (runtime), что делает конечный образ легковесным.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Открываем порт 80.
EXPOSE 80
# Устанавливаем переменную окружения, чтобы Kestrel (веб-сервер ASP.NET) слушал на порту 80.
ENV ASPNETCORE_URLS=http://+:80

# --- Этап 2: Сборка проекта ---
# Используем образ SDK 9.0, который содержит все инструменты для сборки и публикации.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файлы проектов (.csproj) и решения (.sln) для восстановления зависимостей.
COPY ["Account Service.sln", "."]
COPY ["Account Service/Account Service.csproj", "Account Service/"]

# Восстанавливаем NuGet-пакеты для всего решения.
RUN dotnet restore "Account Service.sln"

# Теперь, когда зависимости восстановлены, копируем весь остальной исходный код.
COPY . .
WORKDIR "/src/Account Service"

# Собираем проект в Release-конфигурации.
RUN dotnet build "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Этап 3: Публикация приложения ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# Публикуем приложение. Эта команда создает оптимизированную для запуска версию,
# включая только необходимые для работы файлы.
RUN dotnet publish "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Этап 4: Финальный образ ---
# Возвращаемся к легковесному базовому образу 'base'.
FROM base AS final
WORKDIR /app
# Копируем только опубликованные артефакты из этапа 'publish'.
COPY --from=publish /app/publish .

# Запускаем приложение от имени встроенного непривилегированного пользователя 'app' для повышения безопасности.
# Это стандартная и рекомендуемая практика для production-образов.
USER app

# Указываем точку входа — команду для запуска нашего сервиса.
ENTRYPOINT ["dotnet", "Account Service.dll"]