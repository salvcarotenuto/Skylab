# Piano di migrazione

## Confini verificati

Il sorgente è un'applicazione Windows Forms VB.NET su .NET Framework 4.8, composta da 429 file `.vb` e 201 risorse `.resx`. Usa SQL Server Compact 3.5 (`SkyLabDb.sdf`) e contiene aree per anagrafiche, impianti/lavori, magazzino, produzione, documenti, contabilità e stampe.

## Primo rilascio consigliato

Il primo verticale deve coprire l'intero percorso: intervento assegnato → materiali letti da smartphone → conferma quantità → chiusura rapportino → scarico atomico dal magazzino del tecnico/furgone → storico impianto. Clienti, sedi, impianti, articoli e giacenze sono prerequisiti del verticale.

## Riferimento applicativo

Micronote Fish e Micronote Food sono il riferimento vincolante per struttura ASP.NET Core, Razor Pages, repository, controlli di input, griglie, finestre di messaggio e linguaggio grafico. SkyLab mantiene un proprio dominio ma riusa le medesime convenzioni operative. Login, selezione azienda e selezione utente sono esplicitamente rinviati: la prima milestone è il menu generale con tutte le aree del legacy.

## Decisioni ancora da validare

- database di destinazione e ambiente di hosting;
- deposito di scarico e regole sulle disponibilità insufficienti;
- autenticazione/identità dei tecnici;
- obbligo offline e strategia di sincronizzazione;
- documenti prodotti alla chiusura (rapportino, firma, DDT, fatturazione);
- mappatura effettiva delle tabelle e qualità dei dati del file `.sdf` operativo.

## Regola di sicurezza dati

L'importatore leggerà una copia del database legacy. Ogni trasformazione sarà ripetibile, tracciata e accompagnata da conteggi e riconciliazioni; il database originale non verrà modificato.
