-- SkyLabdb: revisione della tabella operativa dei listini articolo.
UPDATE artlistini
SET Listino=COALESCE(Listino,0),
    Prezzo=COALESCE(Prezzo,0),
    PrIvato=COALESCE(PrIvato,0),
    Ricarico=COALESCE(Ricarico,0);

ALTER TABLE artlistini
  MODIFY COLUMN Codice VARCHAR(30) NOT NULL,
  MODIFY COLUMN Listino TINYINT NOT NULL,
  MODIFY COLUMN Prezzo DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN PrIvato DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN Ricarico DECIMAL(5,2) NOT NULL DEFAULT 0,
  ADD CONSTRAINT PK_artlistini PRIMARY KEY (Codice, Listino),
  ADD INDEX IX_artlistini_Listino_Codice (Listino, Codice),
  ADD CONSTRAINT FK_artlistini_articoli
    FOREIGN KEY (Codice) REFERENCES articoli (Codice)
    ON UPDATE CASCADE ON DELETE CASCADE;
