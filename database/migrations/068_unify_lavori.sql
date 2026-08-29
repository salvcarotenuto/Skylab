-- Unifica pianificazione e consuntivo nella tabella madre Lavori.
-- StatoLavoro descrive il flusso; EsitoLavoro descrive il risultato tecnico.

CREATE TABLE StatiLavoro (
  ID TINYINT NOT NULL,
  Descrizione VARCHAR(30) NOT NULL,
  Ordine TINYINT NOT NULL,
  Chiuso TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (ID),
  UNIQUE KEY UQ_StatiLavoro_Descrizione (Descrizione)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO StatiLavoro (ID, Descrizione, Ordine, Chiuso) VALUES
  (1, 'Pianificato', 10, 0),
  (2, 'Assegnato', 20, 0),
  (3, 'In corso', 30, 0),
  (4, 'Sospeso', 40, 0),
  (5, 'Completato', 50, 1),
  (6, 'Non effettuato', 60, 1),
  (7, 'Annullato', 70, 1);

CREATE TABLE EsitiLavoro (
  ID TINYINT NOT NULL,
  Descrizione VARCHAR(30) NOT NULL,
  Ordine TINYINT NOT NULL,
  PRIMARY KEY (ID),
  UNIQUE KEY UQ_EsitiLavoro_Descrizione (Descrizione)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO EsitiLavoro (ID, Descrizione, Ordine) VALUES
  (1, 'Non risolto', 10),
  (2, 'Parzialmente risolto', 20),
  (3, 'Risolto', 30);

UPDATE Lavori SET OraLav = NULL WHERE COALESCE(OraLav, '') = '';

ALTER TABLE Lavori
  CHANGE COLUMN DataReg DataRedazione DATE NULL,
  CHANGE COLUMN DataLav DataInterventoPianificata DATE NULL,
  CHANGE COLUMN OraLav OraInterventoPianificata TIME NULL,
  CHANGE COLUMN XDataLav DataUltimoIntervento DATE NULL,
  CHANGE COLUMN Incaricato OperatoreAssegnato SMALLINT NOT NULL DEFAULT 0,
  CHANGE COLUMN Descrizione DescrizioneSintetica VARCHAR(255) NULL,
  CHANGE COLUMN Attivita IstruzioniOperative TEXT NULL,
  CHANGE COLUMN PrezzoLav ImportoManodoperaPreventivato DECIMAL(12,2) NOT NULL DEFAULT 0,
  CHANGE COLUMN PrezzoMpr ImportoMaterialiPreventivato DECIMAL(12,2) NOT NULL DEFAULT 0,
  CHANGE COLUMN PrezzoRic ImportoRichiesto DECIMAL(12,2) NOT NULL DEFAULT 0,
  CHANGE COLUMN PrezzoInc ImportoIncassato DECIMAL(12,2) NOT NULL DEFAULT 0,
  ADD COLUMN StatoLavoro_ID TINYINT NULL AFTER OperatoreAssegnato,
  ADD COLUMN EsitoLavoro_ID TINYINT NULL AFTER StatoLavoro_ID,
  ADD COLUMN DataInterventoEffettiva DATE NULL AFTER EsitoLavoro_ID,
  ADD COLUMN OraInterventoEffettiva TIME NULL AFTER DataInterventoEffettiva,
  ADD COLUMN OperatoreEsecutore SMALLINT NULL AFTER OraInterventoEffettiva,
  ADD COLUMN OreUomoConsuntive DECIMAL(8,2) NULL AFTER OperatoreEsecutore,
  ADD COLUMN AttivitaEseguita TEXT NULL AFTER IstruzioniOperative,
  ADD COLUMN ImportoManodoperaConsuntivo DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER PrezzoTot,
  ADD COLUMN ImportoMaterialiConsuntivo DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER ImportoManodoperaConsuntivo,
  ADD COLUMN Fattura_ID INT NULL AFTER ImportoIncassato,
  ADD COLUMN NoteConsuntive VARCHAR(255) NULL AFTER Fattura_ID;

UPDATE Lavori l
LEFT JOIN Lavorichiusi c ON c.ID = l.ID
SET l.StatoLavoro_ID = CASE
      WHEN c.ID IS NOT NULL OR l.Eseguito <> 0 THEN 5
      WHEN l.OperatoreAssegnato <> 0 THEN 2
      ELSE 1
    END,
    l.EsitoLavoro_ID = CASE WHEN c.Esito IN (1, 2, 3) THEN c.Esito ELSE NULL END,
    l.DataInterventoEffettiva = c.DataExe,
    l.OraInterventoEffettiva = CASE
      WHEN COALESCE(c.OraExe, '') = '' THEN NULL
      ELSE STR_TO_DATE(c.OraExe, '%H:%i')
    END,
    l.OperatoreEsecutore = NULLIF(c.Operatore, 0),
    l.OreUomoConsuntive = c.OreUomo,
    l.AttivitaEseguita = NULLIF(c.Attivita, ''),
    l.ImportoManodoperaConsuntivo = COALESCE(c.PrezzoLav, 0),
    l.ImportoMaterialiConsuntivo = COALESCE(c.PrezzoMpr, 0),
    l.ImportoRichiesto = CASE WHEN c.ID IS NULL THEN l.ImportoRichiesto ELSE COALESCE(c.PrezzoRic, 0) END,
    l.ImportoIncassato = CASE WHEN c.ID IS NULL THEN l.ImportoIncassato ELSE COALESCE(c.PrezzoInc, 0) END,
    l.NoteConsuntive = NULLIF(c.Notes, '');

ALTER TABLE Lavori
  MODIFY COLUMN StatoLavoro_ID TINYINT NOT NULL,
  DROP COLUMN PrezzoTot,
  ADD COLUMN ImportoPreventivato DECIMAL(12,2)
    GENERATED ALWAYS AS (ImportoManodoperaPreventivato + ImportoMaterialiPreventivato) STORED
    AFTER ImportoMaterialiPreventivato,
  DROP COLUMN Eseguito,
  ADD KEY IX_Lavori_Stato_DataPianificata (StatoLavoro_ID, DataInterventoPianificata),
  ADD KEY IX_Lavori_Esito (EsitoLavoro_ID),
  ADD KEY IX_Lavori_Fattura (Fattura_ID),
  ADD CONSTRAINT FK_Lavori_StatiLavoro FOREIGN KEY (StatoLavoro_ID) REFERENCES StatiLavoro (ID),
  ADD CONSTRAINT FK_Lavori_EsitiLavoro FOREIGN KEY (EsitoLavoro_ID) REFERENCES EsitiLavoro (ID),
  ADD CONSTRAINT FK_Lavori_Fatture FOREIGN KEY (Fattura_ID) REFERENCES Fatture (ID) ON DELETE RESTRICT ON UPDATE CASCADE;

CREATE TABLE LavoriStorico (
  ID BIGINT NOT NULL AUTO_INCREMENT,
  Lavoro_ID INT NOT NULL,
  DataEvento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  TipoEvento VARCHAR(40) NOT NULL,
  StatoPrecedente_ID TINYINT NULL,
  StatoNuovo_ID TINYINT NULL,
  EsitoPrecedente_ID TINYINT NULL,
  EsitoNuovo_ID TINYINT NULL,
  DataScadenzaPrecedente DATE NULL,
  DataScadenzaNuova DATE NULL,
  DataPianificataPrecedente DATE NULL,
  DataPianificataNuova DATE NULL,
  DataInterventoEffettiva DATE NULL,
  DataSaltata DATE NULL,
  DataRiallineata DATE NULL,
  Note VARCHAR(255) NULL,
  DatiPrecedenti JSON NULL,
  DatiNuovi JSON NULL,
  Utente_ID SMALLINT NULL,
  PRIMARY KEY (ID),
  KEY IX_LavoriStorico_Lavoro_Data (Lavoro_ID, DataEvento),
  KEY IX_LavoriStorico_Tipo_Data (TipoEvento, DataEvento),
  CONSTRAINT FK_LavoriStorico_Lavori FOREIGN KEY (Lavoro_ID) REFERENCES Lavori (ID) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT FK_LavoriStorico_StatoPrecedente FOREIGN KEY (StatoPrecedente_ID) REFERENCES StatiLavoro (ID),
  CONSTRAINT FK_LavoriStorico_StatoNuovo FOREIGN KEY (StatoNuovo_ID) REFERENCES StatiLavoro (ID),
  CONSTRAINT FK_LavoriStorico_EsitoPrecedente FOREIGN KEY (EsitoPrecedente_ID) REFERENCES EsitiLavoro (ID),
  CONSTRAINT FK_LavoriStorico_EsitoNuovo FOREIGN KEY (EsitoNuovo_ID) REFERENCES EsitiLavoro (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO LavoriStorico
  (Lavoro_ID, TipoEvento, StatoNuovo_ID, EsitoNuovo_ID,
   DataPianificataNuova, DataInterventoEffettiva, Note, DatiPrecedenti)
SELECT c.ID,
       'MIGRAZIONE_CONSUNTIVO_LEGACY',
       5,
       CASE WHEN c.Esito IN (1, 2, 3) THEN c.Esito ELSE NULL END,
       l.DataInterventoPianificata,
       c.DataExe,
       CONCAT_WS(' - ',
         CASE WHEN COALESCE(c.Descrizione, '') <> COALESCE(l.DescrizioneSintetica, '')
              THEN 'Descrizione legacy divergente conservata nei dati precedenti' END),
       JSON_OBJECT(
         'DescrizioneLegacy', c.Descrizione,
         'AnnoLegacy', c.Anno,
         'CodiceLegacy', c.Codice)
FROM Lavorichiusi c
INNER JOIN Lavori l ON l.ID = c.ID;

DROP TABLE Lavorichiusi;
