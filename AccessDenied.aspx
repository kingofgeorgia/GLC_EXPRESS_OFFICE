<%@ Page Title="Доступ запрещен" Language="C#" MasterPageFile="~/Site.Master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="jumbotron" style="margin-top: 30px;">
        <h1>Доступ запрещен</h1>
        <p class="lead">У вашей учетной записи пока нет прав на работу с CRM.</p>
        <p class="text-muted">Для доступа нужны роли: <strong>Admin</strong>, <strong>Manager</strong> или <strong>Dispatcher</strong>.</p>
        <p>
            <a href="Default.aspx" class="btn btn-primary btn-lg">На главную</a>
            <a href="Logout.aspx" class="btn btn-default btn-lg">Выйти</a>
        </p>
    </div>
</asp:Content>
