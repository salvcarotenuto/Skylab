-- SkyLab 066 - separazione anagrafica cliente, sedi e installato
-- Il database legacy skylabdb conserva il campo Alias come copia storica.

ALTER TABLE skylab_0001.Clienti
    DROP COLUMN Alias;

ALTER TABLE skylab_0001.Destini
    MODIFY CliFor VARCHAR(1) NOT NULL,
    MODIFY Ditta INT NOT NULL,
    MODIFY Codice INT NOT NULL,
    ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
    ADD PRIMARY KEY (ID),
    ADD UNIQUE KEY UX_Destini_DittaCodice (CliFor, Ditta, Codice),
    ADD KEY IX_Destini_Ditta (CliFor, Ditta);

ALTER TABLE skylab_0001.MacchineCli
    ADD COLUMN DestinoID INT NULL AFTER Cliente,
    ADD KEY IX_MacchineCli_DestinoID (DestinoID),
    ADD CONSTRAINT FK_MacchineCli_Destini
        FOREIGN KEY (DestinoID) REFERENCES skylab_0001.Destini(ID)
        ON UPDATE RESTRICT ON DELETE SET NULL;

ALTER TABLE skylab_master.schema_clienti
    DROP COLUMN Alias;

ALTER TABLE skylab_master.schema_destini
    MODIFY CliFor VARCHAR(1) NOT NULL,
    MODIFY Ditta INT NOT NULL,
    MODIFY Codice INT NOT NULL,
    ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
    ADD PRIMARY KEY (ID),
    ADD UNIQUE KEY UX_Destini_DittaCodice (CliFor, Ditta, Codice),
    ADD KEY IX_Destini_Ditta (CliFor, Ditta);

ALTER TABLE skylab_master.schema_macchinecli
    ADD COLUMN DestinoID INT NULL AFTER Cliente,
    ADD KEY IX_MacchineCli_DestinoID (DestinoID);
