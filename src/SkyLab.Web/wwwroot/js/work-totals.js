document.addEventListener("DOMContentLoaded",()=>{
 const labour=document.querySelector("[data-work-actual-labour]"),materials=document.querySelector("[data-work-actual-materials]"),total=document.querySelector("[data-work-actual-total]");
 if(!labour||!materials||!total)return;
 const number=value=>{const normalized=value.replace(/\./g,"").replace(",",".");return Number(normalized)||0};
 const update=()=>total.value=(number(labour.value)+number(materials.value)).toLocaleString("it-IT",{minimumFractionDigits:2,maximumFractionDigits:2});
 labour.addEventListener("input",update);materials.addEventListener("input",update);labour.addEventListener("change",update);materials.addEventListener("change",update);
});
