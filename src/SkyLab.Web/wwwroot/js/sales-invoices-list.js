document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-sales-invoices]");
  const frame = page?.querySelector(".purchase-invoices-grid-frame");
  const rows = [...(page?.querySelectorAll(".sales-invoices-grid tbody tr[data-invoice-id]") ?? [])];
  if (!page || !frame || rows.length === 0) return;

  const select = (row, focus = false) => {
    if (!row) return;
    rows.forEach(item => {
      item.classList.toggle("selected", item === row);
      item.classList.toggle("selected-row", item === row);
      if (item === row) item.setAttribute("aria-selected", "true");
      else item.removeAttribute("aria-selected");
    });
    if (focus) row.focus({ preventScroll: true });
    row.scrollIntoView({ block: "nearest" });
  };
  const selected=()=>rows.find(row=>row.classList.contains("selected"));
  const buttons=Object.fromEntries([...page.querySelectorAll(".list-title-actions button")].map(button=>[button.textContent.trim(),button]));
  const id=()=>selected()?.dataset.invoiceId??"";
  const closeProgress=callback=>window.SkyProg?.Close?window.SkyProg.Close(callback):callback();
  const showInvoiceMessage=(message,variant="error",onConfirm)=>window.SkyLabMessageBox?.show?.({title:"Fattura elettronica",message,variant,okText:"OK",onConfirm});
  const transmitElectronicInvoice=async(invoiceId,button,row)=>{
    button.disabled=true;window.SkyProg?.Show?.();let error="",fileName="";
    try{const response=await fetch(`/api/sales-invoices/${encodeURIComponent(invoiceId)}/electronic-send`,{method:"POST",credentials:"same-origin",headers:{Accept:"application/json"}});const result=await response.json().catch(()=>null);if(!response.ok||!result?.success)error=result?.message||"Invio della fattura elettronica non riuscito.";else fileName=result.fileName||""}catch{error="Impossibile completare l'invio tramite PEC."}
    closeProgress(()=>{button.disabled=false;if(error){showInvoiceMessage(error);return}if(row?.cells[9])row.cells[9].textContent="FE inviata";showInvoiceMessage(`Il server PEC ha accettato l'invio del file ${fileName}. L'esito dello SdI sarà comunicato con una successiva notifica.` ,"success")});
  };
  buttons["Invia FE"]?.addEventListener("click",async()=>{
    const row=selected(),invoiceId=id(),button=buttons["Invia FE"];if(!invoiceId||!row)return;
    button.disabled=true;window.SkyProg?.Show?.();let error="",fileName="",alreadyPrepared=false;
    try{const response=await fetch(`/api/sales-invoices/${encodeURIComponent(invoiceId)}/electronic-prepare`,{method:"POST",credentials:"same-origin",headers:{Accept:"application/json"}});const result=await response.json().catch(()=>null);if(!response.ok||!result?.success)error=result?.message||"Preparazione della fattura elettronica non riuscita.";else{fileName=result.fileName||"";alreadyPrepared=Boolean(result.alreadyPrepared)}}catch{error="Impossibile completare la preparazione della fattura elettronica."}
    closeProgress(()=>{button.disabled=false;if(error){showInvoiceMessage(error);return}if(row.cells[9])row.cells[9].textContent="FE elaborata";const message=alreadyPrepared?`La fattura elettronica è già stata elaborata con il nome ${fileName}.\nVuoi inviarla ora tramite PEC?`:`La fattura elettronica è stata elaborata con successo\ncon il nome ${fileName}\nVuoi inviarla ora tramite PEC?`;window.SkyLabMessageBox?.show?.({mode:"confirm",variant:"confirm",title:alreadyPrepared?"Fattura elettronica già esistente":"Fattura elettronica pronta",message,okText:"Invia",cancelText:"Esci",onConfirm:()=>void transmitElectronicInvoice(invoiceId,button,row)})});
  });
  buttons.Modifica?.addEventListener("click",()=>{if(id())location.href=`/FattureVendita/Edit?azione=3&id=${encodeURIComponent(id())}`});
  const escape=value=>String(value??"").replace(/[&<>"']/g,character=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"})[character]);
  const money=value=>Number(value||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2});
  buttons.Stampa?.addEventListener("click",async()=>{
    if(!id())return;const preview=window.open("about:blank","_blank");if(!preview){window.SkyLabMessageBox?.show?.({title:"Stampa fattura",message:"Il browser ha bloccato l'anteprima. Consenti le finestre popup per SkyLab e riprova.",variant:"error"});return}
    preview.opener = null;
    try{
      const response=await fetch(`/api/sales-invoices/${encodeURIComponent(id())}`,{headers:{Accept:"application/json"}});if(!response.ok)throw new Error();const invoice=await response.json(),lines=invoice.lines||[],materials=lines.reduce((sum,line)=>sum+Number(line.amount||0),0),taxable=Number(invoice.workAmount||0)+materials,vat=Number(invoice.workAmount||0)*Number(invoice.workVatRate||0)/100+lines.reduce((sum,line)=>sum+Number(line.amount||0)*Number(line.vatRate||0)/100,0),total=taxable+vat,type=invoice.cause===31?"Nota di addebito":invoice.cause===32?"Nota di accredito":"Fattura di vendita",date=String(invoice.documentDate||"").slice(0,10).split("-").reverse().join("/");
      const articleRows=lines.map(line=>`<tr><td>${escape(line.articleCode)}</td><td>${escape(line.description)}</td><td>${escape(line.unit)}</td><td class="num">${Number(line.quantity||0).toLocaleString("it-IT",{minimumFractionDigits:3,maximumFractionDigits:3})}</td><td class="num">${money(line.unitPrice)}</td><td class="num">${money(line.discount)}%</td><td class="num">${money(line.amount)}</td><td class="num">${money(line.vatRate)}%</td></tr>`).join("");
      preview.document.write(`<!doctype html><html lang="it"><head><meta charset="utf-8"><title>Fattura di cortesia ${escape(invoice.documentNumber)}</title><style>
@page{margin:0;size:A4}*{box-sizing:border-box}html,body{margin:0;min-height:100%;padding:0}body{background:#f2f4f7;color:#111827;font-family:Arial,Helvetica,sans-serif;font-size:12pt;line-height:1.22}.invoice-page{background:#fff;height:297mm;margin:0 auto;overflow:hidden;padding:15mm 14mm 11mm;position:relative;width:210mm}.invoice-header{align-items:flex-start;display:grid;grid-template-columns:70mm 1fr;min-height:38mm}.invoice-logo{align-items:flex-start;display:flex;min-height:32mm}.invoice-logo img{display:block;height:28mm;object-fit:contain;width:28mm}.company-data{font-size:10.7pt;line-height:1.28;padding-top:1mm;white-space:pre-line}.invoice-title-row{align-items:baseline;display:grid;grid-template-columns:1fr auto;margin-top:1mm}.invoice-title strong{font-size:11pt}.invoice-number{align-items:baseline;display:flex;gap:3mm;justify-content:flex-end;min-width:68mm;white-space:nowrap}.invoice-number span,.invoice-number strong{font-size:10pt;font-weight:400}.courtesy{color:#647084;font-size:7.8pt;margin-top:1.5mm;text-transform:uppercase}.party-row{display:grid;gap:24mm;grid-template-columns:1fr 1fr;margin-top:9mm}.party-box{font-size:9.2pt;line-height:1.38;min-height:24mm;white-space:pre-line}.party-box h2,.invoice-section h2{font-size:9pt;font-weight:400;line-height:1.1;margin:0 0 4mm}.invoice-section{margin-top:5mm}.description-section{min-height:24mm}.description-text{font-size:9.2pt;line-height:1.3;white-space:pre-line}.articles{border-collapse:collapse;font-size:7.8pt;table-layout:fixed;width:100%}.articles th,.articles td{border-bottom:1px solid #c5ccd5;padding:1.7mm 1.2mm;vertical-align:top}.articles th{border-bottom:1px solid #475569;font-weight:600;text-align:left}.articles .code{width:25mm}.articles .unit{width:12mm}.articles .quantity{width:18mm}.articles .price,.articles .discount,.articles .amount,.articles .vat{width:19mm}.num{text-align:right!important;white-space:nowrap}.amounts-section{margin-top:5mm}.amounts-layout{align-items:start;display:grid;gap:5mm;grid-template-columns:68mm 53mm 1fr}.amount-lines,.amount-totals{border-collapse:collapse;width:100%}.amount-lines td,.amount-totals td{font-size:10pt;line-height:1.35;padding:0 0 1.3mm;vertical-align:top;white-space:nowrap}.amount-lines td:last-child,.amount-totals td:last-child{text-align:right}.net-total{font-size:10.8pt;line-height:1.4;margin-left:10mm;padding-top:4.5mm;text-align:right;white-space:nowrap}.net-total span,.net-total strong{display:block}.net-total strong{font-size:11pt}.payment-section{margin-top:6mm}.payment-grid{display:grid;font-size:9pt;gap:1.2mm 6mm;grid-template-columns:max-content 1fr;line-height:1.35}.final-notes{font-size:7.9pt;line-height:1.28;margin-top:7mm;text-align:center}.empty-row{text-align:center}@media screen{.invoice-page{box-shadow:0 10px 28px rgba(15,23,42,.18);margin:16px auto}}@media print{body{background:#fff}.invoice-page{box-shadow:none;margin:0}}
</style></head><body><main class="invoice-page"><header class="invoice-header"><div class="invoice-logo"><img src="${location.origin}/images/skylab-logo.png" alt="SkyLab"></div><div class="company-data"><strong>SkyLab</strong>\nGestione assistenza tecnica integrata</div></header><section class="invoice-title-row"><div class="invoice-title"><strong>${escape(type).toUpperCase()}</strong><div class="courtesy">Documento di cortesia - non valido ai fini fiscali</div></div><div class="invoice-number"><span>n.ro</span><strong>${escape(invoice.documentNumber)}</strong><span>del</span><strong>${escape(date)}</strong></div></section><section class="party-row"><article class="party-box"><h2>Riferimento</h2>Codice cliente ${escape(String(invoice.customerCode).padStart(5,"0"))}\n${escape(invoice.workSheetReference||"")}</article><article class="party-box"><h2>Destinatario fattura</h2><strong>${escape(invoice.customerName)}</strong></article></section><section class="invoice-section description-section"><h2>Descrizione</h2><div class="description-text">${escape(invoice.activity||"")}</div></section><section class="invoice-section"><h2>Articoli e materiali</h2><table class="articles"><thead><tr><th class="code">Articolo</th><th>Descrizione</th><th class="unit">U.m.</th><th class="quantity num">Quantità</th><th class="price num">Prezzo</th><th class="discount num">Sconto</th><th class="amount num">Importo</th><th class="vat num">IVA %</th></tr></thead><tbody>${articleRows||'<tr><td class="empty-row" colspan="8">Nessun articolo</td></tr>'}</tbody></table></section><section class="invoice-section amounts-section"><h2>Importi fattura</h2><div class="amounts-layout"><table class="amount-lines"><tbody><tr><td>Importo lavoro</td><td>${money(invoice.workAmount)}</td></tr><tr><td>Aliquota IVA lavoro</td><td>${money(invoice.workVatRate)}%</td></tr></tbody></table><table class="amount-totals"><tbody><tr><td>Imponibile</td><td>${money(taxable)}</td></tr><tr><td>IVA</td><td>${money(vat)}</td></tr><tr><td>Totale fattura</td><td>${money(total)}</td></tr></tbody></table><aside class="net-total"><span>Totale documento</span><strong>€ ${money(total)}</strong></aside></div></section><section class="payment-section"><h2>Modalità di pagamento</h2><div class="payment-grid"><span>Condizioni:</span><strong>Come convenuto</strong></div></section><footer class="final-notes">Stampa di cortesia generata da SkyLab</footer></main><script>addEventListener("load",()=>print())<\/script></body></html>`);preview.document.close();
    }catch{preview.close();window.SkyLabMessageBox?.show?.({title:"Stampa fattura",message:"Impossibile preparare la fattura di cortesia.",variant:"error"})}
  });
  buttons["Stampa lista"]?.addEventListener("click",()=>window.print());
  buttons.Cancella?.addEventListener("click",()=>{const row=selected();if(!row)return;window.SkyLabMessageBox?.show?.({mode:"confirm",variant:"confirm",title:"Cancella fattura",message:"Cancellare la fattura selezionata?",okText:"Cancella",onConfirm:async()=>{window.SkyProg?.Show?.();let error="";try{const response=await fetch(`/api/sales-invoices/${encodeURIComponent(row.dataset.invoiceId)}`,{method:"DELETE",credentials:"same-origin"});const result=await response.json().catch(()=>null);if(!response.ok||!result?.success)error=result?.message||"Cancellazione non riuscita."}catch{error="Impossibile completare la cancellazione."}finally{window.SkyProg?.Close?.(()=>{if(error){window.SkyLabMessageBox?.show?.({title:"Fatture di vendita",message:error,variant:"error"});return}location.reload()})}}})});

  const move = (row, key) => {
    const current = Math.max(0, rows.indexOf(row));
    const index = key === "ArrowDown" ? Math.min(current + 1, rows.length - 1)
      : key === "ArrowUp" ? Math.max(current - 1, 0)
      : key === "Home" ? 0
      : rows.length - 1;
    select(rows[index], true);
  };

  rows.forEach(row => {
    row.tabIndex = -1;
    row.addEventListener("click", () => select(row, true));
    row.addEventListener("keydown", event => {
      if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
      event.preventDefault();
      move(row, event.key);
    });
  });

  document.addEventListener("keydown", event => {
    if (event.key !== "Escape") return;
    if (document.body.classList.contains("lookup-open") || document.querySelector("dialog[open]")) return;
    event.preventDefault();
    event.stopPropagation();
    window.location.href = "/";
  });

  select(rows[0], true);
});
