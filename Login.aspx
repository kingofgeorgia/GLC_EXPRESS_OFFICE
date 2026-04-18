<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="GLC_EXPRESS.Login" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-sm-8 col-sm-offset-2 col-md-6 col-md-offset-3">
            <div class="panel panel-default" style="margin-top: 30px;">
                <div class="panel-heading">
                    <h2 class="panel-title">Sign In</h2>
                </div>
                <div class="panel-body">
                    <p class="text-muted">Enter your account credentials to continue.</p>

                    <asp:ValidationSummary ID="LoginValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="LoginForm" />

                    <asp:Panel ID="ErrorPanel" runat="server" CssClass="alert alert-danger" Visible="false">
                        <asp:Literal ID="ErrorLiteral" runat="server" />
                    </asp:Panel>

                    <div class="form-group">
                        <label for="<%= UsernameTextBox.ClientID %>">Username</label>
                        <asp:TextBox ID="UsernameTextBox" runat="server" CssClass="form-control" />
                        <asp:RequiredFieldValidator ID="UsernameRequiredValidator" runat="server" ControlToValidate="UsernameTextBox" CssClass="text-danger" ErrorMessage="Username is required." ValidationGroup="LoginForm" Display="Dynamic" />
                    </div>

                    <div class="form-group">
                        <label for="<%= PasswordTextBox.ClientID %>">Password</label>
                        <asp:TextBox ID="PasswordTextBox" runat="server" CssClass="form-control" TextMode="Password" />
                        <asp:RequiredFieldValidator ID="PasswordRequiredValidator" runat="server" ControlToValidate="PasswordTextBox" CssClass="text-danger" ErrorMessage="Password is required." ValidationGroup="LoginForm" Display="Dynamic" />
                    </div>

                    <div class="checkbox">
                        <label>
                            <asp:CheckBox ID="RememberMeCheckBox" runat="server" />
                            Remember me
                        </label>
                    </div>

                    <asp:Button ID="SignInButton" runat="server" CssClass="btn btn-primary btn-block" Text="Sign In" OnClick="SignInButton_Click" ValidationGroup="LoginForm" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
