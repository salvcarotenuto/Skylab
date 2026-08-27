UPDATE movcont m
JOIN fatture f
  ON f.Anno = m.PrtAnno
 AND f.Codice = m.PrtCode
SET m.Documento = f.ID
WHERE COALESCE(m.PrtCode, 0) <> 0;

INSERT INTO movcontdc (Mov_Id, Settore, Doc_Id, TipoDoc)
SELECT m.ID,
       CASE
         WHEN UPPER(TRIM(COALESCE(m.CliFor, ''))) = 'C' THEN 30
         WHEN UPPER(TRIM(COALESCE(m.CliFor, ''))) = 'F' THEN 10
         ELSE m.PrtSett
       END,
       f.ID,
       'Fattura'
FROM movcont m
JOIN fatture f
  ON f.Anno = m.PrtAnno
 AND f.Codice = m.PrtCode
WHERE COALESCE(m.PrtCode, 0) <> 0
ON DUPLICATE KEY UPDATE
  Settore = VALUES(Settore),
  Doc_Id = VALUES(Doc_Id);
