document.addEventListener("DOMContentLoaded",()=>{
 const code=document.querySelector("[data-article-supplier-code]"),name=document.querySelector("[data-article-supplier-name]"),dialog=document.querySelector('[data-party-lookup="F"]');if(!code||!name||!dialog)return;
 const source=JSON.parse(dialog.querySelector("[data-party-source]")?.textContent||"[]"),find=()=>source.find(x=>Number(x.code)===Number(code.value));
 const refresh=()=>name.value=find()?.name||"";code.addEventListener("input",()=>{code.value=code.value.replace(/\D/g,"");refresh()});code.addEventListener("blur",()=>{const row=find();refresh();if(row)code.value=String(row.code).padStart(5,"0")});
 dialog.addEventListener("skylab:party-selected",event=>{if(event.detail.target&&event.detail.target!=="article")return;code.value=String(event.detail.code).padStart(5,"0");name.value=event.detail.name;code.dispatchEvent(new Event("change",{bubbles:true}));code.focus();});
});
