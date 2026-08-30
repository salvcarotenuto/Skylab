INSERT INTO LavoriStorico
  (Lavoro_ID, DataEvento, TipoEvento, StatoNuovo_ID, EsitoNuovo_ID, Note)
SELECT l.ID,
       COALESCE(l.DataRedazione, l.DataInterventoPianificata, NOW()),
       'REDAZIONE_SCHEDA',
       CASE WHEN l.DataInterventoEffettiva IS NULL THEN l.StatoLavoro_ID ELSE NULL END,
       CASE WHEN l.DataInterventoEffettiva IS NULL THEN l.EsitoLavoro_ID ELSE NULL END,
       'Scheda redatta dal back-office'
FROM Lavori l
WHERE NOT EXISTS (
  SELECT 1 FROM LavoriStorico h
  WHERE h.Lavoro_ID=l.ID AND h.TipoEvento='REDAZIONE_SCHEDA'
);

INSERT INTO LavoriStorico
  (Lavoro_ID, DataEvento, TipoEvento, DataPianificataNuova, Note)
SELECT l.ID,
       COALESCE(l.DataRedazione, l.DataInterventoPianificata),
       'PIANIFICAZIONE',
       l.DataInterventoPianificata,
       'Intervento pianificato'
FROM Lavori l
WHERE l.DataInterventoPianificata IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM LavoriStorico h
    WHERE h.Lavoro_ID=l.ID AND h.DataPianificataNuova=l.DataInterventoPianificata
  );

INSERT INTO LavoriStorico
  (Lavoro_ID, DataEvento, TipoEvento, StatoNuovo_ID, EsitoNuovo_ID,
   DataInterventoEffettiva, Note)
SELECT l.ID,
       TIMESTAMP(l.DataInterventoEffettiva, COALESCE(l.OraInterventoEffettiva,'00:00:00')),
       'INTERVENTO_EFFETTUATO',
       l.StatoLavoro_ID,
       l.EsitoLavoro_ID,
       l.DataInterventoEffettiva,
       'Consuntivo del lavoro'
FROM Lavori l
WHERE l.DataInterventoEffettiva IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM LavoriStorico h
    WHERE h.Lavoro_ID=l.ID AND h.DataInterventoEffettiva=l.DataInterventoEffettiva
  );
