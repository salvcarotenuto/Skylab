# SkyLab Mobile — attività successive al deploy Web

Promemoria concordato il 7 settembre 2026.

## Connessione al server

- Definire la modalità di connessione degli smartphone fisici quando l'applicazione Web sarà installata sul server definitivo.
- Sostituire l'indirizzo di sviluppo `http://localhost:5187` con una configurazione adatta all'ambiente di produzione.
- Utilizzare HTTPS e verificare autenticazione, certificato e raggiungibilità dalla rete utilizzata dagli operatori.

## Invio automatico dei consuntivi

- Applicare un timer o un processo Android in background per ritentare automaticamente gli invii alla prima occasione utile di rete.
- Conservare il consuntivo sul dispositivo fino alla ricezione di una ricevuta valida dal server.
- Dopo la ricevuta, per l'app mobile lo stato conclusivo resta **Trasmesso**: non è necessario conoscere la successiva conferma del back-office.

## Conservazione per 60 giorni

- Mantenere come termine iniziale la **data del lavoro**.
- Cancellare automaticamente dopo 60 giorni soltanto le schede con consuntivo **Trasmesso**.
- Non cancellare automaticamente schede o consuntivi in stato **Bozza**, **In attesa di invio** o **Errore**.

## Cancellazione manuale

- Aggiungere nella scheda il comando **Elimina dal dispositivo**, con richiesta di conferma.
- **Trasmesso**: cancellazione consentita in qualsiasi momento.
- **Bozza**: cancellazione consentita, avvisando che la bozza verrà persa.
- **In attesa di invio**: cancellazione bloccata per evitare la perdita di un consuntivo già confermato dall'operatore.
- **Errore**: cancellazione bloccata finché l'anomalia non viene gestita.
- La cancellazione deve riguardare esclusivamente la copia locale, senza modificare dati sul server.
- Eliminare sia la scheda dalla cache sia l'eventuale record locale del consuntivo.
- Mostrare il messaggio finale: **Scheda eliminata dal dispositivo**.

## Verifiche conclusive

- Collaudare l'intero flusso su uno smartphone fisico: login, sincronizzazione, uso offline, barcode, bozza, conferma, invio differito e cancellazione.
- Proteggere il token locale ed escludere credenziali e database dai backup cloud Android.
- Aggiungere test effettivi per migrazioni Room, stati del consuntivo, reinvio, conservazione e cancellazione.
- Configurare e firmare la build Release definitiva.
