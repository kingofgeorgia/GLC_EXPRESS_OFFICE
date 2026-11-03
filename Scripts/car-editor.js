(function(){
 function init(){
  var forwarder=document.querySelector('[data-car-forwarder]'), input=document.querySelector('[data-new-forwarder]');
  if(forwarder&&input){function toggle(){input.hidden=forwarder.value!=='__new__';input.disabled=input.hidden;}forwarder.addEventListener('change',toggle);toggle();}
  var brand=document.querySelector('[list="CarBrands"]'), model=document.querySelector('[list="CarModels"]'), data=document.getElementById('CarModelsData'), list=document.getElementById('CarModels');
  if(brand&&model&&data&&list){var models=JSON.parse(data.textContent);function update(){list.textContent='';var key=Object.keys(models).find(function(k){return k.toLowerCase()===brand.value.trim().toLowerCase();});(models[key]||[]).forEach(function(value){var option=document.createElement('option');option.value=value;list.appendChild(option);});}brand.addEventListener('input',update);brand.addEventListener('change',update);update();}
 }
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
