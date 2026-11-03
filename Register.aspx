<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="GLC_EXPRESS.Register" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-sm-8 col-sm-offset-2 col-md-6 col-md-offset-3">
            <div class="panel panel-default" style="margin-top: 30px;">
                <div class="panel-heading">
                    <h2 class="panel-title"><%: T("RegisterTitle") %></h2>
                </div>
                <div class="panel-body">
                    <p class="text-muted"><%: T("RegisterLead") %></p>

                    <asp:Panel ID="RegistrationDisabledPanel" runat="server" CssClass="alert alert-info" Visible="false">
                        <%: T("RegisterDisabled") %>
                    </asp:Panel>
                    <asp:Panel ID="RegistrationSuccessPanel" runat="server" CssClass="alert alert-success" Visible="false">
                        <%: T("RegisterSuccess") %>
                    </asp:Panel>
                    <asp:Panel ID="RegistrationErrorPanel" runat="server" CssClass="alert alert-danger" Visible="false">
                        <asp:Literal ID="RegistrationErrorLiteral" runat="server" />
                    </asp:Panel>
                    <asp:Panel ID="RegistrationFormPanel" runat="server">
                        <asp:ValidationSummary ID="RegistrationValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="RegisterForm" />

                        <div class="form-group">
                            <label for="<%= FullNameTextBox.ClientID %>"><%: T("RegisterFullName") %></label>
                            <asp:TextBox ID="FullNameTextBox" runat="server" CssClass="form-control" autocomplete="name" MaxLength="120" />
                            <asp:RequiredFieldValidator ID="FullNameRequiredValidator" runat="server" ControlToValidate="FullNameTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" />
                        </div>

                        <div class="form-group">
                            <label for="<%= EmailTextBox.ClientID %>"><%: T("RegisterEmail") %></label>
                            <asp:TextBox ID="EmailTextBox" runat="server" CssClass="form-control" TextMode="Email" autocomplete="email" MaxLength="254" />
                            <asp:RequiredFieldValidator ID="EmailRequiredValidator" runat="server" ControlToValidate="EmailTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" />
                            <asp:RegularExpressionValidator ID="EmailFormatValidator" runat="server" ControlToValidate="EmailTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" ValidationExpression="^[^\s@]+@[^\s@]+\.[^\s@]+$" />
                        </div>

                        <div class="form-group">
                            <label for="<%= PhoneTextBox.ClientID %>"><%: T("RegisterPhone") %></label>
                            <asp:TextBox ID="PhoneTextBox" runat="server" CssClass="form-control" TextMode="Phone" autocomplete="tel" MaxLength="40" />
                        </div>

                        <div class="form-group">
                            <label for="<%= PasswordTextBox.ClientID %>"><%: T("RegisterPassword") %></label>
                            <asp:TextBox ID="PasswordTextBox" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" />
                            <asp:RequiredFieldValidator ID="PasswordRequiredValidator" runat="server" ControlToValidate="PasswordTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" />
                        </div>

                        <div class="form-group">
                            <label for="<%= ConfirmPasswordTextBox.ClientID %>"><%: T("RegisterConfirmPassword") %></label>
                            <asp:TextBox ID="ConfirmPasswordTextBox" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" />
                            <asp:RequiredFieldValidator ID="ConfirmPasswordRequiredValidator" runat="server" ControlToValidate="ConfirmPasswordTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" />
                            <asp:CompareValidator ID="PasswordMatchValidator" runat="server" ControlToValidate="ConfirmPasswordTextBox" ControlToCompare="PasswordTextBox" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" />
                        </div>

                        <div class="checkbox">
                            <label>
                                <asp:CheckBox ID="PrivacyConsentCheckBox" runat="server" />
                                <%: T("RegisterConsent") %>
                                <a href="<%= GetPrivacyPolicyUrl() %>" target="_blank" rel="noopener"><%: T("FooterPrivacyPolicy") %></a>
                            </label>
                            <asp:CustomValidator ID="PrivacyConsentValidator" runat="server" CssClass="text-danger" ValidationGroup="RegisterForm" Display="Dynamic" OnServerValidate="PrivacyConsentValidator_ServerValidate" />
                        </div>

                        <div aria-hidden="true" style="position:absolute;left:-10000px;width:1px;height:1px;overflow:hidden;">
                            <label for="registration-company">Company</label>
                            <input id="registration-company" name="registration-company" type="text" tabindex="-1" autocomplete="off" />
                        </div>

                        <asp:Button ID="RegisterButton" runat="server" CssClass="btn btn-primary btn-block" OnClick="RegisterButton_Click" ValidationGroup="RegisterForm" />
                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
