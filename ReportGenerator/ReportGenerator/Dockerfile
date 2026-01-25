# Этот этап используется при запуске из VS в быстром режиме (Debug)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Сначала root — чтобы ставить пакеты
USER root

# Таймзона (UTC+5). Для ЯНАО обычно подходит Asia/Yekaterinburg.
ENV TZ=Asia/Yekaterinburg

RUN apt-get update && \
    apt-get install -y --no-install-recommends tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Шрифты для кириллицы (если тебе нужно для PDF)
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    fontconfig \
    fonts-freefont-ttf \
    fonts-liberation \
    fonts-dejavu && \
    fc-cache -f -v && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Возвращаем пользователя приложения (как у тебя было)
USER $APP_UID

WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReportGenerator/ReportGenerator.csproj", "ReportGenerator/"]
RUN dotnet restore "./ReportGenerator/ReportGenerator.csproj"
COPY . .
WORKDIR "/src/ReportGenerator"
RUN dotnet build "./ReportGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build


# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReportGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# Final (Release/обычный запуск)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReportGenerator.dll"]