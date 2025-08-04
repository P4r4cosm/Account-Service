# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# USER $APP_UID # Эту строку можно закомментировать для упрощения, если нет специфических требований к пользователю
WORKDIR /app
# Открываем порт 80.
EXPOSE 80
# Устанавливаем переменную окружения, чтобы Kestrel (веб-сервер ASP.NET) слушал на порту 80.
ENV ASPNETCORE_URLS=http://+:80


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем .sln и все .csproj файлы, чтобы правильно восстановить зависимости
# Предполагается, что ваш .sln файл находится в корне контекста. Если нет, исправьте путь.
# Если у вас нет .sln файла, можно скопировать все .csproj
COPY ["Account Service/Account Service.csproj", "Account Service/"]
# Если есть другие проекты, их тоже нужно скопировать:
# COPY ["AnotherProject/AnotherProject.csproj", "AnotherProject/"]

# Рекомендуется сначала скопировать все файлы проектов, затем восстановить,
# а потом уже копировать все остальные исходники для лучшего кэширования слоев Docker.
RUN dotnet restore "Account Service/Account Service.csproj"

# Копируем все остальные файлы проекта
COPY . .
WORKDIR "/src/Account Service"

# Собираем проект в Release-конфигурации.
RUN dotnet build "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# Публикуем приложение. Эта команда создает оптимизированную для запуска версию,
# включая только необходимые для работы файлы.
RUN dotnet publish "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
# Копируем только опубликованные артефакты из этапа 'publish'.
COPY --from=publish /app/publish .

# Запускаем приложение от имени встроенного непривилегированного пользователя 'app' для повышения безопасности.
# Это стандартная и рекомендуемая практика для production-образов.
USER app

# Указываем точку входа — команду для запуска нашего сервиса.
ENTRYPOINT ["dotnet", "Account Service.dll"]