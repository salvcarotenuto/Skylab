ALTER TABLE Lavori
  ADD COLUMN ImportoPreventivoNetto DECIMAL(12,2) NOT NULL DEFAULT 0
  AFTER ImportoPreventivato;

UPDATE Lavori
SET ImportoPreventivoNetto = ImportoPreventivato;
