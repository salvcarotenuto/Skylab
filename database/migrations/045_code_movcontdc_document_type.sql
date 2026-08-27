UPDATE movcontdc
SET TipoDoc = 'V'
WHERE TipoDoc = 'Fattura';

ALTER TABLE movcontdc
  MODIFY COLUMN TipoDoc VARCHAR(1) NOT NULL;
