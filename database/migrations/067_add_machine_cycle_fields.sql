-- Dati specifici dell'installazione necessari alla previsione dei consumabili.
ALTER TABLE MacchineCli
  ADD COLUMN QuantitaFornita DECIMAL(12,3) NOT NULL DEFAULT 0 AFTER Durata,
  ADD COLUMN ConsumoGiornaliero DECIMAL(10,3) NOT NULL DEFAULT 0 AFTER QuantitaFornita;
