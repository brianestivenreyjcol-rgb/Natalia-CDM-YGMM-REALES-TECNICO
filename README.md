# Cuadro de mando YGMM Técnico · documentación para la migración

Todo lo que hace falta para llevar el **cuadro de mando** a otro proyecto: de dónde salen los datos, cómo se
conecta, qué cuentas usa, cómo se calculan los indicadores, cómo está montado el backend y cómo se ve.
Este repositorio solo trae el cuadro de mando: la sección **Limpieza de datos** del proyecto original se quitó
(ver el apartado 10.1). El código está en `WebApplication1/` y se abre con `WebApplication1.slnx`.

---

## Índice

1. [Qué es](#1-qué-es)
2. [Tecnología](#2-tecnología)
3. [Estructura del proyecto](#3-estructura-del-proyecto)
4. [Base de datos](#4-base-de-datos)
5. [Usuarios, contraseñas y accesos](#5-usuarios-contraseñas-y-accesos)
6. [Lógica de negocio](#6-lógica-de-negocio)
7. [Backend](#7-backend)
8. [Frontend y estilos](#8-frontend-y-estilos)
9. [Despliegue](#9-despliegue)
10. [Cómo migrarlo a un proyecto más grande](#10-cómo-migrarlo-a-un-proyecto-más-grande)

---

## 1. Qué es

Web interna con los indicadores de la **atención técnica (servicio AVERÍAS) de Yoigo y MásMóvil** en dos sites:
**Medellín** (Teleperformance) y **Bogotá** (Jazzplat). Tiene cinco informes:

| Ruta | Informe | Indicador destacado |
|---|---|---|
| `/dashboard` | Dashboard principal | % No solución |
| `/kpi` | KPIs integrados | Atendidas |
| `/no-solucion` | No solución | % No solución |
| `/rellamada-24h` | Rellamada 24H | % Rell 24H |
| `/rellamada-72h` | Rellamada 72H | % Rell 72H |

Además: `/` y `/cuadro` (portada con las cinco tarjetas), `/exportable` (CSV con el detalle por conversación) y
`/estado` (estado de la carga y prueba de la conexión).

Todas las páginas comparten panel de filtros (fechas, semana, site, sector, supervisor y agente) y tira de
cinco indicadores con su variación frente al periodo anterior.

---

## 2. Tecnología

| Pieza | Qué se usa |
|---|---|
| Plataforma | ASP.NET Core MVC sobre **.NET 10** (`net10.0`), C#, `Nullable` e `ImplicitUsings` activados |
| Vistas | Razor (`.cshtml`), sin librerías de JavaScript ni de CSS externas |
| Gráficas | Generadas en el servidor: SVG estirado + textos en HTML (sin Chart.js: la red corporativa bloquea los CDN) |
| BigQuery | **ODBC** con el *Simba ODBC Driver for Google BigQuery* y el paquete `System.Data.Odbc` 10.0.12 |
| SQL Server | `Microsoft.Data.SqlClient` 7.1.1. Está preparado (`ServicioSqlServer`), pero **no se usa**: todos los datos vienen de BigQuery |
| Cultura | `es-ES` para toda la aplicación |
| Sin Python | Todo es C# y Razor |

`WebApplication1.csproj` (solo lo del cuadro de mando):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.1" />
    <PackageReference Include="System.Data.Odbc" Version="10.0.12" />
  </ItemGroup>
  <!-- Las consultas de BigQuery viajan con la web para poder cambiarlas sin recompilar -->
  <ItemGroup>
    <None Update="Consultas\*.sql" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

---

## 3. Estructura del proyecto

El espacio de nombres es `WebApplication1` (nombre de la plantilla de Visual Studio). Al migrar conviene cambiarlo.

```
WebApplication1/
├── Program.cs                     Registro de servicios, cultura es-ES y rutas
├── appsettings.json               Cadena de BigQuery y opciones del cuadro
├── Consultas/                     SQL de BigQuery (se copian junto a la web al compilar)
│   ├── Detalle.sql                La consulta de origen (una fila por conversación y paso del agente)
│   ├── Agregado.sql               Resumen diario que se guarda en memoria
│   └── Exportable.sql             Detalle por conversación para el CSV
├── Servicios/                     Conexiones y lógica
│   ├── ServicioBigQuery.cs        Conexión ODBC, lectura y reintentos
│   ├── ServicioSqlServer.cs       Conexión SQL Server (preparada, sin uso) + Cadenas.SinSecretos
│   ├── ConsultasCuadro.cs         Lee los .sql y sustituye los marcadores
│   ├── AlmacenCuadro.cs           Datos en memoria (InstantaneaCuadro)
│   ├── ActualizadorCuadro.cs      Tarea en segundo plano que carga y refresca
│   ├── OpcionesCuadro.cs          Sección «CuadroMando» de appsettings
│   ├── ServicioTablero.cs         Filtros, indicadores, gráficas y frases de cada página
│   └── ServicioExportable.cs      CSV del detalle por conversación
├── Models/
│   ├── FilaCuadro.cs              Una fila del resumen diario
│   ├── Acumulado.cs               Suma de medidas y fórmulas de los indicadores
│   ├── Indicador.cs               Nombres, tipo, sentido, nota y fórmula de cada indicador
│   ├── Filtros.cs                 Filtros que llegan en la URL
│   ├── Graficos.cs                GraficoLineas, GraficoColumnas, GraficoBarras
│   └── Paginas.cs                 Navegación y modelos de cada página
├── Controllers/
│   ├── HomeController.cs          Portada (/ y /cuadro)
│   ├── CuadroController.cs        Los cinco informes y /exportable
│   └── EstadoController.cs        /estado y /estado/actualizar
├── Infraestructura/
│   ├── Formato.cs                 Cifras y fechas en es-ES
│   ├── Graficas.cs                Escala del eje, líneas suavizadas, rotulado
│   ├── Iconos.cs                  Iconos SVG de trazo
│   ├── Csv.cs                     Celdas de CSV para Excel en español
│   └── ResultadoDescarga.cs       Descarga en streaming
├── Views/
│   ├── Home/Cuadro.cshtml         Portada del cuadro (cinco tarjetas)
│   ├── Cuadro/Dashboard.cshtml, Kpi.cshtml, Indicador.cshtml
│   ├── Estado/Index.cshtml
│   └── Shared/                    _Layout, _Cabecera, _CabeceraMenu, _PanelFiltros, _Desplegable,
│                                  _Indicadores, _GraficoLineas, _GraficoColumnas, _GraficoBarras,
│                                  _Leyenda, _NotaPie, Preparando, Error
└── wwwroot/
    ├── css/site.css               Hoja única (estilo SOLARIS · GAIA)
    ├── js/tema.js                 Tema y panel antes de pintar (sin parpadeo)
    ├── js/site.js                 Filtros, recarga parcial, tablas, ficha, presentar…
    └── img/portada-ygmm.webp      Imagen de la portada
```

---

## 4. Base de datos

### 4.1 Conexión a BigQuery (ODBC)

| Dato | Valor |
|---|---|
| Controlador | **Simba ODBC Driver for Google BigQuery** (64 bits) |
| Origen de datos (DSN) | **`BQCOL`**, DSN **de usuario** de Windows en el equipo donde corre la web |
| Cadena en `appsettings.json` | `ConnectionStrings:BigQuery = "DSN=BQCOL;HTAPI_ActivationThreshold=2147483647"` |
| Dialecto | SQL estándar de BigQuery |
| Autenticación | OAuth con la cuenta de Google del usuario, configurada dentro del DSN (ver apartado 5) |

**Por qué `HTAPI_ActivationThreshold=2147483647`.** Con resultados grandes, el controlador usa por defecto la
Storage API de BigQuery (gRPC). A través del proxy corporativo esa API falla a veces («Storage API Error: failed
to connect to all addresses»). Con el umbral al máximo, los resultados bajan siempre por la API REST.

**Crear el DSN en otro equipo**: *Orígenes de datos ODBC (64 bits) → DSN de usuario → Agregar → Simba ODBC Driver
for Google BigQuery*. Nombre: `BQCOL`. Autenticación: *User Authentication* (OAuth), iniciar sesión con la cuenta de
Google que tenga permiso en los proyectos del apartado 4.2. Catálogo (proyecto de facturación): el que use el
equipo, se ve en el DSN actual. Dialecto: estándar.

Sin DSN, la cadena equivalente sería así (los valores entre `< >` son del entorno):

```
Driver={Simba ODBC Driver for Google BigQuery};Catalog=<proyecto de facturación>;OAuthMechanism=1;RefreshToken=<token>;SQLDialect=1;HTAPI_ActivationThreshold=2147483647
```

El token **nunca** se escribe en el código ni en el repositorio: va en el DSN, en variables de entorno o en un
almacén de secretos.

### 4.2 Tablas de origen

| Tabla | Para qué | Filtro que se aplica |
|---|---|---|
| `mo-customer-ops-reporting.CALLS_CHATS.ALL_LLAMADAS_EJECUTIVO_BRUTOS_RELLAMADA` | Llamadas (una fila por conversación y paso del agente), tiempos y KPI de rellamada y no solución | `agent_agency_norm IN ('JAZZPLAT','TELEPERFORMANCE')`, `agent_service_norm = 'AVERIAS'`, `agent_brand_norm IN ('YOIGO','MASMOVIL')` |
| `mm-operaciones-bigquery.FRONT.MC - SCHAMAN KPI` | Sesión de Schaman: detallado, servicio (fijo/móvil/TV), ATC, DNI, cierre | `plataforma IN ('JAZZPLAT_BOGOTA','TELEPERFORMANCE')`, `MARCA IN ('SIN_MARCA','YOIGO','MASMOVIL')`; una fila por `dim_conversation_id` + `day` (la sesión más reciente) |
| `mo-vendor-management-reporting.JZZBOGOTA.NominaBogota` | Nómina de Bogotá: sector, supervisor, team, nombre del agente, legajo | `Cargo <> 'Aprendiz Sena'`, `Genesys IS NOT NULL`; una fila por `Genesys` + `Fecha` |

Cruces: llamadas `LEFT JOIN` Schaman por `conversation_id = dim_conversation_id` y `day`; llamadas `LEFT JOIN`
nómina por `agente = Genesys` y `day = Fecha`. Medellín no está en la nómina: su sector, supervisor, team y
agente salen como «Medellín».

### 4.3 Campos calculados en la consulta de detalle

| Campo | Regla |
|---|---|
| `ATN` (atendida) | 1 si `direction = 'INBOUND'` |
| `Transferencia` | 1 si es entrante y `transfer_kpi = 1` |
| `Q_rell` | 1 si es entrante **y no** transferida (base de la rellamada 72H) |
| `AHT_nueva` | `AHT` de las entrantes (0 en las salientes) |
| `ACW` | `AHT` de las salientes (el ACW saliente) |
| `Rell_1H`, `Rell_24H` | Salientes sin rellamada al servicio en 1 h / 24 h |
| `Rell_72H` | 1 si `redial_customer_operations_72h_kpi = 1` |
| `Direccion` | `INBOUND` o `OUTBOUND` |
| `Rell_1H_Dep`, `Rell_24H_Dep` | Rellamada al departamento en 1 h / 24 h |
| `Site` | `TELEPERFORMANCE` → Medellín, `JAZZPLAT` → Bogotá |
| `Semana` | `W` + semana ISO |
| `Ultima_historia`, `Cierre_Schaman` | Si `DETALLADO` es `VENTA_INCORRECTO` → «ERROR SESION», `Abandonada` → «Abandonada», `OTROS` → «Cierre excepcional»; si no, el valor de Schaman |
| `Caso_1` | `session_closed_tech_cause1`, con `''` convertido en el texto `'null'` |
| `DNI` | Primera parte de `service_id` antes de `:` |

Una conversación con transferencia o con ACW saliente tiene **varias filas** (una por paso del agente o dirección).

### 4.4 Consulta de detalle (`Consultas/Detalle.sql`)

Es la consulta original del usuario, tal cual. Las demás la usan como subconsulta.

```sql
WITH Llamada AS (

  SELECT * EXCEPT(rn)
  FROM (

    SELECT
      ts_session_created_m,
      FORMAT_TIMESTAMP('%d/%m/%Y', ts_session_created_m) AS Fecha,
      FORMAT_TIMESTAMP('%H:%M:%S', ts_session_created_m) AS Hora,
      DETALLADO,
      FIJO_MOVIL_TV AS Servicio,
      issue_key AS ATC,
      client_service,
      service_id,
      dim_conversation_id,
      day,
      MARCA,
      agent_name,
      SPLIT(service_id, ':')[SAFE_OFFSET(0)] AS DNI,

      CASE
        WHEN DETALLADO = 'VENTA_INCORRECTO' THEN 'ERROR SESION'
        WHEN DETALLADO = 'Abandonada' THEN 'Abandonada'
        WHEN DETALLADO = 'OTROS' THEN 'Cierre excepcional'
        ELSE user_story_last_category
      END AS Ultima_historia,

      CASE
        WHEN DETALLADO = 'VENTA_INCORRECTO' THEN 'ERROR SESION'
        WHEN DETALLADO = 'Abandonada' THEN 'Abandonada'
        WHEN DETALLADO = 'OTROS' THEN 'Cierre excepcional'
        ELSE session_closed_operational_name
      END AS Cierre_Schaman,

      CASE
        WHEN session_closed_tech_cause1 = '' THEN 'null'
        ELSE session_closed_tech_cause1
      END AS Caso_1,

      CASE
        WHEN user_story_name = 'cliente_en_campaña_de_cambio_de_cpe' THEN 1
        ELSE 0
      END AS Campana_cpe,

      -- Numera las filas de cada conversación (por día): 1 = sesión más reciente
      -- Sirve para quedarnos solo con esa (rn = 1) y así eliminar duplicados
      ROW_NUMBER() OVER (
        PARTITION BY dim_conversation_id, day
        ORDER BY ts_session_created_m DESC
      ) AS rn

    FROM `mm-operaciones-bigquery.FRONT.MC - SCHAMAN KPI`
    WHERE plataforma IN ('JAZZPLAT_BOGOTA', 'TELEPERFORMANCE')
      AND MARCA IN ('SIN_MARCA', 'YOIGO', 'MASMOVIL')

  )
  WHERE rn = 1  -- se queda con una sola fila por dim_conversation_id + day
),

Cliente AS (

  SELECT
    *,

    CASE
      WHEN direction = 'INBOUND' THEN 1
      ELSE 0
    END AS ATN,

    CASE
      WHEN direction != 'INBOUND' THEN 0
      WHEN transfer_kpi = 1 THEN 1
      ELSE 0
    END AS Transferencia,

    CASE
      WHEN direction = 'INBOUND'
       AND NOT (direction = 'INBOUND' AND transfer_kpi = 1)
      THEN 1
      ELSE 0
    END AS Q_rell,

    CASE
      WHEN direction = 'INBOUND' THEN AHT
      ELSE 0
    END AS AHT_nueva,

    CASE
      WHEN direction = 'OUTBOUND' THEN AHT
      ELSE 0
    END AS ACW,

    CASE
      WHEN direction = 'OUTBOUND'
       AND NOT (direction = 'OUTBOUND' AND redial_attended_service_1h_kpi = 1)
      THEN 1
      ELSE 0
    END AS Rell_1H,

    CASE
      WHEN direction = 'OUTBOUND'
       AND NOT (direction = 'OUTBOUND' AND redial_attended_service_24h_kpi = 1)
      THEN 1
      ELSE 0
    END AS Rell_24H,

    CASE
      WHEN redial_customer_operations_72h_kpi = 1 THEN 1
      ELSE 0
    END AS Rell_72H,

    CASE
      WHEN direction = 'INBOUND' THEN 'INBOUND'
      ELSE 'OUTBOUND'
    END AS Direccion,

    CASE
      WHEN redial_attended_department_1h_kpi = 1 THEN 1
      ELSE 0
    END AS Rell_1H_Dep,

    CASE
      WHEN redial_attended_department_24h_kpi = 1 THEN 1
      ELSE 0
    END AS Rell_24H_Dep,

    CASE
      WHEN department_transfer = 'null' THEN ''
      ELSE department_transfer
    END AS Destino_transferencia,

    CASE
      WHEN ORB_interactionClassification_interactionType = 'null' THEN ''
      ELSE ORB_interactionClassification_interactionType
    END AS Motivo_consulta,

    CASE
      WHEN ORB_contactReason_contactReasonSummary = 'null' THEN ''
      ELSE ORB_contactReason_contactReasonSummary
    END AS Observaciones

   FROM `mo-customer-ops-reporting.CALLS_CHATS.ALL_LLAMADAS_EJECUTIVO_BRUTOS_RELLAMADA`

  WHERE agent_agency_norm IN ('JAZZPLAT', 'TELEPERFORMANCE')
    AND agent_service_norm = 'AVERIAS'
    AND agent_brand_norm IN ('YOIGO', 'MASMOVIL')
),

Nomina AS (

  -- Una sola fila por agente (Genesys) y fecha para que el JOIN no duplique llamadas
  SELECT * EXCEPT(rn)
  FROM (

    SELECT
      *,
      ROW_NUMBER() OVER (
        PARTITION BY Genesys, Fecha
        ORDER BY Agente
      ) AS rn

    FROM `mo-vendor-management-reporting.JZZBOGOTA.NominaBogota`
      WHERE Cargo <> 'Aprendiz Sena'
      AND Genesys IS NOT NULL

  )
  WHERE rn = 1
)

SELECT

    FORMAT_TIMESTAMP('%d/%m/%Y', Cliente.day) AS Fecha,
    Llamada.ts_session_created_m,
    Cliente.day AS Fecha_dt,

    EXTRACT(YEAR FROM Cliente.day) AS Year,

    CONCAT('W', CAST(EXTRACT(ISOWEEK FROM Cliente.day) AS STRING)) AS Semana,

    EXTRACT(DAY FROM Cliente.day) AS Dia,

    CASE
      WHEN Cliente.agent_agency_norm = 'TELEPERFORMANCE' THEN 'Medellín'
      WHEN Cliente.agent_agency_norm = 'JAZZPLAT' THEN 'Bogotá'
      ELSE ''
    END AS Site,

    CASE
      WHEN Cliente.agent_agency_norm = 'TELEPERFORMANCE' THEN 'Medellín'
      ELSE Nomina.Sector
    END AS Sector,

    CASE
      WHEN Cliente.agent_agency_norm = 'TELEPERFORMANCE' THEN 'Medellín'
      ELSE Nomina.Super
    END AS Super,

    CASE
      WHEN Cliente.agent_agency_norm = 'TELEPERFORMANCE' THEN 'Medellín'
      ELSE Nomina.Team
    END AS Team,

    CASE
      WHEN Cliente.agent_agency_norm = 'TELEPERFORMANCE' THEN 'Medellín'
      ELSE Nomina.Agente
    END AS Personal,

    Cliente.agente,
    Cliente.conversation_id,
    Cliente.customer_id,
    Cliente.agent_brand_norm,
    Cliente.msisdn,
    Cliente.agent_subservice_norm,
    Cliente.ATN,
    Cliente.Q_rell,
    Cliente.AHT_nueva,
    Cliente.ACW,
    Cliente.no_solucion_kpi,
    Cliente.Rell_1H,
    Cliente.Rell_24H,
    Cliente.Rell_72H,
    Cliente.Transferencia,
    Llamada.Detallado,
    Llamada.Ultima_historia,
    Llamada.Cierre_Schaman,
    Llamada.Caso_1,
    Llamada.Servicio,
    Llamada.ATC,
    Llamada.DNI,
    Cliente.Direccion,
    Cliente.Rell_1H_Dep,
    Cliente.Rell_24H_Dep,
    Cliente.Destino_transferencia,
    Cliente.agent_step_order,
    Cliente.agent_disc_type,
    Cliente.Motivo_Consulta,
    Cliente.Observaciones,
    Llamada.MARCA,
    Cliente.redial_attended_service_24h_kpi,
    Cliente.talk_time,
    Cliente.after_call_time,
    Cliente.hold_time,
    Cliente.ringing_time,
    Cliente.redial_customer_operations_72h_kpi,
    Nomina.Legajo,
    Cliente.attended_call_kpi

  FROM Cliente

  LEFT JOIN Llamada
    ON Cliente.conversation_id =  Llamada.dim_conversation_id
   AND Cliente.day = Llamada.day

  LEFT JOIN Nomina
    ON Cliente.agente = Nomina.Genesys
   AND Cliente.day = Nomina.Fecha
```

### 4.5 Resumen diario (`Consultas/Agregado.sql`)

Es lo que se guarda en memoria. Agrupa la consulta de detalle por **día, site, sector, supervisor, agente, marca,
servicio y detallado**: unas 95.000 filas para 100 días, en unos 10 s. `{{DETALLE}}` y `{{DESDE}}` los sustituye
`ConsultasCuadro.ResumenDiario(desde)`.

```sql
SELECT
  Fecha,
  IFNULL(Site, '') AS Site,
  IFNULL(Sector, '') AS Sector,
  IFNULL(Super, '') AS Supervisor,
  COALESCE(Personal, CAST(agente AS STRING), '') AS Agente,
  IFNULL(agent_brand_norm, '') AS Marca,
  IFNULL(Servicio, '') AS Servicio,
  IFNULL(Detallado, '') AS Detallado,
  COUNT(*) AS Filas,
  SUM(ATN) AS Atendidas,
  SUM(Q_rell) AS Q_rell,
  CAST(SUM(IFNULL(AHT_nueva, 0) + IFNULL(ACW, 0)) AS FLOAT64) AS Segundos_atencion,
  SUM(IF(no_solucion_kpi = 1, 1, 0)) AS No_solucion,
  COUNTIF(no_solucion_kpi IS NOT NULL) AS Base_no_solucion,
  SUM(IF(redial_attended_service_24h_kpi = 1, 1, 0)) AS Rell_24h,
  SUM(IF(Q_rell = 1 AND Rell_72H = 1, 1, 0)) AS Rell_72h,
  SUM(Transferencia) AS Transferencias
FROM (
{{DETALLE}}
) AS Detalle
WHERE Fecha_dt >= '{{DESDE}}'
GROUP BY 1, 2, 3, 4, 5, 6, 7, 8
```

> Aviso: no escribas los nombres de los marcadores entre llaves dobles en los comentarios del SQL. El reemplazo
> también los cambiaría ahí y la consulta fallaría (ya pasó una vez: error de sintaxis en la línea 63).

### 4.6 Exportable (`Consultas/Exportable.sql`)

Es el detalle por conversación, con las fechas y los filtros de la pantalla. Lleva 45 columnas con nombres claros
y va ordenado por fecha, conversación y paso del agente. Se pide a BigQuery en el momento de la descarga. Los
marcadores son `{{DETALLE}}`, `{{DESDE}}`, `{{HASTA}}` (este día no se incluye) y `{{FILTROS}}`.

```sql
SELECT
  conversation_id AS ID_conversacion,
  Fecha,
  FORMAT_TIMESTAMP('%H:%M:%S', ts_session_created_m) AS Hora_sesion,
  EXTRACT(ISOWEEK FROM Fecha_dt) AS Semana,
  IFNULL(NULLIF(TRIM(Site), ''), 'Sin dato') AS Site,
  IFNULL(NULLIF(TRIM(Sector), ''), 'Sin dato') AS Sector,
  IFNULL(NULLIF(TRIM(Super), ''), 'Sin dato') AS Supervisor,
  Team,
  IFNULL(NULLIF(TRIM(COALESCE(Personal, CAST(agente AS STRING), '')), ''), 'Sin dato') AS Agente,
  agente AS Login_agente,
  Legajo,
  agent_brand_norm AS Marca,
  MARCA AS Marca_Schaman,
  agent_subservice_norm AS Subservicio,
  Direccion,
  agent_step_order AS Paso_agente,
  agent_disc_type AS Tipo_desconexion,
  customer_id AS ID_cliente,
  msisdn AS MSISDN,
  DNI,
  ATC,
  Servicio,
  Detallado,
  Ultima_historia,
  Cierre_Schaman,
  Caso_1,
  Motivo_Consulta AS Motivo_consulta,
  Observaciones,
  Destino_transferencia,
  ATN AS Atendida,
  Q_rell,
  Transferencia,
  AHT_nueva AS AHT,
  ACW,
  talk_time AS Tiempo_conversacion,
  hold_time AS Tiempo_espera,
  after_call_time AS Tiempo_ACW,
  ringing_time AS Tiempo_timbrado,
  no_solucion_kpi AS No_solucion,
  redial_attended_service_24h_kpi AS Rell_24H_kpi,
  Rell_72H,
  Rell_1H,
  Rell_1H_Dep,
  Rell_24H_Dep,
  attended_call_kpi AS Llamada_atendida
FROM (
{{DETALLE}}
) AS Detalle
WHERE Fecha_dt >= '{{DESDE}}'
  AND Fecha_dt < '{{HASTA}}'{{FILTROS}}
ORDER BY Fecha_dt, ID_conversacion, Paso_agente
```

**El Exportable trae datos personales del cliente** (ID cliente, MSISDN, DNI). Trátalo como información confidencial.

---

## 5. Usuarios, contraseñas y accesos

**En el proyecto no hay ningún usuario ni ninguna contraseña guardados**, y así debe seguir:

| Acceso | Cómo funciona hoy | Dónde está el secreto |
|---|---|---|
| BigQuery | OAuth con la **cuenta de Google** del usuario que configuró el DSN `BQCOL`. La cuenta necesita permiso de lectura en las tres tablas del apartado 4.2 y de ejecutar trabajos en el proyecto de facturación | El token de refresco lo guarda el controlador Simba **dentro del DSN** (registro de Windows del usuario). No está en el código ni en `appsettings.json` |
| SQL Server | Sin configurar (`ConnectionStrings:SqlServer = ""`) | — |
| La web | **Sin inicio de sesión**: cualquiera de la red interna que abra `http://<IP>:5300` la ve | — |

Reglas para el proyecto nuevo:

- Ningún secreto en el repositorio: ni tokens, ni contraseñas, ni ficheros de clave `.json` de cuentas de servicio.
  Usa *user-secrets* en desarrollo y variables de entorno o un almacén de secretos (Azure Key Vault, Secret
  Manager de Google…) en producción.
- Para un servidor compartido, mejor una **cuenta de servicio** de Google con permisos de solo lectura en esas
  tablas que la cuenta personal de alguien. En el controlador Simba sería `OAuthMechanism=0` con `Email` y
  `KeyFilePath`, y el fichero de clave se guarda fuera del repositorio.
- La página de estado enseña la cadena de conexión con los secretos ocultos (`Cadenas.SinSecretos`, en
  `ServicioSqlServer.cs`, tapa `password`, `pwd`, `token`, `secret`, `key` y `refreshtoken`). Si añades
  autenticación a la web, protege también `/estado` y `/exportable`.

---

## 6. Lógica de negocio

### 6.1 Fórmulas de los indicadores

Están en `Models/Acumulado.cs`, sobre las sumas del resumen diario. Cuadran con el informe de referencia de
**septiembre de 2026**: 91.133 atendidas, TMO 739,55, No solución 25,41 %, Rell 24H 10,78 % y Rell 72H 24,25 %.

| Indicador | Fórmula | Tipo | Sentido |
|---|---|---|---|
| **Atendidas** | `SUM(ATN)` | Entero | Neutro |
| **TMO (s)** | `(SUM(AHT_nueva) + SUM(ACW)) / SUM(ATN)` | Segundos | Menos es mejor |
| **% No solución** | llamadas con `no_solucion_kpi = 1` / llamadas con `no_solucion_kpi` informado (`Base_no_solucion`) | % | Menos es mejor |
| **% Rell 24H** | llamadas con `redial_attended_service_24h_kpi = 1` / `SUM(ATN)` | % | Menos es mejor |
| **% Rell 72H** | entrantes no transferidas con `Rell_72H = 1` / `SUM(Q_rell)` | % | Menos es mejor |
| **% Transferencia** | `SUM(Transferencia)` / `SUM(ATN)` (solo en la tabla de KPIs) | % | Neutro |

Las primeras versiones de TMO, Rell 24H y No solución estaban mal. Las buenas salieron de una consulta de
diagnóstico contra las cifras de referencia, así que **no cambies las fórmulas sin volver a cuadrar septiembre**.

```csharp
public double? Valor(Indicador indicador) => indicador switch
{
    Indicador.Atendidas => Atendidas,
    Indicador.Tmo => Atendidas > 0 ? SegundosAtencion / Atendidas : null,
    Indicador.NoSolucion => Porcentaje(NoSolucion, BaseNoSolucion),
    Indicador.Rell24 => Porcentaje(Rell24, Atendidas),
    Indicador.Rell72 => Porcentaje(Rell72, QRell),
    Indicador.Transferencia => Porcentaje(Transferencias, Atendidas),
    _ => null,
};
private static double? Porcentaje(long parte, long total) => total > 0 ? 100.0 * parte / total : null;
```

### 6.2 Flujo de los datos

```
Arranque de la web
  └─ ActualizadorCuadro (BackgroundService)
       └─ AlmacenCuadro.CargarAsync()
            └─ ServicioBigQuery.ConsultarAsync(Agregado.sql con Detalle.sql dentro, desde = hoy − DiasHistorico)
                 └─ InstantaneaCuadro (filas ordenadas por fecha, en memoria, inmutable)
  Cada MinutosActualizacion (60) vuelve a cargar. Si falla: reintenta al minuto (sin datos) o a los 5 min (con datos).
  POST /estado/actualizar pide una carga ya (AlmacenCuadro.PedirActualizacion).

Petición de una página (/dashboard?desde=…&site=…)
  └─ CuadroController → ServicioTablero.Dashboard(filtros, ruta)
       ├─ sin datos todavía → vista «Preparando los datos» (se recarga sola)
       └─ con datos → filtra en memoria, calcula indicadores, variación, gráficas y frases → vista Razor

/exportable → ServicioExportable → BigQuery en el momento (Exportable.sql) → CSV en streaming
```

- Una sola carga a la vez (`SemaphoreSlim`). La instantánea se cambia de golpe (`Volatile.Write`), así que las
  páginas nunca ven datos a medias.
- `InstantaneaCuadro.EntreFechas(desde, hasta)` usa búsqueda binaria sobre las filas ordenadas.
- Los textos repetidos (site, agente…) se guardan una sola vez (*interning* en un diccionario) para ahorrar memoria.
- Un texto vacío o nulo se guarda como **«Sin dato»**.

### 6.3 Filtros

Llegan en la URL (`Models/Filtros.cs`): `desde`, `hasta` (aaaa-mm-dd) y listas `site`, `supervisor`, `sector`,
`semana` (número ISO) y `agente`. Se pueden repetir para elegir varios valores; una lista vacía significa «Todos».

- **Fechas por defecto**: el mes del último día con datos (del día 1 al último día cargado). Nunca fuera de los días
  cargados. Si `desde > hasta`, se intercambian. Las fechas por defecto **no se escriben en la URL**, así que un
  enlace guardado sigue al último día.
- **Atajos**: Mes en curso, Mes anterior, 7 días y 30 días.
- **Filtros en cascada**: cada desplegable ofrece los valores que existen con los demás filtros puestos. Se hace en
  una sola pasada: si una fila falla solo un filtro, su valor sigue siendo opción de ese desplegable.
- En el navegador, cada casilla o fecha filtra al momento y solo se recarga el bloque `#informe`.

### 6.4 Variación frente al periodo anterior

En `ServicioTablero.PeriodoAnterior` se aplican estas reglas, como en el informe de referencia:

| Rango elegido | Se compara con |
|---|---|
| Meses completos (del día 1 al último del mes) | Los mismos meses anteriores (septiembre → agosto) |
| Un mes desde el día 1 hasta un día cualquiera | Los mismos días del mes anterior (1–7 oct → 1–7 sept) |
| Cualquier otro rango | Los mismos días inmediatamente anteriores |

- Se aplican los mismos filtros, **salvo la semana**. Si el periodo anterior no tiene atendidas o queda antes del
  primer día cargado, no hay comparación («sin periodo anterior»).
- Variación = `(actual − anterior) / anterior × 100`, escrita «vs ago ▼ 4,4 %».
- Color: **mejor** en verde, **peor** en rojo y **neutro** en gris, según el sentido del indicador. Por debajo de
  0,05 % se considera neutro.

### 6.5 Gráficas de cada informe

| Informe | Gráficas |
|---|---|
| **Dashboard** | No solución por semana (líneas por site) · No solución por marca (columnas site × marca) · No solución por servicio (barras, 8 principales) · lo mismo para Rellamada 72H · TMO por semana · Atendidas por día (ancho doble, desde 0) |
| **KPIs integrados** | Atendidas por marca y semana · TMO por marca y semana · Atendidas por supervisor (20 con más llamadas) · Evolución de Rell 24H y No solución por marca y semana · **Tabla «Detalle por día»** con Atendidas, TMO, % Rell 24H, % Rell 72H, % No solución y % Transferencia, más TOTAL al pie |
| **No solución / Rell 24H / Rell 72H** | Por marca (columnas) · por servicio (8 principales) · por detallado (10 principales) · por semana · por día |

- **Series**: Medellín y Bogotá en las gráficas por site; YOIGO y MASMOVIL en las de marca.
- **Barras**: se quedan las N categorías con más atendidas y el resto se suma en «Otros», que va al final.
- **Volumen**: las gráficas de línea de un indicador llevan detrás las atendidas de cada periodo en columnas grises.
- **Frase de lectura** al pie de cada tarjeta (`ServicioTablero.Lecturas`):
  - en semanas, el último periodo («En la semana 40, Medellín marca … y Bogotá …: Medellín va 2,71 puntos por encima»);
  - en días, columnas y barras, el valor más alto y el más bajo;
  - en supervisores, cuánto reúne el primero.

### 6.6 Exportable (CSV)

- Una fila por registro de la consulta de detalle: una conversación con transferencia o ACW sale en varias filas.
- CSV para Excel en español: separador `;`, coma decimal, fechas `dd/MM/yyyy`, UTF-8 **con BOM** (para las tildes).
- Se escribe en la respuesta según llegan las filas (`ResultadoDescarga`), sin cargarlo entero en memoria. Una
  semana de Bogotá tarda unos 25 s.
- Los filtros se convierten en `AND … IN (…)` con los textos escapados como literales de BigQuery (`\`, `'` y
  saltos de línea). Las semanas solo admiten números del 1 al 53, así que nada de la URL llega tal cual a la
  consulta. Las expresiones normalizan igual que el resumen (vacío → «Sin dato»).
- Nombre del archivo: `cuadro-mando-ygmm-detalle_<desde>_<hasta>.csv`.

### 6.7 Reintentos y errores de red

`ServicioBigQuery` reintenta hasta 3 veces, esperando 5 s y luego 10 s, cuando el error de ODBC es de red
(«Failure when receiving data from the peer», «Storage API Error»). No reintenta errores de sintaxis o de permisos
(SQLSTATE `42xxx` o `28xxx`). BigQuery sirve la repetición desde su caché sin volver a facturar. En las descargas
solo se reintenta **antes** de empezar a escribir.

---

## 7. Backend

### 7.1 `Program.cs`

```csharp
builder.Services.AddControllersWithViews();
builder.Services.Configure<OpcionesCuadro>(builder.Configuration.GetSection(OpcionesCuadro.Seccion));

builder.Services.AddSingleton<ServicioBigQuery>();
builder.Services.AddSingleton<ServicioSqlServer>();
builder.Services.AddSingleton<AlmacenCuadro>();
builder.Services.AddSingleton<ServicioTablero>();
builder.Services.AddSingleton<ServicioExportable>();
builder.Services.AddHostedService<ActualizadorCuadro>();

var cultura = CultureInfo.GetCultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;
```

Todos los servicios son *singleton*: comparten la instantánea en memoria.

### 7.2 `appsettings.json`

```jsonc
{
  "ConnectionStrings": {
    "BigQuery": "DSN=BQCOL;HTAPI_ActivationThreshold=2147483647",
    "SqlServer": ""
  },
  "CuadroMando": {
    "DiasHistorico": 100,          // días hacia atrás (incluye el periodo anterior para comparar)
    "MinutosActualizacion": 60,    // cada cuánto se refresca la memoria
    "SegundosEsperaConsulta": 600  // tiempo máximo de una consulta en BigQuery
  }
}
```

### 7.3 Rutas

| Método | Ruta | Controlador · acción | Qué hace |
|---|---|---|---|
| GET | `/` y `/cuadro` | `Home.Index` / `Home.Cuadro` | Portada del cuadro: cinco tarjetas y el estado de los datos (vista `Home/Cuadro`) |
| GET | `/dashboard` | `Cuadro.Dashboard` | Dashboard principal |
| GET | `/kpi` | `Cuadro.Kpi` | KPIs integrados |
| GET | `/no-solucion` | `Cuadro.NoSolucion` | Informe del indicador |
| GET | `/rellamada-24h` | `Cuadro.Rellamada24` | Informe del indicador |
| GET | `/rellamada-72h` | `Cuadro.Rellamada72` | Informe del indicador |
| GET | `/exportable` | `Cuadro.Exportable` | CSV con las fechas y los filtros de la URL |
| GET | `/estado` | `Estado.Index` | Estado de la carga; con `?probar=true`, prueba la conexión (`SELECT 1`) |
| POST | `/estado/actualizar` | `Estado.Actualizar` | Pide una carga ya (con antifalsificación) |

### 7.4 Clases principales

| Clase | Responsabilidad |
|---|---|
| `ServicioBigQuery` | `ConsultarAsync<T>` (lee todo en una lista), `LeerAsync` (entrega el lector para ir en streaming), `ProbarAsync`, reintentos `EsDeRed` |
| `ConsultasCuadro` | Lee `Consultas/*.sql` de la carpeta de la web y sustituye los marcadores (`ResumenDiario`, `Exportable`) |
| `AlmacenCuadro` / `InstantaneaCuadro` | Datos en memoria, carga única y petición de actualización |
| `ActualizadorCuadro` | Bucle de carga y espera; la carga va en `Task.Run` porque el controlador ODBC es síncrono |
| `ServicioTablero` | Cada página: `Preparar` (fechas, filtros en cascada, periodo anterior, tarjetas), gráficas (`Lineas`, `Columnas`, `Barras`) y frases (`Lecturas`). Devuelve `null` sin datos |
| `ServicioExportable` | Condiciones de filtro escapadas, cabeceras legibles del CSV y escritura en streaming |
| `Acumulado` | Suma de medidas y fórmulas; `Agrupar(clave)` como extensión |
| `Indicador` (enum) + `Indicadores` | Nombre, nombre corto, tipo de valor, sentido, nota corta y fórmula de cada indicador |
| `Formato` | Cifras y fechas en es-ES |
| `Graficas` / `Escala` | Eje Y con marcas redondas, línea suavizada (Catmull-Rom → Bézier sin pasarse de los extremos), área, rotulado y posición de las pastillas |

### 7.5 Modelos de página

- `MarcoPagina`: lo común a los informes, es decir título, miga, ruta, consulta de la URL, fechas, desplegables,
  atajos, las cinco tarjetas e instante de carga.
- `ModeloDashboard`, `ModeloKpi` y `ModeloIndicador`: el marco y sus gráficas.
- `GraficoLineas` añade `DesdeCero`, `Alto` y `Volumen`. `GraficoColumnas` es igual que la base. `GraficoBarras`
  añade `Gruesas`. Los tres heredan de `Grafico`, que tiene `Id`, `Titulo`, `Tipo`, `Categorias`, `Series`,
  `Fichas`, `Area`, `Nota` y `Lectura`.
- `SerieGrafico(Nombre, Clase, Valores)`: la `Clase` fija el color (`serie-a`, `serie-yoigo`…).

### 7.6 Formatos

| Qué | Ejemplo | Función |
|---|---|---|
| Porcentaje | `25,41 %` (con espacio fino que no se parte, U+202F) | `Formato.Porcentaje` |
| Entero | `91.133` | `Formato.Entero` |
| Segundos | `739,55` | `Formato.Decimal` |
| Variación | `▼ 4,4 %` | `Formato.Variacion` |
| Fecha | `1 sept 2026`, `lun 1 sept 2026`, `1 sept – 30 sept 2026` | `Fecha`, `FechaLarga`, `Rango` |
| Comparación | `vs ago`, `vs jul–ago`, `vs 1–7 sept` | `ComparacionCorta` |
| Valor crudo para ordenar | `25.4123` (en `data-valor`) | `Formato.Crudo` |

---

## 8. Frontend y estilos

### 8.1 Principios (guía SOLARIS · GAIA, modelo: el portal SOLARIS)

- **Una sola hoja**, `wwwroot/css/site.css`, organizada en 15 secciones: variables, base, cabecera, panel,
  indicadores, tarjetas, gráficas, ficha, tabla, portada, estado, presentación, movimiento, móvil e impresión.
- **Todo color es una variable** de `:root`, redefinida en `:root[data-tema="oscuro"]`. En las vistas no hay
  colores, `<style>` ni `<script>`. Lo que cambia de color se resuelve con una clase que fija una variable
  (`.serie-a { --serie: var(--serie-1) }`).
- Tema **claro, oscuro y automático**.
- **Un solo punto de ruptura, 760 px**. La portada se apila desde 1.100 px.
- Negrita **600 como máximo** (500 en nombres de tabla). Sin emojis; iconos de trazo (`Iconos.Svg("nombre")`).
- Cifras con `tabular-nums`.

### 8.2 Colores (claro / oscuro)

| Variable | Claro | Oscuro | Uso |
|---|---|---|---|
| `--fondo` | `#f4f6f9` | `#0e1116` | Fondo de la página |
| `--superficie` | `#ffffff` | `#151a21` | Tarjetas y panel |
| `--superficie-2` | `#f7f8fa` | `#1b2129` | Campos, hover |
| `--superficie-3` | `#eef1f5` | `#222934` | Pastillas, chips |
| `--borde` / `--borde-fuerte` | `#e2e6ec` / `#cdd3dd` | `#262d38` / `#353e4c` | Bordes |
| `--texto` / `--texto-suave` / `--texto-tenue` | `#121826` / `#535c6d` / `#8890a0` | `#e7eaf0` / `#a4acba` / `#78818f` | Textos |
| `--acento` | **`#7b38c9`** (morado YGMM) | `#b48af0` | Acento de la marca |
| `--acento-osc` | `#5f27a3` | `#9a6ae0` | Hover, migas |
| `--acento-texto` | `#ffffff` | `#141414` | Texto sobre el acento |
| `--thead` / `--thead-borde` / `--thead-texto` | `#efe6fa` / `#dcc8f2` / `#4a1f7d` | `#27263a` / `#3a3356` / `#d9c8fb` | Cabecera de tablas |
| `--serie-1` … `--serie-5` | `#7b38c9` `#c2408f` `#4b55c4` `#a98ae0` `#6e6680` | `#b48af0` `#ee8cc4` `#8f97f0` `#cdb6f5` `#a59cb8` | Series de las gráficas |
| `--marca-yoigo` | `#ab3e91` | `#d777bd` | Serie YOIGO |
| `--marca-masmovil` | `#3a3a3a` | `#c9c9c9` | Serie MASMOVIL |
| `--bueno` / `--malo` / `--atencion` | `#228722` / `#cd3c14` / `#e0a100` | `#5cc67a` / `#ff7a59` / `#e9b44c` | Significado; no cambian con la marca |
| `--mapa-verde` / `--mapa-ambar` / `--mapa-rojo` | `#c6efce` / `#ffeb9c` / `#ffc7ce` | `#1f4a34` / `#4b4122` / `#57262f` | Semáforo de tablas |

Clases de serie: `.serie-a` (Medellín, `--serie-1`), `.serie-b` (Bogotá, `--serie-2`), `.serie-c`, `.serie-d`,
`.serie-e`, `.serie-yoigo` y `.serie-masmovil`.

### 8.3 Tipografía, forma y movimiento

- Letra: `"Segoe UI Variable Text", "Segoe UI", …` (interfaz) y `"Segoe UI Variable Display"` (títulos). Base de 14 px.
- Radios de 16 px (tarjetas) y 8 px (botones y campos). Tres sombras: `--sombra-1`, `--sombra-2` y `--sombra-3`.
- Cabecera de 72 px pegajosa, con efecto cristal (`backdrop-filter`). Pestañas de 52 px con una línea de 3 px del
  acento debajo.
- Movimiento de 160, 220, 440 y 780 ms con la curva `cubic-bezier(.22,.61,.36,1)`. Las tarjetas aparecen con un
  pequeño escalonado, las columnas crecen con `scaleY` y las barras con `scaleX`. Con `prefers-reduced-motion` no
  se anima nada, y al filtrar (recarga parcial) no se repiten las animaciones (clase `.actualizado`).

### 8.4 Componentes

| Pieza | Clases | Cómo es |
|---|---|---|
| Cabecera | `.cabecera`, `.logotipo` (YGMM / TÉCNICO), `.cabecera-titulo` con `.migas`, `.btn-tema` | Botones Imprimir, Presentar y Tema |
| Pestañas | `.navegacion > .pastilla` (`.activa` rellena del acento) | Una por informe |
| Distribución | `.lienzo > form.tablero[data-filtros]` | Rejilla de 268 px (panel) + contenido, de 1.120 a 1.720 px |
| Panel de filtros | `.panel-filtros`, `.grupo-filtros`, `.filtro`, `.rango-fechas`, `.atajos`, `.multi` (desplegable con casillas y buscador), `.btn-panel(.primario)`, `.panel-estado` | Pegajoso con `z-index: 20`; se pliega a un raíl (`html[data-panel="plegado"]`) |
| Tira de indicadores | `.resumen.resumen-cinco > .resumen-dato(.destacado)`, `.tend-dif.mejor/.peor/.neutro` | Cinco datos; el principal, con borde del acento |
| Tarjetas | `.tarjeta`, `.tarjeta-cabecera` (h2 + `.nota`), `.pie-tarjeta` (frase de lectura) | Rejillas `.rejilla-dashboard` (1,3fr 1fr 1fr), `.rejilla-kpi`, `.rejilla-indicador` |
| Gráfica de líneas | `.grafica`, `.leyenda`, `.plano(.alto)(.con-volumen)`, `svg` 1000×1000, `.linea`, `.area` (degradado), `.columna-gris`, `.valor-volumen`, `.punto`, `.etiqueta-punto.arriba/.abajo`, `.banda`, `.eje-x`, `.eje-y` | Línea suavizada con degradado, una pastilla por punto y volumen gris detrás |
| Columnas y barras | `.columna`, `.hbarras > .hbarra-grupo > .hbarra > .hbarra-relleno + .hbarra-valor` | Con la cifra de cada una |
| Ficha | `#ficha.ficha`, `.ficha-linea` | Se abre al pasar el ratón por una `.banda[data-ficha]` |
| Tabla | `.envoltura-tabla > table.ranking[data-mapa]`, `th[data-sentido]`, `button.ordenar`, `td.barra-volumen`, `tfoot tr.total` | Primera columna fija y nunca cortada, semáforo por columna, TOTAL al pie pegado abajo |
| Portada | `.menu-solaris`, `.menu-hero`, `.portada-imagen`, `.modulos > .modulo` | Tarjetas de informe con icono, antetítulo, título, descripción, estado y flecha |
| Avisos | `.aviso`, `.pantalla-aviso`, `.girando` | Preparando datos y errores |

### 8.5 Gráficas generadas en el servidor (sin librerías)

En `_GraficoLineas.cshtml` (y de forma parecida en columnas y barras):

1. `Escala.Calcular` saca el eje Y con marcas redondas. Arranca en 0 para volúmenes; si no, se ajusta al rango de
   los datos con margen para las pastillas.
2. Un `<svg viewBox="0 0 1000 1000" preserveAspectRatio="none">` estirado al plano dibuja la rejilla, las columnas
   grises de volumen, el área con `linearGradient` y la línea (`vector-effect="non-scaling-stroke"`).
3. Los textos (ejes, pastillas, cifras) van en **HTML encima**, posicionados en % con `left` y `top` en línea, para
   que no se deformen.
4. Con volumen, la línea ocupa el 62 % de arriba y las columnas grises el 34 % de abajo.
5. Las pastillas van en todos los puntos hasta 12 periodos; con más, una de cada pocas (unas diez), siempre con la
   primera y la última. En cada periodo, la serie más alta pone la pastilla encima y la siguiente debajo.
6. Cada periodo tiene una `.banda` invisible con
   `data-ficha="Cabecera|serie-a~Medellín · 25,41 %|serie-b~Bogotá · 24,10 %|Atendidas · 1.234"`, y `site.js` la
   pinta como ficha.
7. Sin líneas de media, de meta ni de umbral.

### 8.6 JavaScript

**`tema.js`** se carga en el `<head>` antes de pintar. Lee `localStorage` (`ygmm-tema`: `auto`, `claro` u
`oscuro`; `ygmm-panel`: `plegado`) y pone `data-tema` y `data-panel` en `<html>`, para que no parpadee.

**`site.js`** se encarga de:

| Funcionalidad | Cómo funciona |
|---|---|
| Tema | Pasa por automático, claro y oscuro; en automático sigue `prefers-color-scheme` |
| Panel plegable | Raíl vertical y lo recuerda |
| Presentar | Pantalla completa sin panel ni botones (`html[data-presentacion]`); Esc para salir |
| Imprimir | Siempre en tema claro (`beforeprint` / `afterprint`) |
| Recarga parcial | Cada casilla o fecha del `form[data-filtros]` pide la página con `fetch` y cambia solo `#informe`. Usa `history.pushState`, conserva el desplegable abierto con su búsqueda y su scroll, y muestra la barra de carga `#barra-carga`. Las fechas por defecto no se escriben en la URL |
| Desplegables `.multi` | Uno abierto a la vez, buscador sin tildes y «Quitar selección» |
| Tablas | Orden al pulsar la cabecera (con `data-valor`). Semáforo pastel por columna según `th[data-sentido]` (1: más es mejor, −1: más es peor, 0: sin color). La barrita de volumen usa `--v` |
| Ficha | Tooltip que sigue al ratón |
| Recarga sola | Las páginas con `data-recargar-en` (Preparando, Estado) se recargan solas |

### 8.7 Móvil, impresión y presentación

- **≤ 760 px**:
  - la cabecera es compacta (sin migas ni Imprimir o Presentar);
  - las pestañas tienen scroll horizontal;
  - el panel va arriba y no es pegajoso;
  - la tira de indicadores pasa a 2 columnas;
  - las rejillas se apilan;
  - las gráficas tienen un mínimo de 520 px con scroll.
- **Impresión**: sin panel, pestañas, botones ni ficha, y sin cortar tarjetas.
- **Presentar**: el contenido ocupa todo el ancho (hasta 1.840 px) y el título es más grande.

---

## 9. Despliegue

Publicación actual en la red interna: `http://10.148.216.222:5300`.

```bash
dotnet publish WebApplication1/WebApplication1.csproj -c Release -o C:\Proyecto\Publicado\CuadroMandoYGMM
```

`iniciar.cmd`, en la carpeta publicada, con saltos de línea **CRLF**:

```bat
@echo off
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5300
if not exist "%~dp0logs" mkdir "%~dp0logs"
echo Cuadro de mando YGMM Tecnico publicado en http://%COMPUTERNAME%:5300 - no cierres esta ventana
"%~dp0WebApplication1.exe" >> "%~dp0logs\cuadro-mando.log" 2>&1
```

Requisitos del equipo que lo sirve:

- Runtime de **ASP.NET Core 10**.
- **Simba ODBC Driver for Google BigQuery** de 64 bits y el DSN `BQCOL` creado por el **mismo usuario de Windows**
  que arranca la web (es un DSN de usuario).
- Regla de **firewall de entrada** para el puerto TCP 5300. Hoy el perfil de red es «Público» y no hay regla: la
  tiene que crear un administrador.
- Mientras la ventana siga abierta, la web está publicada. Para algo estable, conviene IIS, un servicio de Windows
  o un contenedor.

---

## 10. Cómo migrarlo a un proyecto más grande

### 10.1 Qué copiar

Todo lo que hay en `WebApplication1/` es del cuadro de mando (apartado 3).

Del proyecto original se quitó la sección **Limpieza de datos**:

- `Controllers/LimpiezaController.cs`
- `Models/Limpieza.cs`
- `Servicios/`: `ServicioLimpieza.cs`, `AnalizadorBigQuery.cs`, `AnalizadorMemoria.cs`, `FuentesLimpieza.cs`,
  `LectoresFuente.cs`, `OpcionesLimpieza.cs`
- `Consultas/`: `Esquema.sql`, `Perfil.sql`, `DuplicadosCuadro.sql`, `DuplicadosGenerico.sql`
- `Views/Limpieza/`, `Views/Shared/_PanelLimpieza.cshtml`, `_EstadoAnalisis.cshtml` y la portada de dos tarjetas
  (`Views/Home/Index.cshtml`)
- el paquete `ExcelDataReader`

También se ajustaron estos archivos:

| Archivo | Qué se cambió |
|---|---|
| `Models/Paginas.cs` | Solo las pestañas del cuadro (`Navegacion.Enlaces`) |
| `Views/Shared/_Cabecera.cshtml` | Una sola sección, «Cuadro de mando» |
| `ServicioTablero` | Sin `Fechas()` ni `RegistrosPorDia()`, que eran de Limpieza |
| `ConsultasCuadro` | Solo `ResumenDiario` y `Exportable` |
| `Program.cs` y `appsettings.json` | Sin los servicios ni la sección de Limpieza |
| `HomeController` | `/` enseña directamente la portada del cuadro |

En `site.css` y `site.js` quedan algunas reglas de Limpieza (tabla de campos, hallazgos, formularios de fuente,
`data-sin-auto`). No molestan; se pueden borrar al migrar.

### 10.2 Pasos

1. **Espacio de nombres**: cambia `WebApplication1` por el del proyecto nuevo, por ejemplo `Empresa.Cuadros.Ygmm`.
2. **Registra los servicios** del apartado 7.1 y `Configure<OpcionesCuadro>`. Si el proyecto grande ya tiene
   caché, el `AlmacenCuadro` se puede pasar a `IMemoryCache` o Redis, pero mantén la idea: una carga a la vez y
   una instantánea inmutable que se cambia de golpe.
3. **Consultas**: deja los `.sql` como archivos con `CopyToOutputDirectory`, para cambiarlos sin recompilar, o como
   recursos incrustados. Respeta los marcadores entre llaves dobles.
4. **Conexión**: crea el DSN `BQCOL` en el servidor o usa una cadena sin DSN con una cuenta de servicio (apartado 5).
   Los secretos van en variables de entorno o en un almacén de secretos, nunca en el repositorio.
5. **Rutas**: si el proyecto nuevo tiene varias secciones, pon un prefijo (`[Route("ygmm-tecnico")]`) y revisa los
   enlaces absolutos de las vistas (`/dashboard`, `/exportable`, `/estado`) y de `Navegacion.Enlaces`.
6. **Estilos**: si el proyecto grande tiene su propia hoja, mete `site.css` bajo un contenedor (por ejemplo
   `.cuadro-ygmm`) o pasa las variables de la sección 1 a sus *tokens*. Las vistas solo usan clases y variables,
   así que basta con mantener los nombres.
7. **Autenticación**: la web actual no tiene. Si el proyecto grande la tiene, pon `[Authorize]` en `CuadroController`
   y `EstadoController`, sobre todo en `/exportable`, que trae datos personales.
8. **Comprobación**: con septiembre de 2026 deben salir **91.133 atendidas (±1 por datos en vivo), TMO 739,55,
   No solución 25,41 %, Rell 24H 10,78 % y Rell 72H 24,25 %**.

### 10.3 Lista de comprobación

- [ ] Compila y arranca. Mientras carga sale «Preparando los datos» y al terminar, los informes.
- [ ] `/estado?probar=true` conecta con BigQuery.
- [ ] Las cifras de septiembre cuadran (10.2, paso 8).
- [ ] Los filtros en cascada, los atajos y «Limpiar» funcionan, y la URL conserva los filtros.
- [ ] El Exportable descarga con las mismas fechas y filtros, y Excel lo abre bien (tildes y decimales).
- [ ] Se ve bien en claro y en oscuro, a 375 px de ancho y al imprimir.
- [ ] No hay ningún secreto en el repositorio.
