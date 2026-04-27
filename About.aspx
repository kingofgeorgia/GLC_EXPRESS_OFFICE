<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="GLC_EXPRESS.About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="landing-section" style="padding-top: 40px;">
        <div class="landing-section-header">
            <span class="landing-section-kicker"><%: T("AboutKicker") %></span>
            <h1><%: T("AboutTitle") %></h1>
            <p class="landing-section-lead"><%: T("AboutLead") %></p>
        </div>

        <div class="row" style="margin-top: 30px;">
            <div class="col-md-4">
                <h3><%: T("AboutFeatureAnalyticsTitle") %></h3>
                <p><%: T("AboutFeatureAnalyticsBody") %></p>
            </div>
            <div class="col-md-4">
                <h3><%: T("AboutFeatureDocumentsTitle") %></h3>
                <p><%: T("AboutFeatureDocumentsBody") %></p>
            </div>
            <div class="col-md-4">
                <h3><%: T("AboutFeatureInvestmentTitle") %></h3>
                <p><%: T("AboutFeatureInvestmentBody") %></p>
            </div>
        </div>
    </section>
</asp:Content>
