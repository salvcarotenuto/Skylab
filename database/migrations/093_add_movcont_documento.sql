-- Collega il movimento contabile al documento origine.
-- MovCont.Origine identifica il settore/origine del movimento;
-- MovCont.Documento conserva l'ID del documento collegato.

ALTER TABLE MovCont
    ADD COLUMN Documento INT NULL DEFAULT NULL AFTER Descrizione,
    ADD KEY IX_MovCont_Origine_Documento (Origine, Documento);
