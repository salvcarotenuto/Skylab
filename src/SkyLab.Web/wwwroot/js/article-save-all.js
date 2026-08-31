document.addEventListener("DOMContentLoaded",()=>{
 const target="article-save-all-form";
 document.querySelectorAll("#article-form input,#article-form select,#article-form textarea,#price-list-form input").forEach(control=>{
  if(control.name!=="__RequestVerificationToken"&&control.name!=="codice")control.setAttribute("form",target);
 });
});
