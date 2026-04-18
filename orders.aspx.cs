using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class orders : Page
    {
        private const string DefaultTab = "trips";

        protected void Page_Load(object sender, EventArgs e)
        {
            EnsureAuthenticated();
            HideMessage();

            if (!IsPostBack)
            {
                ActiveTab = DefaultTab;
                BindAll();
                return;
            }

            ApplyFormState();
        }

        protected void TabLinkButton_Click(object sender, EventArgs e)
        {
            var button = sender as LinkButton;
            ActiveTab = button != null ? button.CommandArgument : DefaultTab;
            BindAll();
        }

        protected void AddTripButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";

            if (!Page.IsValid)
            {
                BindAll();
                return;
            }

            var data = CrmRepository.Load();
            var client = data.Clients.FirstOrDefault(item => StringEquals(item.Id, TripsClientDropDownList.SelectedValue));
            var driver = data.Drivers.FirstOrDefault(item => StringEquals(item.Id, TripsDriverDropDownList.SelectedValue));
            var vehicle = data.FleetVehicles.FirstOrDefault(item => StringEquals(item.Id, TripsVehicleDropDownList.SelectedValue));

            if (client == null || driver == null || vehicle == null)
            {
                ShowMessage("Для создания рейса сначала добавьте клиента, водителя и автомобиль.", "warning");
                BindAll(data);
                return;
            }

            var existingTrip = FindById(data.Trips, TripsEditingId);
            var isEditing = existingTrip != null;

            if (IsEditing(TripsEditingId) && existingTrip == null)
            {
                ClearTripsForm();
                ClearTripsEditState();
                ShowMessage("Редактируемый рейс не найден.", "warning");
                BindAll(data);
                return;
            }

            if (!isEditing)
            {
                existingTrip = new TripRecord();
                data.Trips.Insert(0, existingTrip);
            }

            existingTrip.Number = TripsNumberTextBox.Text.Trim();
            existingTrip.ClientId = client.Id;
            existingTrip.ClientName = client.Name;
            existingTrip.Status = TripsStatusDropDownList.SelectedValue;
            existingTrip.Country = TripsCountryTextBox.Text.Trim();
            existingTrip.VehicleId = vehicle.Id;
            existingTrip.VehicleName = BuildVehicleName(vehicle);
            existingTrip.DriverId = driver.Id;
            existingTrip.DriverName = driver.FullName;
            existingTrip.StartDate = TripsStartDateTextBox.Text.Trim();
            existingTrip.EndDate = TripsEndDateTextBox.Text.Trim();
            existingTrip.Freight = TripsFreightTextBox.Text.Trim();
            existingTrip.Prepayment = TripsPrepaymentTextBox.Text.Trim();

            CrmRepository.Save(data);
            ClearTripsForm();
            ClearTripsEditState();
            ShowMessage(isEditing ? "Рейс обновлен." : "Рейс успешно добавлен.", "success");
            BindAll();
        }

        protected void AddDriverButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";

            if (!Page.IsValid)
            {
                BindAll();
                return;
            }

            var fullName = DriversFullNameTextBox.Text.Trim();
            var data = CrmRepository.Load();
            var existingDriver = FindById(data.Drivers, DriversEditingId);
            var isEditing = existingDriver != null;

            if (IsEditing(DriversEditingId) && existingDriver == null)
            {
                ClearDriversForm();
                ClearDriversEditState();
                ShowMessage("Редактируемый водитель не найден.", "warning");
                BindAll(data);
                return;
            }

            if (!isEditing)
            {
                existingDriver = new DriverRecord();
                data.Drivers.Insert(0, existingDriver);
            }

            var previousName = existingDriver.FullName;
            var passportPath = SaveUploadedFile(DriversPassportUpload, "Uploads/Drivers/Passports", fullName + "_passport");
            var licensePath = SaveUploadedFile(DriversLicenseUpload, "Uploads/Drivers/Licenses", fullName + "_license");

            existingDriver.FullName = fullName;
            existingDriver.BirthDate = DriversBirthDateTextBox.Text.Trim();
            existingDriver.PhoneNumber = DriversPhoneTextBox.Text.Trim();
            existingDriver.Address = DriversAddressTextBox.Text.Trim();
            existingDriver.PassportScanPath = string.IsNullOrWhiteSpace(passportPath) ? existingDriver.PassportScanPath : passportPath;
            existingDriver.LicenseScanPath = string.IsNullOrWhiteSpace(licensePath) ? existingDriver.LicenseScanPath : licensePath;

            if (isEditing)
            {
                SyncDriverReferences(data, existingDriver, previousName);
            }

            CrmRepository.Save(data);
            ClearDriversForm();
            ClearDriversEditState();
            ShowMessage(isEditing ? "Водитель обновлен." : "Водитель успешно добавлен.", "success");
            BindAll();
        }

        protected void AddFleetButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";

            if (!Page.IsValid)
            {
                BindAll();
                return;
            }

            var data = CrmRepository.Load();
            var selectedDriverIds = GetSelectedValues(FleetDriversCheckBoxList);
            var selectedDrivers = data.Drivers
                .Where(driver => selectedDriverIds.Contains(driver.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var existingVehicle = FindById(data.FleetVehicles, FleetEditingId);
            var isEditing = existingVehicle != null;

            if (IsEditing(FleetEditingId) && existingVehicle == null)
            {
                ClearFleetForm();
                ClearFleetEditState();
                ShowMessage("Редактируемый автомобиль не найден.", "warning");
                BindAll(data);
                return;
            }

            if (!isEditing)
            {
                existingVehicle = new FleetVehicleRecord();
                data.FleetVehicles.Insert(0, existingVehicle);
            }

            var previousVehicleName = BuildVehicleName(existingVehicle);
            var documentsPath = SaveUploadedFile(FleetDocumentsUpload, "Uploads/Fleet/Documents", FleetLicensePlateTextBox.Text.Trim());

            existingVehicle.CarBrand = FleetCarBrandTextBox.Text.Trim();
            existingVehicle.CarModel = FleetCarModelTextBox.Text.Trim();
            existingVehicle.LicensePlate = FleetLicensePlateTextBox.Text.Trim();
            existingVehicle.VinCode = FleetVinTextBox.Text.Trim();
            existingVehicle.TrailerBrand = FleetTrailerBrandTextBox.Text.Trim();
            existingVehicle.TrailerModel = FleetTrailerModelTextBox.Text.Trim();
            existingVehicle.TrailerLicensePlate = FleetTrailerLicensePlateTextBox.Text.Trim();
            existingVehicle.AssignedDriverIds = selectedDrivers.Select(driver => driver.Id).ToList();
            existingVehicle.AssignedDriverNames = selectedDrivers.Select(driver => driver.FullName).ToList();
            existingVehicle.DocumentsScanPath = string.IsNullOrWhiteSpace(documentsPath) ? existingVehicle.DocumentsScanPath : documentsPath;

            if (isEditing)
            {
                SyncVehicleReferences(data, existingVehicle, previousVehicleName);
            }

            CrmRepository.Save(data);
            ClearFleetForm();
            ClearFleetEditState();
            ShowMessage(isEditing ? "Автомобиль обновлен." : "Автомобиль успешно добавлен в автопарк.", "success");
            BindAll();
        }

        protected void AddClientButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";

            if (!Page.IsValid)
            {
                BindAll();
                return;
            }

            var data = CrmRepository.Load();
            var existingClient = FindById(data.Clients, ClientsEditingId);
            var isEditing = existingClient != null;

            if (IsEditing(ClientsEditingId) && existingClient == null)
            {
                ClearClientsForm();
                ClearClientsEditState();
                ShowMessage("Редактируемый клиент не найден.", "warning");
                BindAll(data);
                return;
            }

            if (!isEditing)
            {
                existingClient = new ClientRecord();
                data.Clients.Insert(0, existingClient);
            }

            var previousName = existingClient.Name;

            existingClient.Name = ClientsNameTextBox.Text.Trim();
            existingClient.Direction = ClientsDirectionTextBox.Text.Trim();
            existingClient.Manager = ClientsManagerTextBox.Text.Trim();
            existingClient.PhoneNumber = ClientsPhoneTextBox.Text.Trim();
            existingClient.Email = ClientsEmailTextBox.Text.Trim();

            if (isEditing)
            {
                SyncClientReferences(data, existingClient, previousName);
            }

            CrmRepository.Save(data);
            ClearClientsForm();
            ClearClientsEditState();
            ShowMessage(isEditing ? "Клиент обновлен." : "Клиент успешно добавлен.", "success");
            BindAll();
        }

        protected void TripsCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            ClearTripsForm();
            ClearTripsEditState();
            BindAll();
        }

        protected void DriversCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            ClearDriversForm();
            ClearDriversEditState();
            BindAll();
        }

        protected void FleetCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            ClearFleetForm();
            ClearFleetEditState();
            BindAll();
        }

        protected void ClientsCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            ClearClientsForm();
            ClearClientsEditState();
            BindAll();
        }

        protected void TripsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "trips";
            var tripId = Convert.ToString(e.CommandArgument);

            if (string.Equals(e.CommandName, "EditItem", StringComparison.OrdinalIgnoreCase))
            {
                StartTripEdit(tripId);
                return;
            }

            if (string.Equals(e.CommandName, "DeleteItem", StringComparison.OrdinalIgnoreCase))
            {
                DeleteTrip(tripId);
            }
        }

        protected void DriversRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "drivers";
            var driverId = Convert.ToString(e.CommandArgument);

            if (string.Equals(e.CommandName, "EditItem", StringComparison.OrdinalIgnoreCase))
            {
                StartDriverEdit(driverId);
                return;
            }

            if (string.Equals(e.CommandName, "DeleteItem", StringComparison.OrdinalIgnoreCase))
            {
                DeleteDriver(driverId);
            }
        }

        protected void FleetRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "fleet";
            var vehicleId = Convert.ToString(e.CommandArgument);

            if (string.Equals(e.CommandName, "EditItem", StringComparison.OrdinalIgnoreCase))
            {
                StartFleetEdit(vehicleId);
                return;
            }

            if (string.Equals(e.CommandName, "DeleteItem", StringComparison.OrdinalIgnoreCase))
            {
                DeleteVehicle(vehicleId);
            }
        }

        protected void ClientsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "clients";
            var clientId = Convert.ToString(e.CommandArgument);

            if (string.Equals(e.CommandName, "EditItem", StringComparison.OrdinalIgnoreCase))
            {
                StartClientEdit(clientId);
                return;
            }

            if (string.Equals(e.CommandName, "DeleteItem", StringComparison.OrdinalIgnoreCase))
            {
                DeleteClient(clientId);
            }
        }

        protected string GetTabCss(string tabName)
        {
            return string.Equals(ActiveTab, tabName, StringComparison.OrdinalIgnoreCase) ? "active" : string.Empty;
        }

        protected string GetPaneCss(string tabName)
        {
            return string.Equals(ActiveTab, tabName, StringComparison.OrdinalIgnoreCase)
                ? "tab-pane fade in active"
                : "tab-pane fade";
        }

        protected string FormatDateValue(object value)
        {
            var rawValue = Convert.ToString(value);
            DateTime dateValue;

            return DateTime.TryParse(rawValue, out dateValue)
                ? dateValue.ToString("dd.MM.yyyy")
                : rawValue;
        }

        protected string FormatDriverNames(object value)
        {
            var names = value as IEnumerable<string>;

            if (names == null)
            {
                return "Не назначены";
            }

            var filtered = names.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
            return filtered.Count > 0 ? string.Join(", ", filtered) : "Не назначены";
        }

        protected string FormatDocumentLink(object value)
        {
            var path = Convert.ToString(value);

            if (string.IsNullOrWhiteSpace(path))
            {
                return "<span class=\"text-muted\">Файл не загружен</span>";
            }

            var fileName = HttpUtility.HtmlEncode(Path.GetFileName(path));
            var url = ResolveUrl(path);

            return string.Format("<a href=\"{0}\" target=\"_blank\" rel=\"noopener\">{1}</a>", url, fileName);
        }

        private string ActiveTab
        {
            get
            {
                return Convert.ToString(ViewState["ActiveTab"]) ?? DefaultTab;
            }
            set
            {
                ViewState["ActiveTab"] = value;
            }
        }

        private string TripsEditingId
        {
            get
            {
                return TripsEditingIdHiddenField.Value;
            }
            set
            {
                TripsEditingIdHiddenField.Value = value ?? string.Empty;
            }
        }

        private string DriversEditingId
        {
            get
            {
                return DriversEditingIdHiddenField.Value;
            }
            set
            {
                DriversEditingIdHiddenField.Value = value ?? string.Empty;
            }
        }

        private string FleetEditingId
        {
            get
            {
                return FleetEditingIdHiddenField.Value;
            }
            set
            {
                FleetEditingIdHiddenField.Value = value ?? string.Empty;
            }
        }

        private string ClientsEditingId
        {
            get
            {
                return ClientsEditingIdHiddenField.Value;
            }
            set
            {
                ClientsEditingIdHiddenField.Value = value ?? string.Empty;
            }
        }

        private void EnsureAuthenticated()
        {
            if (!Request.IsAuthenticated)
            {
                Response.Redirect(FormsAuthentication.LoginUrl + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        private void BindAll()
        {
            BindAll(CrmRepository.Load());
        }

        private void BindAll(CrmDataStore data)
        {
            BindReferenceData(data);
            BindTrips(data);
            BindDrivers(data);
            BindFleet(data);
            BindClients(data);
            ApplyFormState();
        }

        private void BindReferenceData(CrmDataStore data)
        {
            var selectedClientId = GetSelectedValue(TripsClientDropDownList);
            var selectedDriverId = GetSelectedValue(TripsDriverDropDownList);
            var selectedVehicleId = GetSelectedValue(TripsVehicleDropDownList);
            var selectedFleetDriverIds = GetSelectedValues(FleetDriversCheckBoxList);

            BindClientsDropDownList(data.Clients, selectedClientId);
            BindDriversDropDownList(data.Drivers, selectedDriverId);
            BindVehiclesDropDownList(data.FleetVehicles, selectedVehicleId);
            BindFleetDriversCheckBoxList(data.Drivers, selectedFleetDriverIds);
        }

        private void BindTrips(CrmDataStore data)
        {
            var trips = data.Trips
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            TripsRepeater.DataSource = trips;
            TripsRepeater.DataBind();
            TripsRepeater.Visible = trips.Count > 0;
            TripsEmptyPanel.Visible = trips.Count == 0;
        }

        private void BindDrivers(CrmDataStore data)
        {
            var drivers = data.Drivers
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            DriversRepeater.DataSource = drivers;
            DriversRepeater.DataBind();
            DriversRepeater.Visible = drivers.Count > 0;
            DriversEmptyPanel.Visible = drivers.Count == 0;
        }

        private void BindFleet(CrmDataStore data)
        {
            var vehicles = data.FleetVehicles
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            FleetRepeater.DataSource = vehicles;
            FleetRepeater.DataBind();
            FleetRepeater.Visible = vehicles.Count > 0;
            FleetEmptyPanel.Visible = vehicles.Count == 0;
        }

        private void BindClients(CrmDataStore data)
        {
            var clients = data.Clients
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            ClientsRepeater.DataSource = clients;
            ClientsRepeater.DataBind();
            ClientsRepeater.Visible = clients.Count > 0;
            ClientsEmptyPanel.Visible = clients.Count == 0;
        }

        private void BindClientsDropDownList(IEnumerable<ClientRecord> clients, string selectedValue)
        {
            TripsClientDropDownList.Items.Clear();
            TripsClientDropDownList.Items.Add(new ListItem("Выберите клиента", string.Empty));

            foreach (var client in clients.OrderBy(item => item.Name))
            {
                TripsClientDropDownList.Items.Add(new ListItem(client.Name, client.Id));
            }

            SetSelectedValue(TripsClientDropDownList, selectedValue);
        }

        private void BindDriversDropDownList(IEnumerable<DriverRecord> drivers, string selectedValue)
        {
            TripsDriverDropDownList.Items.Clear();
            TripsDriverDropDownList.Items.Add(new ListItem("Выберите водителя", string.Empty));

            foreach (var driver in drivers.OrderBy(item => item.FullName))
            {
                TripsDriverDropDownList.Items.Add(new ListItem(driver.FullName, driver.Id));
            }

            SetSelectedValue(TripsDriverDropDownList, selectedValue);
        }

        private void BindVehiclesDropDownList(IEnumerable<FleetVehicleRecord> vehicles, string selectedValue)
        {
            TripsVehicleDropDownList.Items.Clear();
            TripsVehicleDropDownList.Items.Add(new ListItem("Выберите автомобиль", string.Empty));

            foreach (var vehicle in vehicles.OrderBy(item => item.CarBrand).ThenBy(item => item.CarModel))
            {
                TripsVehicleDropDownList.Items.Add(new ListItem(BuildVehicleName(vehicle), vehicle.Id));
            }

            SetSelectedValue(TripsVehicleDropDownList, selectedValue);
        }

        private void BindFleetDriversCheckBoxList(IEnumerable<DriverRecord> drivers, IEnumerable<string> selectedValues)
        {
            FleetDriversCheckBoxList.Items.Clear();

            foreach (var driver in drivers.OrderBy(item => item.FullName))
            {
                FleetDriversCheckBoxList.Items.Add(new ListItem(driver.FullName, driver.Id));
            }

            SetSelectedValues(FleetDriversCheckBoxList, selectedValues);
        }

        private void ApplyFormState()
        {
            var isTripsEditing = IsEditing(TripsEditingId);
            var isDriversEditing = IsEditing(DriversEditingId);
            var isFleetEditing = IsEditing(FleetEditingId);
            var isClientsEditing = IsEditing(ClientsEditingId);

            TripsFormTitleLiteral.Text = isTripsEditing ? "Редактировать рейс" : "Новый рейс";
            AddTripButton.Text = isTripsEditing ? "Сохранить рейс" : "Добавить рейс";
            TripsCancelEditButton.Visible = isTripsEditing;

            DriversFormTitleLiteral.Text = isDriversEditing ? "Редактировать водителя" : "Новый водитель";
            AddDriverButton.Text = isDriversEditing ? "Сохранить водителя" : "Добавить водителя";
            DriversCancelEditButton.Visible = isDriversEditing;

            FleetFormTitleLiteral.Text = isFleetEditing ? "Редактировать автомобиль" : "Новый автомобиль";
            AddFleetButton.Text = isFleetEditing ? "Сохранить автомобиль" : "Добавить автомобиль";
            FleetCancelEditButton.Visible = isFleetEditing;

            ClientsFormTitleLiteral.Text = isClientsEditing ? "Редактировать клиента" : "Новый клиент";
            AddClientButton.Text = isClientsEditing ? "Сохранить клиента" : "Добавить клиента";
            ClientsCancelEditButton.Visible = isClientsEditing;
        }

        private void ShowMessage(string message, string type)
        {
            PageAlertPanel.Visible = true;
            PageAlertPanel.CssClass = "alert alert-" + type;
            PageAlertLiteral.Text = HttpUtility.HtmlEncode(message);
        }

        private void HideMessage()
        {
            PageAlertPanel.Visible = false;
            PageAlertLiteral.Text = string.Empty;
        }

        private void StartTripEdit(string tripId)
        {
            var data = CrmRepository.Load();
            var trip = FindById(data.Trips, tripId);

            if (trip == null)
            {
                ShowMessage("Рейс не найден.", "warning");
                BindAll(data);
                return;
            }

            TripsEditingId = trip.Id;
            BindAll(data);
            LoadTripForm(trip, data);
            ApplyFormState();
        }

        private void StartDriverEdit(string driverId)
        {
            var data = CrmRepository.Load();
            var driver = FindById(data.Drivers, driverId);

            if (driver == null)
            {
                ShowMessage("Водитель не найден.", "warning");
                BindAll(data);
                return;
            }

            DriversEditingId = driver.Id;
            BindAll(data);
            DriversFullNameTextBox.Text = driver.FullName;
            DriversBirthDateTextBox.Text = driver.BirthDate;
            DriversPhoneTextBox.Text = driver.PhoneNumber;
            DriversAddressTextBox.Text = driver.Address;
            ApplyFormState();
        }

        private void StartFleetEdit(string vehicleId)
        {
            var data = CrmRepository.Load();
            var vehicle = FindById(data.FleetVehicles, vehicleId);

            if (vehicle == null)
            {
                ShowMessage("Автомобиль не найден.", "warning");
                BindAll(data);
                return;
            }

            FleetEditingId = vehicle.Id;
            BindAll(data);

            FleetCarBrandTextBox.Text = vehicle.CarBrand;
            FleetCarModelTextBox.Text = vehicle.CarModel;
            FleetLicensePlateTextBox.Text = vehicle.LicensePlate;
            FleetVinTextBox.Text = vehicle.VinCode;
            FleetTrailerBrandTextBox.Text = vehicle.TrailerBrand;
            FleetTrailerModelTextBox.Text = vehicle.TrailerModel;
            FleetTrailerLicensePlateTextBox.Text = vehicle.TrailerLicensePlate;
            SetSelectedValues(FleetDriversCheckBoxList, ResolveDriverIds(vehicle, data.Drivers));
            ApplyFormState();
        }

        private void StartClientEdit(string clientId)
        {
            var data = CrmRepository.Load();
            var client = FindById(data.Clients, clientId);

            if (client == null)
            {
                ShowMessage("Клиент не найден.", "warning");
                BindAll(data);
                return;
            }

            ClientsEditingId = client.Id;
            BindAll(data);
            ClientsNameTextBox.Text = client.Name;
            ClientsDirectionTextBox.Text = client.Direction;
            ClientsManagerTextBox.Text = client.Manager;
            ClientsPhoneTextBox.Text = client.PhoneNumber;
            ClientsEmailTextBox.Text = client.Email;
            ApplyFormState();
        }

        private void DeleteTrip(string tripId)
        {
            var data = CrmRepository.Load();
            var trip = FindById(data.Trips, tripId);

            if (trip == null)
            {
                ShowMessage("Рейс не найден.", "warning");
                BindAll(data);
                return;
            }

            data.Trips.Remove(trip);

            if (StringEquals(TripsEditingId, tripId))
            {
                ClearTripsForm();
                ClearTripsEditState();
            }

            CrmRepository.Save(data);
            ShowMessage("Рейс удален.", "success");
            BindAll();
        }

        private void DeleteDriver(string driverId)
        {
            var data = CrmRepository.Load();
            var driver = FindById(data.Drivers, driverId);

            if (driver == null)
            {
                ShowMessage("Водитель не найден.", "warning");
                BindAll(data);
                return;
            }

            data.Drivers.Remove(driver);
            RemoveDriverReferences(data, driver);

            if (StringEquals(DriversEditingId, driverId))
            {
                ClearDriversForm();
                ClearDriversEditState();
            }

            CrmRepository.Save(data);
            ShowMessage("Водитель удален.", "success");
            BindAll();
        }

        private void DeleteVehicle(string vehicleId)
        {
            var data = CrmRepository.Load();
            var vehicle = FindById(data.FleetVehicles, vehicleId);

            if (vehicle == null)
            {
                ShowMessage("Автомобиль не найден.", "warning");
                BindAll(data);
                return;
            }

            data.FleetVehicles.Remove(vehicle);
            RemoveVehicleReferences(data, vehicle);

            if (StringEquals(FleetEditingId, vehicleId))
            {
                ClearFleetForm();
                ClearFleetEditState();
            }

            CrmRepository.Save(data);
            ShowMessage("Автомобиль удален.", "success");
            BindAll();
        }

        private void DeleteClient(string clientId)
        {
            var data = CrmRepository.Load();
            var client = FindById(data.Clients, clientId);

            if (client == null)
            {
                ShowMessage("Клиент не найден.", "warning");
                BindAll(data);
                return;
            }

            data.Clients.Remove(client);
            RemoveClientReferences(data, client);

            if (StringEquals(ClientsEditingId, clientId))
            {
                ClearClientsForm();
                ClearClientsEditState();
            }

            CrmRepository.Save(data);
            ShowMessage("Клиент удален.", "success");
            BindAll();
        }

        private void LoadTripForm(TripRecord trip, CrmDataStore data)
        {
            TripsNumberTextBox.Text = trip.Number;
            TripsCountryTextBox.Text = trip.Country;
            TripsStartDateTextBox.Text = trip.StartDate;
            TripsEndDateTextBox.Text = trip.EndDate;
            TripsFreightTextBox.Text = trip.Freight;
            TripsPrepaymentTextBox.Text = trip.Prepayment;

            SetSelectedValue(TripsStatusDropDownList, trip.Status);
            SetSelectedValue(TripsClientDropDownList, ResolveClientId(trip, data.Clients));
            SetSelectedValue(TripsDriverDropDownList, ResolveDriverId(trip, data.Drivers));
            SetSelectedValue(TripsVehicleDropDownList, ResolveVehicleId(trip, data.FleetVehicles));
        }

        private void ClearTripsForm()
        {
            TripsNumberTextBox.Text = string.Empty;
            TripsStatusDropDownList.SelectedIndex = 0;
            TripsCountryTextBox.Text = string.Empty;
            TripsStartDateTextBox.Text = string.Empty;
            TripsEndDateTextBox.Text = string.Empty;
            TripsFreightTextBox.Text = string.Empty;
            TripsPrepaymentTextBox.Text = string.Empty;

            if (TripsClientDropDownList.Items.Count > 0)
            {
                TripsClientDropDownList.SelectedIndex = 0;
            }

            if (TripsVehicleDropDownList.Items.Count > 0)
            {
                TripsVehicleDropDownList.SelectedIndex = 0;
            }

            if (TripsDriverDropDownList.Items.Count > 0)
            {
                TripsDriverDropDownList.SelectedIndex = 0;
            }
        }

        private void ClearDriversForm()
        {
            DriversFullNameTextBox.Text = string.Empty;
            DriversBirthDateTextBox.Text = string.Empty;
            DriversPhoneTextBox.Text = string.Empty;
            DriversAddressTextBox.Text = string.Empty;
        }

        private void ClearFleetForm()
        {
            FleetCarBrandTextBox.Text = string.Empty;
            FleetCarModelTextBox.Text = string.Empty;
            FleetLicensePlateTextBox.Text = string.Empty;
            FleetVinTextBox.Text = string.Empty;
            FleetTrailerBrandTextBox.Text = string.Empty;
            FleetTrailerModelTextBox.Text = string.Empty;
            FleetTrailerLicensePlateTextBox.Text = string.Empty;

            foreach (ListItem item in FleetDriversCheckBoxList.Items)
            {
                item.Selected = false;
            }
        }

        private void ClearClientsForm()
        {
            ClientsNameTextBox.Text = string.Empty;
            ClientsDirectionTextBox.Text = string.Empty;
            ClientsManagerTextBox.Text = string.Empty;
            ClientsPhoneTextBox.Text = string.Empty;
            ClientsEmailTextBox.Text = string.Empty;
        }

        private void ClearTripsEditState()
        {
            TripsEditingId = string.Empty;
        }

        private void ClearDriversEditState()
        {
            DriversEditingId = string.Empty;
        }

        private void ClearFleetEditState()
        {
            FleetEditingId = string.Empty;
        }

        private void ClearClientsEditState()
        {
            ClientsEditingId = string.Empty;
        }

        private string SaveUploadedFile(FileUpload upload, string relativeFolder, string filePrefix)
        {
            if (upload == null || !upload.HasFile)
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(upload.FileName);
            var safePrefix = MakeSafeFileName(string.IsNullOrWhiteSpace(filePrefix) ? "document" : filePrefix);
            var fileName = safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
            var virtualFolder = "~/" + relativeFolder.TrimStart('~', '/').Replace("\\", "/");
            var physicalFolder = Server.MapPath(virtualFolder);

            Directory.CreateDirectory(physicalFolder);

            var physicalPath = Path.Combine(physicalFolder, fileName);
            upload.SaveAs(physicalPath);

            return VirtualPathUtility.ToAbsolute(virtualFolder + "/" + fileName);
        }

        private static string MakeSafeFileName(string input)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(input.Where(character => !invalidChars.Contains(character)).ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized.Replace(" ", "_");
        }

        private static string BuildVehicleName(FleetVehicleRecord vehicle)
        {
            return string.Format("{0} {1} ({2})", vehicle.CarBrand, vehicle.CarModel, vehicle.LicensePlate).Trim();
        }

        private static TRecord FindById<TRecord>(IEnumerable<TRecord> records, string id) where TRecord : class
        {
            if (records == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return records.FirstOrDefault(item => StringEquals(GetRecordId(item), id));
        }

        private static string GetRecordId(object record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var property = record.GetType().GetProperty("Id");
            return property == null ? string.Empty : Convert.ToString(property.GetValue(record, null));
        }

        private static bool IsEditing(string recordId)
        {
            return !string.IsNullOrWhiteSpace(recordId);
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSelectedValue(ListControl control)
        {
            return control == null ? string.Empty : Convert.ToString(control.SelectedValue);
        }

        private static List<string> GetSelectedValues(ListControl control)
        {
            if (control == null)
            {
                return new List<string>();
            }

            return control.Items.Cast<ListItem>()
                .Where(item => item.Selected)
                .Select(item => item.Value)
                .ToList();
        }

        private static void SetSelectedValue(ListControl control, string value)
        {
            if (control == null)
            {
                return;
            }

            control.ClearSelection();

            if (string.IsNullOrWhiteSpace(value))
            {
                if (control.Items.Count > 0)
                {
                    control.SelectedIndex = 0;
                }

                return;
            }

            var item = control.Items.FindByValue(value);

            if (item != null)
            {
                item.Selected = true;
                return;
            }

            if (control.Items.Count > 0)
            {
                control.SelectedIndex = 0;
            }
        }

        private static void SetSelectedValues(ListControl control, IEnumerable<string> values)
        {
            if (control == null)
            {
                return;
            }

            var selectedValues = values == null
                ? new List<string>()
                : values.Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            foreach (ListItem item in control.Items)
            {
                item.Selected = selectedValues.Contains(item.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string ResolveClientId(TripRecord trip, IEnumerable<ClientRecord> clients)
        {
            if (!string.IsNullOrWhiteSpace(trip.ClientId))
            {
                return trip.ClientId;
            }

            var client = clients.FirstOrDefault(item => StringEquals(item.Name, trip.ClientName));
            return client == null ? string.Empty : client.Id;
        }

        private static string ResolveDriverId(TripRecord trip, IEnumerable<DriverRecord> drivers)
        {
            if (!string.IsNullOrWhiteSpace(trip.DriverId))
            {
                return trip.DriverId;
            }

            var driver = drivers.FirstOrDefault(item => StringEquals(item.FullName, trip.DriverName));
            return driver == null ? string.Empty : driver.Id;
        }

        private static string ResolveVehicleId(TripRecord trip, IEnumerable<FleetVehicleRecord> vehicles)
        {
            if (!string.IsNullOrWhiteSpace(trip.VehicleId))
            {
                return trip.VehicleId;
            }

            var vehicle = vehicles.FirstOrDefault(item => StringEquals(BuildVehicleName(item), trip.VehicleName));
            return vehicle == null ? string.Empty : vehicle.Id;
        }

        private static List<string> ResolveDriverIds(FleetVehicleRecord vehicle, IEnumerable<DriverRecord> drivers)
        {
            var resolvedIds = new List<string>();
            var driverList = drivers.ToList();

            if (vehicle.AssignedDriverIds != null && vehicle.AssignedDriverIds.Count > 0)
            {
                resolvedIds.AddRange(vehicle.AssignedDriverIds.Where(id => driverList.Any(driver => StringEquals(driver.Id, id))));
            }
            else if (vehicle.AssignedDriverNames != null)
            {
                resolvedIds.AddRange(driverList
                    .Where(driver => vehicle.AssignedDriverNames.Any(name => StringEquals(name, driver.FullName)))
                    .Select(driver => driver.Id));
            }

            return resolvedIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void SyncClientReferences(CrmDataStore data, ClientRecord client, string previousName)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.ClientId, client.Id) || (string.IsNullOrWhiteSpace(item.ClientId) && StringEquals(item.ClientName, previousName))))
            {
                trip.ClientId = client.Id;
                trip.ClientName = client.Name;
            }
        }

        private static void SyncDriverReferences(CrmDataStore data, DriverRecord driver, string previousName)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.DriverId, driver.Id) || (string.IsNullOrWhiteSpace(item.DriverId) && StringEquals(item.DriverName, previousName))))
            {
                trip.DriverId = driver.Id;
                trip.DriverName = driver.FullName;
            }

            foreach (var vehicle in data.FleetVehicles.Where(item => (item.AssignedDriverIds == null || item.AssignedDriverIds.Count == 0) && item.AssignedDriverNames != null && item.AssignedDriverNames.Any(name => StringEquals(name, previousName))))
            {
                vehicle.AssignedDriverNames = vehicle.AssignedDriverNames
                    .Select(name => StringEquals(name, previousName) ? driver.FullName : name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            RefreshFleetDriverNames(data);
        }

        private static void SyncVehicleReferences(CrmDataStore data, FleetVehicleRecord vehicle, string previousVehicleName)
        {
            var currentVehicleName = BuildVehicleName(vehicle);

            foreach (var trip in data.Trips.Where(item => StringEquals(item.VehicleId, vehicle.Id) || (string.IsNullOrWhiteSpace(item.VehicleId) && StringEquals(item.VehicleName, previousVehicleName))))
            {
                trip.VehicleId = vehicle.Id;
                trip.VehicleName = currentVehicleName;
            }
        }

        private static void RemoveClientReferences(CrmDataStore data, ClientRecord client)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.ClientId, client.Id) || (string.IsNullOrWhiteSpace(item.ClientId) && StringEquals(item.ClientName, client.Name))))
            {
                trip.ClientId = string.Empty;
            }
        }

        private static void RemoveDriverReferences(CrmDataStore data, DriverRecord driver)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.DriverId, driver.Id) || (string.IsNullOrWhiteSpace(item.DriverId) && StringEquals(item.DriverName, driver.FullName))))
            {
                trip.DriverId = string.Empty;
            }

            foreach (var vehicle in data.FleetVehicles)
            {
                vehicle.AssignedDriverIds = (vehicle.AssignedDriverIds ?? new List<string>())
                    .Where(id => !StringEquals(id, driver.Id))
                    .ToList();

                vehicle.AssignedDriverNames = (vehicle.AssignedDriverNames ?? new List<string>())
                    .Where(name => !StringEquals(name, driver.FullName))
                    .ToList();
            }

            RefreshFleetDriverNames(data);
        }

        private static void RemoveVehicleReferences(CrmDataStore data, FleetVehicleRecord vehicle)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.VehicleId, vehicle.Id) || (string.IsNullOrWhiteSpace(item.VehicleId) && StringEquals(item.VehicleName, BuildVehicleName(vehicle)))))
            {
                trip.VehicleId = string.Empty;
            }
        }

        private static void RefreshFleetDriverNames(CrmDataStore data)
        {
            var driversById = data.Drivers
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id, item => item.FullName, StringComparer.OrdinalIgnoreCase);

            foreach (var vehicle in data.FleetVehicles)
            {
                var currentNames = new List<string>();

                foreach (var driverId in vehicle.AssignedDriverIds ?? new List<string>())
                {
                    string fullName;

                    if (driversById.TryGetValue(driverId, out fullName) && !currentNames.Contains(fullName, StringComparer.OrdinalIgnoreCase))
                    {
                        currentNames.Add(fullName);
                    }
                }

                if (currentNames.Count == 0 && vehicle.AssignedDriverNames != null)
                {
                    currentNames = vehicle.AssignedDriverNames
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                vehicle.AssignedDriverNames = currentNames;
            }
        }
    }
}
