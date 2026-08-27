# SkyLab — migrazione ASP.NET Core 10

Baseline della nuova versione web mobile-first del gestionale legacy VB.NET/WinForms.

## Avvio

```powershell
dotnet run --project .\src\SkyLab.Web\SkyLab.Web.csproj
```

## Stato

- ASP.NET Core Razor Pages su `net10.0`.
- Dashboard responsive e agenda interventi.
- Dettaglio intervento, inserimento/scansione barcode e chiusura lavoro.
- Repository in memoria con dati dimostrativi: non è ancora collegato al database legacy.

## Strategia di migrazione

1. Congelare e inventariare schema/dati SQL Server Compact, senza scrivere nel file `.sdf` originale.
2. Introdurre database server (SQL Server o PostgreSQL), migrazioni versionate e importatore ripetibile.
3. Migrare il nucleo: utenti/ruoli, clienti, sedi, impianti, tecnici, interventi, articoli e magazzini.
4. Rendere atomica la chiusura: rapportino + righe materiali + movimento/scarico di magazzino.
5. Aggiungere PWA/offline con coda locale e chiavi idempotenti per evitare doppi scarichi.
6. Migrare per moduli: documenti commerciali, produzione/distinte, contabilità, stampe e integrazioni.

La sezione mobile del legacy è considerata codice sperimentale incompleto e non una specifica funzionale.
