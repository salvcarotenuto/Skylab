# Distribuzione Android SkyLab

APK release 1.0 (versionCode 2), API `https://skylab.sigmadata.it`.
Download pubblico: `https://sigmadata.it/download/skylab/SkyLab-Mobile-1.0.apk`.
Accesso ai dati tramite login; la disponibilità pubblica dell'APK non concede accesso ai lavori.

## Firma

La firma definitiva è configurata tramite variabili `SKYLAB_ANDROID_KEYSTORE` e `SKYLAB_ANDROID_STORE_PASSWORD`, alias `skylab`. Nessuna password o chiave va nel repository o sul sito pubblico. Il materiale privato iniziale è nella cartella locale protetta `C:\Codex\SkyLabPrivate\AndroidSigning`: conservarne una copia sicura separata prima di distribuire aggiornamenti. La perdita della chiave impedisce gli aggiornamenti diretti.

L'APK di test sul Redmi è firmato con la chiave debug: la prima installazione release non può sostituirlo direttamente. Prima della disinstallazione manuale verificare che tutte le bozze siano gestite e tutti gli invii abbiano ricevuta. La disinstallazione elimina la cache e i dati locali. Gli aggiornamenti release successivi devono mantenere applicationId e chiave e incrementare versionCode.

## Verifiche prima di pubblicare

- Compilazione assembleRelease con firma obbligatoria.
- apksigner verify: nessuna chiave debug e firma valida.
- APK non debuggable, API HTTPS, nessun localhost.
- Confronto SHA-256 del file caricato.
- Conservazione della pagina Sigmadata precedente.

Distribuzione diretta APK, non pubblicazione sul Play Store. L'app resta in fase di test operativo, con alcune funzionalità pianificate ancora da completare.
