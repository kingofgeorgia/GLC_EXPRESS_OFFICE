<%@ Page Title="GLC Express | Аукционы и доставка авто из США" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GLC_EXPRESS._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="HomeLeadAlertPanel" runat="server" Visible="false" CssClass="alert landing-alert">
        <asp:Literal ID="HomeLeadAlertLiteral" runat="server" />
    </asp:Panel>

    <div class="landing-page">
        <section class="landing-hero" id="home">
            <div class="landing-shell landing-hero-grid">
                <div class="landing-hero-copy">
                    <span class="landing-eyebrow"><%: T("HomeHeroEyebrow") %></span>
                    <h1><%: T("HomeHeroTitle") %></h1>
                    <p class="landing-lead"><%: T("HomeHeroLead") %></p>
                    <div class="landing-hero-actions">
                        <a class="btn btn-primary btn-lg" href="#turnkey-order"><%: T("HomeHeroPrimaryCta") %></a>
                        <a class="btn btn-default btn-lg" href="#contacts"><%: T("HomeHeroSecondaryCta") %></a>
                    </div>
                    <div class="landing-stat-grid">
                        <div class="landing-stat-card">
                            <strong><%: T("HomeHeroStatYearsValue") %></strong>
                            <span><%: T("HomeHeroStatYearsLabel") %></span>
                        </div>
                        <div class="landing-stat-card">
                            <strong><%: T("HomeHeroStatDeliveryValue") %></strong>
                            <span><%: T("HomeHeroStatDeliveryLabel") %></span>
                        </div>
                        <div class="landing-stat-card">
                            <strong><%: T("HomeHeroStatContainerValue") %></strong>
                            <span><%: T("HomeHeroStatContainerLabel") %></span>
                        </div>
                        <div class="landing-stat-card">
                            <strong><%: T("HomeHeroStatSupportValue") %></strong>
                            <span><%: T("HomeHeroStatSupportLabel") %></span>
                        </div>
                    </div>
                </div>
                <div class="landing-hero-panel">
                    <div class="landing-hero-panel-inner">
                        <h2><%: T("HomeHeroPanelTitle") %></h2>
                        <ul class="landing-check-list">
                            <li><%: T("HomeHeroCheckBudget") %></li>
                            <li><%: T("HomeHeroCheckHistory") %></li>
                            <li><%: T("HomeHeroCheckBidding") %></li>
                            <li><%: T("HomeHeroCheckShipping") %></li>
                            <li><%: T("HomeHeroCheckRepair") %></li>
                        </ul>
                        <div class="landing-route-card">
                            <span class="landing-route-label"><%: T("HomeHeroRouteLabel") %></span>
                            <strong><%: T("HomeHeroRouteTitle") %></strong>
                            <p><%: T("HomeHeroRouteDescription") %></p>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <section class="landing-section" id="about">
            <div class="landing-shell">
                <div class="landing-section-heading">
                    <span class="landing-section-kicker"><%: T("AboutKicker") %></span>
                    <h2><%: T("AboutTitle") %></h2>
                    <p><%: T("AboutLead") %></p>
                </div>
                <div class="landing-feature-grid landing-feature-grid-wide">
                    <article class="landing-feature-card">
                        <h3><%: T("AboutFeatureAnalyticsTitle") %></h3>
                        <p><%: T("AboutFeatureAnalyticsBody") %></p>
                    </article>
                    <article class="landing-feature-card">
                        <h3><%: T("AboutFeatureDocumentsTitle") %></h3>
                        <p><%: T("AboutFeatureDocumentsBody") %></p>
                    </article>
                    <article class="landing-feature-card">
                        <h3><%: T("AboutFeatureInvestmentTitle") %></h3>
                        <p><%: T("AboutFeatureInvestmentBody") %></p>
                    </article>
                </div>
            </div>
        </section>

        <section class="landing-section landing-section-alt" id="cargo-types">
            <div class="landing-shell">
                <div class="landing-section-heading compact">
                    <span class="landing-section-kicker"><%: T("CargoKicker") %></span>
                    <h2><%: T("CargoTitle") %></h2>
                </div>
                <div class="landing-transport-grid">
                    <div class="landing-transport-card"><strong><%: T("CargoSedanTitle") %></strong><span><%: T("CargoSedanBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoSuvTitle") %></strong><span><%: T("CargoSuvBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoPickupTitle") %></strong><span><%: T("CargoPickupBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoVanTitle") %></strong><span><%: T("CargoVanBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoMotorcycleTitle") %></strong><span><%: T("CargoMotorcycleBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoExclusiveTitle") %></strong><span><%: T("CargoExclusiveBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoBoatsTitle") %></strong><span><%: T("CargoBoatsBody") %></span></div>
                    <div class="landing-transport-card"><strong><%: T("CargoBuggyTitle") %></strong><span><%: T("CargoBuggyBody") %></span></div>
                </div>
            </div>
        </section>

        <section class="landing-section" id="advantages">
            <div class="landing-shell">
                <div class="landing-section-heading compact">
                    <span class="landing-section-kicker"><%: T("AdvantagesKicker") %></span>
                    <h2><%: T("AdvantagesTitle") %></h2>
                </div>
                <div class="landing-advantage-grid">
                    <div class="landing-advantage-card"><strong><%: T("AdvantageYearsTitle") %></strong><span><%: T("AdvantageYearsBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageTransparentTitle") %></strong><span><%: T("AdvantageTransparentBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageBiddingTitle") %></strong><span><%: T("AdvantageBiddingBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageInspectionTitle") %></strong><span><%: T("AdvantageInspectionBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageLogisticsTitle") %></strong><span><%: T("AdvantageLogisticsBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageTrackingTitle") %></strong><span><%: T("AdvantageTrackingBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageInsuranceTitle") %></strong><span><%: T("AdvantageInsuranceBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageManagerTitle") %></strong><span><%: T("AdvantageManagerBody") %></span></div>
                    <div class="landing-advantage-card"><strong><%: T("AdvantageSupportTitle") %></strong><span><%: T("AdvantageSupportBody") %></span></div>
                </div>
            </div>
        </section>

        <section class="landing-section landing-section-dark" id="turnkey-order">
            <div class="landing-shell">
                <div class="landing-section-heading light">
                    <span class="landing-section-kicker"><%: T("TurnkeyKicker") %></span>
                    <h2><%: T("TurnkeyTitle") %></h2>
                    <p><%: T("TurnkeyLead") %></p>
                </div>
                <div class="landing-process-grid">
                    <div class="landing-process-step"><span>1</span><strong><%: T("TurnkeyStep1Title") %></strong><p><%: T("TurnkeyStep1Body") %></p></div>
                    <div class="landing-process-step"><span>2</span><strong><%: T("TurnkeyStep2Title") %></strong><p><%: T("TurnkeyStep2Body") %></p></div>
                    <div class="landing-process-step"><span>3</span><strong><%: T("TurnkeyStep3Title") %></strong><p><%: T("TurnkeyStep3Body") %></p></div>
                    <div class="landing-process-step"><span>4</span><strong><%: T("TurnkeyStep4Title") %></strong><p><%: T("TurnkeyStep4Body") %></p></div>
                    <div class="landing-process-step"><span>5</span><strong><%: T("TurnkeyStep5Title") %></strong><p><%: T("TurnkeyStep5Body") %></p></div>
                    <div class="landing-process-step"><span>6</span><strong><%: T("TurnkeyStep6Title") %></strong><p><%: T("TurnkeyStep6Body") %></p></div>
                    <div class="landing-process-step"><span>7</span><strong><%: T("TurnkeyStep7Title") %></strong><p><%: T("TurnkeyStep7Body") %></p></div>
                    <div class="landing-process-step"><span>8</span><strong><%: T("TurnkeyStep8Title") %></strong><p><%: T("TurnkeyStep8Body") %></p></div>
                    <div class="landing-process-step"><span>9</span><strong><%: T("TurnkeyStep9Title") %></strong><p><%: T("TurnkeyStep9Body") %></p></div>
                    <div class="landing-process-step"><span>10</span><strong><%: T("TurnkeyStep10Title") %></strong><p><%: T("TurnkeyStep10Body") %></p></div>
                </div>
            </div>
        </section>

        <section class="landing-section" id="auction-center">
            <div class="landing-shell landing-split-grid">
                <div>
                    <div class="landing-section-heading compact align-left">
                        <span class="landing-section-kicker"><%: T("AuctionKicker") %></span>
                        <h2><%: T("AuctionTitle") %></h2>
                    </div>
                    <p class="landing-section-copy"><%: T("AuctionLead") %></p>
                    <ul class="landing-check-list dark">
                        <li><%: T("AuctionCheck1") %></li>
                        <li><%: T("AuctionCheck2") %></li>
                        <li><%: T("AuctionCheck3") %></li>
                        <li><%: T("AuctionCheck4") %></li>
                        <li><%: T("AuctionCheck5") %></li>
                    </ul>
                </div>
                <div class="landing-info-board">
                    <div class="landing-info-board-card">
                        <span class="landing-info-label"><%: T("AuctionInfoPlatformsLabel") %></span>
                        <strong><%: T("AuctionInfoPlatformsValue") %></strong>
                    </div>
                    <div class="landing-info-board-card">
                        <span class="landing-info-label"><%: T("AuctionInfoFocusLabel") %></span>
                        <strong><%: T("AuctionInfoFocusValue") %></strong>
                    </div>
                    <div class="landing-info-board-card">
                        <span class="landing-info-label"><%: T("AuctionInfoResultLabel") %></span>
                        <strong><%: T("AuctionInfoResultValue") %></strong>
                    </div>
                </div>
            </div>
        </section>

        <section class="landing-section landing-section-alt" id="delivery">
            <div class="landing-shell">
                <div class="landing-section-heading compact">
                    <span class="landing-section-kicker"><%: T("DeliveryKicker") %></span>
                    <h2><%: T("DeliveryTitle") %></h2>
                </div>
                <div class="landing-delivery-grid">
                    <article class="landing-delivery-card">
                        <h3><%: T("DeliveryStep1Title") %></h3>
                        <p><%: T("DeliveryStep1Body") %></p>
                    </article>
                    <article class="landing-delivery-card">
                        <h3><%: T("DeliveryStep2Title") %></h3>
                        <p><%: T("DeliveryStep2Body") %></p>
                    </article>
                    <article class="landing-delivery-card">
                        <h3><%: T("DeliveryStep3Title") %></h3>
                        <p><%: T("DeliveryStep3Body") %></p>
                    </article>
                </div>
                <div class="landing-route-strip">
                    <div><strong><%: T("DeliveryStripPortsLabel") %></strong><span><%: T("DeliveryStripPortsValue") %></span></div>
                    <div><strong><%: T("DeliveryStripFormatLabel") %></strong><span><%: T("DeliveryStripFormatValue") %></span></div>
                    <div><strong><%: T("DeliveryStripControlLabel") %></strong><span><%: T("DeliveryStripControlValue") %></span></div>
                </div>
            </div>
        </section>

        <section class="landing-section" id="repair">
            <div class="landing-shell landing-split-grid">
                <div>
                    <div class="landing-section-heading compact align-left">
                        <span class="landing-section-kicker"><%: T("RepairKicker") %></span>
                        <h2><%: T("RepairTitle") %></h2>
                    </div>
                    <p class="landing-section-copy"><%: T("RepairLead") %></p>
                </div>
                <div class="landing-tag-grid">
                    <span><%: T("RepairTag1") %></span>
                    <span><%: T("RepairTag2") %></span>
                    <span><%: T("RepairTag3") %></span>
                    <span><%: T("RepairTag4") %></span>
                    <span><%: T("RepairTag5") %></span>
                    <span><%: T("RepairTag6") %></span>
                    <span><%: T("RepairTag7") %></span>
                    <span><%: T("RepairTag8") %></span>
                    <span><%: T("RepairTag9") %></span>
                    <span><%: T("RepairTag10") %></span>
                </div>
            </div>
        </section>

        <section class="landing-partner-banner">
            <div class="landing-shell landing-partner-banner-inner">
                <div>
                    <span class="landing-section-kicker"><%: T("PartnerKicker") %></span>
                    <h2><%: T("PartnerTitle") %></h2>
                    <p><%: T("PartnerLead") %></p>
                </div>
                <a class="btn btn-primary btn-lg" href="#contacts"><%: T("PartnerCta") %></a>
            </div>
        </section>

        <section class="landing-section landing-section-alt" id="partners">
            <div class="landing-shell">
                <div class="landing-section-heading compact">
                    <span class="landing-section-kicker"><%: T("PartnersKicker") %></span>
                    <h2><%: T("PartnersTitle") %></h2>
                </div>
                <div class="landing-logo-grid">
                    <asp:Repeater ID="HomePartnersRepeater" runat="server">
                        <ItemTemplate>
                            <div class="landing-logo-card"><%#: Container.DataItem %></div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </section>

        <section class="landing-section" id="reviews">
            <div class="landing-shell">
                <div class="landing-section-heading compact">
                    <span class="landing-section-kicker"><%: T("ReviewsKicker") %></span>
                    <h2><%: T("ReviewsTitle") %></h2>
                </div>
                <div class="landing-review-grid">
                    <asp:Repeater ID="HomeReviewsRepeater" runat="server">
                        <ItemTemplate>
                            <article class="landing-review-card">
                                <p>&laquo;<%#: Eval("Quote") %>&raquo;</p>
                                <strong><%#: Eval("Author") %></strong>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </section>

        <section class="landing-section landing-section-dark" id="contacts">
            <div class="landing-shell landing-contact-grid">
                <div class="landing-contact-copy">
                    <div class="landing-section-heading light align-left">
                        <span class="landing-section-kicker"><%: T("ContactsKicker") %></span>
                        <h2><%: T("ContactsTitle") %></h2>
                    </div>
                    <div class="landing-contact-stack">
                        <div class="landing-contact-card">
                            <span><%: T("ContactWhatsAppLabel") %></span>
                            <a id="HomeWhatsAppLink" runat="server" target="_blank" rel="noopener">
                                <asp:Literal ID="HomeContactPhoneLiteral" runat="server" />
                            </a>
                        </div>
                        <div class="landing-contact-card">
                            <span><%: T("ContactAddressLabel") %></span>
                            <strong><asp:Literal ID="HomeContactAddressLiteral" runat="server" /></strong>
                        </div>
                        <div class="landing-contact-card">
                            <span><%: T("ContactHoursLabel") %></span>
                            <strong><asp:Literal ID="HomeContactHoursLiteral" runat="server" /></strong>
                        </div>
                    </div>
                </div>
                <div class="landing-contact-form-card">
                    <h3><%: T("ContactFormTitle") %></h3>
                    <p><%: T("ContactFormLead") %></p>

                    <asp:ValidationSummary ID="HomeLeadValidationSummary" runat="server" CssClass="alert alert-danger landing-form-summary" ValidationGroup="HomeLeadGroup" />

                    <div class="form-group">
                        <label for="<%= HomeLeadNameTextBox.ClientID %>"><%: T("FormNameLabel") %></label>
                        <asp:TextBox ID="HomeLeadNameTextBox" runat="server" CssClass="form-control" />
                        <asp:RequiredFieldValidator ID="HomeLeadNameRequiredValidator" runat="server" ControlToValidate="HomeLeadNameTextBox" ValidationGroup="HomeLeadGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите имя." />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadEmailTextBox.ClientID %>"><%: T("FormEmailLabel") %></label>
                        <asp:TextBox ID="HomeLeadEmailTextBox" runat="server" CssClass="form-control" TextMode="Email" />
                        <asp:RequiredFieldValidator ID="HomeLeadEmailRequiredValidator" runat="server" ControlToValidate="HomeLeadEmailTextBox" ValidationGroup="HomeLeadGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите email." />
                        <asp:RegularExpressionValidator ID="HomeLeadEmailFormatValidator" runat="server" ControlToValidate="HomeLeadEmailTextBox" ValidationGroup="HomeLeadGroup" CssClass="text-danger" Display="Dynamic" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Укажите корректный email." />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadPhoneTextBox.ClientID %>"><%: T("FormPhoneLabel") %></label>
                        <asp:TextBox ID="HomeLeadPhoneTextBox" runat="server" CssClass="form-control" TextMode="Phone" />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadMessengerTextBox.ClientID %>"><%: T("FormMessengerLabel") %></label>
                        <asp:TextBox ID="HomeLeadMessengerTextBox" runat="server" CssClass="form-control" placeholder="WhatsApp / Telegram / WeChat" />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadDirectionTextBox.ClientID %>"><%: T("FormDirectionLabel") %></label>
                        <asp:TextBox ID="HomeLeadDirectionTextBox" runat="server" CssClass="form-control" placeholder="Например: США -> Грузия" />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadCargoTypeDropDownList.ClientID %>"><%: T("FormCargoTypeLabel") %></label>
                        <asp:DropDownList ID="HomeLeadCargoTypeDropDownList" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Не выбран" Value="" />
                            <asp:ListItem Text="Седан" Value="Седан" />
                            <asp:ListItem Text="Внедорожник" Value="Внедорожник" />
                            <asp:ListItem Text="Пикап" Value="Пикап" />
                            <asp:ListItem Text="Микроавтобус" Value="Микроавтобус" />
                            <asp:ListItem Text="Мотоцикл" Value="Мотоцикл" />
                            <asp:ListItem Text="Эксклюзивные авто" Value="Эксклюзивные авто" />
                            <asp:ListItem Text="Катеры" Value="Катеры" />
                            <asp:ListItem Text="Багги" Value="Багги" />
                        </asp:DropDownList>
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadCommentTextBox.ClientID %>"><%: T("FormCommentLabel") %></label>
                        <asp:TextBox ID="HomeLeadCommentTextBox" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" placeholder="Опишите задачу, бюджет, VIN, интересующий лот или пожелания по доставке" />
                    </div>

                    <div class="form-group">
                        <label for="<%= HomeLeadAttachmentUpload.ClientID %>"><%: T("FormAttachmentLabel") %></label>
                        <asp:FileUpload ID="HomeLeadAttachmentUpload" runat="server" CssClass="form-control landing-file-input" />
                        <small class="text-muted landing-form-hint"><%: T("FormAttachmentHint") %></small>
                    </div>

                    <asp:Button ID="HomeLeadSubmitButton" runat="server" CssClass="btn btn-primary btn-lg btn-block" Text="Отправить" ValidationGroup="HomeLeadGroup" OnClick="HomeLeadSubmitButton_Click" />
                </div>
            </div>
        </section>
    </div>

</asp:Content>
