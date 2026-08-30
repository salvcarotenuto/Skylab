CREATE TABLE IF NOT EXISTS LavoriDocumenti (
    Lavoro_ID INT NOT NULL,
    Numero SMALLINT NOT NULL,
    NomeFile VARCHAR(160) NOT NULL,
    NomeOriginale VARCHAR(255) NOT NULL,
    DataOra DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Descrizione VARCHAR(255) NULL,
    PRIMARY KEY (Lavoro_ID, Numero),
    INDEX IX_LavoriDocumenti_DataOra (Lavoro_ID, DataOra),
    CONSTRAINT FK_LavoriDocumenti_Lavori FOREIGN KEY (Lavoro_ID) REFERENCES Lavori(ID) ON DELETE CASCADE
) ENGINE=InnoDB;
