document.addEventListener("DOMContentLoaded",()=>{
 const dialog=document.querySelector("[data-detail-dialog]");if(!dialog)return;
 const row=dialog.querySelector("[data-detail-row]"),scope=dialog.querySelector("[data-detail-scope-input]"),reference=dialog.querySelector("[data-detail-reference]"),description=dialog.querySelector("[data-detail-description]"),quantity=dialog.querySelector("[data-detail-quantity]"),price=dialog.querySelector("[data-detail-price]");
 document.querySelectorAll("[data-detail-edit]").forEach(button=>button.addEventListener("click",()=>{row.value=button.dataset.row;scope.value=button.dataset.detailScope||"P";reference.textContent=button.dataset.reference;description.textContent=button.dataset.description;quantity.value=Number(button.dataset.quantity).toLocaleString("it-IT",{minimumFractionDigits:3,maximumFractionDigits:3});price.value=Number(button.dataset.price).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2});dialog.showModal();requestAnimationFrame(()=>quantity.focus())}));
 dialog.querySelector("[data-detail-cancel]").addEventListener("click",()=>dialog.close());
 dialog.addEventListener("cancel",event=>{event.preventDefault();dialog.close()});
 const deleteForm=document.querySelector("[data-detail-delete-form]");
 document.querySelectorAll("[data-detail-delete]").forEach(button=>button.addEventListener("click",()=>{if(!window.confirm(`Eliminare la riga “${button.dataset.description}”?`))return;deleteForm.querySelector("[data-detail-delete-row]").value=button.dataset.row;deleteForm.querySelector("[data-detail-delete-scope]").value=button.dataset.detailScope||"P";deleteForm.submit()}));
});
