(function () {
 'use strict';
 var input = document.getElementById('glc-lot');
 if (!input) return;
 var lang = document.documentElement.lang;
 var en = lang.indexOf('en') === 0, ge = lang.indexOf('ka') === 0;
 function text(ru, english, geo) { return en ? english : ge ? geo : ru; }
 var mode = 'vehicle';
 var feedback = document.getElementById('glc-calc-feedback');
 var modes = { vehicle: text('Расчёт автомобиля', 'Vehicle estimate', 'ავტომობილის შეფასება'), selection: text('Подбор под бюджет', 'Budget selection', 'ბიუჯეტით შერჩევა'), delivery: text('Расчёт доставки', 'Delivery estimate', 'მიწოდების შეფასება') };
 document.querySelectorAll('[data-enquiry-mode]').forEach(function (button) {
  button.addEventListener('click', function () {
   mode = button.getAttribute('data-enquiry-mode');
   document.querySelectorAll('[data-enquiry-mode]').forEach(function (item) { var active = item === button; item.classList.toggle('is-active', active); item.setAttribute('aria-pressed', String(active)); });
   input.placeholder = mode === 'selection' ? text('Бюджет и пожелания к автомобилю', 'Budget and vehicle preferences', 'ბიუჯეტი და სასურველი ავტომობილი') : text('Ссылка Copart, IAAI, Manheim или VIN', 'Copart, IAAI, Manheim URL or VIN', 'Copart, IAAI, Manheim ბმული ან VIN');
   input.removeAttribute('aria-invalid');
   feedback.textContent = text('Стоимость уточнит менеджер после проверки лота и маршрута.', 'A manager confirms pricing after reviewing your lot and route.', 'მენეჯერი ფასს ლოტისა და მარშრუტის შემოწმების შემდეგ დააზუსტებს.');
  });
 });
 document.querySelectorAll('[data-lot-example]').forEach(function (button) { button.addEventListener('click', function () { document.querySelector('[data-enquiry-mode="vehicle"]').click(); input.value = button.getAttribute('data-lot-example'); input.focus(); }); });
 function requestEstimate() {
  var value = input.value.trim(), valid = mode === 'selection' ? value.length >= 3 : /^[A-HJ-NPR-Z0-9]{17}$/i.test(value);
  if (!valid && mode !== 'selection') {
   try { var url = new URL(value); valid = url.protocol === 'https:' && /(^|\.)(copart\.com|copart\.ca|iaai\.com|manheim\.com)$/i.test(url.hostname) && url.pathname.length > 1; } catch (_) { valid = false; }
  }
  if (!valid) { input.setAttribute('aria-invalid', 'true'); feedback.textContent = mode === 'selection' ? text('Укажите бюджет и пожелания.', 'Enter your budget and preferences.', 'მიუთითეთ ბიუჯეტი და სურვილები.') : text('Введите VIN из 17 символов или HTTPS-ссылку на лот Copart, IAAI или Manheim.', 'Enter a 17-character VIN or an HTTPS lot URL from Copart, IAAI or Manheim.', 'შეიყვანეთ 17-სიმბოლოიანი VIN ან Copart, IAAI, Manheim-ის HTTPS ბმული.'); input.focus(); return; }
  input.removeAttribute('aria-invalid');
  var comment = document.querySelector('[id$="HomeLeadCommentTextBox"]');
  var direction = document.querySelector('[id$="HomeLeadDirectionTextBox"]');
  var reference = '[' + modes[mode] + '] ' + value;
  if (comment && comment.value.indexOf(reference) === -1) comment.value = reference + (comment.value ? '\n' + comment.value : '');
  if (direction && !direction.value) direction.value = 'USA → Georgia';
  feedback.textContent = text('Лот добавлен в заявку. Оставьте контакты для персонального расчёта.', 'Added to your enquiry. Leave your contact details for a personalised estimate.', 'დაემატა მოთხოვნას. დატოვეთ საკონტაქტო ინფორმაცია შეფასებისთვის.');
  document.getElementById('contacts').scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
  var name = document.querySelector('[id$="HomeLeadNameTextBox"]');
  if (name) name.focus({ preventScroll: true });
 }
 document.getElementById('glc-calculate').addEventListener('click', requestEstimate);
 input.addEventListener('keydown', function (event) { if (event.key === 'Enter') { event.preventDefault(); requestEstimate(); } });
}());

// Collapse the mobile navigation after choosing an in-page section.
(function () {
 var nav = document.getElementById('siteNavigation');
 if (!nav) return;
 nav.querySelectorAll('a').forEach(function (link) {
  link.addEventListener('click', function () {
   if (link.hash && link.pathname === location.pathname && window.jQuery && window.jQuery.fn.collapse) window.jQuery(nav).collapse('hide');
  });
 });
}());
