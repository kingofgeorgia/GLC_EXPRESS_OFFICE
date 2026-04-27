<%@ Page Title="Документ" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DocumentViewer.aspx.cs" Inherits="GLC_EXPRESS.DocumentViewer" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="document-viewer-page">
        <asp:Panel ID="DocumentAlertPanel" runat="server" Visible="false" CssClass="alert alert-warning">
            <asp:Literal ID="DocumentAlertLiteral" runat="server" />
        </asp:Panel>

        <asp:Panel ID="DocumentContentPanel" runat="server" Visible="false">
            <div class="page-header">
                <h1>Просмотр документа</h1>
                <p class="text-muted">Файл: <strong><asp:Literal ID="DocumentFileNameLiteral" runat="server" /></strong></p>
            </div>

            <div class="document-viewer-actions">
                <asp:HyperLink ID="DownloadDocumentHyperLink" runat="server" CssClass="btn btn-primary" Text="Скачать документ" />
                <asp:HyperLink ID="BackToCrmHyperLink" runat="server" CssClass="btn btn-default" NavigateUrl="~/orders" Text="Вернуться в CRM" />
            </div>

            <asp:Panel ID="DocumentPreviewAvailablePanel" runat="server" CssClass="panel panel-default document-preview-panel" Visible="false">
                <div class="panel-heading">
                    <h2 class="panel-title">Предпросмотр</h2>
                </div>
                <div class="panel-body">
                    <asp:Literal ID="DocumentPreviewLiteral" runat="server" />
                </div>
            </asp:Panel>

            <asp:Panel ID="DocumentPreviewUnavailablePanel" runat="server" CssClass="alert alert-info" Visible="false">
                Для этого типа файла предпросмотр недоступен. Используйте кнопку скачивания.
            </asp:Panel>

            <div class="panel panel-default">
                <div class="panel-heading">
                    <h2 class="panel-title">Последние обращения</h2>
                </div>
                <div class="panel-body">
                    <asp:Panel ID="DocumentAuditEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                        Журнал обращений для этого файла пока пуст.
                    </asp:Panel>
                    <div class="table-responsive">
                        <asp:Repeater ID="DocumentAuditRepeater" runat="server">
                            <HeaderTemplate>
                                <table class="table table-striped table-bordered crm-table">
                                    <thead>
                                        <tr>
                                            <th>Дата и время</th>
                                            <th>Пользователь</th>
                                            <th>Действие</th>
                                            <th>IP</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td><%# Eval("AccessedAtUtc", "{0:dd.MM.yyyy HH:mm:ss}") %></td>
                                    <td><%# Eval("Username") %></td>
                                    <td><%# Eval("Action") %></td>
                                    <td><%# Eval("IpAddress") %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate>
                                    </tbody>
                                </table>
                            </FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
