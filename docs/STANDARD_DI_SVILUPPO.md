# Standard Di Sviluppo

## 01 - Scopo

Questo documento raccoglie le regole grafiche, funzionali e strutturali da applicare nello sviluppo dei moduli delle applicazioni gestionali.

Non e' un documento chiuso: viene aggiornato ogni volta che una regola viene definita, chiarita o migliorata durante lo sviluppo.

### 01.01 - Regola Principale

Lo Standard Di Sviluppo deve essere consultato:

- all'inizio dello sviluppo di ogni nuovo modulo, per acquisire preventivamente le regole da seguire;
- al termine dello sviluppo, per verificare a consuntivo la corretta applicazione delle regole.

Senza questa doppia verifica, preventiva e finale, il disciplinare non ha valore operativo.

### 01.02 - Valore Vincolante Delle Regole

Nel presente disciplinare, una regola e' sempre vincolante.

Una prescrizione definita come regola deve essere applicata. In caso contrario, il modulo non puo' essere considerato conforme allo Standard Di Sviluppo.

Non sono ammesse regole meramente indicative.

Quando un contenuto ha valore solo descrittivo, esplicativo o di orientamento, deve essere qualificato come nota, criterio o eccezione, non come regola.

## 02 - Principi Generali

- Le applicazioni sono gestionali, non siti web.
- Le interfacce devono essere stabili, prevedibili e orientate al data entry.
- Le regole comuni devono essere scritte una volta e poi applicate ai nuovi moduli.
- Le correzioni non devono diventare toppe su toppe.
- Il codice deve restare pulito, leggibile e manutenibile.

### 02.01 - Controlli Di Form

Le regole sui controlli di form sono trasversali.

Si applicano a:

- schede CRUD;
- barre filtri;
- zoom e lookup;
- dialoghi modali;
- maschere di servizio;
- ogni altro form operativo dell'applicazione.

#### 02.01.R01 - Controlli Standard Specializzati

Quando un form richiede controlli specializzati, devono essere utilizzati i controlli standard personali gia' definiti per l'applicazione.

In particolare:

- i campi data devono usare il datepicker standard dell'applicazione;
- le checkbox devono usare il controllo checkbox standard personale;
- i campi numerici incrementali devono usare il controllo updown standard personale.

Non devono essere introdotte varianti locali dei controlli specializzati quando esiste gia' un controllo standard disponibile.

#### 02.01.R02 - Stato Grafico Delle Select

Le select abilitate devono avere sfondo bianco, sia quando consentono inserimento/selezione guidata sia quando consentono la sola selezione da elenco.

Le select devono assumere sfondo grigio soltanto quando sono disabilitate.

Lo stato disabled deve restare chiaramente leggibile: il controllo non deve risultare sbiadito o poco contrastato.

#### 02.01.R03 - Tasto Invio Per Navigazione

Nei form operativi, il tasto `Invio` deve essere usato per la navigazione tra i campi.

La pressione di `Invio` deve portare al campo operativo successivo, secondo l'ordine naturale di compilazione del modulo.

Il tasto `Invio` non deve attivare salvataggi impliciti o azioni distruttive non confermate.

Sono ammesse eccezioni solo per controlli nei quali `Invio` abbia una funzione specifica e riconoscibile, come la conferma di una selezione guidata, la scelta di una riga in elenco o un comando esplicitamente focalizzato.

### 02.02 - Font Applicativo

Le regole sui font sono trasversali.

Si applicano a:

- schede CRUD;
- liste;
- barre filtri;
- zoom e lookup;
- dialoghi modali;
- messaggi e conferme;
- ogni altro form operativo dell'applicazione.

#### 02.02.R01 - Uniformita' Del Font

Le interfacce operative devono usare un font coerente e uniforme.

Non devono essere introdotte varianti locali di font, dimensione o peso se non per una esigenza grafica o funzionale specifica.

#### 02.02.C01 - Valori Base Dei Campi

Come criterio iniziale, i campi di data entry devono usare questi valori:

- label dei campi: `13px / 400`;
- input, select e textarea: `13px / 420`.

#### 02.02.C02 - Font E Colore Dei Pulsanti

Tutti i pulsanti dell'applicazione devono essere disciplinati dallo Standard.

Come criterio generale iniziale, i pulsanti devono rispettare questi valori:

- font proporzionato alla dimensione e alla funzione del pulsante;
- testo nero su sfondo chiaro;
- testo bianco su sfondo `BlueButton`.

Il criterio riguarda:

- pulsanti operativi di schede, liste, box e dialoghi;
- pulsanti di conferma, annullamento, chiusura e navigazione;
- pulsanti piccoli o specializzati, come cerca, frecce, calendario, clear e comandi interni di controllo.

Le dimensioni possono cambiare in base alla famiglia del pulsante, ma nessun pulsante deve restare privo di disciplina grafica.

Le dimensioni sono un criterio da specializzare per categoria.

Anche il font deve essere specializzato per categoria, tenendo conto della dimensione fisica del pulsante e della funzione svolta.

Non deve essere applicato automaticamente lo stesso font a pulsanti di dimensione o funzione molto diversa.

Categorie iniziali di pulsanti:

- pulsanti di titolo e navigazione;
- pulsanti di comando nel corpo del modulo;
- pulsanti di zoom, lookup, box di selezione e dialoghi operativi;
- pulsanti del box messaggi;
- pulsanti piccoli o specializzati, come cerca, frecce, calendario, clear e comandi interni di controllo.

