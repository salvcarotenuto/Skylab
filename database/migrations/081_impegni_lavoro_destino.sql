ALTER TABLE ImpegniLavoro
  ADD COLUMN Destino_ID INT NULL AFTER Cliente_ID,
  ADD KEY IX_ImpegniLavoro_Destino (Destino_ID),
  ADD CONSTRAINT FK_ImpegniLavoro_Destini FOREIGN KEY (Destino_ID) REFERENCES Destini(ID);
