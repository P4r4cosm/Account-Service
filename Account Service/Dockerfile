# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# USER $APP_UID # Эту строку можно закомментировать для упрощения, если нет специфических требований к пользователю
WORKDIR /app
EXPOSE 80


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
RUN dotnet build "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Account Service.dll"]