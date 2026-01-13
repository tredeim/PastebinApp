# Многоступенчатая сборка для оптимизации размера образа

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы решения и проектов
COPY ["PastebinApp.sln", "./"]
COPY ["src/PastebinApp.Api/PastebinApp.Api.csproj", "src/PastebinApp.Api/"]
COPY ["src/PastebinApp.Application/PastebinApp.Application.csproj", "src/PastebinApp.Application/"]
COPY ["src/PastebinApp.Domain/PastebinApp.Domain.csproj", "src/PastebinApp.Domain/"]
COPY ["src/PastebinApp.Infrastructure/PastebinApp.Infrastructure.csproj", "src/PastebinApp.Infrastructure/"]

# Восстанавливаем зависимости (кэшируется если .csproj не изменились)
RUN dotnet restore "src/PastebinApp.Api/PastebinApp.Api.csproj"

# Копируем весь исходный код
COPY . .

# Сборка проекта
WORKDIR "/src/src/PastebinApp.Api"
RUN dotnet build "PastebinApp.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "PastebinApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Создаем non-root пользователя для безопасности
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Копируем опубликованное приложение из stage publish
COPY --from=publish /app/publish .

# Открываем порты
EXPOSE 8080

# Точка входа
ENTRYPOINT ["dotnet", "PastebinApp.Api.dll"]