ALTER TABLE Lavori
  ADD COLUMN ScaricatoLavorazione TINYINT(1) NOT NULL DEFAULT 0 AFTER OperatoreAssegnato,
  ADD COLUMN DataScaricoLavorazione DATETIME NULL AFTER ScaricatoLavorazione,
  ADD INDEX IX_Lavori_ScaricoOperatore (ScaricatoLavorazione, OperatoreAssegnato, DataInterventoPianificata);
