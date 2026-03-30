# OnceInfo

[![.NET Core Desktop](https://github.com/rafacc87/OnceInfo/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/rafacc87/OnceInfo/actions/workflows/dotnet-desktop.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Obtención de los porcentajes por rasca de premio de la ONCE.

## Descripción

OnceInfo es una aplicación de consola en .NET 8 que analiza las probabilidades de premio en los rascas de la ONCE. La aplicación obtiene datos de la web oficial de juegosonce.es, calcula los porcentajes de premios y genera un informe HTML interactivo que se abre automáticamente en el navegador.

## Características

- Scraping automático de datos de la web de la ONCE
- Cálculo de probabilidades de premio por rasca
- Clasificación por cuartiles (Bajo, Medio-Bajo, Medio-Alto, Alto)
- Informe HTML con filtros interactivos
- Exportación a CSV
- Soporte para diferentes modos de análisis

## Requisitos

- .NET 8.0 Runtime o SDK
- Navegador Chromium (instalado automáticamente por Playwright en la primera ejecución)

## Instalación

```bash
# Clonar el repositorio
git clone https://github.com/rafacc87/OnceInfo.git
cd OnceInfo

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build
```

## Uso

```bash
# Ejecutar con configuración por defecto
dotnet run --project OnceInfo

# Mostrar todos los rascas
dotnet run --project OnceInfo -- /t 0

# Top 10 rascas por porcentaje de premio
dotnet run --project OnceInfo -- /t 10

# Excluir premios iguales al precio del rasca
dotnet run --project OnceInfo -- /nomin

# Mostrar euros ganados por euro gastado (en lugar de probabilidad)
dotnet run --project OnceInfo -- /euro

# Filtrar premios con valor mínimo de 5€
dotnet run --project OnceInfo -- /p 5

# Combinar argumentos
dotnet run --project OnceInfo -- /t 10 /nomin /euro /p 5

# Publicar para distribución
dotnet publish OnceInfo -c Release
```

## Argumentos

| Argumento | Descripción |
|-----------|-------------|
| `/t x` | Mostrar top x rascas. Si no se especifica, muestra todos. |
| `/nomin` | Excluir premios del mismo valor que el precio del rasca. |
| `/euro` | Mostrar euros ganados por euro gastado en lugar de probabilidad por cupón. |
| `/p x` | Filtrar premios con valor mínimo de x euros. |

## Informe HTML

El informe generado incluye:

- **Filtros**: Por nombre, serie, precio, porcentaje y rascas premiados
- **Clasificación por cuartiles**:
  - 🔴 **Bajo**: < Q1%
  - 🟡 **Medio-Bajo**: Q1% - Q2%
  - 🔵 **Medio-Alto**: Q2% - Q3%
  - 🟢 **Alto**: > Q3%
- **Ordenación**: Por cualquier columna
- **Exportación**: CSV con los resultados filtrados

## Fuentes de datos

- https://www.sorteonacional.com/sorteo-de-la-once/rascas-que-mas-tocan/
- https://www.juegosonce.es/rascas-todos

## Desarrollo

### Estructura del proyecto

```
OnceInfo/
├── Models/
│   └── RascaResultado.cs      # Modelo de datos
├── Services/
│   ├── PlaywrightService.cs   # Scraping web
│   └── HtmlReportGenerator.cs # Generación de informe
├── Properties/
│   └── Resources.resx         # Selectores XPath y URLs
└── Program.cs                 # Punto de entrada
```

### Ejecutar tests

```bash
dotnet test OnceInfo.Tests
```

## Tecnologías

- .NET 8.0
- Microsoft.Playwright - Automatización de navegador
- HtmlAgilityPack - Parsing HTML

## Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo [LICENSE](LICENSE) para más detalles.