ALTER TABLE movcont
  ADD COLUMN Documento INT NULL AFTER Descrizione,
  ADD INDEX IX_movcont_Documento (Documento);

CREATE TABLE movcontdc
(
  Mov_Id INT NOT NULL,
  Settore TINYINT NOT NULL,
  Doc_Id INT NOT NULL,
  TipoDoc VARCHAR(20) NOT NULL,
  CONSTRAINT PK_movcontdc PRIMARY KEY (Mov_Id, TipoDoc, Doc_Id),
  INDEX IX_movcontdc_Documento (TipoDoc, Doc_Id),
  CONSTRAINT FK_movcontdc_movcont
    FOREIGN KEY (Mov_Id) REFERENCES movcont(ID)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB;
