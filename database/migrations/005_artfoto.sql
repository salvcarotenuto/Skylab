-- SkyLabdb: più fotografie per ciascun articolo.
ALTER TABLE artfoto
  MODIFY COLUMN Codice VARCHAR(30) NOT NULL,
  MODIFY COLUMN FileName VARCHAR(255) NOT NULL,
  ADD CONSTRAINT PK_artfoto PRIMARY KEY (Codice, FileName),
  ADD CONSTRAINT FK_artfoto_articoli
    FOREIGN KEY (Codice) REFERENCES articoli (Codice)
    ON UPDATE CASCADE ON DELETE CASCADE;
