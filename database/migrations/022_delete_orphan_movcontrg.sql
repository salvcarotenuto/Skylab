-- Elimina le righe contabili prive di testata.
-- La copia integrale precedente è conservata in movcontrg_legacy_backup.
DELETE r
FROM movcontrg r
LEFT JOIN movcont m
  ON m.Anno=r.Anno AND m.Settore=r.Settore AND m.Codice=r.Codice
WHERE m.Codice IS NULL;
