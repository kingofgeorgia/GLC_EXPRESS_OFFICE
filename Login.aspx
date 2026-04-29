<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="GLC_EXPRESS.Login" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-sm-8 col-sm-offset-2 col-md-6 col-md-offset-3">
            <div class="panel panel-default" style="margin-top: 30px;">
                <div class="panel-heading">
                    <h2 class="panel-title"><%: T("AuthSignIn") %></h2>
                </div>
                <div class="panel-body">
                    <p class="text-muted"><%: T("LoginLead") %></p>

                    <asp:ValidationSummary ID="LoginValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="LoginForm" />

                    <asp:Panel ID="ErrorPanel" runat="server" CssClass="alert alert-danger" Visible="false">
                        <asp:Literal ID="ErrorLiteral" runat="server" />
                    </asp:Panel>

                    <div class="form-group">
                        <label for="<%= UsernameTextBox.ClientID %>"><%: T("LoginUsername") %></label>
                        <asp:TextBox ID="UsernameTextBox" runat="server" CssClass="form-control" autocomplete="username" autocapitalize="none" spellcheck="false" />
                        <asp:RequiredFieldValidator ID="UsernameRequiredValidator" runat="server" ControlToValidate="UsernameTextBox" CssClass="text-danger" ValidationGroup="LoginForm" Display="Dynamic" />
                    </div>

                    <div class="form-group">
                        <label for="<%= PasswordTextBox.ClientID %>"><%: T("LoginPassword") %></label>
                        <asp:TextBox ID="PasswordTextBox" runat="server" CssClass="form-control" TextMode="Password" autocomplete="current-password" />
                        <asp:RequiredFieldValidator ID="PasswordRequiredValidator" runat="server" ControlToValidate="PasswordTextBox" CssClass="text-danger" ValidationGroup="LoginForm" Display="Dynamic" />
                    </div>

                    <div class="checkbox">
                        <label>
                            <asp:CheckBox ID="RememberMeCheckBox" runat="server" />
                            <%: T("LoginRememberMe") %>
                        </label>
                    </div>

                    <asp:Button ID="SignInButton" runat="server" CssClass="btn btn-primary btn-block" OnClick="SignInButton_Click" ValidationGroup="LoginForm" />
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        (function () {
            var usernameId = '<%= UsernameTextBox.ClientID %>';
            var passwordId = '<%= PasswordTextBox.ClientID %>';
            var rememberMeId = '<%= RememberMeCheckBox.ClientID %>';
            var signInButtonId = '<%= SignInButton.ClientID %>';
            var errorPanelId = '<%= ErrorPanel.ClientID %>';
            var autoSubmitAttempted = false;
            var autoSubmitDelays = [150, 450, 900, 1500];

            function hasVisibleError() {
                var errorPanel = document.getElementById(errorPanelId);
                return !!(errorPanel && errorPanel.offsetParent !== null);
            }

            function hasRememberedCredentials() {
                var username = document.getElementById(usernameId);
                var password = document.getElementById(passwordId);
                var rememberMe = document.getElementById(rememberMeId);

                return !!(username
                    && password
                    && rememberMe
                    && rememberMe.checked
                    && username.value
                    && username.value.trim().length > 0
                    && password.value
                    && password.value.length > 0);
            }

            function tryAutoSubmit() {
                var signInButton;

                if (autoSubmitAttempted || hasVisibleError() || !hasRememberedCredentials()) {
                    return;
                }

                signInButton = document.getElementById(signInButtonId);

                if (!signInButton) {
                    return;
                }

                autoSubmitAttempted = true;
                signInButton.click();
            }

            window.addEventListener('load', function () {
                var index;

                for (index = 0; index < autoSubmitDelays.length; index++) {
                    window.setTimeout(tryAutoSubmit, autoSubmitDelays[index]);
                }
            });
        })();
    </script>
</asp:Content>
