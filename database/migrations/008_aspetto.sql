-- SkyLabdb: impedisce descrizioni duplicate nella tabella aspetto beni.
ALTER TABLE aspetto
  ADD CONSTRAINT UX_aspetto_Descrizione UNIQUE (Descrizione);
