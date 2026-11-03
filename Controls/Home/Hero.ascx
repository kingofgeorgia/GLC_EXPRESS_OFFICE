<%@ Control Language="C#" Inherits="GLC_EXPRESS.HomeSection" %>
<%@ Register Src="Calculator.ascx" TagPrefix="glc" TagName="Calculator" %>
<%@ Register Src="TrackingCard.ascx" TagPrefix="glc" TagName="TrackingCard" %>
<section class="glc-hero" id="home">
 <div class="glc-hero-image" role="img" aria-label="<%: L("Автовоз с автомобилями в морском порту", "Car transporter at a seaport", "ავტოვოზი საზღვაო პორტში") %>"></div>
 <div class="landing-shell glc-hero-grid"><div class="glc-hero-copy">
 <p class="glc-eyebrow">USA • CANADA AUTO AUCTIONS</p>
 <h1><%: L("Автомобили", "Cars from the USA", "ავტომობილები აშშ-დან") %><br/><%: L("из США под ключ", "Delivered. Turnkey.", "სრული სერვისით") %></h1>
 <p class="glc-hero-lead"><%: L("Подбор, покупка, доставка, таможенное оформление, ремонт и отправка по Грузии и странам СНГ.", "Selection, purchase, shipping, customs and repair. Delivered across Georgia and the CIS.", "შერჩევა, შეძენა, მიწოდება, განბაჟება, შეკეთება და გაგზავნა საქართველოსა და დსთ-ში.") %></p>
 <div class="glc-benefits"><div><b aria-hidden="true"><%= Icon("auction") %></b><span>Copart<br/>IAAI · Manheim</span></div><div><b aria-hidden="true"><%= Icon("ship") %></b><span><%: L("Доставка через", "Shipping via", "მიწოდება") %><br/><%: L("Поти и Батуми", "Poti & Batumi", "ფოთი და ბათუმი") %></span></div><div><b aria-hidden="true"><%= Icon("repair") %></b><span><%: L("Восстановление", "Repair at our", "აღდგენა ჩვენს") %><br/><%: L("в своём техцентре", "own workshop", "სერვისცენტრში") %></span></div><div><b aria-hidden="true"><%= Icon("document") %></b><span><%: L("Документы", "Documents", "დოკუმენტები") %><br/><%: L("и страхование", "& insurance", "და დაზღვევა") %></span></div></div>
 <glc:Calculator runat="server" />
 </div><glc:TrackingCard runat="server" /></div>
</section>

