INSERT INTO Prestazioni (Codice,Descrizione,Prezzo)
VALUES (4,'Controllo durezza acqua e taratura impianto',0)
ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione);

INSERT INTO Prestazioni (Codice,Descrizione,Prezzo)
VALUES (5,'Sanificazione circuito e verifica tenuta',0)
ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione);

INSERT INTO LavoriRg (ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
SELECT ID,Anno,Codice,4,'4','P',1,35 FROM Lavori
WHERE ID=87 AND NOT EXISTS (SELECT 1 FROM LavoriRg WHERE ID=87 AND Riga=4);

INSERT INTO LavoriRg (ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
SELECT ID,Anno,Codice,5,'5','P',1,45 FROM Lavori
WHERE ID=87 AND NOT EXISTS (SELECT 1 FROM LavoriRg WHERE ID=87 AND Riga=5);
