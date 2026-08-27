CREATE TABLE IF NOT EXISTS fatture_legacy_backup LIKE fatture;
INSERT INTO fatture_legacy_backup SELECT * FROM fatture
WHERE NOT EXISTS (SELECT 1 FROM fatture_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS fatturerg_legacy_backup LIKE fatturerg;
INSERT INTO fatturerg_legacy_backup SELECT * FROM fatturerg
WHERE NOT EXISTS (SELECT 1 FROM fatturerg_legacy_backup LIMIT 1);

ALTER TABLE fatture
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Settore TINYINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  ADD CONSTRAINT UQ_fatture_Anno_Settore_Codice UNIQUE (Anno,Settore,Codice),
  ADD INDEX IX_fatture_DataDoc (DataDoc),
  ADD INDEX IX_fatture_CliFor_Ditta (CliFor,Ditta);

ALTER TABLE fatturerg
  ADD COLUMN ID INT NULL FIRST;

UPDATE fatturerg r
JOIN fatture h
  ON h.Anno=r.Anno AND h.Settore=r.Settore AND h.Codice=r.Codice
SET r.ID=h.ID;

ALTER TABLE fatturerg
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Settore TINYINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN Riga SMALLINT NOT NULL,
  ADD CONSTRAINT PK_fatturerg PRIMARY KEY (ID,Riga),
  ADD INDEX IX_fatturerg_Anno_Settore_Codice_Riga (Anno,Settore,Codice,Riga),
  ADD INDEX IX_fatturerg_Articolo (Articolo),
  ADD CONSTRAINT FK_fatturerg_fatture FOREIGN KEY (ID)
    REFERENCES fatture(ID) ON UPDATE RESTRICT ON DELETE CASCADE;
