-- SkyLabdb: ricostruzione della chiave tecnica di carico e della relazione delle righe.
ALTER TABLE carico
  MODIFY COLUMN ID INT NOT NULL AUTO_INCREMENT;

UPDATE caricorg r
INNER JOIN carico c ON c.Anno=r.Anno AND c.Codice=r.Codice
SET r.ID=c.ID;

-- La verifica delle righe ancora orfane va eseguita prima di introdurre PK/FK su caricorg.
SELECT r.Anno, r.Codice, r.Riga, r.Articolo
FROM caricorg r
LEFT JOIN carico c ON c.ID=r.ID
WHERE c.ID IS NULL;
