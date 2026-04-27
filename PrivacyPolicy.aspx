<%@ Page Title="Privacy Policy" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PrivacyPolicy.aspx.cs" Inherits="GLC_EXPRESS.PrivacyPolicy" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="privacy-policy-page">
        <div class="landing-shell">
            <div class="privacy-policy-card">
                <span class="landing-section-kicker"><%: T("PrivacyKicker") %></span>
                <h1><%: T("PrivacyTitle") %></h1>
                <p class="privacy-policy-meta"><%: T("PrivacyMeta") %></p>

                <p><%: T("PrivacyIntro") %></p>

                <h2><%: T("PrivacyCollectedTitle") %></h2>
                <ul>
                    <li><%: T("PrivacyCollectedItem1") %></li>
                    <li><%: T("PrivacyCollectedItem2") %></li>
                    <li><%: T("PrivacyCollectedItem3") %></li>
                </ul>

                <h2><%: T("PrivacyUsageTitle") %></h2>
                <ul>
                    <li><%: T("PrivacyUsageItem1") %></li>
                    <li><%: T("PrivacyUsageItem2") %></li>
                    <li><%: T("PrivacyUsageItem3") %></li>
                    <li><%: T("PrivacyUsageItem4") %></li>
                </ul>

                <h2><%: T("PrivacyThirdPartyTitle") %></h2>
                <p><%: T("PrivacyThirdPartyBody") %></p>

                <h2><%: T("PrivacyCookiesTitle") %></h2>
                <p><%: T("PrivacyCookiesBody") %></p>

                <h2><%: T("PrivacyRetentionTitle") %></h2>
                <p><%: T("PrivacyRetentionBody") %></p>

                <h2><%: T("PrivacyRightsTitle") %></h2>
                <ul>
                    <li><%: T("PrivacyRightsItem1") %></li>
                    <li><%: T("PrivacyRightsItem2") %></li>
                    <li><%: T("PrivacyRightsItem3") %></li>
                    <li><%: T("PrivacyRightsItem4") %></li>
                </ul>

                <h2><%: T("PrivacyContactTitle") %></h2>
                <p><%: T("PrivacyContactBody") %></p>
            </div>
        </div>
    </div>
</asp:Content>