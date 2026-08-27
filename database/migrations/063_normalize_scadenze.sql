-- Normalizza Scadenze collegandola direttamente a MovIva.

CREATE TABLE scadenze_before_normalize_backup_063 LIKE Scadenze;
INSERT INTO scadenze_before_normalize_backup_063 SELECT * FROM Scadenze;

ALTER TABLE Scadenze
    ADD COLUMN MovIva_Id INT NULL AFTER ID,
    ADD COLUMN DataPagamento DATE NULL AFTER Pagato;

UPDATE Scadenze s
JOIN moviva_before_types_backup_061 b
  ON b.Anno = s.Anno
 AND b.Settore = s.Settore
 AND b.Codice = s.Codice
JOIN MovIva v ON v.ID = b.ID
SET s.MovIva_Id = v.ID;

UPDATE Scadenze s
JOIN MovContDc dc
  ON dc.TipoDoc = 'SD'
 AND dc.Doc_Id = s.ID
JOIN MovCont m ON m.ID = dc.Mov_Id
SET s.DataPagamento = m.DataMov
WHERE s.Pagato = 1;

UPDATE Scadenze SET Banca = NULL WHERE Banca = 0;

ALTER TABLE Scadenze
    DROP INDEX UX_scadenze_Mnemonica,
    DROP COLUMN Anno,
    DROP COLUMN Settore,
    DROP COLUMN Codice,
    DROP COLUMN Tipo,
    DROP COLUMN Sigla,
    DROP COLUMN NumDoc,
    DROP COLUMN DataDoc,
    DROP COLUMN CliFor,
    DROP COLUMN Ditta,
    DROP COLUMN TotaleDoc,
    CHANGE COLUMN CodPag CodPagamento SMALLINT NOT NULL,
    CHANGE COLUMN DataScad DataScadenza DATE NOT NULL,
    CHANGE COLUMN Pagato Pagata TINYINT NOT NULL DEFAULT 0,
    MODIFY COLUMN MovIva_Id INT NOT NULL,
    MODIFY COLUMN Numero SMALLINT NOT NULL,
    MODIFY COLUMN NumTitolo VARCHAR(25) NULL DEFAULT '',
    MODIFY COLUMN Importo DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    MODIFY COLUMN Banca INT NULL DEFAULT NULL,
    ADD UNIQUE KEY UX_Scadenze_MovIva_Numero (MovIva_Id, Numero),
    ADD KEY IX_Scadenze_Pagata_DataScadenza (Pagata, DataScadenza),
    ADD KEY IX_Scadenze_CodPagamento (CodPagamento),
    ADD KEY IX_Scadenze_Banca (Banca),
    ADD CONSTRAINT FK_Scadenze_MovIva
        FOREIGN KEY (MovIva_Id) REFERENCES MovIva (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Scadenze_Pagamenti
        FOREIGN KEY (CodPagamento) REFERENCES Pagamenti (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Scadenze_Banche
        FOREIGN KEY (Banca) REFERENCES Banche (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;
