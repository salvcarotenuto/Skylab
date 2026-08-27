-- Normalizza Titoli e consolida il riferimento a TipoTitoli.

CREATE TABLE titoli_before_normalize_backup_064 LIKE Titoli;
INSERT INTO titoli_before_normalize_backup_064 SELECT * FROM Titoli;

CREATE TABLE pagamenti_before_titolitipo_backup_064 LIKE Pagamenti;
INSERT INTO pagamenti_before_titolitipo_backup_064 SELECT * FROM Pagamenti;

UPDATE Titoli SET Banca = NULL WHERE Banca = 0;
UPDATE Titoli SET Mov_Id = NULL WHERE Mov_Id = 0;

ALTER TABLE Titoli
    ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
    ADD COLUMN Scadenza_Id INT NULL AFTER Cliente,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    CHANGE COLUMN Tipo TipoTitolo SMALLINT NOT NULL,
    MODIFY COLUMN Numero VARCHAR(30) NOT NULL DEFAULT '',
    MODIFY COLUMN Cliente INT NOT NULL,
    CHANGE COLUMN Scadenza DataScadenza DATE NOT NULL,
    MODIFY COLUMN Importo DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    MODIFY COLUMN Banca INT NULL DEFAULT NULL,
    CHANGE COLUMN Mov_Id MovCont_Id INT NULL DEFAULT NULL,
    MODIFY COLUMN Girato TINYINT NOT NULL DEFAULT 0,
    MODIFY COLUMN Stato TINYINT NOT NULL DEFAULT 0,
    MODIFY COLUMN Spese DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ADD PRIMARY KEY (ID),
    ADD UNIQUE KEY UX_Titoli_Anno_Codice (Anno, Codice),
    ADD KEY IX_Titoli_Cliente_DataScadenza (Cliente, DataScadenza),
    ADD KEY IX_Titoli_Banca_DataScadenza (Banca, DataScadenza),
    ADD KEY IX_Titoli_Tipo_Stato_DataScadenza (TipoTitolo, Stato, DataScadenza),
    ADD KEY IX_Titoli_Scadenza (Scadenza_Id),
    ADD KEY IX_Titoli_MovCont (MovCont_Id),
    ADD CONSTRAINT FK_Titoli_TipoTitoli
        FOREIGN KEY (TipoTitolo) REFERENCES TipoTitoli (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Titoli_Clienti
        FOREIGN KEY (Cliente) REFERENCES Clienti (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Titoli_Scadenze
        FOREIGN KEY (Scadenza_Id) REFERENCES Scadenze (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Titoli_Banche
        FOREIGN KEY (Banca) REFERENCES Banche (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Titoli_MovCont
        FOREIGN KEY (MovCont_Id) REFERENCES MovCont (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE Pagamenti
    MODIFY COLUMN TipoTitolo SMALLINT NULL DEFAULT NULL,
    ADD KEY IX_Pagamenti_TipoTitolo (TipoTitolo),
    ADD CONSTRAINT FK_Pagamenti_TipoTitoli
        FOREIGN KEY (TipoTitolo) REFERENCES TipoTitoli (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;
