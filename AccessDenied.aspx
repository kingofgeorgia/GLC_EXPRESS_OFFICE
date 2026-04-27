<%@ Page Title="Доступ запрещен" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="GLC_EXPRESS.AccessDenied" %>
<%@ Import Namespace="System.Linq" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="jumbotron" style="margin-top: 30px;">
        <h1><%: T("AccessDeniedTitle") %></h1>
        <p class="lead"><%: T("AccessDeniedLead") %></p>
        <p class="text-muted"><%: string.Format(T("AccessDeniedRolesLead"), GetAllowedRolesText()) %></p>
        <p>
            <a href="<%= GetHomeUrl() %>" class="btn btn-primary btn-lg"><%: T("NavHome") %></a>
            <a href="<%= GetLogoutUrl() %>" class="btn btn-default btn-lg"><%: T("AuthLogout") %></a>
        </p>
    </div>
</asp:Content>
