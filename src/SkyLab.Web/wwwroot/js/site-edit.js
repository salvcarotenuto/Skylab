(() => {
    const city = document.getElementById("Sede_City");
    const postalCode = document.getElementById("Sede_PostalCode");
    const province = document.getElementById("Sede_Province");
    const list = document.getElementById("site-city-suggestions");
    if (!city || !postalCode || !province || !list) return;
    let timer, selected = -1, requestId = 0;
    const close = () => { list.hidden = true; list.replaceChildren(); selected = -1; };
    const choose = item => { city.value=item.name||"";postalCode.value=item.postalCode||"";province.value=(item.province||"").toUpperCase();close();postalCode.focus(); };
    const highlight = index => {const buttons=[...list.querySelectorAll("button")];if(!buttons.length)return;selected=(index+buttons.length)%buttons.length;buttons.forEach((button,i)=>button.classList.toggle("selected",i===selected));buttons[selected].scrollIntoView({block:"nearest"});};
    const render = items => {list.replaceChildren();selected=-1;for(const item of items){const button=document.createElement("button");button.type="button";button.setAttribute("role","option");const name=document.createElement("strong");name.textContent=item.name||"";const detail=document.createElement("span");detail.textContent=[item.postalCode,item.province].filter(Boolean).join(" · ");button.append(name,detail);button.addEventListener("mousedown",event=>{event.preventDefault();choose(item);});list.append(button);}list.hidden=items.length===0;};
    const search = async () => {const term=city.value.trim();if(term.length<2){close();return;}const current=++requestId;try{const response=await fetch(`${location.pathname}?handler=Cities&q=${encodeURIComponent(term)}`,{headers:{Accept:"application/json"}});if(!response.ok||current!==requestId)return;render(await response.json());}catch{if(current===requestId)close();}};
    city.addEventListener("input",()=>{clearTimeout(timer);timer=setTimeout(search,180);});
    city.addEventListener("keydown",event=>{const buttons=list.querySelectorAll("button");if(event.key==="ArrowDown"&&buttons.length){event.preventDefault();highlight(selected+1);}else if(event.key==="ArrowUp"&&buttons.length){event.preventDefault();highlight(selected-1);}else if(event.key==="Enter"&&selected>=0){event.preventDefault();buttons[selected].dispatchEvent(new MouseEvent("mousedown"));}else if(event.key==="Escape")close();});
    city.addEventListener("blur",()=>setTimeout(close,120));
})();