Ogni categoria deve avere una propria disciplina grafica, mantenendo comunque coerenza di font, colore, contrasto, bordo, hover e focus.

#### 02.02.R01 - Pulsanti Disabilitati

Un pulsante disabilitato deve conservare colori, font e aspetto dello stato di riposo.

Al passaggio del mouse non deve cambiare colore, non deve applicare effetti `hover` e non deve mostrare il cursore operativo.

La regola si applica a tutte le categorie di pulsanti dell'applicazione, comprese Schede, Liste, Zoom, lookup, dialoghi e box messaggi.

#### 02.02.R02 - Testo Non Selezionabile Nei Pulsanti

Il testo, le icone e ogni altro contenuto visibile dei pulsanti non devono essere selezionabili con il mouse o mediante trascinamento.

La regola si applica sia agli elementi `button` sia agli altri controlli che svolgono graficamente e funzionalmente il ruolo di pulsante.

Come criterio iniziale, i pulsanti di titolo e navigazione hanno dimensione `140px x 40px` e font `13px / 600`.

Eventuali altri valori ricorrenti, come titoli, testate griglia e messaggi, devono essere registrati nello Standard quando vengono fissati in modo stabile.

### 02.03 - Spazi Interni Ed Esterni

Le regole sugli spazi sono trasversali e riguardano tutti i moduli dell'applicazione.

#### 02.03.R01 - Divieto Generale Di Padding

Il padding e' vietato come criterio generale di layout.

Il padding non deve essere usato per creare:

- distanze tra blocchi;
- rientri grafici;
- allineamenti;
- altezze apparenti;
- larghezze apparenti;
- spazi di respiro interni;
- correzioni visive locali.

La distanza e l'allineamento devono essere ottenuti con:

- misure esplicite;
- griglie di layout;
- gap;
- margini esterni;
- separatori strutturali;
- dimensioni dei controlli;
- line-height;
- text-indent;
- posizionamento controllato degli elementi interni.

Il divieto riguarda tutti gli elementi dell'interfaccia, compresi:

- pannelli;
- riquadri;
- box;
- barre titolo;
- barre filtri;
- cornici di schede;
- cornici di griglie;
- dialoghi modali;
- label;
- input;
- select;
- textarea;
- pulsanti;
- celle di griglia;
- controlli specializzati.

Le eccezioni sono ammesse soltanto se previste in modo specifico e tassativo dallo Standard.

Non sono ammesse eccezioni implicite, locali, discrezionali o motivate soltanto da comodita' grafica.

Se si presenta una nuova necessita' tecnica, lo Standard deve essere aggiornato prima di applicare l'eccezione.

L'eccezione deve indicare esattamente:

- elemento interessato;
- motivo tecnico;
- misura applicabile;
- limiti di utilizzo.

L'eccezione non deve mai rendere ambigue le misure stabilite dallo Standard.

## 03 - Struttura Dello Standard

### 03.01 - Nomenclatura

Lo standard e' organizzato con struttura ad albero:

- `NN - Area`
- `NN.NN - Sezione`
- `NN.NN.NNN - Regola`
- `NN.NN.CNN - Criterio`
- `NN.NN.NNN - Nota`

La numerazione deve rendere ogni regola facilmente richiamabile e permettere l'aggiunta di nuove regole senza alterare l'impianto gia' definito.

Il termine `Regola` identifica una prescrizione obbligatoria.

Il termine `Criterio` identifica una direttiva da seguire nel primo sviluppo, valida fino a diversa decisione o modifica specifica.

Il termine `Nota` identifica un chiarimento descrittivo, senza valore prescrittivo.

### 03.02 - Ciclo Di Applicazione

Ogni nuovo modulo deve essere sviluppato applicando la regola principale:

- acquisizione preventiva delle regole da seguire;
- verifica a consuntivo della corretta applicazione delle regole.

La consultazione preventiva serve a imboccare subito la strada corretta.

La verifica finale serve a controllare che il modulo sia allineato allo standard prima di considerare concluso il lavoro.

### 03.03 - Sezioni Di Riferimento

- Menu
- Liste
- Schede
- Dialoghi modali
- Zoom e lookup
- Messaggi e conferme
- Stampe
- Persistenza dati
- Moduli contabili
- Schema Azione

### 03.04 - Regola Operativa

Quando si crea o si modifica un modulo, si applica questo standard come riferimento generale.

Formula di lavoro:

`Applica Standard Di Sviluppo al modulo [nome modulo]`

Quando si applica uno schema standard, occorre verificare se lo schema e' gia' riportato in questo documento.

Se lo schema non e' ancora registrato, va aggiunto allo standard prima di considerarlo regola comune.

Durante l'applicazione o la verifica dello Standard, se viene individuato un miglioramento utile non ancora previsto dal disciplinare, il miglioramento deve essere proposto esplicitamente prima di essere considerato nuovo riferimento comune.

### 03.05 - Autosufficienza Delle Sezioni

#### 03.05.001 - Evitare Rinvii Operativi Nascosti

Ogni sezione dello Standard deve essere, per quanto possibile, autosufficiente.

Le regole e i criteri operativi necessari per applicare una sezione devono essere riportati direttamente nella sezione stessa.

I rinvii ad altre sezioni sono ammessi solo come chiarimento o raccordo, ma non devono creare dipendenze operative nascoste.

Quando un criterio comune si applica a piu' sezioni, esso deve essere ripetuto nelle sezioni interessate oppure estratto in una sezione generale chiaramente identificata.

## 04 - Schede

### 04.01 - Schema Azione

