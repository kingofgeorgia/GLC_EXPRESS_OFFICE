<%@ Control Language="C#" Inherits="GLC_EXPRESS.HomeSection" %>
<div class="landing-transport-grid glc-vehicle-grid">
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/sedan.png") %>" alt="Toyota Camry" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoSedanTitle") %></h3><p><%: T("CargoSedanBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Toyota Camry</strong></li><li>Honda Accord</li><li>BMW 3 Series</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/suv.png") %>" alt="BMW X5" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoSuvTitle") %></h3><p><%: T("CargoSuvBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>BMW X5</strong></li><li>Toyota Highlander</li><li>Chevrolet Tahoe</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/pickup.png") %>" alt="Ford F-150" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoPickupTitle") %></h3><p><%: T("CargoPickupBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Ford F-150</strong></li><li>RAM 1500</li><li>Chevrolet Silverado</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/van.png") %>" alt="Mercedes-Benz Sprinter" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoVanTitle") %></h3><p><%: T("CargoVanBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Mercedes-Benz Sprinter</strong></li><li>Ford Transit</li><li>RAM ProMaster</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/motorcycle.png") %>" alt="Harley-Davidson Fat Boy" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoMotorcycleTitle") %></h3><p><%: T("CargoMotorcycleBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Harley-Davidson Fat Boy</strong></li><li>BMW R 1250 GS</li><li>Indian Scout</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/exclusive.png") %>" alt="Porsche 911" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoExclusiveTitle") %></h3><p><%: T("CargoExclusiveBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Porsche 911</strong></li><li>Chevrolet Corvette</li><li>Ford Mustang Fastback 1967</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/boat.png") %>" alt="Bayliner VR5" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoBoatsTitle") %></h3><p><%: T("CargoBoatsBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Bayliner VR5</strong></li><li>Sea Ray SPX 190</li><li>Yamaha AR190</li></ul>
        </div>
    </article>
    <article class="landing-transport-card glc-vehicle-card">
        <div class="glc-vehicle-art"><img src="<%= ResolveUrl("~/Content/images/categories/buggy.png") %>" alt="Polaris RZR" width="640" height="420" loading="lazy" decoding="async" /></div>
        <div class="glc-vehicle-copy"><h3><%: T("CargoBuggyTitle") %></h3><p><%: T("CargoBuggyBody") %></p>
            <ul class="glc-vehicle-models" aria-label="<%: L("Примеры моделей", "Example models", "მოდელების მაგალითები") %>"><li><strong>Polaris RZR</strong></li><li>Can-Am Maverick X3</li><li>Yamaha YXZ1000R</li></ul>
        </div>
    </article>
</div>
