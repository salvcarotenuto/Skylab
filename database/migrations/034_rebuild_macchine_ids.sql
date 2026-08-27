CREATE TABLE IF NOT EXISTS macchinecli_legacy_backup LIKE macchinecli;
INSERT INTO macchinecli_legacy_backup SELECT * FROM macchinecli
WHERE NOT EXISTS (SELECT 1 FROM macchinecli_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS macchinefoto_legacy_backup LIKE macchinefoto;
INSERT INTO macchinefoto_legacy_backup SELECT * FROM macchinefoto
WHERE NOT EXISTS (SELECT 1 FROM macchinefoto_legacy_backup LIMIT 1);

ALTER TABLE macchinecli
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST,
  MODIFY COLUMN Cliente INT NOT NULL,
  MODIFY COLUMN Riga TINYINT NOT NULL,
  ADD CONSTRAINT UQ_macchinecli_Cliente_Riga UNIQUE (Cliente,Riga),
  ADD INDEX IX_macchinecli_Cliente_Articolo (Cliente,Articolo),
  ADD INDEX IX_macchinecli_Articolo (Articolo),
  ADD INDEX IX_macchinecli_ProxData (ProxData),
  ADD CONSTRAINT FK_macchinecli_clienti FOREIGN KEY (Cliente)
    REFERENCES clienti(Codice) ON UPDATE RESTRICT ON DELETE RESTRICT,
  ADD CONSTRAINT FK_macchinecli_articoli FOREIGN KEY (Articolo)
    REFERENCES articoli(Codice) ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE macchinefoto
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST,
  ADD COLUMN MacchinaID INT NULL AFTER ID,
  MODIFY COLUMN Cliente INT NOT NULL,
  ADD INDEX IX_macchinefoto_MacchinaID (MacchinaID),
  ADD INDEX IX_macchinefoto_Cliente_Articolo (Cliente,Articolo),
  ADD CONSTRAINT FK_macchinefoto_macchinecli FOREIGN KEY (MacchinaID)
    REFERENCES macchinecli(ID) ON UPDATE RESTRICT ON DELETE RESTRICT;
