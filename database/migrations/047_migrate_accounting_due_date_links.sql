INSERT INTO movcontdc (Mov_Id, Settore, Doc_Id, TipoDoc)
SELECT m.ID,
       CASE
         WHEN UPPER(TRIM(COALESCE(m.CliFor, ''))) = 'C' THEN 30
         WHEN UPPER(TRIM(COALESCE(m.CliFor, ''))) = 'F' THEN 10
         ELSE m.PrtSett
       END,
       s.ID,
       'S'
FROM movcont m
JOIN scadenze s
  ON s.Anno = m.PrtAnno
 AND s.Settore = m.PrtSett
 AND s.Codice = m.PrtCode
 AND s.Numero = m.NumScad
WHERE COALESCE(m.PrtCode, 0) <> 0
  AND COALESCE(m.NumScad, 0) <> 0
ON DUPLICATE KEY UPDATE
  Settore = VALUES(Settore),
  Doc_Id = VALUES(Doc_Id);
