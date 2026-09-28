-- Aliquota IVA standard applicata alle vendite quando il documento o
-- l'anagrafica articolo non forniscono un'aliquota specifica.
-- INSERT IGNORE rende l'inizializzazione idempotente e conserva sempre
-- l'eventuale valore gia configurato dall'utente.

INSERT IGNORE INTO Opzioni (Chiave, Valore)
VALUES ('AliqIvaVendite', '22.00');
