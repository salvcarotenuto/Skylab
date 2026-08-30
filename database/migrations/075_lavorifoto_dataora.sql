ALTER TABLE Lavorifoto
  ADD COLUMN DataOraFoto DATETIME NULL AFTER FileName,
  ADD KEY IX_Lavorifoto_DataOra (ID, DataOraFoto);
