(() => {
  const page=document.querySelector(".sales-invoice-edit-page"),form=page?.querySelector("form"),header=page?.querySelector("header"),activity=page?.querySelector("[data-sales-invoice-work-description]"),amount=page?.querySelector(".sales-invoice-work-amount input"),customerCode=page?.querySelector("#sales-invoice-edit-customer"),customerName=page?.querySelector("#sales-invoice-edit-customer-name"),footerTotal=page?.querySelector(".purchase-load-total"),linesBody=page?.querySelector("[data-sales-invoice-lines-grid] tbody");
  if(!page||!form||!header||!activity||!amount)return;
  const actions=header.lastElementChild;if(!actions)return;const cancelAction=actions.lastElementChild;
  const button=document.createElement("button");button.type="button";button.className="btn btn-outline-primary sales-invoice-work-open";button.textContent="Carica scheda";actions.insertBefore(button,cancelAction);
  const viewButton=document.createElement("button");viewButton.type="button";viewButton.className="btn btn-outline-primary sales-invoice-work-view-button";viewButton.textContent="Vedi scheda";viewButton.disabled=true;actions.insertBefore(viewButton,cancelAction);
  const typeField=[...page.querySelectorAll(".purchase-load-field")].find(field=>field.querySelector("label")?.textContent.trim()==="Tipo documento");if(!typeField)return;const reference=document.createElement("div");reference.className="purchase-load-field sales-invoice-agent sales-invoice-work-reference-general";reference.innerHTML=`<input type="hidden" name="SchedaLavoro" value="0" data-work-sheet-id><input type="hidden" name="PrintScheda" value="false" data-work-sheet-print><label>Scheda lavoro</label><input type="text" readonly tabindex="-1" data-work-sheet-reference>`;typeField.insertAdjacentElement("afterend",reference);
  const workLayout=activity.closest(".sales-invoice-work-layout");const materials=document.createElement("label");materials.className="sales-invoice-work-materials";materials.innerHTML=`<span>Importo m. prime</span><input type="text" value="0,00" readonly tabindex="-1" onmousedown="event.preventDefault()" data-sales-invoice-materials>`;workLayout?.append(materials);const taxable=document.createElement("label");taxable.className="sales-invoice-work-vat sales-invoice-work-taxable";taxable.innerHTML=`<span>Totale imponibile</span><input type="text" value="0,00" readonly tabindex="-1" onmousedown="event.preventDefault()" data-sales-invoice-taxable>`;workLayout?.append(taxable);const total=document.createElement("label");total.className="sales-invoice-work-vat sales-invoice-work-total";total.innerHTML=`<span>Totale fattura</span><input type="text" value="${footerTotal?.querySelector("strong")?.textContent?.trim()||"0,00"}" readonly tabindex="-1" onmousedown="event.preventDefault()" data-sales-invoice-total>`;workLayout?.append(total);if(footerTotal)footerTotal.hidden=true;
  const id=reference.querySelector("[data-work-sheet-id]"),display=reference.querySelector("[data-work-sheet-reference]"),print=reference.querySelector("[data-work-sheet-print]");
  const clearWorkSheet=()=>{page.querySelectorAll(".sales-invoice-general input").forEach(input=>{if(!input.closest(".date"))input.value=""});page.querySelectorAll(".sales-invoice-general select").forEach(select=>select.selectedIndex=0);id.value="0";display.value="";print.value="false";viewButton.disabled=true;if(customerCode)customerCode.value="";if(customerName)customerName.value="";activity.value="";amount.value="";if(workVat)workVat.value=`${number(standardRate)} %`;linesBody?.replaceChildren();recalculate()};const clearAllWhenWorkSheetIsEmpty=()=>{if(!id.value||id.value==="0"||!display.value.trim())clearWorkSheet()};id.addEventListener("change",clearAllWhenWorkSheetIsEmpty);display.addEventListener("change",clearAllWhenWorkSheetIsEmpty);form.addEventListener("reset",()=>setTimeout(clearWorkSheet,0));window.SkyLabSalesInvoiceWorkSheet={clear:clearWorkSheet};
  const materialsField=page.querySelector("[data-sales-invoice-materials]"),taxableField=page.querySelector("[data-sales-invoice-taxable]"),totalField=page.querySelector("[data-sales-invoice-total]"),workVat=page.querySelector(".sales-invoice-work-vat [data-percent-field]"),vatTotal=document.createElement("label");vatTotal.className="sales-invoice-work-vat sales-invoice-work-vat-total";vatTotal.innerHTML=`<span>Totale IVA</span><input type="text" value="0,00" readonly tabindex="-1" onmousedown="event.preventDefault()" data-sales-invoice-vat-total>`;workLayout?.append(vatTotal);const vatTotalField=vatTotal.querySelector("[data-sales-invoice-vat-total]");
  const number=(value,digits=2)=>Number(value||0).toLocaleString("it-IT",{minimumFractionDigits:digits,maximumFractionDigits:digits});
  const parse=value=>(window.SkyLabDecimal?.parse(value)??Number.parseFloat(String(value||0).replace(/\./g,"").replace(",",".")))||0;
  const cellValue=(row,index)=>{const cell=row.cells[index];return cell?.querySelector("input,select")?.value??cell?.textContent??""};
  const standardRate=parse(workVat?.dataset?.standardRate||22);
  const cellRate=(row,index)=>{const cell=row.cells[index],control=cell?.querySelector("select"),option=control?.selectedOptions?.[0],raw=option?.dataset?.rate??cellValue(row,index).replace("%","");return String(raw).trim()===""?standardRate:parse(raw)};
  const recalculateRow=row=>{const quantity=parse(cellValue(row,3)),price=parse(cellValue(row,4)),discount=parse(cellValue(row,5)),value=Math.round(quantity*price*(1-discount/100)*100)/100,cell=row.cells[6],control=cell?.querySelector("input");if(control)control.value=number(value);else if(cell)cell.textContent=number(value);return value};
  const recalculate=event=>{if(event?.target?.closest("tr")){const cell=event.target.closest("td");if(cell&&[3,4,5].includes(cell.cellIndex))recalculateRow(cell.parentElement)}const workAmount=parse(amount.value),workRate=parse(workVat?.value),rows=[...(linesBody?.rows||[])].filter(row=>!row.classList.contains("is-placeholder")),articlesTaxable=rows.reduce((sum,row)=>sum+parse(cellValue(row,6)),0),articlesTotal=rows.reduce((sum,row)=>{const rowTaxable=parse(cellValue(row,6)),rate=cellRate(row,7);return sum+rowTaxable+(rowTaxable*rate/100)},0),taxableTotal=workAmount+articlesTaxable,total=workAmount+(workAmount*workRate/100)+articlesTotal,vatTotal=total-taxableTotal;if(materialsField)materialsField.value=number(articlesTaxable);if(taxableField)taxableField.value=number(taxableTotal);vatTotalField.value=number(vatTotal);if(totalField)totalField.value=number(total)};
  const loadDetails=async workId=>{
    const response=await fetch(`/Lavori/Schede?handler=InvoiceCandidate&id=${encodeURIComponent(workId)}`,{headers:{Accept:"application/json"}});if(!response.ok)throw new Error();const detail=await response.json();
    activity.value=detail.activity||"";activity.scrollTop=0;amount.value=window.SkyLabDecimal?.format(detail.serviceTotal,2)??number(detail.serviceTotal);
    if(!linesBody)return;linesBody.replaceChildren();detail.materials.forEach(material=>{const row=document.createElement("tr");row.dataset.articleCode=material.code;const vatRate=material.vatRate??standardRate;row.innerHTML=`<td>${material.code||""}</td><td>${material.description||""}</td><td>${material.unit||""}</td><td>${number(material.quantity,3)}</td><td>${number(material.unitPrice,2)}</td><td>${number(0,2)}</td><td>${number(material.amount,2)}</td><td>${number(vatRate,2)}%</td><td hidden data-article-state>1</td><td hidden data-vat-code>${material.vatCode||""}</td>`;linesBody.append(row)});recalculate();
  };
  button.addEventListener("click",async()=>{
    const item=await window.SkyLabWorkSheetZoom?.open({customer:Number.parseInt(customerCode?.value||"0",10)||0});if(!item)return;
    id.value=String(item.id);display.value=`Numero ${String(item.code).padStart(6,"0")}/${item.year}  del ${item.completedOnText}`;print.value="true";viewButton.disabled=false;
    if(customerCode)customerCode.value=String(item.customerId).padStart(5,"0");if(customerName)customerName.value=item.customer||"";
    try{await loadDetails(item.id)}catch{activity.value=item.workPerformed||"";amount.value=window.SkyLabDecimal?.format(item.requestedAmount,2)??String(item.requestedAmount).replace(".",",")}
  });
  viewButton.addEventListener("click", () => {
    if (viewButton.disabled || !(Number(id.value) > 0)) return;
    let viewer = document.querySelector("[data-sales-invoice-work-viewer]");
    if (!viewer) {
      viewer = document.createElement("dialog");
      viewer.dataset.salesInvoiceWorkViewer = "";
      viewer.setAttribute("aria-label", "Scheda lavoro");
      viewer.style.cssText = "position:fixed;inset:0;width:100vw;max-width:none;height:100dvh;max-height:none;margin:0;padding:0;border:0;background:white";
      const frame = document.createElement("iframe");
      frame.title = "Scheda lavoro";
      frame.style.cssText = "display:block;width:100%;height:100%;border:0";
      const close = () => { viewer.close(); viewButton.focus(); };
      frame.addEventListener("load", () => {
        const doc = frame.contentDocument;
        if (!doc) return;
        doc.querySelectorAll("input, select, textarea").forEach(control => {
          control.disabled = true;
          control.tabIndex = -1;
        });
        doc.querySelectorAll("button").forEach(control => {
          if (control.matches("[data-work-tab]")) return;
          control.disabled = true;
          control.tabIndex = -1;
        });
        doc.addEventListener("submit", event => {
          event.preventDefault();
          event.stopImmediatePropagation();
        }, true);
        doc.addEventListener("click", event => {
          const exit = event.target.closest(".customer-form-toolbar a, .skylab-header-brand");
          if (!exit) return;
          event.preventDefault();
          event.stopImmediatePropagation();
          close();
        }, true);
        doc.addEventListener("keydown", event => {
          if (event.key !== "Escape" || doc.querySelector("dialog[open]")) return;
          event.preventDefault();
          event.stopImmediatePropagation();
          close();
        }, true);
      });
      viewer.append(frame);
      document.body.append(viewer);
      viewer.addEventListener("cancel", event => { event.preventDefault(); close(); });
    }
    viewer.querySelector("iframe").src = `/Lavori/Scheda?id=${encodeURIComponent(id.value)}&azione=101`;
    viewer.showModal();
  });
  amount.addEventListener("input",recalculate);amount.addEventListener("change",recalculate);workVat?.addEventListener("input",recalculate);workVat?.addEventListener("change",recalculate);workVat?.addEventListener("blur",()=>{const value=Math.min(100,Math.max(0,parse(workVat.value)));setTimeout(()=>{workVat.value=`${number(value)} %`;recalculate()},0)});linesBody?.addEventListener("input",recalculate);linesBody?.addEventListener("change",recalculate);if(linesBody)new MutationObserver(()=>recalculate()).observe(linesBody,{subtree:true,childList:true,characterData:true});window.SkyLabSalesInvoiceTotals={recalculate};recalculate();
  const updateViewButton = () => { viewButton.disabled = !(Number(id.value) > 0); };
  id.addEventListener("change", updateViewButton);
  window.SkyLabSalesInvoiceTotals.recalculate = (...args) => {
    updateViewButton();
    return recalculate(...args);
  };
  updateViewButton();
})();
