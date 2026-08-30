ALTER TABLE ImpegniLavoro
  ADD COLUMN Articolo VARCHAR(30) NULL AFTER MacchinaCli_ID,
  ADD KEY IX_ImpegniLavoro_Articolo (Articolo),
  ADD CONSTRAINT FK_ImpegniLavoro_Articoli FOREIGN KEY (Articolo) REFERENCES Articoli(Codice);
