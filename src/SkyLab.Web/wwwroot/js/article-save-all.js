document.addEventListener("DOMContentLoaded",()=>{
 const target="article-save-all-form";
 document.querySelectorAll("#article-form input,#article-form select,#article-form textarea,#price-list-form input").forEach(control=>{
  if(control.name!=="__RequestVerificationToken"&&control.name!=="codice")control.setAttribute("form",target);
 });
 const form=document.getElementById(target),save=document.querySelector("[data-article-save]");
 if(!form||!save)return;
 form.addEventListener("submit",async event=>{
  if(event.defaultPrevented)return;
  event.preventDefault();
  if(!form.reportValidity())return;
  const progress=window.parent!==window&&window.parent.SkyProg?window.parent.SkyProg:window.SkyProg;
  save.disabled=true;
  progress?.Show();
  let response,html="",error="";
  try{
   response=await fetch(form.action||location.href,{method:"POST",body:new FormData(form),credentials:"same-origin",redirect:"follow"});
   html=await response.text();
   if(!response.ok)error="Salvataggio non riuscito.";
  }catch{
   error="Impossibile completare il salvataggio.";
  }finally{
   const complete=()=>{
    save.disabled=false;
    if(error){window.SkyLabMessageBox?.show({title:"SkyLab - attenzione",message:error,variant:"error"});return;}
    if(response?.redirected){location.href=response.url;return;}
    document.open();document.write(html);document.close();
   };
   if(progress)progress.Close(complete);else complete();
  }
 });

 document.querySelectorAll("form[data-article-progress-save]").forEach(childForm=>{
  childForm.addEventListener("submit",async event=>{
   if(event.defaultPrevented)return;
   event.preventDefault();
   if(!childForm.reportValidity())return;
   const button=event.submitter||childForm.querySelector('[type="submit"]');
   const progress=window.parent!==window&&window.parent.SkyProg?window.parent.SkyProg:window.SkyProg;
   if(button)button.disabled=true;
   progress?.Show();
   let response,html="",error="";
   try{
    response=await fetch(childForm.action||location.href,{method:"POST",body:new FormData(childForm),credentials:"same-origin",redirect:"follow"});
    html=await response.text();
    if(!response.ok)error="Salvataggio non riuscito.";
   }catch{
    error="Impossibile completare il salvataggio.";
   }finally{
    const complete=()=>{
     if(button)button.disabled=false;
     if(error){window.SkyLabMessageBox?.show({title:"SkyLab - attenzione",message:error,variant:"error"});return;}
     if(response?.redirected){location.href=response.url;return;}
     document.open();document.write(html);document.close();
    };
    if(progress)progress.Close(complete);else complete();
   }
  });
 });
});
