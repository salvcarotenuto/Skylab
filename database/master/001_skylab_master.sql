-- Execute separately from company migrations. Does not modify skylab_0001.
-- Base structures follow Micronote Food/Fish.
CREATE DATABASE IF NOT EXISTS skylab_master CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE skylab_master;
CREATE TABLE IF NOT EXISTS Aziende (
 Codice INT NOT NULL PRIMARY KEY,
 Nome VARCHAR(120) NOT NULL,
 Password VARCHAR(255) NOT NULL,
 Attiva TINYINT(1) NOT NULL DEFAULT 1,
 Bloccata TINYINT(1) NOT NULL DEFAULT 0,
 NomeDatabase VARCHAR(120) NULL,
 VersioneDbAttuale DATETIME NULL,
 VersioneDbRichiesta DATETIME NULL,
 UNIQUE KEY UX_Aziende_Nome (Nome)
);
CREATE TABLE IF NOT EXISTS Parametri (
 Chiave VARCHAR(100) NOT NULL PRIMARY KEY,
 Valore TEXT NULL,
 VersioneSchemaDatabase DATETIME NULL
);
CREATE TABLE IF NOT EXISTS Accessi (
 ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 Utente INT UNSIGNED NOT NULL,
 Azienda INT UNSIGNED NOT NULL,
 TheDate DATE NULL,
 TheTime TIME NULL,
 INDEX IX_Accessi_Utente_Azienda_DataOra (Utente,Azienda,TheDate,TheTime)
);
CREATE TABLE IF NOT EXISTS FixedTable (
 Nome VARCHAR(254) NOT NULL,
 Descrizione VARCHAR(254) NULL,
 Record INT NULL,
 UNIQUE KEY UX_FixedTable_Nome (Nome)
);
-- Do not declare the company schema aligned before template reconciliation.
INSERT INTO Parametri (Chiave) VALUES ('VersioneSchemaDatabase')
ON DUPLICATE KEY UPDATE Chiave=Chiave;
-- Automatic company 0001; no weak default company password.
INSERT INTO Aziende (Codice,Nome,Password,Attiva,Bloccata,NomeDatabase)
VALUES (1,'0001',HEX(RANDOM_BYTES(32)),1,0,'skylab_0001')
ON DUPLICATE KEY UPDATE Codice=Codice;
