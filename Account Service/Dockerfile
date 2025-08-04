
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# --- Этап сборки (build) ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем .sln и .csproj файлы, сохраняя структуру папок для кэширования слоев
COPY ["Account Service.sln", "."]

# Копируем .csproj из папки "Account Service"
COPY ["Account Service/Account Service.csproj", "Account Service/"]

# Восстанавливаем NuGet-пакеты
RUN dotnet restore "Account Service.sln"

# Копируем все остальное. Так как .csproj уже скопированы, 
# изменение только в .cs файлах не вызовет повторный 'dotnet restore'.
COPY . .
WORKDIR "/src/Account Service"
RUN dotnet build "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Этап публикации (publish) ---
FROM build AS publish
RUN dotnet publish "Account Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Финальный этап ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# USER app # Для Windows-контейнеров эту строку лучше закомментировать, если будут проблемы
ENTRYPOINT ["dotnet", "Account Service.dll"]