# Pubblicazione SkyLab su Contabo

## Risorse isolate

- Dominio: `skylab.sigmadata.it`.
- Servizio: `skylab`, ascolto solo su `127.0.0.1:5002`.
- Release: `/opt/skylab/releases/<versione>`, collegamento `/opt/skylab/current`.
- Dati persistenti: `/opt/skylab/shared` (allegati, documenti e chiavi DataProtection).
- Configurazione privata: `/etc/skylab/skylab.env`, non inclusa nel pacchetto o nel repository.
- Database centrale: `skylab_master`; azienda iniziale: `skylab_0001`.
- Utente MySQL dedicato con accesso esclusivo ai database SkyLab.

La configurazione centrale è predisposta per più aziende, ma questa prima installazione seleziona una sola azienda per processo. La selezione interattiva e l'isolamento multi-azienda per sessione richiedono un successivo sviluppo: non cambiare il codice azienda a richiesta sul singleton attuale.

## Configurazione del servizio

Variabili richieste (credenziali da impostare privatamente):

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5002
AllowedHosts=skylab.sigmadata.it
ConnectionStrings__SkyLabServer=<connessione MySQL con utente dedicato>
SkyLab__Database__UseMasterRegistry=true
SkyLab__Database__DefaultCompanyCode=1
SkyLab__Database__MasterDatabase=skylab_master
SkyLab__Database__CompanyDatabasePattern=skylab_{0:0000}
SkyLab__Mobile__Enabled=true
SkyLab__DataRoot=/opt/skylab/shared/data
SkyLab__DataProtectionKeysPath=/opt/skylab/shared/keys
```

Il servizio deve avere utente Linux dedicato, directory di lavoro `/opt/skylab/current`, avvio `/opt/skylab/current/SkyLab.Web` e riavvio su errore. Non eseguire come root. Conservare le chiavi tra gli aggiornamenti e mantenere il percorso degli upload persistente.

## Ordine di pubblicazione

1. Verificare che porta, database aziendale e servizio non siano già occupati; non sovrascrivere risorse esistenti.
2. Esportare il database locale in area privata e importarlo nell'azienda iniziale; non includere il dump nella release pubblica.
3. Creare utenze dedicate, configurazione privata e cartelle persistenti.
4. Pubblicare una release Linux x64 self-contained, verificarne l'integrità e attivare il servizio.
5. Configurare DNS e vhost Nginx separato, con intestazioni Host, X-Forwarded-For e X-Forwarded-Proto. Verificare la configurazione prima del reload.
6. Attivare certificato HTTPS e verificare accesso, protezione pagine, catalogo e ricevuta consuntivo.
7. Collegare l'Android all'indirizzo HTTPS reale e testare con smartphone fisico.

## Stato della preparazione

- Registro centrale creato su Contabo, azienda 0001 registrata.
- Accesso web obbligatorio in Production, sessione di 30 minuti e limite ai tentativi di login.
- API mobile attivabili in Production, con autenticazione propria.
- Chiavi di sicurezza configurabili fuori dalla release.
- Importazione azienda, servizio, DNS, HTTPS e collegamento Android ancora da completare.

Seguire lo schema Micronote Fish/Food, senza eseguire i loro script di ricreazione database sulle risorse SkyLab o Micronote.