La variabile `Azione` identifica il modo in cui una scheda viene aperta e stabilisce il comportamento della scheda stessa, soprattutto per:

- stato dei campi;
- disponibilita' dei comandi;
- destinazione di ritorno dopo `Salva` o `Annulla`.

#### 04.01.001 - Valori Standard

- `1` = visualizzazione readonly, con ritorno a lista o menu.
- `2` = inserimento, con ritorno a lista o menu dopo il salvataggio.
- `3` = modifica, con ritorno a lista o menu dopo il salvataggio.
- `4` = inserimento continuativo, con reset della scheda dopo il salvataggio per un nuovo inserimento.
- `101` = visualizzazione readonly, con ritorno al modulo esterno chiamante.
- `102` = inserimento, con ritorno al modulo esterno chiamante.
- `103` = modifica, con ritorno al modulo esterno chiamante.

Il valore `4` e' riservato all'inserimento continuativo e non deve essere riutilizzato per altri significati.

#### 04.01.002 - Regole Funzionali

- Le azioni `1` e `101` aprono la scheda in sola lettura.
- Le azioni `2`, `3`, `4`, `102` e `103` abilitano la modifica secondo le regole del modulo.
- Le azioni `101`, `102` e `103` sono riservate ai casi in cui la scheda viene richiamata da un modulo esterno.
- L'azione `4` si usa solo quando l'inserimento nasce dal menu principale e l'utente deve poter registrare piu' record consecutivi.
- Dopo il salvataggio, la destinazione non deve essere decisa caso per caso nel codice, ma deve derivare dallo schema `Azione`.
- Le azioni `1xx` comportano sempre apertura del modulo richiamato in formato modale.

#### 04.01.003 - Regola Di Applicazione

Ogni nuova scheda deve dichiarare esplicitamente quale schema `Azione` applica.

Prima di introdurre nuovi valori o varianti, verificare se lo stesso comportamento puo' essere rappresentato dai valori standard gia' definiti.

### 04.02 - Salvataggio

#### 04.02.001 - Flusso Standard Del Salvataggio

Ogni salvataggio di scheda deve seguire un flusso ordinato e riconoscibile.

Il ciclo standard e':

- esecuzione dei controlli preliminari, di coerenza e di validazione previsti dal modulo;
- comunicazione all'utente degli eventuali errori o impedimenti mediante il box messaggi standard;
- acquisizione delle eventuali risposte o conferme dell'utente, quando il flusso lo prevede;
- ritorno alla fase di inserimento dati quando occorre correggere dati mancanti, incoerenti o non validi;
- prosecuzione del salvataggio solo in caso di validazione positiva o di conferma espressa dell'utente nei casi ammessi;
- apertura di `SkyProg` prima dell'avvio materiale delle operazioni di scrittura;
- esecuzione delle operazioni di registrazione sul database;
- chiusura di `SkyProg` al completamento delle operazioni di scrittura;
- ritorno al punto previsto dal flusso del modulo.

`SkyProg` deve quindi accompagnare soltanto la fase effettiva di scrittura e deve fornire all'utente un riscontro visuale delle operazioni in corso.

I messaggi di esito non devono comparire mentre `SkyProg` e' ancora attivo.

La destinazione finale dopo il salvataggio deve derivare dal flusso del modulo, ad esempio:

- reset della scheda per nuovo inserimento;
- chiusura della scheda con ritorno alla lista;
- chiusura della scheda con ritorno al modulo chiamante;
- altro comportamento previsto dallo schema `Azione`.

### 04.03 - Campi Anagrafici

#### 04.03.001 - Inserimento Guidato Di Citta Provincia Cap

Nei moduli che contengono dati anagrafici, i campi Citta, Provincia e Cap devono essere assistiti con il meccanismo di inserimento guidato standard dell'applicazione.

Il controllo deve permettere l'inserimento libero, ma dopo i primi caratteri deve proporre i comuni filtrati con comportamento analogo a una select senza freccia grafica.

La selezione deve essere possibile anche da tastiera, con frecce e conferma tramite Tab o Invio.

Il controllo standard e' una select a lettura/scrittura:

- accetta digitazione libera;
- non espone la freccia grafica della select tradizionale;
- apre automaticamente l'elenco dopo i primi tre caratteri inseriti;
- propone i comuni presenti in tabella filtrati in base alla digitazione;
- accetta comunque il valore digitato anche se il comune non e' presente in tabella;
- in caso di selezione di un comune presente in tabella, valorizza automaticamente i campi Provincia e Cap collegati.

### 04.04 - Grafica Schede CRUD

#### 04.04.C01 - Impostazione Grafica Di Primo Sviluppo

Per le schede CRUD, il primo sviluppo deve seguire questi criteri grafici:

- sfondo del modulo chiaro e opaco;
- titolo del modulo in alto, con sfondo bianco opaco;
- logo programma a sinistra nel titolo;
- pulsanti operativi a destra nel titolo;
- altezza iniziale del titolo pari a `96px`;
- spazio vuoto trasparente sotto il titolo pari a `10px`;
- scheda dati sottostante con sfondo bianco;
- cornice sottile intorno alla scheda dati;
- margine bianco interno intorno al contenuto della scheda dati;
- footer presente solo quando la form lascia uno spazio adeguato e non crea confusione grafica.

Questi criteri guidano l'impostazione iniziale della scheda e restano applicati fino a diversa scelta grafica specifica.

#### 04.04.C02 - Pulsanti Di Titolo E Navigazione Della Scheda

Per le schede CRUD, i pulsanti collocati nel titolo devono seguire questi criteri grafici:

