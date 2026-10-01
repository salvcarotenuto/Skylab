document.addEventListener("DOMContentLoaded", () => {
  const page=document.querySelector("[data-sales-invoice-edit]"),form=page?.querySelector("form"),workDescription=page?.querySelector("[data-sales-invoice-work-description]");
  if(!page||!form||!workDescription)return;
  workDescription.addEventListener("blur",()=>{workDescription.scrollTop=0;workDescription.scrollLeft=0});

  const fields=[...page.querySelectorAll(".purchase-load-field")];
  const field=label=>fields.find(item=>item.querySelector("label")?.textContent.trim().toLocaleLowerCase("it-IT")===label.toLocaleLowerCase("it-IT"));
  const control=label=>field(label)?.querySelector("input:not([type=hidden]),select,textarea")??null;
  const number=value=>(window.SkyLabDecimal?.parse(value)??Number.parseFloat(String(value??"").replace(/\s|%/g,"").replace(/\./g,"").replace(",",".")))||0;
  const message=(text,focus)=>{window.SkyLabMessageBox?.show?.({title:"Fattura di vendita",message:text,variant:"error",onConfirm:()=>focus?.focus?.()});if(!window.SkyLabMessageBox?.show)window.alert(text)};
  const documentNumber=control("Numero documento"),documentDate=form.elements.namedItem("DocumentDate")??control("Data documento"),customer=page.querySelector("#sales-invoice-edit-customer"),type=control("Tipo documento"),store=control("Unità locale"),agent=control("Agente"),workAmount=page.querySelector(".sales-invoice-work-amount input"),workVat=page.querySelector("[data-percent-field]"),total=page.querySelector("[data-sales-invoice-total]"),taxable=page.querySelector("[data-sales-invoice-taxable]"),vatTotal=page.querySelector("[data-sales-invoice-vat-total]"),workSheet=page.querySelector("[data-work-sheet-id]"),linesBody=page.querySelector("[data-sales-invoice-lines-grid] tbody");
  const save=[...page.querySelectorAll("header button")].find(button=>button.textContent.trim().toLocaleLowerCase("it-IT")==="salva");
  if(!save)return;
  const parameters=new URLSearchParams(location.search),invoiceId=Number.parseInt(parameters.get("id")||"0",10)||0,readOnly=parameters.get("azione")==="101";

  const exercise=()=>{const named=form.elements.namedItem("Anno"),namedValue=named instanceof RadioNodeList?named.value:named?.value;if(/^\d{4}$/.test(String(namedValue??"").trim()))return Number.parseInt(namedValue,10);const value=[...page.querySelectorAll(".sales-invoice-general input")].map(input=>input.value.trim()).find(item=>/^\d{4}$/.test(item));return Number.parseInt(value??"0",10)||0};
  const isoDate=value=>{const text=String(value??"").trim();if(/^\d{4}-\d{2}-\d{2}$/.test(text))return text;const match=text.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);return match?`${match[3]}-${match[2]}-${match[1]}`:""};
  const cause=()=>{const value=Number.parseInt(type?.value??"",10);if(value>=30&&value<=32)return value;const text=type?.selectedOptions?.[0]?.textContent?.toLocaleLowerCase("it-IT")??"";return text.includes("accredito")?32:text.includes("addebito")?31:30};
  const rows=()=>[...(linesBody?.rows??[])].filter(row=>!row.classList.contains("is-placeholder")&&row.cells[0]?.textContent.trim());
  const payload=()=>({
    documentNumber:documentNumber?.value.trim()??"",documentDate:isoDate(documentDate?.value),cause:cause(),
    customerCode:Number.parseInt(customer?.value??"0",10)||0,storeCode:Number.parseInt(store?.value??"0",10)||0,agentCode:Number.parseInt(agent?.value??"0",10)||0,
    activity:workDescription.value.trim(),workSheetId:Number.parseInt(workSheet?.value??"0",10)||0,workAmount:number(workAmount?.value),workVatRate:number(workVat?.value),taxableTotal:number(taxable?.value),vatTotal:number(vatTotal?.value),invoiceTotal:number(total?.value),
    lines:rows().map(row=>({articleCode:row.cells[0].textContent.trim(),description:row.cells[1].textContent.trim(),unit:row.cells[2].textContent.trim(),quantity:number(row.cells[3].textContent),unitPrice:number(row.cells[4].textContent),discount:number(row.cells[5].textContent),amount:number(row.cells[6].textContent),vatRate:number(row.cells[7].textContent),vatCode:row.cells[9]?.textContent.trim()??""}))
  });
  const validate=data=>{
    if(!data.documentNumber){message("Campo Numero documento obbligatorio.",documentNumber);return false}
    if(!data.documentDate){message("Campo Data documento obbligatorio.",documentDate);return false}
    if(Number.parseInt(data.documentDate.slice(0,4),10)!==exercise()){message(`La data documento deve appartenere all'esercizio contabile in linea (${exercise()}).`,documentDate);return false}
    if(data.customerCode<=0){message("Campo Cliente obbligatorio.",customer);return false}
    if(data.invoiceTotal<=0){message("Il Totale fattura deve essere maggiore di zero.",workAmount);return false}
    return true;
  };
  const closeProgress=callback=>window.SkyProg?.Close?window.SkyProg.Close(callback):callback();

  save.addEventListener("click",async()=>{
    if(save.disabled)return;const data=payload();if(!validate(data))return;
    save.disabled=true;window.SkyProg?.Show?.();let error="";
    try{
      const response=await fetch(invoiceId?`/api/sales-invoices/${invoiceId}`:"/api/sales-invoices",{method:invoiceId?"PUT":"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},credentials:"same-origin",body:JSON.stringify(data)});
      const result=await response.json().catch(()=>null);if(!response.ok||!result?.success){const validation=result?.errors?Object.values(result.errors).flat().join(" "):"";error=result?.message||result?.detail||validation||`Registrazione della fattura non riuscita (errore ${response.status}).`}
    }catch{error="Impossibile completare il salvataggio della fattura."}
    finally{closeProgress(()=>{save.disabled=false;if(error){message(error);return}location.href="/FattureVendita"})}
  });
  if(invoiceId)fetch(`/api/sales-invoices/${invoiceId}`,{headers:{Accept:"application/json"}}).then(response=>{if(!response.ok)throw new Error();return response.json()}).then(data=>{documentNumber.value=data.documentNumber;const visibleDate=control("Data documento"),parts=String(data.documentDate).slice(0,10).split("-");documentDate.value=String(data.documentDate).slice(0,10);if(visibleDate)visibleDate.value=parts.length===3?`${parts[2]}/${parts[1]}/${parts[0]}`:"";type.value=String(data.cause);customer.value=String(data.customerCode).padStart(5,"0");const customerName=page.querySelector("#sales-invoice-edit-customer-name");if(customerName)customerName.value=data.customerName||"";if(store)store.value=String(data.storeCode||"");if(agent)agent.value=String(data.agentCode||"").padStart(3,"0");workDescription.value=data.activity||"";workAmount.value=Number(data.workAmount||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2});workVat.value=`${Number(data.workVatRate||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2})} %`;if(workSheet)workSheet.value=String(data.workSheetId||0);const reference=page.querySelector("[data-work-sheet-reference]");if(reference)reference.value=data.workSheetReference||"";linesBody.replaceChildren();(data.lines||[]).forEach(line=>{const row=document.createElement("tr");row.innerHTML=`<td>${line.articleCode||""}</td><td>${line.description||""}</td><td>${line.unit||""}</td><td>${Number(line.quantity||0).toLocaleString("it-IT",{minimumFractionDigits:3,maximumFractionDigits:3})}</td><td>${Number(line.unitPrice||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2})}</td><td>${Number(line.discount||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2})}</td><td>${Number(line.amount||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2})}</td><td>${Number(line.vatRate||0).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2})}%</td><td hidden data-article-state>1</td>`;const vatCell=row.insertCell();vatCell.hidden=true;vatCell.dataset.vatCode="";vatCell.textContent=line.vatCode||"";linesBody.append(row)});window.SkyLabSalesInvoiceTotals?.recalculate?.();if(readOnly){save.hidden=true;form.querySelectorAll("input,select,textarea,button").forEach(control=>control.disabled=true);page.querySelectorAll("header a,header button").forEach(control=>{if(control.textContent.trim()!=="Annulla")control.hidden=true})}}).catch(()=>message("Impossibile caricare la fattura selezionata."));
});
