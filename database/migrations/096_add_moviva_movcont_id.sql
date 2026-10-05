-- Database release 1.26.10.05.
-- Add the reciprocal link from an IVA document to its accounting movement.

SET @movcont_id_column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'Moviva'
      AND column_name = 'Movcont_Id'
);

SET @movcont_id_ddl = IF(
    @movcont_id_column_exists = 0,
    'ALTER TABLE Moviva ADD COLUMN Movcont_Id INT NULL AFTER ULocale',
    'SELECT 1'
);

PREPARE add_movcont_id FROM @movcont_id_ddl;
EXECUTE add_movcont_id;
DEALLOCATE PREPARE add_movcont_id;

UPDATE Moviva AS iva
JOIN MovCont AS mov ON mov.Documento = iva.ID
                    AND mov.Settore = iva.Settore
SET iva.Movcont_Id = mov.ID
WHERE iva.Movcont_Id IS NULL
  AND iva.Settore IN (10, 30);