- il primo pulsante, corrispondente all'azione di maggiore importanza operativa, deve avere sfondo `BlueButton`;
- il primo pulsante deve avere testo bianco;
- i pulsanti ulteriori, collocati a destra del primo, devono avere sfondo bianco;
- i pulsanti ulteriori devono avere testo nero;
- al passaggio del mouse, i pulsanti ulteriori devono assumere lo sfondo `BlueButton`;
- al passaggio del mouse, i pulsanti ulteriori devono assumere testo bianco;
- tutti i pulsanti di titolo e navigazione della scheda devono avere dimensione iniziale `140px x 40px`;
- tutti i pulsanti di titolo e navigazione della scheda devono usare font `13px / 600`.

#### 04.04.C03 - Allineamento Verticale Della Testata

Nella testata delle schede CRUD, logo, blocco titolo/sottotitolo e pulsanti operativi devono risultare centrati verticalmente rispetto all'altezza della testata.

Il titolo deve restare visivamente allineato al centro del logo.

I pulsanti operativi devono avere asse verticale coerente con il blocco titolo.

#### 04.04.C04 - Margini Interni Della Testata

La testata delle schede CRUD deve mantenere margini interni laterali coerenti.

Come criterio iniziale:

- il logo deve avere un distacco sufficiente dal bordo sinistro;
- il gruppo pulsanti deve avere un distacco sufficiente dal bordo destro;
- lo spazio tra logo e titolo deve essere regolare;
- lo spazio tra i pulsanti deve essere regolare;
- il contenuto della testata non deve apparire ne' schiacciato contro i bordi ne' eccessivamente disperso.

Misure iniziali di riferimento:

- margine sinistro testata: `18px`;
- margine destro testata: `28px`;
- spazio tra logo e titolo: `14px`;
- spazio tra pulsanti: `10px`.

#### 04.04.C05 - Spazio Superiore Della Testata

La testata delle schede CRUD deve mantenere uno spazio superiore visibile prima del titolo.

Come criterio iniziale, lo spazio sopra la testata deve essere di circa `8px`.

Lo spazio superiore serve a evitare che il titolo appaia appoggiato al bordo alto della finestra o del contenitore.

#### 04.04.C06 - Comandi Come Pulsanti

Nelle schede CRUD, i comandi operativi della testata devono essere rappresentati graficamente come pulsanti.

I comandi non devono essere visualizzati come semplici link testuali quando svolgono la funzione di azione della scheda.

Anche il comando `Annulla`, quando collocato nella testata insieme agli altri comandi operativi, deve assumere lo stile di pulsante.

### 04.05 - Controlli Di Input

#### 04.05.C01 - Label Dei Campi

Nelle schede CRUD, le label dei campi devono avere grafica uniforme.

Come criterio iniziale:

- sfondo azzurro standard dei campi;
- bordo sottile;
- angoli arrotondati;
- raggio iniziale `5px`;
- altezza coerente con il controllo associato.

Il colore iniziale di riferimento e' l'azzurro gia' utilizzato nelle schede standard (`#b8cbe0`), salvo rettifica successiva dello standard grafico.

#### 04.05.C02 - Caselle Di Input E Select

Le caselle di input e le select devono avere grafica uniforme.

Come criterio iniziale:

- sfondo bianco opaco;
- bordo sottile;
- angoli arrotondati;
- raggio iniziale `5px`;
- altezza iniziale `30px`;
- focus leggero, visibile ma non invasivo.

Il focus deve aiutare l'utente nel data entry senza alterare pesantemente la grafica del modulo.

#### 04.05.R01 - Stato Grafico Delle Select

Le select abilitate devono avere sfondo bianco, sia quando consentono inserimento/selezione guidata sia quando consentono la sola selezione da elenco.

Le select devono assumere sfondo grigio soltanto quando sono disabilitate.

Lo stato disabled deve restare chiaramente leggibile: il controllo non deve risultare sbiadito o poco contrastato.

#### 04.05.C03 - Spaziatura Dei Campi

La disposizione dei campi nelle schede CRUD deve mantenere spaziature regolari.

Come criterio iniziale:

- gap interno tra label e controllo: `3px`;
- gap tra campi: `10px`;
- altezza campo: `30px`.

Le misure sono soggette a rettifica in base alla leggibilita' e alla densita' della scheda.

#### 04.05.C04 - Controlli Standard Specializzati

Quando una scheda richiede controlli specializzati, devono essere utilizzati i controlli standard personali gia' definiti per l'applicazione.

Come criterio iniziale:

- i campi data devono usare il datepicker standard dell'applicazione;
- le checkbox devono usare il controllo checkbox standard personale;
- i campi numerici incrementali devono usare il controllo updown standard personale.

Non devono essere introdotte varianti locali dei controlli specializzati quando esiste gia' un controllo standard disponibile.

## 05 - Liste

### 05.01 - Grafica Liste CRUD

#### 05.01.C01 - Impostazione Grafica Di Primo Sviluppo

Le liste CRUD devono applicare, per la grafica di fondo, i criteri generali gia' definiti per le schede CRUD, adattandoli alla funzione di consultazione e selezione dei record.

In aggiunta, il primo sviluppo delle liste deve seguire questi criteri:

- larghezza iniziale a tutto schermo;
- altezza iniziale tale da sfruttare bene lo spazio verticale disponibile;
- assenza del footer, per riservare il massimo spazio utile ai dati;
- titolo in alto con logo programma a sinistra e pulsanti di navigazione a destra;
- altezza iniziale del titolo lista pari a `80px`;
- casella contatore collocata a sinistra del primo pulsante operativo.

