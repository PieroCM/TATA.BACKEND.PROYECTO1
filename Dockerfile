# ==============================================================================
# Multi-stage Dockerfile para TATA.BACKEND.PROYECTO1 (.NET 9 Web API)
# ==============================================================================

# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copiar primero los archivos de proyecto para aprovechar el caché de Docker
COPY ["TATA.BACKEND.PROYECTO1.API/TATA.BACKEND.PROYECTO1.API.csproj", "TATA.BACKEND.PROYECTO1.API/"]
COPY ["TATA.BACKEND.PROYECTO1.CORE/TATA.BACKEND.PROYECTO1.CORE.csproj", "TATA.BACKEND.PROYECTO1.CORE/"]

# 2. Restaurar dependencias NuGet
RUN dotnet restore "TATA.BACKEND.PROYECTO1.API/TATA.BACKEND.PROYECTO1.API.csproj"

# 3. Copiar el resto del código fuente
COPY . .

# 4. Compilar y publicar artefactos en modo Release sin el host ejecutable específico de SO
WORKDIR "/src/TATA.BACKEND.PROYECTO1.API"
RUN dotnet publish "TATA.BACKEND.PROYECTO1.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------------------------
# STAGE 2: Runtime liviano para producción
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Instalar curl para el mecanismo de healthcheck en contenedores
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Puerto estándar de ASP.NET Core en contenedores
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copiar artefactos compilados desde el stage de build
COPY --from=build /app/publish .

# Crear carpeta para logs de log4net si fuera necesario y asignar propiedad
USER app

# Punto de entrada de la aplicación
ENTRYPOINT ["dotnet", "TATA.BACKEND.PROYECTO1.API.dll"]
