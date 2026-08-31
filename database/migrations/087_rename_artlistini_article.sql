-- Migrazione già applicata manualmente al database operativo.
ALTER TABLE ArtListini
  RENAME COLUMN Codice TO Articolo,
  RENAME INDEX IX_artlistini_Listino_Codice TO IX_ArtListini_Listino_Articolo;
