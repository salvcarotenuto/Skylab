ALTER TABLE fornitori
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN Nome VARCHAR(250) NOT NULL,
  ADD CONSTRAINT PK_fornitori PRIMARY KEY (Codice),
  ADD INDEX IX_fornitori_Nome_Codice (Nome,Codice),
  ADD INDEX IX_fornitori_Piva (Piva),
  ADD INDEX IX_fornitori_Codfi (Codfi);
