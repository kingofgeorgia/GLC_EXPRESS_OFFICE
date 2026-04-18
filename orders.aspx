<%@ Page Title="CRM" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="orders.aspx.cs" Inherits="GLC_EXPRESS.orders" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="crm-page">
        <asp:Panel ID="PageAlertPanel" runat="server" Visible="false" CssClass="alert">
            <asp:Literal ID="PageAlertLiteral" runat="server" />
        </asp:Panel>

        <div class="page-header crm-header">
            <h1>CRM</h1>
            <p class="text-muted">Управление рейсами, водителями, автопарком и клиентами.</p>
        </div>

        <div class="alert alert-info crm-user-banner">
            Вы вошли как <strong><%: Context.User.Identity.Name %></strong>.
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
            <li class="<%= GetTabCss("clients") %>">
                <asp:LinkButton ID="ClientsTabButton" runat="server" CommandArgument="clients" OnClick="TabLinkButton_Click" CausesValidation="false">Клиенты</asp:LinkButton>
            </li>
        </ul>

        <div class="tab-content crm-tab-content">
            <div class="<%= GetPaneCss("trips") %>">
                <div class="row">
                    <div class="col-md-4">
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
                                    <asp:DropDownList ID="TripsStatusDropDownList" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Новый" Value="Новый" />
                                        <asp:ListItem Text="В работе" Value="В работе" />
                                        <asp:ListItem Text="Завершен" Value="Завершен" />
                                        <asp:ListItem Text="Отложен" Value="Отложен" />
                                    </asp:DropDownList>
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
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsFreightTextBox.ClientID %>">Фрахт</label>
                                    <asp:TextBox ID="TripsFreightTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsFreightRequiredValidator" runat="server" ControlToValidate="TripsFreightTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите фрахт." />
                                </div>

                                <div class="form-group">
                                    <label for="<%= TripsPrepaymentTextBox.ClientID %>">Предоплата</label>
                                    <asp:TextBox ID="TripsPrepaymentTextBox" runat="server" CssClass="form-control" />
                                    <asp:RequiredFieldValidator ID="TripsPrepaymentRequiredValidator" runat="server" ControlToValidate="TripsPrepaymentTextBox" ValidationGroup="TripsGroup" CssClass="text-danger" Display="Dynamic" ErrorMessage="Укажите предоплату." />
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddTripButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="TripsGroup" OnClick="AddTripButton_Click" />
                                    <asp:Button ID="TripsCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="TripsCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-8">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Список рейсов</h2>
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="TripsEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного рейса.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="TripsRepeater" runat="server" OnItemCommand="TripsRepeater_ItemCommand">
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
                                            <tr>
                                                <td><%#: Eval("Number") %></td>
                                                <td><%#: Eval("ClientName") %></td>
                                                <td><span class="label label-default"><%#: Eval("Status") %></span></td>
                                                <td><%#: Eval("Country") %></td>
                                                <td><%#: Eval("VehicleName") %></td>
                                                <td><%#: Eval("DriverName") %></td>
                                                <td><%#: FormatDateValue(Eval("StartDate")) %></td>
                                                <td><%#: FormatDateValue(Eval("EndDate")) %></td>
                                                <td><%#: Eval("Freight") %></td>
                                                <td><%#: Eval("Prepayment") %></td>
                                                <td class="crm-actions-cell">
                                                    <asp:LinkButton ID="EditTripButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                    <asp:LinkButton ID="DeleteTripButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этот рейс?');">Удалить</asp:LinkButton>
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
                    <div class="col-md-5">
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
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится.</small>
                                </div>

                                <div class="form-group">
                                    <label for="<%= DriversLicenseUpload.ClientID %>">Скан копия водительского удостоверения</label>
                                    <asp:FileUpload ID="DriversLicenseUpload" runat="server" CssClass="form-control" />
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится.</small>
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddDriverButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="DriversGroup" OnClick="AddDriverButton_Click" />
                                    <asp:Button ID="DriversCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="DriversCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-7">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Список водителей</h2>
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="DriversEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного водителя.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="DriversRepeater" runat="server" OnItemCommand="DriversRepeater_ItemCommand">
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
                                            <tr>
                                                <td><%#: Eval("FullName") %></td>
                                                <td><%#: FormatDateValue(Eval("BirthDate")) %></td>
                                                <td><%#: Eval("PhoneNumber") %></td>
                                                <td><%#: Eval("Address") %></td>
                                                <td><%# FormatDocumentLink(Eval("PassportScanPath")) %></td>
                                                <td><%# FormatDocumentLink(Eval("LicenseScanPath")) %></td>
                                                <td class="crm-actions-cell">
                                                    <asp:LinkButton ID="EditDriverButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                    <asp:LinkButton ID="DeleteDriverButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этого водителя?');">Удалить</asp:LinkButton>
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
                    <div class="col-md-5">
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
                                    <small class="text-muted crm-form-hint">Если новый файл не выбрать, текущий скан сохранится.</small>
                                </div>

                                <div class="crm-form-actions">
                                    <asp:Button ID="AddFleetButton" runat="server" CssClass="btn btn-primary btn-block" ValidationGroup="FleetGroup" OnClick="AddFleetButton_Click" />
                                    <asp:Button ID="FleetCancelEditButton" runat="server" CssClass="btn btn-default btn-block" Text="Отменить редактирование" CausesValidation="false" Visible="false" OnClick="FleetCancelEditButton_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-7">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Автопарк</h2>
                            </div>
                            <div class="panel-body">
                                <asp:Panel ID="FleetEmptyPanel" runat="server" CssClass="alert alert-warning" Visible="false">
                                    Пока нет ни одного автомобиля.
                                </asp:Panel>
                                <div class="table-responsive">
                                    <asp:Repeater ID="FleetRepeater" runat="server" OnItemCommand="FleetRepeater_ItemCommand">
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
                                            <tr>
                                                <td><%#: Eval("CarBrand") %></td>
                                                <td><%#: Eval("CarModel") %></td>
                                                <td><%#: Eval("LicensePlate") %></td>
                                                <td><%#: Eval("VinCode") %></td>
                                                <td><%#: Eval("TrailerBrand") %></td>
                                                <td><%#: Eval("TrailerModel") %></td>
                                                <td><%#: Eval("TrailerLicensePlate") %></td>
                                                <td><%#: FormatDriverNames(Eval("AssignedDriverNames")) %></td>
                                                <td><%# FormatDocumentLink(Eval("DocumentsScanPath")) %></td>
                                                <td class="crm-actions-cell">
                                                    <asp:LinkButton ID="EditFleetButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                    <asp:LinkButton ID="DeleteFleetButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этот автомобиль?');">Удалить</asp:LinkButton>
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
                    <div class="col-md-4">
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
                    <div class="col-md-8">
                        <div class="panel panel-default crm-list-panel">
                            <div class="panel-heading">
                                <h2 class="panel-title">Список клиентов</h2>
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
                                            <tr>
                                                <td><%#: Eval("Name") %></td>
                                                <td><%#: Eval("Direction") %></td>
                                                <td><%#: Eval("Manager") %></td>
                                                <td><%#: Eval("PhoneNumber") %></td>
                                                <td><%#: Eval("Email") %></td>
                                                <td class="crm-actions-cell">
                                                    <asp:LinkButton ID="EditClientButton" runat="server" CssClass="btn btn-xs btn-default" CommandName="EditItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Изменить</asp:LinkButton>
                                                    <asp:LinkButton ID="DeleteClientButton" runat="server" CssClass="btn btn-xs btn-danger" CommandName="DeleteItem" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" OnClientClick="return confirm('Удалить этого клиента?');">Удалить</asp:LinkButton>
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
        </div>
    </div>
</asp:Content>