#### 05.01.C02 - Spaziatura Verticale Della Lista

La lista deve mantenere spazi vuoti trasparenti e regolari tra le sue sezioni principali.

Come criterio iniziale, gli spazi trasparenti devono essere di `8px`:

- sopra il titolo;
- sotto il titolo;
- sotto la barra filtri.

#### 05.01.C03 - Barra Filtri

La barra filtri deve essere separata visivamente dal titolo e dalla griglia.

Come criterio iniziale:

- sfondo bianco opaco;
- stessa larghezza del titolo;
- altezza pari a `52px`;
- contenuto ordinato e coerente con la funzione di filtro rapido;
- i campi filtro devono adottare lo standard grafico generale dei campi applicativi, compresi label, input, select e controlli specializzati;
- i campi filtro devono avere gap interno tra label e controllo pari a `3px`;
- i campi filtro devono avere gap esterno minimo tra campi pari a `10px`;
- i campi filtro devono essere centrati verticalmente rispetto alla barra filtri e allineati tra loro sulla stessa posizione verticale;
- i filtri devono essere reattivi e aggiornare immediatamente i dati in lettura, senza pulsante `Applica filtri`.

La barra filtri delle liste non deve quindi prevedere un pulsante di conferma manuale per applicare i filtri, salvo eccezione esplicitamente motivata nello Standard.

#### 05.01.R03 - Separazione Strutturale Tra Filtri E Griglia

Nelle liste CRUD, la barra filtri e la griglia dati devono risultare due blocchi distinti.

La separazione non deve dipendere soltanto da differenze cromatiche o da effetti visivi poco percepibili.

Quando il margine tra i due blocchi non garantisce una separazione evidente, deve essere introdotto uno spazio strutturale autonomo tra barra filtri e griglia.

Lo spazio strutturale deve restare vuoto e non deve appartenere ne' alla barra filtri ne' alla griglia.

Come criterio iniziale, lo spazio strutturale tra barra filtri e griglia e' pari a `12px`.

#### 05.01.C04 - Griglia Dati

La griglia dati deve avere fondo bianco opaco, leggera cornice esterna e margini di respiro.

La testata della griglia deve avere caratteristiche grafiche coerenti con l'effetto tridimensionale usato nelle liste standard.

Le colonne ordinabili devono evidenziare la possibilita' di ordinamento e mostrare la freccetta della direzione di sort quando attiva.

Come criterio iniziale, l'altezza della testata griglia e' pari a `30px`.

Come criterio iniziale, l'altezza delle righe dati e' pari a `28px`.

Il criterio sull'altezza delle righe e' espressamente soggetto a rettifica in base alla leggibilita' e alla densita' informativa della lista.

Le celle della griglia devono avere testo allineato verticalmente al centro mediante `vertical-align: middle`.

Le colonne della griglia devono seguire questo criterio:

- colonne tecniche o a lunghezza prevedibile: larghezza fissa;
- colonne descrittive: larghezza elastica;
- la colonna descrittiva principale assorbe preferibilmente lo spazio residuo.

Esempi di colonne a larghezza fissa:

- codice;
- data;
- numero;
- quantita';
- prezzo;
- importo;
- stato;
- provincia;
- CAP.

Esempi di colonne descrittive a larghezza elastica:

- descrizione;
- nome;
- denominazione;
- ragione sociale;
- indirizzo;
- annotazioni.

#### 05.01.R02 - Testata Sticky Delle Griglie Scrollabili

Le griglie con corpo scrollabile e testata fissa o sticky devono impedire che le righe dati invadano visivamente la testata durante lo scroll.

Quando si usa una testata sticky, la griglia deve applicare:

- `border-collapse: separate`;
- `border-spacing: 0`;
- testata, riga di testata o celle di testata con `position: sticky`, secondo la struttura tecnica piu' efficace;
- elemento sticky con `top: 0`;
- elemento sticky con `z-index` sufficiente a restare sopra le righe dati;
- celle di testata con sfondo pieno, non trasparente.

Non devono essere usate impostazioni che permettano alle righe di scorrere sopra o attraverso la testata.

Quando la soluzione sui singoli `th` non e' sufficiente, la testata deve essere resa sticky a livello di `thead`, mantenendo le celle di testata sopra le righe mediante `z-index` coerente.

#### 05.01.C05 - Interazione Con La Griglia

Nelle liste CRUD, la griglia dati deve comportarsi come uno strumento di selezione record, non come un'area di testo.

Come criterio iniziale:

- le celle e i campi della griglia non devono mostrare il puntatore testuale del mouse;
- il testo delle celle non deve essere selezionabile durante l'uso ordinario della griglia;
- la navigazione da tastiera deve supportare almeno i tasti Freccia Su, Freccia Giu', Home ed End;
- la riga selezionata deve restare sempre visibile durante la navigazione da tastiera;
- la riga selezionata deve restare sempre visibile anche durante la navigazione con rotellina del mouse.

#### 05.01.R04 - Divieto Globale Di Selezione Del Testo Nelle Liste

Il testo visualizzato nei contenuti delle liste non deve essere selezionabile durante l'uso ordinario.

La regola si applica a ogni tipologia di lista, comprese:

- liste CRUD tabellari;
- liste a righe composte;
- griglie e liste inserite nelle schede;
- finestre Zoom e dialoghi di selezione;
- elenchi di ricerca e selezione record;
- eventuali strutture equivalenti basate su tabelle, righe, schede o contenitori.

