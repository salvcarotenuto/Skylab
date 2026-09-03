-- Coda dei consuntivi confermati dal mobile. L'acquisizione nel lavoro resta un'azione esplicita del back-office.
CREATE TABLE IF NOT EXISTS MobileConsuntivi (
  ID BIGINT NOT NULL AUTO_INCREMENT,
  SubmissionId CHAR(36) NOT NULL,
  Lavoro_ID INT NOT NULL,
  Username VARCHAR(100) NOT NULL,
  Payload JSON NOT NULL,
  Stato VARCHAR(20) NOT NULL DEFAULT 'RICEVUTO',
  RicevutoIl DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  AcquisitoIl DATETIME NULL,
  Errore VARCHAR(500) NULL,
  PRIMARY KEY (ID),
  UNIQUE KEY UX_MobileConsuntivi_SubmissionId (SubmissionId),
  KEY IX_MobileConsuntivi_LavoroStato (Lavoro_ID, Stato),
  CONSTRAINT FK_MobileConsuntivi_Lavori FOREIGN KEY (Lavoro_ID) REFERENCES Lavori(ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
