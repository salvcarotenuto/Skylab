-- Ricostruisce MovCont.Importo dai dettagli contabili quando dare e avere quadrano.
-- I movimenti senza righe o non quadrati restano invariati.

CREATE TABLE movcont_importo_backup_096 LIKE MovCont;
INSERT INTO movcont_importo_backup_096 SELECT * FROM MovCont;

CREATE TEMPORARY TABLE tmp_movcont_importo_096 AS
SELECT ID,
       SUM(CASE WHEN Segno = 'D' THEN Importo ELSE 0 END) AS ImportoDare,
       SUM(CASE WHEN Segno = 'A' THEN Importo ELSE 0 END) AS ImportoAvere
FROM MovContRg
GROUP BY ID
HAVING SUM(CASE WHEN Segno = 'D' THEN Importo ELSE 0 END) > 0
   AND SUM(CASE WHEN Segno = 'D' THEN Importo ELSE 0 END)
       = SUM(CASE WHEN Segno = 'A' THEN Importo ELSE 0 END);

UPDATE MovCont m
JOIN tmp_movcont_importo_096 t ON t.ID = m.ID
SET m.Importo = t.ImportoDare
WHERE m.Importo <> t.ImportoDare;

DROP TEMPORARY TABLE tmp_movcont_importo_096;
