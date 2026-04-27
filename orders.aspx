<%@ Page Title="CRM" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="orders.aspx.cs" Inherits="GLC_EXPRESS.orders" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="crm-page">
        <asp:Panel ID="PageAlertPanel" runat="server" Visible="false" CssClass="alert">
            <asp:Literal ID="PageAlertLiteral" runat="server" />
        </asp:Panel>

        <div class="page-header crm-header">
            <h1>CRM</h1>
            <p class="text-muted">Управление рейсами, водителями, автопарком, автомобилями, клиентами и заявками.</p>
        </div>

        <div class="alert alert-info crm-user-banner">
            Вы вошли как <strong><%: Context.User.Identity.Name %></strong>.
            Роли: <strong><%: GetCurrentUserRolesLabel() %></strong>.
        </div>

        <ul class="nav nav-tabs crm-tabs">
            <li class="<%= GetTabCss("trips") %>">
                <asp:LinkButton ID="TripsTabButton" runat="server" CommandArgument="trips" OnClick="TabLinkButton_Click" CausesValidation="false">Рейсы</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("drivers") %>">
                <asp:LinkButton ID="DriversTabButton" runat="server" CommandArgument="drivers" OnClick="TabLinkButton_Click" CausesValidation="false">Водители</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("fleet") %>">
                <asp:LinkButton ID="FleetTabButton" runat="server" CommandArgument="fleet" OnClick="TabLinkButton_Click" CausesValidation="false">Автопарк</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("cars") %>">
                <asp:LinkButton ID="CarsTabButton" runat="server" CommandArgument="cars" OnClick="TabLinkButton_Click" CausesValidation="false">Автомобили</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("clients") %>">
                <asp:LinkButton ID="ClientsTabButton" runat="server" CommandArgument="clients" OnClick="TabLinkButton_Click" CausesValidation="false">Клиенты</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("inquiries") %>">
                <asp:LinkButton ID="InquiriesTabButton" runat="server" CommandArgument="inquiries" OnClick="TabLinkButton_Click" CausesValidation="false">Заявки</asp:LinkButton>
            </li>
            <li class="<%= GetTabCss("settings") %>">
                <asp:LinkButton ID="SettingsTabButton" runat="server" CommandArgument="settings" OnClick="TabLinkButton_Click" CausesValidation="false">Настройки</asp:LinkButton>
            </li>
        </ul>

        <div class="tab-content crm-tab-content">
            <div class="<%= GetPaneCss("trips") %>">
                <div class="row">
                    <div class="<%= GetFormColumnCss("trips") %>">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">
                                    <asp:Literal ID="TripsFormTitleLiteral" runat="server" />
                                </h2>
                            </div>
                            <div class="panel-body">
                                <asp:HiddenField ID="TripsEditingIdHiddenField" runat="server" />
                                <asp:ValidationSummary ID="TripsValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="TripsGroup" />

                                <div class="form-group">
                                    <label for="<%= TripsNumberTextBox.ClientID %>">№</label>
                                    <asp:TextBox ID="TripsNumberTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsNumberRequiredValidator" runat="server" ControlToValidate="TripsNumberTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите номер рейса." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsClientDropDownList.ClientID %>">Клиент</label>
                                    <asp:DropDownList ID="TripsClientDropDownList" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsClientRequiredValidator" runat="server" ControlToValidate="TripsClientDropDownList" InitialValue="" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Выберите клиента." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsStatusDropDownList.ClientID %>">Статус</label>
                                    <asp:DropDownList ID="TripsStatusDropDownList" runat="server" CssClass="form-control" />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsCountryTextBox.ClientID %>">Страна</label>
                                    <asp:TextBox ID="TripsCountryTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsCountryRequiredValidator" runat="server" ControlToValidate="TripsCountryTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите страну." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsVehicleDropDownList.ClientID %>">Автомобиль</label>
                                    <asp:DropDownList ID="TripsVehicleDropDownList" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsVehicleRequiredValidator" runat="server" ControlToValidate="TripsVehicleDropDownList" InitialValue="" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Выберите автомобиль." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsDriverDropDownList.ClientID %>">Водитель</label>
                                    <asp:DropDownList ID="TripsDriverDropDownList" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsDriverRequiredValidator" runat="server" ControlToValidate="TripsDriverDropDownList" InitialValue="" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Выберите водителя." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsStartDateTextBox.ClientID %>">Дата начала</label>
                                    <asp:TextBox ID="TripsStartDateTextBox" runat="server" CssClass="form-control" TextMode="Date" />
                                    <asp:RequiredFieldValidator ID="TripsStartDateRequiredValidator" runat="server" ControlToValidate="TripsStartDateTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите дату начала." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsEndDateTextBox.ClientID %>">Дата окончания</label>
                                    <asp:TextBox ID="TripsEndDateTextBox" runat="server" CssClass="form-control" TextMode="Date" />
                                    <asp:RequiredFieldValidator ID="TripsEndDateRequiredValidator" runat="server" ControlToValidate="TripsEndDateTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите дату окончания." />
                                    <asp:CompareValidator ID="TripsDateRangeValidator" runat="server" ControlToValidate="TripsEndDateTextBox" ControlToCompare="TripsStartDateTextBox" Operator="GreaterThanEqual" Type="Date" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Дата окончания не может быть раньше даты начала." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsFreightTextBox.ClientID %>">Фрахт</label>
                                    <asp:TextBox ID="TripsFreightTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsFreightRequiredValidator" runat="server" ControlToValidate="TripsFreightTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите фрахт." />
                                    <asp:RegularExpressionValidator ID="TripsFreightFormatValidator" runat="server" ControlToValidate="TripsFreightTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ValidationExpression="^[0-9]+([\.,][0-9]{1,2})?$" ErrorMessage="Фрахт должен быть числом." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsPrepaymentTextBox.ClientID %>">Предоплата</label>
                                    <asp:TextBox ID="TripsPrepaymentTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsPrepaymentRequiredValidator" runat="server" ControlToValidate="TripsPrepaymentTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите предоплату." />
                                    <asp:RegularExpressionValidator ID="TripsPrepaymentFormatValidator" runat="server" ControlToValidate="TripsPrepaymentTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ValidationExpression="^[0-9]+([\.,][0-9]{1,2})?$" ErrorMessage="Предоплата должна быть числом." />
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddTripButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="TripsGroup" OnClick="AddTripButton_Click" />
                                    <asp:Button ID="TripsCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="TripsCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="<%= GetListColumnCss("trips") %>">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading crm-panel-heading">
                                <h2 class="panel-title">Список рейсов</h2>
                                <asp:Button ID="NewTripButton" runat="server" CssClass="btn btn-primary btn-xs crm-new-entry-button" Text="Новый рейс" CausesValidation="false" OnClick="NewTripButton_Click" />
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="TripsEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного рейса.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="TripsRepeater" runat="server" OnItemCommand="TripsRepeater_ItemCommand" OnItemDataBound="TripsRepeater_ItemDataBound">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>№</th>
                                                        <th>Клиент</th>
                                                        <th>Статус</th>
                                                        <th>Страна</th>
                                                        <th>Автомобиль</th>
                                                        <th>Водитель</th>
                                                        <th>Дата начала</th>
                                                        <th>Дата окончания</th>
                                                        <th>Фрахт</th>
                                                        <th>Предоплата</th>
                                                        <th>Действия</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# IsTripsInlineEditRow(Eval("Id")) ? "crm-inline-edit-row" : string.Empty %>">
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("Number") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripNumberTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Number") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("ClientName") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:DropDownList ID="InlineTripClientDropDownList" runat="server" CssClass="form-control input-sm" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><span class="label label-default"><%#: Eval("Status") %></span></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:DropDownList ID="InlineTripStatusDropDownList" runat="server" CssClass="form-control input-sm" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("Country") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripCountryTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Country") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("VehicleName") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:DropDownList ID="InlineTripVehicleDropDownList" runat="server" CssClass="form-control input-sm" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("DriverName") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:DropDownList ID="InlineTripDriverDropDownList" runat="server" CssClass="form-control input-sm" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: FormatDateValue(Eval("StartDate")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripStartDateTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("StartDate") %>' TextMode="Date" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: FormatDateValue(Eval("EndDate")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripEndDateTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("EndDate") %>' TextMode="Date" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("Freight") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripFreightTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Freight") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'><%#: Eval("Prepayment") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineTripPrepaymentTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Prepayment") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td class="crm-actions-cell">
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="EditTripButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                        <asp:LinkButton ID="DeleteTripButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этот рейс?');">Удалить</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsTripsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="SaveInlineTripButton" runat="server" CssClass="btn btn-xs btn-primary" CommandName="SaveInlineItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Сохранить</asp:LinkButton>
                                                        <asp:LinkButton ID="CancelInlineTripButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="CancelInlineEdit" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Отмена</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                </td>
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
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("drivers") %>">
                <div class="row">
                    <div class="<%= GetFormColumnCss("drivers") %>">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">
                                    <asp:Literal ID="DriversFormTitleLiteral" runat="server" />
                                </h2>
                            </div>
                            <div class="panel-body">
                                <asp:HiddenField ID="DriversEditingIdHiddenField" runat="server" />
                                <asp:ValidationSummary ID="DriversValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="DriversGroup" />

                                <div class="form-group">
                                    <label for="<%= DriversFullNameTextBox.ClientID %>">ФИО</label>
                                    <asp:TextBox ID="DriversFullNameTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="DriversFullNameRequiredValidator" runat="server" ControlToValidate="DriversFullNameTextBox" ValidationGroup="DriversGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите ФИО водителя." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversBirthDateTextBox.ClientID %>">Дата рождения</label>
                                    <asp:TextBox ID="DriversBirthDateTextBox" runat="server" CssClass="form-control" TextMode="Date" />
                                    <asp:RequiredFieldValidator ID="DriversBirthDateRequiredValidator" runat="server" ControlToValidate="DriversBirthDateTextBox" ValidationGroup="DriversGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите дату рождения." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversPhoneTextBox.ClientID %>">Номер телефона</label>
                                    <asp:TextBox ID="DriversPhoneTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="DriversPhoneRequiredValidator" runat="server" ControlToValidate="DriversPhoneTextBox" ValidationGroup="DriversGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите номер телефона." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversAddressTextBox.ClientID %>">Адрес проживания</label>
                                    <asp:TextBox ID="DriversAddressTextBox" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" />
                                    <asp:RequiredFieldValidator ID="DriversAddressRequiredValidator" runat="server" ControlToValidate="DriversAddressTextBox" ValidationGroup="DriversGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите адрес проживания." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversPassportUpload.ClientID %>">Скан копия паспорта</label>
                                    <asp:FileUpload ID="DriversPassportUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится. Допустимы PDF, PNG, JPG, JPEG, GIF и WEBP.</small>
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversLicenseUpload.ClientID %>">Скан копия водительского удостоверения</label>
                                    <asp:FileUpload ID="DriversLicenseUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится. Допустимы PDF, PNG, JPG, JPEG, GIF и WEBP.</small>
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddDriverButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="DriversGroup" OnClick="AddDriverButton_Click" />
                                    <asp:Button ID="DriversCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="DriversCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="<%= GetListColumnCss("drivers") %>">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading crm-panel-heading">
                                <h2 class="panel-title">Список водителей</h2>
                                <asp:Button ID="NewDriverButton" runat="server" CssClass="btn btn-primary btn-xs crm-new-entry-button" Text="Новый водитель" CausesValidation="false" OnClick="NewDriverButton_Click" />
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="DriversEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного водителя.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="DriversRepeater" runat="server" OnItemCommand="DriversRepeater_ItemCommand" OnItemDataBound="DriversRepeater_ItemDataBound">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>ФИО</th>
                                                        <th>Дата рождения</th>
                                                        <th>Телефон</th>
                                                        <th>Адрес проживания</th>
                                                        <th>Паспорт</th>
                                                        <th>Водительское удостоверение</th>
                                                        <th>Действия</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# IsDriversInlineEditRow(Eval("Id")) ? "crm-inline-edit-row" : string.Empty %>">
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%#: Eval("FullName") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineDriverFullNameTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("FullName") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%#: FormatDateValue(Eval("BirthDate")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineDriverBirthDateTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("BirthDate") %>' TextMode="Date" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%#: Eval("PhoneNumber") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineDriverPhoneTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("PhoneNumber") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%#: Eval("Address") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineDriverAddressTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Address") %>' TextMode="MultiLine" Rows="2" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%# FormatDocumentLink(Eval("PassportScanPath")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <div class="crm-inline-document-cell"><%# FormatDocumentLink(Eval("PassportScanPath")) %></div>
                                                        <asp:FileUpload ID="InlineDriverPassportUpload" runat="server" CssClass="form-control input-sm crm-inline-file-upload" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'><%# FormatDocumentLink(Eval("LicenseScanPath")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <div class="crm-inline-document-cell"><%# FormatDocumentLink(Eval("LicenseScanPath")) %></div>
                                                        <asp:FileUpload ID="InlineDriverLicenseUpload" runat="server" CssClass="form-control input-sm crm-inline-file-upload" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td class="crm-actions-cell">
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="EditDriverButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                        <asp:LinkButton ID="DeleteDriverButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этого водителя?');">Удалить</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsDriversInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="SaveInlineDriverButton" runat="server" CssClass="btn btn-xs btn-primary" CommandName="SaveInlineItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Сохранить</asp:LinkButton>
                                                        <asp:LinkButton ID="CancelInlineDriverButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="CancelInlineEdit" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Отмена</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                </td>
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
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("fleet") %>">
                <div class="row">
                    <div class="<%= GetFormColumnCss("fleet") %>">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">
                                    <asp:Literal ID="FleetFormTitleLiteral" runat="server" />
                                </h2>
                            </div>
                            <div class="panel-body">
                                <asp:HiddenField ID="FleetEditingIdHiddenField" runat="server" />
                                <asp:ValidationSummary ID="FleetValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="FleetGroup" />

                                <div class="form-group">
                                    <label for="<%= FleetCarBrandTextBox.ClientID %>">Марка автомобиля</label>
                                    <asp:TextBox ID="FleetCarBrandTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="FleetCarBrandRequiredValidator" runat="server" ControlToValidate="FleetCarBrandTextBox" ValidationGroup="FleetGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите марку автомобиля." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetCarModelTextBox.ClientID %>">Модель автомобиля</label>
                                    <asp:TextBox ID="FleetCarModelTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="FleetCarModelRequiredValidator" runat="server" ControlToValidate="FleetCarModelTextBox" ValidationGroup="FleetGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите модель автомобиля." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetLicensePlateTextBox.ClientID %>">Гос. номер</label>
                                    <asp:TextBox ID="FleetLicensePlateTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="FleetLicensePlateRequiredValidator" runat="server" ControlToValidate="FleetLicensePlateTextBox" ValidationGroup="FleetGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите гос. номер автомобиля." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetVinTextBox.ClientID %>">VIN код</label>
                                    <asp:TextBox ID="FleetVinTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="FleetVinRequiredValidator" runat="server" ControlToValidate="FleetVinTextBox" ValidationGroup="FleetGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите VIN код." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetTrailerBrandTextBox.ClientID %>">Марка прицепа</label>
                                    <asp:TextBox ID="FleetTrailerBrandTextBox" runat="server" CssClass="form-control" />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetTrailerModelTextBox.ClientID %>">Модель прицепа</label>
                                    <asp:TextBox ID="FleetTrailerModelTextBox" runat="server" CssClass="form-control" />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetTrailerLicensePlateTextBox.ClientID %>">Гос. номер прицепа</label>
                                    <asp:TextBox ID="FleetTrailerLicensePlateTextBox" runat="server" CssClass="form-control" />
                                </div>

                                <div class="form-group">
                                    <label>Закрепленные водители</label>
                                    <asp:CheckBoxList ID="FleetDriversCheckBoxList" runat="server" CssClass="crm-checkbox-list" />
                                </div>

                                <div class="form-group">
                                    <label for="<%= FleetDocumentsUpload.ClientID %>">Скан копия документов</label>
                                    <asp:FileUpload ID="FleetDocumentsUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится. Допустимы PDF, PNG, JPG, JPEG, GIF и WEBP.</small>
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddFleetButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="FleetGroup" OnClick="AddFleetButton_Click" />
                                    <asp:Button ID="FleetCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="FleetCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="<%= GetListColumnCss("fleet") %>">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading crm-panel-heading">
                                <h2 class="panel-title">Автопарк</h2>
                                <asp:Button ID="NewFleetButton" runat="server" CssClass="btn btn-primary btn-xs crm-new-entry-button" Text="Новый автомобиль" CausesValidation="false" OnClick="NewFleetButton_Click" />
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="FleetEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного автомобиля.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="FleetRepeater" runat="server" OnItemCommand="FleetRepeater_ItemCommand" OnItemDataBound="FleetRepeater_ItemDataBound">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>Марка автомобиля</th>
                                                        <th>Модель автомобиля</th>
                                                        <th>Гос. номер</th>
                                                        <th>VIN код</th>
                                                        <th>Марка прицепа</th>
                                                        <th>Модель прицепа</th>
                                                        <th>Гос. номер прицепа</th>
                                                        <th>Водители</th>
                                                        <th>Документы</th>
                                                        <th>Действия</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# IsFleetInlineEditRow(Eval("Id")) ? "crm-inline-edit-row" : string.Empty %>">
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("CarBrand") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetCarBrandTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("CarBrand") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("CarModel") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetCarModelTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("CarModel") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("LicensePlate") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetLicensePlateTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("LicensePlate") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("VinCode") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetVinTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("VinCode") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("TrailerBrand") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetTrailerBrandTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("TrailerBrand") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("TrailerModel") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetTrailerModelTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("TrailerModel") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: Eval("TrailerLicensePlate") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineFleetTrailerLicensePlateTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("TrailerLicensePlate") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%#: FormatDriverNames(Eval("AssignedDriverNames")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:CheckBoxList ID="InlineFleetDriversCheckBoxList" runat="server" CssClass="crm-checkbox-list crm-inline-checkbox-list" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'><%# FormatDocumentLink(Eval("DocumentsScanPath")) %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <div class="crm-inline-document-cell"><%# FormatDocumentLink(Eval("DocumentsScanPath")) %></div>
                                                        <asp:FileUpload ID="InlineFleetDocumentsUpload" runat="server" CssClass="form-control input-sm crm-inline-file-upload" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td class="crm-actions-cell">
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="EditFleetButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                        <asp:LinkButton ID="DeleteFleetButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этот автомобиль?');">Удалить</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsFleetInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="SaveInlineFleetButton" runat="server" CssClass="btn btn-xs btn-primary" CommandName="SaveInlineItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Сохранить</asp:LinkButton>
                                                        <asp:LinkButton ID="CancelInlineFleetButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="CancelInlineEdit" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Отмена</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                </td>
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
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("clients") %>">
                <div class="row">
                    <div class="<%= GetFormColumnCss("clients") %>">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">
                                    <asp:Literal ID="ClientsFormTitleLiteral" runat="server" />
                                </h2>
                            </div>
                            <div class="panel-body">
                                <asp:HiddenField ID="ClientsEditingIdHiddenField" runat="server" />
                                <asp:ValidationSummary ID="ClientsValidationSummary" runat="server" CssClass="alert alert-danger" ValidationGroup="ClientsGroup" />

                                <div class="form-group">
                                    <label for="<%= ClientsNameTextBox.ClientID %>">Клиент</label>
                                    <asp:TextBox ID="ClientsNameTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="ClientsNameRequiredValidator" runat="server" ControlToValidate="ClientsNameTextBox" ValidationGroup="ClientsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите клиента." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= ClientsDirectionTextBox.ClientID %>">Направление</label>
                                    <asp:TextBox ID="ClientsDirectionTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="ClientsDirectionRequiredValidator" runat="server" ControlToValidate="ClientsDirectionTextBox" ValidationGroup="ClientsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите направление." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= ClientsManagerTextBox.ClientID %>">Менеджер</label>
                                    <asp:TextBox ID="ClientsManagerTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="ClientsManagerRequiredValidator" runat="server" ControlToValidate="ClientsManagerTextBox" ValidationGroup="ClientsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите менеджера." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= ClientsPhoneTextBox.ClientID %>">Телефон</label>
                                    <asp:TextBox ID="ClientsPhoneTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="ClientsPhoneRequiredValidator" runat="server" ControlToValidate="ClientsPhoneTextBox" ValidationGroup="ClientsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите телефон." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= ClientsEmailTextBox.ClientID %>">Эл. почта</label>
                                    <asp:TextBox ID="ClientsEmailTextBox" runat="server" CssClass="form-control" TextMode="Email" />
                                    <asp:RequiredFieldValidator ID="ClientsEmailRequiredValidator" runat="server" ControlToValidate="ClientsEmailTextBox" ValidationGroup="ClientsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите эл. почту." />
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddClientButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="ClientsGroup" OnClick="AddClientButton_Click" />
                                    <asp:Button ID="ClientsCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="ClientsCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="<%= GetListColumnCss("clients") %>">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading crm-panel-heading">
                                <h2 class="panel-title">Список клиентов</h2>
                                <asp:Button ID="NewClientButton" runat="server" CssClass="btn btn-primary btn-xs crm-new-entry-button" Text="Новый клиент" CausesValidation="false" OnClick="NewClientButton_Click" />
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="ClientsEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного клиента.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="ClientsRepeater" runat="server" OnItemCommand="ClientsRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>Клиент</th>
                                                        <th>Направление</th>
                                                        <th>Менеджер</th>
                                                        <th>Телефон</th>
                                                        <th>Эл. почта</th>
                                                        <th>Действия</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# IsClientsInlineEditRow(Eval("Id")) ? "crm-inline-edit-row" : string.Empty %>">
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'><%#: Eval("Name") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineClientNameTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Name") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'><%#: Eval("Direction") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineClientDirectionTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Direction") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'><%#: Eval("Manager") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineClientManagerTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Manager") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'><%#: Eval("PhoneNumber") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineClientPhoneTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("PhoneNumber") %>' />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td>
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'><%#: Eval("Email") %></asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:TextBox ID="InlineClientEmailTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Email") %>' TextMode="Email" />
                                                    </asp:PlaceHolder>
                                                </td>
                                                <td class="crm-actions-cell">
                                                    <asp:PlaceHolder runat="server" Visible='<%# !IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="EditClientButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                        <asp:LinkButton ID="DeleteClientButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этого клиента?');">Удалить</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# IsClientsInlineEditRow(Eval("Id")) %>'>
                                                        <asp:LinkButton ID="SaveInlineClientButton" runat="server" CssClass="btn btn-xs btn-primary" CommandName="SaveInlineItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Сохранить</asp:LinkButton>
                                                        <asp:LinkButton ID="CancelInlineClientButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="CancelInlineEdit" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Отмена</asp:LinkButton>
                                                    </asp:PlaceHolder>
                                                </td>
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
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("cars") %>">
                <div class="row">
                    <div class="col-md-12">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading crm-panel-heading">
                                <h2 class="panel-title">Автомобили</h2>
                                <span class="text-muted">В списке показаны только ключевые поля. Нажмите на строку, чтобы открыть полную карточку автомобиля.</span>
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="CarsEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет автомобилей, созданных из закрытых заявок.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="CarsRepeater" runat="server" OnItemCommand="CarsRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>Год</th>
                                                        <th>Марка</th>
                                                        <th>Модель</th>
                                                        <th>Локация</th>
                                                        <th>Дата создания</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# GetCarRowCss(Eval("Id")) %>" onclick="document.getElementById('<%# ((LinkButton)Container.FindControl("SelectCarButton")).ClientID %>').click();">
                                                <td>
                                                    <asp:LinkButton ID="SelectCarButton" runat="server" CssClass="crm-hidden-row-button" Text="Открыть карточку" CommandName="SelectItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" TabIndex="-1" />
                                                    <%#: FormatCarGridText(Eval("Year")) %>
                                                </td>
                                                <td><%#: FormatCarGridText(Eval("Brand")) %></td>
                                                <td><%#: FormatCarGridText(Eval("Model")) %></td>
                                                <td><%#: FormatCarGridText(Eval("Location")) %></td>
                                                <td><%#: FormatInquiryTimestamp(Eval("CreatedAtUtc")) %></td>
                                            </tr>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                                </tbody>
                                            </table>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>

                                <asp:Panel ID="CarsDetailPanel" runat="server" CssClass="crm-car-detail-panel" Visible="false">
                                    <asp:Literal ID="CarsDetailLiteral" runat="server" />
                                </asp:Panel>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("settings") %>">
                <div class="row">
                    <div class="col-md-6">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Настройки CRM</h2>
                            </div>
                            <div class="panel-body">
                                <p class="text-muted">Здесь можно менять бренд, доступ, справочники статусов и ограничения на загрузку файлов без правки кода.</p>

                                <div class="form-group">
                                    <label for="<%= SettingsCompanyNameTextBox.ClientID %>">Название компании</label>
                                    <asp:TextBox ID="SettingsCompanyNameTextBox" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Используется в шапке сайта, если логотип не загружен.</small>
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsNewRoleTextBox.ClientID %>">Роли с доступом в CRM</label>
                                    <div class="input-group crm-settings-editor-input">
                                        <asp:TextBox ID="SettingsNewRoleTextBox" runat="server" CssClass="form-control" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="AddSettingsRoleButton" runat="server" CssClass="btn btn-default" Text="Добавить" CausesValidation="false" OnClick="AddSettingsRoleButton_Click" />
                                        </span>
                                    </div>
                                    <small class="text-muted crm-form-hint">Указывайте по одной роли в строке или через запятую. Должна остаться хотя бы одна ваша текущая роль.</small>
                                    <asp:Repeater ID="SettingsRolesRepeater" runat="server" OnItemCommand="SettingsRolesRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <div class="crm-settings-token-list">
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <div class="crm-settings-token-item">
                                                <span class="crm-settings-token-text"><%#: Container.DataItem %></span>
                                                <asp:LinkButton ID="RemoveSettingsRoleButton" runat="server" CssClass="btn btn-link btn-xs" Text="Удалить" CausesValidation="false" CommandName="RemoveItem" CommandArgument='<%# Container.DataItem %>' />
                                            </div>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            </div>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsNewTripStatusTextBox.ClientID %>">Статусы рейсов</label>
                                    <div class="input-group crm-settings-editor-input">
                                        <asp:TextBox ID="SettingsNewTripStatusTextBox" runat="server" CssClass="form-control" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="AddSettingsTripStatusButton" runat="server" CssClass="btn btn-default" Text="Добавить" CausesValidation="false" OnClick="AddSettingsTripStatusButton_Click" />
                                        </span>
                                    </div>
                                    <small class="text-muted crm-form-hint">Каждый статус с новой строки. Эти значения будут доступны при создании и редактировании рейсов.</small>
                                    <asp:Repeater ID="SettingsTripStatusesRepeater" runat="server" OnItemCommand="SettingsTripStatusesRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <div class="crm-settings-token-list">
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <div class="crm-settings-token-item">
                                                <span class="crm-settings-token-text"><%#: Container.DataItem %></span>
                                                <asp:LinkButton ID="RemoveSettingsTripStatusButton" runat="server" CssClass="btn btn-link btn-xs" Text="Удалить" CausesValidation="false" CommandName="RemoveItem" CommandArgument='<%# Container.DataItem %>' />
                                            </div>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            </div>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsDefaultTripStatusDropDownList.ClientID %>">Статус нового рейса по умолчанию</label>
                                    <asp:DropDownList ID="SettingsDefaultTripStatusDropDownList" runat="server" CssClass="form-control" />
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsDefaultTabDropDownList.ClientID %>">Вкладка CRM по умолчанию</label>
                                    <asp:DropDownList ID="SettingsDefaultTabDropDownList" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Рейсы" Value="trips" />
                                        <asp:ListItem Text="Водители" Value="drivers" />
                                        <asp:ListItem Text="Автопарк" Value="fleet" />
                                        <asp:ListItem Text="Автомобили" Value="cars" />
                                        <asp:ListItem Text="Клиенты" Value="clients" />
                                        <asp:ListItem Text="Заявки" Value="inquiries" />
                                        <asp:ListItem Text="Настройки" Value="settings" />
                                    </asp:DropDownList>
                                </div>

                                <div class="checkbox">
                                    <label>
                                        <asp:CheckBox ID="SettingsRequireUniqueTripNumbersCheckBox" runat="server" />
                                        Требовать уникальный номер рейса
                                    </label>
                                </div>

                                <div class="checkbox">
                                    <label>
                                        <asp:CheckBox ID="SettingsValidatePrepaymentCheckBox" runat="server" />
                                        Проверять, что предоплата не превышает фрахт
                                    </label>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Файлы и бренд</h2>
                            </div>
                            <div class="panel-body">
                                <div class="form-group">
                                    <label for="<%= SettingsLogoUpload.ClientID %>">Логотип</label>
                                    <asp:FileUpload ID="SettingsLogoUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Если файл не выбран, текущий логотип останется без изменений. Большие логотипы автоматически уменьшаются при загрузке.</small>
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsAllowedDocumentExtensionsTextBox.ClientID %>">Разрешенные расширения документов</label>
                                    <asp:TextBox ID="SettingsAllowedDocumentExtensionsTextBox" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" />
                                    <small class="text-muted crm-form-hint">Например: .pdf, .png, .jpg. Можно указывать по одному значению в строке или через запятую.</small>
                                </div>

                                <div class="form-group">
                                    <label for="<%= SettingsAllowedLogoExtensionsTextBox.ClientID %>">Разрешенные расширения логотипа</label>
                                    <asp:TextBox ID="SettingsAllowedLogoExtensionsTextBox" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" />
                                    <small class="text-muted crm-form-hint">Поддерживаются только форматы изображений, которые сайт умеет показать в шапке.</small>
                                </div>

                                <div class="form-group">
                                    <asp:Button ID="SaveSettingsButton" runat="server" CssClass="btn btn-primary btn-block" Text="Сохранить настройки" CausesValidation="false" OnClick="SaveSettingsButton_Click" />
                                </div>
                            </div>
                        </div>

                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Импорт и экспорт</h2>
                            </div>
                            <div class="panel-body">
                                <div class="form-group">
                                    <label for="<%= SettingsImportJsonUpload.ClientID %>">Импорт настроек из JSON</label>
                                    <asp:FileUpload ID="SettingsImportJsonUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Импорт обновит CRM-настройки, но не заменит текущий логотип и не перенесет путь к нему между окружениями.</small>
                                </div>

                                <div class="row crm-settings-exchange-actions">
                                    <div class="col-sm-6">
                                        <asp:Button ID="ImportSettingsButton" runat="server" CssClass="btn btn-default btn-block" Text="Импортировать JSON" CausesValidation="false" OnClick="ImportSettingsButton_Click" />
                                    </div>
                                    <div class="col-sm-6">
                                        <asp:Button ID="ExportSettingsButton" runat="server" CssClass="btn btn-default btn-block" Text="Скачать JSON" CausesValidation="false" OnClick="ExportSettingsButton_Click" />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Текущий бренд</h2>
                            </div>
                            <div class="panel-body">
                                <p><strong>Компания:</strong> <asp:Literal ID="SettingsCurrentCompanyNameLiteral" runat="server" /></p>

                                <asp:Panel ID="SettingsCurrentLogoPanel" runat="server" Visible="false">
                                    <a id="SettingsCurrentLogoLink" runat="server" target="_blank" class="crm-logo-preview-link">
                                        <asp:Image ID="SettingsLogoPreviewImage" runat="server" CssClass="crm-logo-preview-image" AlternateText="Текущий логотип" />
                                    </a>
                                    <p class="text-muted crm-settings-caption">Нажмите на изображение, чтобы открыть логотип отдельно.</p>
                                </asp:Panel>

                                <asp:Panel ID="SettingsNoLogoPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пользовательский логотип еще не загружен. Сейчас в шапке показывается текст GLC EXPRESS.
                                </asp:Panel>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row crm-settings-secondary-row">
                    <div class="col-md-12">
                        <div class="panel panel-default crm-form-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Контент главной страницы</h2>
                            </div>
                            <div class="panel-body">
                                <p class="text-muted">Отзывы, партнёры и контакты на home-странице теперь читаются из этих настроек.</p>

                                <div class="row">
                                    <div class="col-sm-4">
                                        <div class="form-group">
                                            <label for="<%= SettingsHomeContentLanguageDropDownList.ClientID %>">Язык public-контента</label>
                                            <asp:DropDownList ID="SettingsHomeContentLanguageDropDownList" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="SettingsHomeContentLanguageDropDownList_SelectedIndexChanged">
                                                <asp:ListItem Value="ru" Text="Русский" />
                                                <asp:ListItem Value="en" Text="English" />
                                                <asp:ListItem Value="ge" Text="ქართული" />
                                            </asp:DropDownList>
                                        </div>
                                    </div>
                                    <div class="col-sm-8">
                                        <p class="text-muted crm-form-hint">Переключайте язык, чтобы редактировать отдельные версии партнёров, отзывов, адреса и часов работы для RU, EN и GE.</p>
                                    </div>
                                </div>

                                <div class="row">
                                    <div class="col-sm-6">
                                        <div class="form-group">
                                            <label for="<%= SettingsHomeContactAddressTextBox.ClientID %>">Адрес</label>
                                            <asp:TextBox ID="SettingsHomeContactAddressTextBox" runat="server" CssClass="form-control" />
                                        </div>
                                    </div>
                                    <div class="col-sm-6">
                                        <div class="form-group">
                                            <label for="<%= SettingsHomeContactPhoneTextBox.ClientID %>">Телефон</label>
                                            <asp:TextBox ID="SettingsHomeContactPhoneTextBox" runat="server" CssClass="form-control" />
                                        </div>
                                    </div>
                                </div>

                                <div class="row">
                                    <div class="col-sm-6">
                                        <div class="form-group">
                                            <label for="<%= SettingsHomeContactHoursTextBox.ClientID %>">Часы работы</label>
                                            <asp:TextBox ID="SettingsHomeContactHoursTextBox" runat="server" CssClass="form-control" />
                                        </div>
                                    </div>
                                    <div class="col-sm-6">
                                        <div class="form-group">
                                            <label for="<%= SettingsHomeContactWhatsAppUrlTextBox.ClientID %>">WhatsApp URL</label>
                                            <asp:TextBox ID="SettingsHomeContactWhatsAppUrlTextBox" runat="server" CssClass="form-control" />
                                            <small class="text-muted crm-form-hint">Например: https://wa.me/995577115757</small>
                                        </div>
                                    </div>
                                </div>

                                <hr class="crm-settings-divider" />

                                <div class="form-group">
                                    <label for="<%= SettingsNewHomePartnerTextBox.ClientID %>">Наши партнёры</label>
                                    <div class="input-group crm-settings-editor-input">
                                        <asp:TextBox ID="SettingsNewHomePartnerTextBox" runat="server" CssClass="form-control" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="AddSettingsHomePartnerButton" runat="server" CssClass="btn btn-default" Text="Добавить" CausesValidation="false" OnClick="AddSettingsHomePartnerButton_Click" />
                                        </span>
                                    </div>
                                    <asp:Repeater ID="SettingsHomePartnersRepeater" runat="server" OnItemCommand="SettingsHomePartnersRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <div class="crm-settings-token-list">
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <div class="crm-settings-token-item">
                                                <span class="crm-settings-token-text"><%#: Container.DataItem %></span>
                                                <asp:LinkButton ID="RemoveSettingsHomePartnerButton" runat="server" CssClass="btn btn-link btn-xs" Text="Удалить" CausesValidation="false" CommandName="RemoveItem" CommandArgument='<%# Container.DataItem %>' />
                                            </div>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            </div>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>

                                <hr class="crm-settings-divider" />

                                <div class="form-group">
                                    <label for="<%= SettingsNewHomeReviewQuoteTextBox.ClientID %>">Отзывы</label>
                                    <asp:TextBox ID="SettingsNewHomeReviewQuoteTextBox" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" />
                                </div>

                                <div class="row">
                                    <div class="col-sm-8">
                                        <div class="form-group">
                                            <label for="<%= SettingsNewHomeReviewAuthorTextBox.ClientID %>">Автор отзыва</label>
                                            <asp:TextBox ID="SettingsNewHomeReviewAuthorTextBox" runat="server" CssClass="form-control" />
                                        </div>
                                    </div>
                                    <div class="col-sm-4">
                                        <div class="form-group crm-settings-action-group">
                                            <label>&nbsp;</label>
                                            <asp:Button ID="AddSettingsHomeReviewButton" runat="server" CssClass="btn btn-default btn-block" Text="Добавить отзыв" CausesValidation="false" OnClick="AddSettingsHomeReviewButton_Click" />
                                        </div>
                                    </div>
                                </div>

                                <asp:Repeater ID="SettingsHomeReviewsRepeater" runat="server" OnItemCommand="SettingsHomeReviewsRepeater_ItemCommand">
                                    <ItemTemplate>
                                        <div class="crm-settings-review-card">
                                            <p class="crm-settings-review-quote">&laquo;<%#: Eval("Quote") %>&raquo;</p>
                                            <div class="crm-settings-review-footer">
                                                <strong><%#: Eval("Author") %></strong>
                                                <asp:LinkButton ID="RemoveSettingsHomeReviewButton" runat="server" CssClass="btn btn-link btn-xs" Text="Удалить" CausesValidation="false" CommandName="RemoveItem" CommandArgument='<%# Container.ItemIndex %>' />
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="<%= GetPaneCss("inquiries") %>">
                <div class="row">
                    <div class="col-md-12">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Заявки с главной</h2>
                            </div>
                            <div class="panel-body">
                                <p class="text-muted">Новые публичные заявки с home-страницы сохраняются в CRM и выделяют вкладку, пока у них остается статус &laquo;Новая&raquo;.</p>

                                <div class="panel panel-default">
                                    <div class="panel-heading">
                                        <h3 class="panel-title">Фильтры по заявкам</h3>
                                    </div>
                                    <div class="panel-body">
                                        <div class="row">
                                            <div class="col-sm-6">
                                                <div class="form-group">
                                                    <label for="<%= SettingsLeadFilterPhoneTextBox.ClientID %>">Телефон</label>
                                                    <asp:TextBox ID="SettingsLeadFilterPhoneTextBox" runat="server" CssClass="form-control input-sm" />
                                                </div>
                                            </div>
                                            <div class="col-sm-6">
                                                <div class="form-group">
                                                    <label for="<%= SettingsLeadFilterMessengerTextBox.ClientID %>">Мессенджер</label>
                                                    <asp:TextBox ID="SettingsLeadFilterMessengerTextBox" runat="server" CssClass="form-control input-sm" />
                                                </div>
                                            </div>
                                        </div>
                                        <div class="row">
                                            <div class="col-sm-6">
                                                <div class="form-group">
                                                    <label for="<%= SettingsLeadFilterDirectionTextBox.ClientID %>">Направление</label>
                                                    <asp:TextBox ID="SettingsLeadFilterDirectionTextBox" runat="server" CssClass="form-control input-sm" />
                                                </div>
                                            </div>
                                            <div class="col-sm-6">
                                                <div class="form-group">
                                                    <label for="<%= SettingsLeadFilterCargoTypeDropDownList.ClientID %>">Тип груза</label>
                                                    <asp:DropDownList ID="SettingsLeadFilterCargoTypeDropDownList" runat="server" CssClass="form-control input-sm">
                                                        <asp:ListItem Text="Все типы" Value="" />
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
                                            </div>
                                        </div>
                                        <div class="form-group">
                                            <label for="<%= SettingsLeadFilterCommentTextBox.ClientID %>">Комментарий клиента</label>
                                            <asp:TextBox ID="SettingsLeadFilterCommentTextBox" runat="server" CssClass="form-control input-sm" />
                                        </div>
                                        <div class="crm-settings-lead-actions">
                                            <asp:Button ID="ApplySettingsLeadFiltersButton" runat="server" CssClass="btn btn-default btn-sm" Text="Применить фильтры" CausesValidation="false" OnClick="ApplySettingsLeadFiltersButton_Click" />
                                            <asp:Button ID="ResetSettingsLeadFiltersButton" runat="server" CssClass="btn btn-link btn-sm" Text="Сбросить" CausesValidation="false" OnClick="ResetSettingsLeadFiltersButton_Click" />
                                        </div>
                                    </div>
                                </div>

                                <div class="table-responsive">
                                    <asp:Repeater ID="SettingsInquiryListRepeater" runat="server" OnItemCommand="SettingsInquiryListRepeater_ItemCommand">
                                        <HeaderTemplate>
                                            <table class="table table-striped table-bordered crm-table">
                                                <thead>
                                                    <tr>
                                                        <th>Дата</th>
                                                        <th>Имя</th>
                                                        <th>Email</th>
                                                        <th>Телефон</th>
                                                        <th>Статус</th>
                                                        <th>Менеджер</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <tr class="<%# GetInquiryRowCss(Eval("Id")) %>" onclick="document.getElementById('<%# ((LinkButton)Container.FindControl("SelectInquiryButton")).ClientID %>').click();">
                                                <td>
                                                    <asp:LinkButton ID="SelectInquiryButton" runat="server" CssClass="crm-hidden-row-button" Text="Открыть карточку" CommandName="SelectItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" TabIndex="-1" />
                                                    <%#: FormatInquiryTimestamp(Eval("CreatedAtUtc")) %>
                                                </td>
                                                <td><%#: FormatCarGridText(Eval("Name")) %></td>
                                                <td><%#: FormatCarGridText(Eval("Email")) %></td>
                                                <td><%#: FormatCarGridText(Eval("Phone")) %></td>
                                                <td><%#: FormatLeadStatus(Eval("Status")) %></td>
                                                <td><%#: FormatLeadManager(Eval("AssignedManager")) %></td>
                                            </tr>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                                </tbody>
                                            </table>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>

                                <asp:Panel ID="SettingsInquiryDetailPanel" runat="server" CssClass="crm-inquiry-detail-panel" Visible="false">
                                    <div class="crm-detail-panel-heading">
                                        <h3 class="panel-title">Карточка заявки</h3>
                                        <asp:Button ID="CloseSettingsInquiryDetailButton" runat="server" CssClass="btn btn-default btn-sm" Text="Закрыть карточку" CausesValidation="false" OnClick="CloseSettingsInquiryDetailButton_Click" />
                                    </div>

                                    <asp:Repeater ID="SettingsRecentInquiriesRepeater" runat="server" OnItemCommand="SettingsRecentInquiriesRepeater_ItemCommand">
                                        <ItemTemplate>
                                            <div class="crm-settings-lead-card">
                                                <div class="crm-settings-lead-header">
                                                    <strong><%#: Eval("Name") %></strong>
                                                    <span><%# FormatInquiryTimestamp(Eval("CreatedAtUtc")) %></span>
                                                </div>
                                                <p><a href='mailto:<%#: Eval("Email") %>'><%#: Eval("Email") %></a></p>
                                                <div class="well well-sm">
                                                    <div class="row">
                                                        <div class="col-sm-6"><strong>Телефон:</strong> <%# FormatLeadText(Eval("Phone"), "Не указан") %></div>
                                                        <div class="col-sm-6"><strong>Мессенджер:</strong> <%# FormatLeadText(Eval("Messenger"), "Не указан") %></div>
                                                    </div>
                                                    <div class="row">
                                                        <div class="col-sm-6"><strong>Направление:</strong> <%# FormatLeadText(Eval("Direction"), "Не указано") %></div>
                                                        <div class="col-sm-6"><strong>Тип груза:</strong> <%# FormatLeadText(Eval("CargoType"), "Не указан") %></div>
                                                    </div>
                                                    <div class="form-group" style="margin-bottom: 0; margin-top: 10px;">
                                                        <label>Комментарий клиента</label>
                                                        <div class="form-control" style="height: auto; min-height: 68px; white-space: pre-line;"><%# FormatLeadMultilineText(Eval("ClientComment"), "Комментарий не указан") %></div>
                                                    </div>
                                                </div>
                                                <div class="row">
                                                    <div class="col-sm-6">
                                                        <div class="form-group">
                                                            <label for='<%# ((DropDownList)Container.FindControl("SettingsLeadStatusDropDownList")).ClientID %>'>Статус</label>
                                                            <asp:DropDownList ID="SettingsLeadStatusDropDownList" runat="server" CssClass="form-control input-sm" />
                                                        </div>
                                                    </div>
                                                    <div class="col-sm-6">
                                                        <div class="form-group">
                                                            <label for='<%# ((TextBox)Container.FindControl("SettingsLeadSourceTextBox")).ClientID %>'>Источник</label>
                                                            <asp:TextBox ID="SettingsLeadSourceTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("Source") %>' />
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="form-group">
                                                    <label for='<%# ((TextBox)Container.FindControl("SettingsLeadAssignedManagerTextBox")).ClientID %>'>Ответственный менеджер</label>
                                                    <asp:TextBox ID="SettingsLeadAssignedManagerTextBox" runat="server" CssClass="form-control input-sm" Text='<%# Bind("AssignedManager") %>' />
                                                </div>
                                                <div class="crm-settings-lead-attachment">
                                                    <asp:Literal ID="SettingsLeadAttachmentLiteral" runat="server" Text='<%# FormatDocumentLink(Eval("AttachmentPath")) %>' />
                                                </div>
                                                <div class="panel panel-default" style="margin-top: 12px; margin-bottom: 12px;">
                                                    <div class="panel-heading">
                                                        <h3 class="panel-title">Внутренние комментарии</h3>
                                                    </div>
                                                    <div class="panel-body">
                                                        <div class="small"><%# FormatLeadComments(Eval("Comments")) %></div>
                                                        <div class="form-group" style="margin-top: 12px; margin-bottom: 10px;">
                                                            <label for='<%# ((TextBox)Container.FindControl("SettingsLeadNewCommentTextBox")).ClientID %>'>Новый комментарий</label>
                                                            <asp:TextBox ID="SettingsLeadNewCommentTextBox" runat="server" CssClass="form-control input-sm" TextMode="MultiLine" Rows="3" />
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="crm-settings-lead-actions">
                                                    <asp:Button ID="SaveSettingsLeadButton" runat="server" CssClass="btn btn-default btn-sm" Text="Сохранить lead" CausesValidation="false" CommandName="SaveLead" CommandArgument='<%# Eval("Id") %>' />
                                                    <asp:Button ID="AddSettingsLeadCommentButton" runat="server" CssClass="btn btn-primary btn-sm" Text="Добавить комментарий" CausesValidation="false" CommandName="AddLeadComment" CommandArgument='<%# Eval("Id") %>' />
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>

                                <asp:Panel ID="SettingsRecentInquiriesEmptyPanel" runat="server" CssClass="alert alert-info" Visible="false">
                                    Пока нет заявок с главной страницы.
                                </asp:Panel>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
