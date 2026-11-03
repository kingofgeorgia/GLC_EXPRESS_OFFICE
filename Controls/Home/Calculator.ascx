<%@ Control Language="C#" Inherits="GLC_EXPRESS.HomeSection" %>
<div class="glc-calculator" id="calculator" aria-label="<%: L("Расчёт автомобиля", "Vehicle enquiry", "ავტომობილის გაანგარიშება") %>">
 <div class="glc-calc-tabs" role="group" aria-label="<%: L("Тип заявки", "Enquiry type", "მოთხოვნის ტიპი") %>">
  <button type="button" class="is-active" data-enquiry-mode="vehicle" aria-pressed="true"><%: L("Рассчитать автомобиль", "Vehicle estimate", "ავტომობილის შეფასება") %></button>
  <button type="button" data-enquiry-mode="selection" aria-pressed="false"><%: L("Подобрать под бюджет", "Find within budget", "ბიუჯეტით შერჩევა") %></button>
  <button type="button" data-enquiry-mode="delivery" aria-pressed="false"><%: L("Рассчитать доставку", "Delivery estimate", "მიწოდების შეფასება") %></button>
 </div>
 <label class="sr-only" for="glc-lot"><%: L("VIN или ссылка на лот", "VIN or lot URL", "VIN ან ლოტის ბმული") %></label>
 <div class="glc-calc-input"><span aria-hidden="true">↗</span><input id="glc-lot" type="text" maxlength="500" placeholder="<%: L("Ссылка Copart, IAAI, Manheim или VIN", "Copart, IAAI, Manheim URL or VIN", "Copart, IAAI, Manheim ბმული ან VIN") %>" aria-describedby="glc-calc-feedback" /><button type="button" id="glc-calculate"><%: L("Рассчитать", "Get estimate", "შეფასება") %> →</button></div>
 <div class="glc-calc-examples"><span><%: L("Например:", "Try:", "მაგალითი:") %></span><button type="button" data-lot-example="https://www.copart.com/lot/44189717">copart.com/lot/44189717</button><button type="button" data-lot-example="7SAYGAEE2RF009485">7SAYGAEE2RF009485</button></div>
 <p id="glc-calc-feedback" role="status" aria-live="polite"><%: L("Стоимость уточнит менеджер после проверки лота и маршрута.", "A manager confirms pricing after reviewing your lot and route.", "მენეჯერი ფასს ლოტისა და მარშრუტის შემოწმების შემდეგ დააზუსტებს.") %></p>
</div>
