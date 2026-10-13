-- Exportable: el detalle por conversación de la consulta de origen (Detalle.sql), con las fechas y
-- filtros elegidos en pantalla. Una fila por cada registro: una conversación con transferencia o con
-- ACW saliente sale en varias filas (una por paso del agente), ordenadas por conversación.
--
-- Los marcadores entre llaves dobles de más abajo (detalle, fechas y filtros) los sustituye
-- ConsultasCuadro antes de enviarla a BigQuery: no los escribas en los comentarios.
-- Site, sector, supervisor y agente se normalizan igual que en Agregado.sql (vacío → «Sin dato»)
-- para que los filtros de la pantalla encuentren lo mismo.
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
