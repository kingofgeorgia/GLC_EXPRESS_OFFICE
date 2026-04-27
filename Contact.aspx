<%@ Page Title="Contact" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="GLC_EXPRESS.Contact" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="landing-section" style="padding-top: 40px;">
        <div class="landing-section-header">
            <span class="landing-section-kicker"><%: T("ContactsKicker") %></span>
            <h1><%: T("ContactsTitle") %></h1>
            <p class="landing-section-lead"><%: T("ContactFormLead") %></p>
        </div>

        <div class="row" style="margin-top: 30px;">
            <div class="col-md-6">
                <div class="panel panel-default crm-form-panel">
                    <div class="panel-body">
                        <p><strong><%: T("ContactAddressLabel") %>:</strong> <%: HomeContactAddress %></p>
                        <p><strong><%: T("ContactWhatsAppLabel") %>:</strong> <a href="<%= HomeWhatsAppUrl %>"><%: HomeContactPhone %></a></p>
                        <p><strong><%: T("ContactHoursLabel") %>:</strong> <%: HomeContactWorkingHours %></p>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="panel panel-default crm-form-panel">
                    <div class="panel-body">
                        <h3 style="margin-top: 0;"><%: T("ContactFormTitle") %></h3>
                        <p><%: T("ContactsTitle") %></p>
                        <p>
                            <a href="<%= GetContactFormUrl() %>" class="btn btn-primary"><%: T("ContactFormTitle") %></a>
                        </p>
                    </div>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
