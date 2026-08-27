-- Uniforma Ordini e OrdiniRg alla struttura MicroFish.
-- Le tabelle legacy sono conservate come copie di sicurezza.

RENAME TABLE
    Ordini TO ordini_legacy_backup_055,
    OrdiniRg TO ordinirg_legacy_backup_055;

CREATE TABLE Ordini (
    ID          INT NOT NULL AUTO_INCREMENT,
    Anno        SMALLINT NOT NULL,
    Codice      INT NOT NULL,
    DataDoc     DATE NULL,
    Fornitore   INT NULL,
    ULocale     SMALLINT NULL,
    DataSca     DATE NULL,
    DataEva     DATE NULL,
    Fattura     VARCHAR(25) NULL,
    Totale      DECIMAL(12,2) NULL,
    Evaso       TINYINT NOT NULL DEFAULT 0,
    Note        VARCHAR(255) NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UX_Ordini_Anno_Codice (Anno, Codice),
    KEY IX_Ordini_Fornitore (Fornitore),
    KEY IX_Ordini_DataDoc (DataDoc)
) ENGINE=InnoDB;

INSERT INTO Ordini
    (Anno, Codice, DataDoc, Fornitore, ULocale, DataSca, DataEva,
     Fattura, Totale, Evaso, Note)
SELECT
    Anno,
    Codice,
    DataDoc,
    Ditta,
    ULocale,
    NULL,
    NULL,
    NULLIF(TRIM(NumFattura), ''),
    CAST(Totale AS DECIMAL(12,2)),
    0,
    NULLIF(TRIM(Notes), '')
FROM ordini_legacy_backup_055
ORDER BY Anno, Settore, Codice;

CREATE TABLE OrdiniRg (
    ID          INT NOT NULL,
    Anno        SMALLINT NOT NULL,
    Codice      INT NOT NULL,
    Riga        SMALLINT NOT NULL,
    Articolo    VARCHAR(30) NULL,
    Ums         VARCHAR(10) NULL,
    Quantita    DECIMAL(12,3) NULL,
    Costo       DECIMAL(12,3) NULL,
    AliqIva     DECIMAL(5,2) NULL,
    Importo     DECIMAL(12,2) NULL,
    Evaso       DECIMAL(12,3) NOT NULL DEFAULT 0,
    PRIMARY KEY (ID, Riga),
    KEY IX_OrdiniRg_Articolo (Articolo),
    CONSTRAINT FK_OrdiniRg_Ordini
        FOREIGN KEY (ID) REFERENCES Ordini (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

INSERT INTO OrdiniRg
    (ID, Anno, Codice, Riga, Articolo, Ums, Quantita, Costo,
     AliqIva, Importo, Evaso)
SELECT
    o.ID,
    r.Anno,
    r.Codice,
    r.Riga,
    NULLIF(TRIM(r.Articolo), ''),
    NULLIF(TRIM(r.Um), ''),
    CAST(r.Quantita AS DECIMAL(12,3)),
    CAST(r.Costo AS DECIMAL(12,3)),
    NULL,
    CAST(r.Importo AS DECIMAL(12,2)),
    0
FROM ordinirg_legacy_backup_055 r
JOIN Ordini o
  ON o.Anno = r.Anno
 AND o.Codice = r.Codice
ORDER BY r.Anno, r.Settore, r.Codice, r.Riga;
