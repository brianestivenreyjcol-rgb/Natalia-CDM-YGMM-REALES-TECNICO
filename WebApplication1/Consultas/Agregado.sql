-- Resumen diario que alimenta el cuadro de mando.
-- Toma la consulta de detalle (Detalle.sql) tal cual y la agrupa por día, site, sector,
-- supervisor, agente, marca, servicio y detallado. Así a la web llegan unas decenas de miles
-- de filas en lugar de cada llamada, y los filtros y gráficas se calculan en memoria.
--
-- Los dos marcadores entre llaves dobles de más abajo (la consulta de detalle y la fecha desde)
-- los sustituye ConsultasCuadro antes de enviarla a BigQuery: no los escribas en los comentarios.
--
-- Fórmulas de los indicadores (las usa Models/Acumulado.cs; cuadran con el informe de referencia
-- de septiembre de 2026: 91.133 atendidas, TMO 739,55, No solución 25,41 %, Rell 24H 10,78 %, Rell 72H 24,25 %):
--   TMO             = Segundos_atencion (AHT_nueva + ACW) / Atendidas
--   % No solución   = No_solucion / Base_no_solucion (llamadas con no_solucion_kpi informado)
--   % Rell 24H      = Rell_24h (redial_attended_service_24h_kpi, todas las filas) / Atendidas
--   % Rell 72H      = Rell_72h (Rell_72H de las entrantes no transferidas) / Q_rell
--   % Transferencia = Transferencias / Atendidas
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
