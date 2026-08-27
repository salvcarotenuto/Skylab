DELETE FROM movcontrg
WHERE Importo = 0
  AND COALESCE(Dare, 0) = 0
  AND COALESCE(Avere, 0) = 0
  AND COALESCE(TRIM(Segno), '') = '';

ALTER TABLE movcontrg
  DROP FOREIGN KEY FK_movcontrg_stage_conti,
  DROP INDEX IX_movcontrg_stage_Conto,
  DROP COLUMN Mastro,
  DROP COLUMN Conto,
  DROP COLUMN Dare,
  DROP COLUMN Avere,
  CHANGE COLUMN ContoMicrofish Conto SMALLINT NOT NULL,
  ADD INDEX IX_movcontrg_Conto_ID (Conto, ID),
  ADD CONSTRAINT FK_movcontrg_conti
    FOREIGN KEY (Conto) REFERENCES conti(Codice)
    ON UPDATE RESTRICT ON DELETE RESTRICT;
