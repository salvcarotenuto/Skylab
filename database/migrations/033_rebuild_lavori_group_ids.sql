CREATE TABLE IF NOT EXISTS lavori_legacy_backup LIKE lavori;
INSERT INTO lavori_legacy_backup SELECT * FROM lavori
WHERE NOT EXISTS (SELECT 1 FROM lavori_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS lavorirg_legacy_backup LIKE lavorirg;
INSERT INTO lavorirg_legacy_backup SELECT * FROM lavorirg
WHERE NOT EXISTS (SELECT 1 FROM lavorirg_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS lavorich_legacy_backup LIKE lavorich;
INSERT INTO lavorich_legacy_backup SELECT * FROM lavorich
WHERE NOT EXISTS (SELECT 1 FROM lavorich_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS lavorichrg_legacy_backup LIKE lavorichrg;
INSERT INTO lavorichrg_legacy_backup SELECT * FROM lavorichrg
WHERE NOT EXISTS (SELECT 1 FROM lavorichrg_legacy_backup LIMIT 1);

CREATE TABLE IF NOT EXISTS lavorifoto_legacy_backup LIKE lavorifoto;
INSERT INTO lavorifoto_legacy_backup SELECT * FROM lavorifoto
WHERE NOT EXISTS (SELECT 1 FROM lavorifoto_legacy_backup LIMIT 1);

ALTER TABLE lavori
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  ADD CONSTRAINT UQ_lavori_Anno_Codice UNIQUE (Anno,Codice),
  ADD INDEX IX_lavori_DataLav (DataLav),
  ADD INDEX IX_lavori_Cliente (Cliente);

ALTER TABLE lavorich ADD COLUMN ID INT NULL FIRST;
UPDATE lavorich c
JOIN lavori l ON l.Anno=c.Anno AND l.Codice=c.Codice
SET c.ID=l.ID;
ALTER TABLE lavorich
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  ADD CONSTRAINT PK_lavorichiusi PRIMARY KEY (ID),
  ADD CONSTRAINT UQ_lavorichiusi_Anno_Codice UNIQUE (Anno,Codice),
  ADD INDEX IX_lavorichiusi_DataExe (DataExe),
  ADD CONSTRAINT FK_lavorichiusi_lavori FOREIGN KEY (ID)
    REFERENCES lavori(ID) ON UPDATE RESTRICT ON DELETE RESTRICT;
RENAME TABLE lavorich TO lavorichiusi;

ALTER TABLE lavorirg
  ADD COLUMN ID INT NULL FIRST,
  ADD COLUMN _MigrationRowId BIGINT NOT NULL AUTO_INCREMENT UNIQUE;
UPDATE lavorirg r
JOIN lavori l ON l.Anno=r.Anno AND l.Codice=r.Codice
SET r.ID=l.ID;
UPDATE lavorirg r
JOIN (
  SELECT _MigrationRowId,ROW_NUMBER() OVER (PARTITION BY ID ORDER BY Riga,_MigrationRowId) NewRiga
  FROM lavorirg
) x ON x._MigrationRowId=r._MigrationRowId
SET r.Riga=x.NewRiga;
ALTER TABLE lavorirg
  DROP COLUMN _MigrationRowId,
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN Riga SMALLINT NOT NULL,
  MODIFY COLUMN Articolo VARCHAR(30) NULL,
  ADD CONSTRAINT PK_lavorirg PRIMARY KEY (ID,Riga),
  ADD INDEX IX_lavorirg_Anno_Codice_Riga (Anno,Codice,Riga),
  ADD INDEX IX_lavorirg_Articolo (Articolo),
  ADD CONSTRAINT FK_lavorirg_lavori FOREIGN KEY (ID)
    REFERENCES lavori(ID) ON UPDATE RESTRICT ON DELETE CASCADE;

ALTER TABLE lavorichrg
  ADD COLUMN ID INT NULL FIRST,
  ADD COLUMN _MigrationRowId BIGINT NOT NULL AUTO_INCREMENT UNIQUE;
UPDATE lavorichrg r
JOIN lavorichiusi l ON l.Anno=r.Anno AND l.Codice=r.Codice
SET r.ID=l.ID;
UPDATE lavorichrg r
JOIN (
  SELECT _MigrationRowId,ROW_NUMBER() OVER (PARTITION BY ID ORDER BY Riga,_MigrationRowId) NewRiga
  FROM lavorichrg
) x ON x._MigrationRowId=r._MigrationRowId
SET r.Riga=x.NewRiga;
ALTER TABLE lavorichrg
  DROP COLUMN _MigrationRowId,
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN Riga SMALLINT NOT NULL,
  ADD CONSTRAINT PK_lavorichrg PRIMARY KEY (ID,Riga),
  ADD INDEX IX_lavorichrg_Anno_Codice_Riga (Anno,Codice,Riga),
  ADD INDEX IX_lavorichrg_Articolo (Articolo),
  ADD CONSTRAINT FK_lavorichrg_lavorichiusi FOREIGN KEY (ID)
    REFERENCES lavorichiusi(ID) ON UPDATE RESTRICT ON DELETE CASCADE;

ALTER TABLE lavorifoto ADD COLUMN ID INT NULL FIRST;
UPDATE lavorifoto f
JOIN lavori l ON l.Anno=f.Anno AND l.Codice=f.Codice
SET f.ID=l.ID;
ALTER TABLE lavorifoto
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN FileName VARCHAR(255) NOT NULL,
  ADD CONSTRAINT PK_lavorifoto PRIMARY KEY (ID,FileName),
  ADD INDEX IX_lavorifoto_Anno_Codice (Anno,Codice),
  ADD CONSTRAINT FK_lavorifoto_lavori FOREIGN KEY (ID)
    REFERENCES lavori(ID) ON UPDATE RESTRICT ON DELETE CASCADE;