Il contenitore della lista e i suoi elementi informativi devono applicare `user-select: none` e, quando necessario per compatibilita', `-webkit-user-select: none`.

Restano esclusi dal divieto soltanto i controlli realmente editabili, come campi di ricerca, `input`, `textarea` ed elementi `contenteditable`, nei quali la selezione del testo e' funzionale all'immissione o alla modifica dei dati.

#### 05.01.R05 - Righe Vuote Non Selezionabili Nelle Griglie

Le righe vuote visualizzate per completare graficamente una griglia hanno esclusivamente funzione riempitiva.

Una riga priva di dati non deve:

- accettare la selezione tramite mouse o tastiera;
- assumere lo stato grafico di riga selezionata;
- ricevere il focus operativo;
- entrare nella navigazione con Freccia Su, Freccia Giu', Home ed End;
- attivare comandi associati alla selezione della riga.

La regola si applica alle griglie di Liste, Schede, Zoom, lookup, dialoghi e componenti specializzati.

#### 05.01.R01 - Tasto Esc Per Ritorno Al Menu

Nelle liste CRUD, il tasto `Esc` deve riportare al menu o al modulo chiamante previsto dal flusso.

Il comportamento deve essere coerente con il contesto di apertura della lista.

Se e' aperto un box, un dialogo modale o una selezione guidata, il tasto `Esc` deve prima chiudere l'elemento sovrapposto e soltanto dopo tornare al menu o al modulo chiamante.

#### 05.01.C06 - Colori Standard Della Griglia

Le griglie delle liste CRUD devono utilizzare colori uniformi per testata, passaggio del puntatore e selezione della riga.

Come criterio iniziale:

- testata griglia: gradiente `linear-gradient(#c7d7e8, #b8cbe0 52%, #a9bed6)`;
- bordo inferiore testata: `#758aa1`;
- testo testata: `#0e1824`;
- testata della colonna ordinata: gradiente `linear-gradient(#bbcee1, #abc2da 52%, #9db6cf)`;
- passaggio puntatore mouse sulla riga: `#e3edf8`;
- riga selezionata: `#b8d2ee`;
- riga di posizione corrente del cursore, sia da mouse sia da tastiera: `#b8d2ee`;
- indicatore laterale della riga selezionata: `#2f6faa`.

La riga selezionata e la riga di posizione corrente del cursore condividono intenzionalmente lo stesso colore, per rappresentare un unico stato operativo. Il semplice passaggio del puntatore mantiene invece il colore distinto previsto per l'hover.

Questi colori costituiscono il riferimento grafico delle liste e devono restare uniformi salvo rettifica esplicita dello Standard.

#### 05.01.C07 - Pulsanti Di Titolo E Navigazione Della Lista

Per le liste CRUD, i pulsanti collocati nella barra titolo devono seguire questi criteri grafici:

- il primo pulsante, corrispondente all'azione di maggiore importanza operativa, deve avere sfondo `BlueButton`;
- il primo pulsante deve avere testo bianco;
- i pulsanti ulteriori, collocati a destra del primo, devono avere sfondo bianco;
- i pulsanti ulteriori devono avere testo nero;
- al passaggio del mouse, i pulsanti ulteriori devono assumere lo sfondo `BlueButton`;
- al passaggio del mouse, i pulsanti ulteriori devono assumere testo bianco;
- tutti i pulsanti di titolo e navigazione della lista devono avere dimensione iniziale `140px x 40px`;
- tutti i pulsanti di titolo e navigazione della lista devono usare font `13px / 600`;
- i comandi operativi devono essere rappresentati come pulsanti, non come semplici link testuali;
- la casella contatore deve essere collocata a sinistra del primo pulsante operativo.

#### 05.01.C08 - Font Delle Griglie

Per le griglie dati, il criterio iniziale dei font e':

- testata griglia: `13px / 400`;
- righe griglia: `13px / 500`.

Il criterio vale sia per le liste CRUD grandi a pieno schermo sia per le griglie piu' piccole inserite nelle schede o nei dialoghi.

Le griglie piccole possono essere rettificate quando la dimensione del contenitore o la densita' dei dati richiedono una misura piu' compatta.

#### 05.01.C09 - Scroll Unico Della Griglia

Nelle liste CRUD, lo scorrimento verticale della pagina deve agire esclusivamente sulla griglia dati (o sul contenitore della griglia), non sul contenitore generale della lista.

Come criterio iniziale:

- il contenitore principale della lista non deve generare scroll verticale autonomo;
- il contenitore della griglia (`list-grid-frame` o contenitore equivalente) deve gestire lo scroll verticale;
- la parte titolo e la barra filtri devono restare visibili durante lo scroll della lista.

Questa regola serve a mantenere stabile il riferimento visivo della lista e a ridurre salti di contesto durante la navigazione dati.

## 06 - Zoom E Lookup

### 06.01 - Box Di Selezione

#### 06.01.C01 - Altezza Calcolata Dello Zoom

Come criterio iniziale, gli zoom con griglia di selezione devono mostrare righe dati complete, senza righe tagliate in fondo alla griglia.

Il riferimento iniziale e' lo zoom Articoli:

- righe dati visibili: `13`;
- altezza titolo: `30px`;
- altezza testata griglia: `28px`;
- altezza riga dati nominale: `27px`;
- altezza griglia effettiva: `395px`;
- altezza box effettiva: `521px`.

L'altezza della griglia deve essere calcolata sommando:

