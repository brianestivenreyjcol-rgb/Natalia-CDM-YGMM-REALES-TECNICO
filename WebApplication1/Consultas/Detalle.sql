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
