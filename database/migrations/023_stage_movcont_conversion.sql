-- Conversione intermedia non distruttiva di movcont/movcontrg al modello ID MicroFish.
ALTER TABLE movcont
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST,
  ADD CONSTRAINT UX_movcont_Anno_Settore_Codice UNIQUE (Anno,Settore,Codice);

ALTER TABLE movcontrg
  ADD COLUMN ID INT NULL FIRST,
  ADD COLUMN ContoMicrofish SMALLINT NULL AFTER Conto,
  ADD COLUMN Importo DECIMAL(12,2) NULL AFTER Avere,
  ADD COLUMN Segno VARCHAR(1) NULL AFTER Importo;

UPDATE movcontrg r
JOIN movcont m ON m.Anno=r.Anno AND m.Settore=r.Settore AND m.Codice=r.Codice
JOIN conti_legacy_map x ON x.MastroLegacy=r.Mastro AND x.ContoLegacy=r.Conto
SET r.ID=m.ID,
    r.ContoMicrofish=x.ContoMicrofish,
    r.Importo=ABS(CASE WHEN COALESCE(r.Dare,0)<>0 THEN r.Dare ELSE COALESCE(r.Avere,0) END),
    r.Segno=CASE
      WHEN COALESCE(r.Dare,0)>0 OR COALESCE(r.Avere,0)<0 THEN 'D'
      WHEN COALESCE(r.Avere,0)>0 OR COALESCE(r.Dare,0)<0 THEN 'A'
      ELSE '' END;

ALTER TABLE movcontrg
  MODIFY COLUMN ID INT NOT NULL,
  MODIFY COLUMN ContoMicrofish SMALLINT NOT NULL,
  MODIFY COLUMN Importo DECIMAL(12,2) NOT NULL DEFAULT 0,
  MODIFY COLUMN Segno VARCHAR(1) NOT NULL DEFAULT '',
  ADD CONSTRAINT PK_movcontrg_stage PRIMARY KEY (ID,Riga),
  ADD INDEX IX_movcontrg_stage_Mnemonica (Anno,Settore,Codice,Riga),
  ADD INDEX IX_movcontrg_stage_Conto (ContoMicrofish,ID),
  ADD CONSTRAINT FK_movcontrg_stage_movcont FOREIGN KEY (ID) REFERENCES movcont(ID)
    ON UPDATE RESTRICT ON DELETE CASCADE,
  ADD CONSTRAINT FK_movcontrg_stage_conti FOREIGN KEY (ContoMicrofish) REFERENCES conti(Codice)
    ON UPDATE CASCADE ON DELETE RESTRICT;