- altezza della testata griglia;
- altezza delle righe dati visibili;
- compensazione dei bordi reali della tabella, quando necessaria.

L'altezza complessiva del box deve essere calcolata sommando:

- bordo superiore del box;
- spazio vuoto sopra il titolo;
- titolo;
- spazio vuoto di separazione tra titolo e griglia;
- altezza griglia calcolata;
- eventuale bordo/cornice della griglia, se non gia' compreso nella sua altezza;
- spazio vuoto di separazione tra griglia e comandi;
- barra comandi, filtri e contatore;
- spazio vuoto finale eventualmente previsto dalla grafica;
- bordo inferiore del box.

Nel calcolo devono essere inclusi tutti gli spazi strutturali e tutti i bordi che concorrono all'altezza visiva effettiva.

L'altezza del box non deve essere impostata a occhio.

#### 06.01.C02 - Spazi Interni Del Box Zoom

Come criterio iniziale, gli spazi interni del box zoom sono:

- spazio tra bordo superiore e titolo: `15px`;
- spazio tra titolo e griglia: `15px`;
- spazio tra griglia e barra comandi: `15px`;
- spazio tra barra comandi e bordo inferiore: `15px`;
- bordo laterale sinistro e destro rispetto a titolo, griglia e barra comandi: `15px`.

I margini devono essere ottenuti con misure strutturali esplicite, non con padding generici del contenitore.

#### 06.01.C03 - Titolo Dello Zoom

Il titolo dello zoom deve avere:

- altezza `30px`;
- sfondo azzurro standard;
- font coerente con gli zoom;
- allineamento verticale centrato;
- larghezza allineata alla griglia e alla barra comandi.

#### 06.01.C04 - Griglia Dello Zoom

La griglia dello zoom deve avere:

- testata alta `28px`;
- righe dati con altezza nominale `27px`;
- testo delle celle con `vertical-align: middle`;
- assenza di padding interno nelle celle, salvo compensazioni specifiche e motivate;
- testata sticky quando la griglia e' scrollabile;
- nessuna riga dati tagliata nella visualizzazione iniziale.

#### 06.01.C05 - Colonne Dello Zoom

Le colonne dello zoom devono seguire questo criterio:

- colonne tecniche o a lunghezza prevedibile: larghezza fissa;
- colonne descrittive: larghezza elastica;
- la colonna descrittiva principale assorbe preferibilmente lo spazio residuo.

Esempio zoom Articoli:

- `Codice`: fisso;
- `Descrizione`: elastico;
- `Categoria`: fisso;
- `Prezzo`: fisso.

#### 06.01.C06 - Compensazione Scrollbar Verticale

Quando la griglia puo' contenere piu' record delle righe visibili, occorre prevedere l'ingombro della scrollbar verticale.

Come criterio iniziale:

- l'ultima colonna deve includere lo spazio della scrollbar;
- misura orientativa della compensazione: `22px`;
- l'eventuale spazio interno destro dell'ultima colonna deve evitare che il testo venga coperto o compresso dalla scrollbar.

Il criterio va applicato solo quando la griglia e' o puo' diventare scrollabile.

#### 06.01.C07 - Barra Comandi Dello Zoom

La barra comandi dello zoom deve contenere, quando previsti:

- label di ricerca;
- campo di ricerca;
- label contatore record;
- campo contatore record;
- pulsante principale;
- pulsante secondario.

Le label della barra comandi devono seguire lo standard grafico delle label:

- sfondo azzurro standard;
- bordo standard;
- raggio bordo `5px`;
- altezza coerente con i controlli della barra;
- testo non selezionabile.

I pulsanti operativi dello stesso gruppo devono avere dimensione uniforme.

#### 06.01.R01 - Selezione Iniziale Dello Zoom

All'apertura di uno zoom o lookup, la selezione deve essere posizionata sulla prima riga utile della griglia.

La prima riga selezionata deve essere visibile e deve avere evidenza grafica immediata.

Se la griglia non contiene record, non deve essere selezionata alcuna riga.

#### 06.01.R02 - Ordinamento Colonne Dello Zoom

Le colonne ordinabili dello zoom devono essere chiaramente riconoscibili.

Il click sulla testata deve ordinare la griglia secondo la colonna selezionata.

La colonna di ordinamento attiva deve mostrare una freccetta di sort.

La freccetta deve indicare la direzione corrente dell'ordinamento.

Il comportamento deve restare uniforme in tutti gli zoom e lookup.

## 07 - Messaggi

### 07.01 - Box Messaggi Standard

#### 07.01.001 - Uso Obbligatorio Del Box Messaggi

Per tutti i messaggi di avviso, conferma ed errore deve essere utilizzato il box messaggi standard dell'applicazione.

Non devono essere usati messaggi nativi del browser quando e' disponibile il box standard.

Il box messaggi deve rispettare obbligatoriamente i colori e il comportamento gia' definiti per:

- avviso;
- conferma;
- errore.

#### 07.01.002 - Colori Standard Del Box Messaggi

I colori standard del box messaggi sono:

- informazione / avviso ordinario: blu;
- esito positivo: verde;
- conferma: giallo/arancio;
- errore o blocco: rosso.

La codifica dei colori ha valore funzionale, e' vincolante e deve restare uniforme in tutta l'applicazione.

Non sono ammesse varianti cromatiche locali, salvo aggiornamento preventivo dello Standard.

#### 07.01.003 - Pulsanti Del Box Messaggi

I pulsanti del box messaggi costituiscono una categoria autonoma di pulsanti.

Come criterio iniziale:

