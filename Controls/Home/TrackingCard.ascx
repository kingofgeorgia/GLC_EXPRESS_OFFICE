<%@ Control Language="C#" Inherits="GLC_EXPRESS.HomeSection" %>
<aside class="glc-tracker" aria-labelledby="glc-tracker-title">
 <div class="glc-tracker-top"><span>GLC TRACKING</span><span class="glc-demo"><%: L("Демо", "Demo", "დემო") %></span></div>
 <div class="glc-car-icon"><img src="<%= ResolveUrl(Tracking.ImageUrl) %>" alt="<%: Tracking.Vehicle %>" width="270" height="120" /></div>
 <h2 id="glc-tracker-title"><%: Tracking.Vehicle %></h2><div class="glc-lot-badges"><strong><%: Tracking.Auction %></strong><span>Lot #<%: Tracking.Lot %></span></div>
 <ol class="glc-tracking-steps">
 <% foreach (var step in Tracking.Steps) { %>
 <li class="<%: step.State %>"><i aria-hidden="true"><%= Icon(step.Icon) %></i><div><strong><%: L(step.TitleRu, step.TitleEn, step.TitleGe) %></strong><small><%: L(step.DetailRu, step.DetailEn, step.DetailGe) %></small></div><span class="glc-track-date"><%: step.DateLabel %></span></li>
 <% } %>
 </ol>
 <a href="<%= ResolveUrl("~/orders") %>" class="glc-track-link"><%: L("Отслеживать мой автомобиль", "Track my vehicle", "ჩემი ავტომობილის თვალთვალი") %> →</a>
 <p class="glc-demo-note"><%: L("Пример маршрута. Ваши данные — в аккаунте.", "Sample route. Your data is in your account.", "მარშრუტის ნიმუში. თქვენი მონაცემები ანგარიშშია.") %></p>
</aside>



