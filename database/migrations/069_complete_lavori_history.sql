INSERT INTO LavoriStorico
  (Lavoro_ID, TipoEvento, StatoNuovo_ID, EsitoNuovo_ID,
   DataPianificataNuova, DataInterventoEffettiva, Note, DatiPrecedenti)
SELECT c.ID,
       'MIGRAZIONE_CONSUNTIVO_LEGACY',
       5,
       CASE WHEN c.Esito IN (1, 2, 3) THEN c.Esito ELSE NULL END,
       l.DataInterventoPianificata,
       c.DataExe,
       CASE WHEN COALESCE(c.Descrizione, '') <> COALESCE(l.DescrizioneSintetica, '')
            THEN 'Descrizione legacy divergente conservata nei dati precedenti' END,
       JSON_OBJECT(
         'DescrizioneLegacy', c.Descrizione,
         'AnnoLegacy', c.Anno,
         'CodiceLegacy', c.Codice)
FROM Lavorichiusi c
INNER JOIN Lavori l ON l.ID = c.ID;

ALTER TABLE Lavorichrg
  DROP FOREIGN KEY FK_lavorichrg_lavorichiusi,
  ADD CONSTRAINT FK_Lavorichrg_Lavori FOREIGN KEY (ID) REFERENCES Lavori (ID) ON DELETE RESTRICT ON UPDATE RESTRICT;

DROP TABLE Lavorichiusi;
