-- Catalogo IVA normalizzato sulle specifiche della fattura elettronica.
-- Fonti istituzionali:
-- https://www.fatturapa.gov.it/export/documenti/fatturapa/v1.3/Rappresentazione-tabellare-fattura-ordinaria.pdf
-- https://peppol-docs.agid.gov.it/xml/ITA/peppol-bis-invoice-3/codelist/Natura_VATCategory_VATEX.html
--
-- I codici aziendali non elencati in questa migrazione non vengono modificati.

CREATE TABLE IF NOT EXISTS FECodiciIva_backup_20260924 LIKE FECodiciIva;
INSERT IGNORE INTO FECodiciIva_backup_20260924
SELECT * FROM FECodiciIva;

CREATE TABLE IF NOT EXISTS CodiciIva_backup_20260924 LIKE CodiciIva;
INSERT IGNORE INTO CodiciIva_backup_20260924
SELECT * FROM CodiciIva;

INSERT INTO FECodiciIva (Codice, Descrizione) VALUES
('N1',   'escluse ex art. 15 del DPR 633/72'),
('N2',   'non soggette (codice non piu valido dal 1 gennaio 2021)'),
('N2.1', 'non soggette ad IVA ai sensi degli artt. da 7 a 7-septies del DPR 633/72'),
('N2.2', 'non soggette - altri casi'),
('N3',   'non imponibili (codice non piu valido dal 1 gennaio 2021)'),
('N3.1', 'non imponibili - esportazioni'),
('N3.2', 'non imponibili - cessioni intracomunitarie'),
('N3.3', 'non imponibili - cessioni verso San Marino'),
('N3.4', 'non imponibili - operazioni assimilate alle cessioni all''esportazione'),
('N3.5', 'non imponibili - a seguito di dichiarazioni d''intento'),
('N3.6', 'non imponibili - altre operazioni che non concorrono alla formazione del plafond'),
('N4',   'esenti'),
('N5',   'regime del margine / IVA non esposta in fattura'),
('N6',   'inversione contabile (codice non piu valido dal 1 gennaio 2021)'),
('N6.1', 'inversione contabile - cessione di rottami e altri materiali di recupero'),
('N6.2', 'inversione contabile - cessione di oro e argento puro'),
('N6.3', 'inversione contabile - subappalto nel settore edile'),
('N6.4', 'inversione contabile - cessione di fabbricati'),
('N6.5', 'inversione contabile - cessione di telefoni cellulari'),
('N6.6', 'inversione contabile - cessione di prodotti elettronici'),
('N6.7', 'inversione contabile - prestazioni comparto edile e settori connessi'),
('N6.8', 'inversione contabile - operazioni settore energetico'),
('N6.9', 'inversione contabile - altri casi'),
('N7',   'IVA assolta in altro Stato UE')
ON DUPLICATE KEY UPDATE
    Descrizione = VALUES(Descrizione);

INSERT INTO CodiciIva (Codice, Descrizione, Aliquota, Detrazione, CodiceFE) VALUES
('04',   'Aliquota ridotta 4%', 4.00, 100.00, NULL),
('05',   'Aliquota ridotta 5%', 5.00, 100.00, NULL),
('10',   'Aliquota ridotta 10%', 10.00, 100.00, NULL),
('22',   'Aliquota ordinaria 22%', 22.00, 100.00, NULL),
('N1',   'Escluse ex art. 15 del DPR 633/72', 0.00, 0.00, 'N1'),
('N2',   'Non soggette - codice non piu valido dal 1 gennaio 2021', 0.00, 0.00, 'N2'),
('N2.1', 'Non soggette ad IVA ai sensi degli artt. da 7 a 7-septies del DPR 633/72', 0.00, 0.00, 'N2.1'),
('N2.2', 'Non soggette - altri casi', 0.00, 0.00, 'N2.2'),
('N3',   'Non imponibili - codice non piu valido dal 1 gennaio 2021', 0.00, 0.00, 'N3'),
('N3.1', 'Non imponibili - esportazioni', 0.00, 0.00, 'N3.1'),
('N3.2', 'Non imponibili - cessioni intracomunitarie', 0.00, 0.00, 'N3.2'),
('N3.3', 'Non imponibili - cessioni verso San Marino', 0.00, 0.00, 'N3.3'),
('N3.4', 'Non imponibili - operazioni assimilate alle cessioni all''esportazione', 0.00, 0.00, 'N3.4'),
('N3.5', 'Non imponibili - a seguito di dichiarazioni d''intento', 0.00, 0.00, 'N3.5'),
('N3.6', 'Non imponibili - altre operazioni che non concorrono alla formazione del plafond', 0.00, 0.00, 'N3.6'),
('N4',   'Esenti', 0.00, 0.00, 'N4'),
('N5',   'Regime del margine / IVA non esposta in fattura', 0.00, 0.00, 'N5'),
('N6',   'Inversione contabile - codice non piu valido dal 1 gennaio 2021', 0.00, 0.00, 'N6'),
('N6.1', 'Inversione contabile - cessione di rottami e altri materiali di recupero', 0.00, 0.00, 'N6.1'),
('N6.2', 'Inversione contabile - cessione di oro e argento puro', 0.00, 0.00, 'N6.2'),
('N6.3', 'Inversione contabile - subappalto nel settore edile', 0.00, 0.00, 'N6.3'),
('N6.4', 'Inversione contabile - cessione di fabbricati', 0.00, 0.00, 'N6.4'),
('N6.5', 'Inversione contabile - cessione di telefoni cellulari', 0.00, 0.00, 'N6.5'),
('N6.6', 'Inversione contabile - cessione di prodotti elettronici', 0.00, 0.00, 'N6.6'),
('N6.7', 'Inversione contabile - prestazioni comparto edile e settori connessi', 0.00, 0.00, 'N6.7'),
('N6.8', 'Inversione contabile - operazioni settore energetico', 0.00, 0.00, 'N6.8'),
('N6.9', 'Inversione contabile - altri casi', 0.00, 0.00, 'N6.9'),
('N7',   'IVA assolta in altro Stato UE', 0.00, 0.00, 'N7')
ON DUPLICATE KEY UPDATE
    Descrizione = VALUES(Descrizione),
    Aliquota = VALUES(Aliquota),
    Detrazione = VALUES(Detrazione),
    CodiceFE = VALUES(CodiceFE);
