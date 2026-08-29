-- Prepara una scheda realistica per la verifica del nuovo modulo.
INSERT INTO LavoriRg (ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
SELECT ID,Anno,Codice,1,'2','P',1.000,50.000 FROM Lavori
WHERE ID=87 AND NOT EXISTS (SELECT 1 FROM LavoriRg WHERE ID=87);

INSERT INTO LavoriRg (ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
SELECT ID,Anno,Codice,2,'BGIPIDROSAL','A',4.000,11.000 FROM Lavori
WHERE ID=87 AND NOT EXISTS (SELECT 1 FROM LavoriRg WHERE ID=87 AND Riga=2);

INSERT INTO LavoriRg (ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
SELECT ID,Anno,Codice,3,'EV9601-00','A',1.000,95.000 FROM Lavori
WHERE ID=87 AND NOT EXISTS (SELECT 1 FROM LavoriRg WHERE ID=87 AND Riga=3);
