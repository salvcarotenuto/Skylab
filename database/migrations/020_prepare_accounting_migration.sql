-- Fase 1: mastri MicroFish, archivi integrali e mappa dei conti legacy.
CREATE TABLE mastri (
  Codice SMALLINT NOT NULL PRIMARY KEY,
  Descrizione VARCHAR(100) NOT NULL,
  Tipo VARCHAR(1) NOT NULL,
  Locked TINYINT(1) NOT NULL DEFAULT 0,
  INDEX IX_mastri_Tipo_Descrizione (Tipo, Descrizione)
) ENGINE=InnoDB;

INSERT INTO mastri (Codice,Descrizione,Tipo,Locked) VALUES
(1,'Beni Strumentali','P',1),(3,'Crediti vs clienti','P',1),(4,'Crediti vs soci','P',1),(7,'Disponibilità Liquide','P',1),(30,'Sopravvenienze passive','C',1),(6,'Altri crediti','P',1),(2,'Partecipazioni','P',1),(11,'Debiti per finanziamenti','P',1),(12,'Debiti vs erario / Enti previdenz.','P',1),(13,'Debiti vs fornitori','P',1),(15,'Fondo TFR','P',1),(16,'Fondi per rischi e oneri','P',1),(17,'Fondi di ammortamento','P',1),(9,'Ratei E Risconti Attivi','P',1),(14,'Altri debiti','P',1),(18,'Ratei e risconti passivi','P',1),(33,'Plusvalenze','R',1),(28,'Oneri diversi di gestione','C',1),(21,'Merci conto acquisti','C',1),(22,'Costi per servizi','C',1),(32,'Ricavi da partecipazioni','R',1),(23,'Costi per l''utilizzo di beni di terzi','C',1),(24,'Costi della manodopera','C',1),(31,'Ricavi delle vendite e prestazioni','R',1),(8,'Crediti e debiti vs banche','P',1),(26,'Accantonamenti per rischi e oneri','C',1),(27,'Oneri fiscali','C',1),(25,'Ammortamento di immobilizzazioni','C',1),(35,'Proventi finanziari','R',1),(29,'Oneri finanziari','C',1),(34,'Sopravvenienze attive','R',1),(19,'Debiti vs. enti di previdenza','P',1),(5,'Titoli e Fondi Comuni','P',1);

CREATE TABLE movcont_legacy_backup LIKE movcont;
INSERT INTO movcont_legacy_backup SELECT * FROM movcont;
CREATE TABLE movcontrg_legacy_backup LIKE movcontrg;
INSERT INTO movcontrg_legacy_backup SELECT * FROM movcontrg;

CREATE TABLE conti_legacy_map (
  MastroLegacy SMALLINT NOT NULL,
  ContoLegacy INT NOT NULL,
  ContoMicrofish SMALLINT NULL,
  Regola VARCHAR(80) NOT NULL DEFAULT '',
  PRIMARY KEY (MastroLegacy, ContoLegacy),
  INDEX IX_conti_legacy_map_ContoMicrofish (ContoMicrofish),
  CONSTRAINT FK_conti_legacy_map_conti FOREIGN KEY (ContoMicrofish)
    REFERENCES conti (Codice) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

INSERT INTO conti_legacy_map (MastroLegacy,ContoLegacy,ContoMicrofish,Regola)
SELECT DISTINCT Mastro,Conto,
  CASE WHEN Mastro=11 THEN 82 WHEN Mastro=22 THEN 81
       WHEN Mastro=13 AND Conto=10 THEN 83 WHEN Mastro=28 AND Conto=10 THEN 84
       WHEN Mastro=15 AND Conto=1 THEN 61 ELSE NULL END,
  CASE WHEN Mastro=11 THEN 'Cliente -> Crediti vs clienti'
       WHEN Mastro=22 THEN 'Fornitore -> Debiti vs fornitori'
       WHEN Mastro=13 AND Conto=10 THEN 'IVA su acquisti'
       WHEN Mastro=28 AND Conto=10 THEN 'IVA su vendite'
       WHEN Mastro=15 AND Conto=1 THEN 'Cassa contanti'
       ELSE 'Da definire' END
FROM movcontrg;
