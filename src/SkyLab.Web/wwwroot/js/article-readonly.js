document.addEventListener("DOMContentLoaded",()=>{
 const page=document.querySelector('[data-article-readonly="true"]');if(!page)return;
 page.querySelectorAll('form input:not([type="hidden"]),form textarea').forEach(control=>{control.readOnly=true;control.tabIndex=-1});
 page.querySelectorAll('form select,form button').forEach(control=>{control.disabled=true;control.tabIndex=-1});
 page.querySelectorAll('[data-barcode-new],[data-barcode-edit],[data-barcode-delete],[data-article-photo-add],[data-article-photo-delete]').forEach(control=>control.hidden=true);
});
