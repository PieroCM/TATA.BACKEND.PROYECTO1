# Despliegue con Docker Compose

Levanta el sistema completo: SQL Server 2022, la API .NET 9 y el frontend Quasar servido por Nginx.

## Requisitos

- Docker Desktop (o Docker Engine con Compose).
- Los dos repositorios clonados uno al lado del otro:

```
carpeta/
├── TATA.BACKEND.PROYECTO1/   (este repo; el compose está en despliegue/)
└── TATA.FRONTEND.PROYECTO1/
```

## Pasos

```bash
cd TATA.BACKEND.PROYECTO1/despliegue
cp .env.example .env      # completar contraseñas
docker compose up -d      # usa las imágenes publicadas josed20/proyectofinal-*:1.0.0
# o, para compilar desde el código:
docker compose up -d --build
```

| Servicio | URL |
|---|---|
| Frontend (Nginx + SPA) | http://localhost:9000 |
| API (.NET 9) | http://localhost:5260 |
| Health check | http://localhost:9000/health |
| SQL Server | localhost:1433 (usuario `sa`, contraseña en `.env`) |

Usuario inicial (lo crea el DataSeeder): `22200150@ue.edu.pe` / `Admin123!`.

## Imágenes en Docker Hub

- https://hub.docker.com/r/josed20/proyectofinal-backend
- https://hub.docker.com/r/josed20/proyectofinal-frontend

## Informe

El informe de las Entregas 1 y 2 está en `docs/`, junto con los diagramas (`docs/img/`) y sus fuentes HTML (`docs/diagramas/`).