- dimensione fissa: `148px x 40px`;
- font: `16px / 600`;
- pulsante principale: sfondo `BlueButton`, testo bianco;
- pulsante secondario: sfondo bianco, testo nero;
- al passaggio del mouse, il pulsante secondario assume sfondo `BlueButton` e testo bianco;
- il focus deve essere leggero, visibile e non deve produrre un doppio bordo invasivo;
- i pulsanti devono essere centrati orizzontalmente nel box;
- lo spazio tra pulsanti deve essere pari a `10px`.

Il pulsante principale deve rappresentare l'azione confermativa o l'unica azione disponibile.

Il pulsante secondario deve rappresentare l'azione di annullamento, rinuncia o ritorno.

#### 07.01.004 - Grafica Del Box Messaggi

Il box messaggi deve essere chiaramente visibile, ma non invadente.

Come criterio iniziale:

- larghezza minima box: `540px`;
- larghezza massima box: `720px`;
- altezza minima box: `220px`;
- larghezza massima su schermi piccoli: `calc(100vw - 48px)`;
- sfondo box: bianco opaco;
- bordo box: sottile, colore `#b8c5d6`;
- raggio angoli box: `8px`;
- ombra: presente ma leggera;
- overlay pagina: scuro trasparente, senza oscurare eccessivamente il modulo sottostante;
- titolo: barra colorata secondo il tipo di messaggio;
- font titolo: `15px / 700`;
- altezza visiva titolo: circa `40px`;
- testo titolo rientrato di `16px` senza usare padding strutturale;
- corpo messaggio: centrato;
- corpo messaggio senza padding strutturale;
- larghezza utile del testo: larghezza box meno `48px`;
- font messaggio principale: `16px / 650`;
- interlinea messaggio principale: circa `1.48`;
- font dettaglio: `14px / 600`;
- interlinea dettaglio: circa `1.5`;
- spazio tra messaggio principale e dettaglio: `10px`;
- area pulsanti: centrata sotto il messaggio;
- distanza tra bordo inferiore dei pulsanti e bordo inferiore interno del box: `36px`.

Il box messaggi non deve assumere dimensioni eccessive per effetto di testi brevi.

I testi lunghi devono andare a capo dentro il box senza allargarlo oltre la misura prevista.

L'eventuale altezza residua prodotta dall'altezza minima del box deve essere assorbita dal corpo del messaggio, non dallo spazio sotto i pulsanti.

## 08 - Moduli Contabili

### 08.01 - Documenti IVA

#### 08.01.001 - Documento Gia' Esistente

Nel caso di inserimento manuale, il controllo di documento gia' esistente e' bloccante.

Nel caso di importazione da XML, il documento gia' esistente deve essere rilevato prima del salvataggio e deve richiedere conferma esplicita di sovrascrittura.

La conferma di sovrascrittura data in fase di importazione XML esclude la ripetizione del blocco previsto per l'inserimento manuale.

Il controllo di validazione previsto per l'inserimento manuale deve attivarsi soltanto quando l'inserimento non deriva da elaborazione XML.

Se l'inserimento deriva da XML e l'utente ha gia' confermato la sovrascrittura, non deve comparire un secondo avviso sul documento gia' esistente.

#### 08.01.002 - Sovrascrittura E Riuso Delle Partite

In caso di sovrascrittura di un documento IVA gia' registrato, devono essere recuperate e riutilizzate le chiavi funzionali esistenti:

- Partita del movimento IVA;
- Partita del movimento contabile collegato.

I record di dettaglio e le scadenze collegate devono essere rigenerati in coerenza con il documento sostituito.

Le scadenze devono mantenere il collegamento tecnico al movimento IVA e, quando previste dalla struttura della tabella, devono riportare anche la Partita del movimento IVA.

## 09 - Dialoghi Modali

### 09.01 - Form Modali

#### 09.01.001 - Assenza Del Footer

Nelle form aperte in formato modale non deve essere visualizzato il footer della pagina o del layout generale.

La form modale deve contenere solo il modulo operativo richiamato e i relativi comandi contestuali.

La presenza del footer in una form modale e' vietata per evitare sovrapposizioni, confusione grafica e interferenze con il modulo chiamante.

### 09.02 - Contenitore Modale

#### 09.02.001 - Grafica Del Contenitore Modale

Ogni form aperta in formato modale deve essere contenuta in un pannello modale standard.

Il contenitore modale deve:

- oscurare in modo coerente il modulo chiamante;
- avere bordo esterno, colore di fondo e ombra uniformi;
- centrare orizzontalmente e verticalmente il modulo richiamato;
- mantenere una separazione visiva chiara tra modulo chiamante e modulo richiamato;
- contenere soltanto la scheda operativa richiamata;
- avere priorita' visiva rispetto al modulo sottostante.

Il contenitore modale non deve alterare la grafica propria della scheda richiamata, ma deve incorniciarla e separarla chiaramente dal contesto sottostante.

#### 09.02.002 - Margini Del Contenitore Modale

Il contenitore modale deve mantenere un margine esterno standard di almeno `24px` rispetto ai bordi della finestra.

Il pannello modale deve inoltre lasciare un bordo visibile e proporzionato intorno alla scheda contenuta.

Il margine interno intorno alla scheda deve essere sufficiente a evitare l'effetto di modulo incollato al bordo, ma non deve produrre spazi vuoti eccessivi.

Eventuali adattamenti dimensionali specifici sono ammessi solo per migliorare il centraggio visivo o la leggibilita' del modulo, senza violare il margine minimo standard.

