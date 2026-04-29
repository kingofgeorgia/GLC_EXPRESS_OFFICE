using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
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
        private const int MaxStoredLogoWidth = 320;
        private const int MaxStoredLogoHeight = 96;
        private const int DefaultDashboardPeriodDays = 30;
        private const string DashboardPeriodQueryKey = "dashboardPeriod";
        private const string DashboardInquiryStatusQueryKey = "dashboardInquiryStatus";
        private const string DashboardInquiryDateQueryKey = "dashboardInquiryDate";
        private const string DashboardCarBrandQueryKey = "dashboardCarBrand";
        private static readonly int[] SupportedDashboardPeriods = { 7, 30, 90, 365 };
        private static readonly string[] DashboardPalette = { "#0f6d62", "#d98f39", "#4f87b7", "#ba5d5d", "#7c6aa6", "#2b516d", "#5a8a3e", "#8d6e63" };
        private CrmSettingsRecord _crmSettings;
        private bool? _hasNewInquiries;
        private CarRecord _selectedCar;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (IsPostBack && Request != null && Request.IsAuthenticated)
            {
                BindPostbackListControls();

                if (RequiresEarlyInquiryBinding())
                {
                    BindInquiryPostbackControls();
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnsureAuthenticated() || !EnsureCrmAccess())
            {
                return;
            }

            DisableClientCaching();

            HideMessage();

            if (!IsPostBack)
            {
                ActiveTab = ResolveDefaultCrmTab();
                BindAll();
                return;
            }

            ApplyFormState();
        }

        private void DisableClientCaching()
        {
            if (Response == null)
            {
                return;
            }

            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetNoStore();
            Response.Cache.SetNoServerCaching();
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            Response.Cache.SetValidUntilExpires(false);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetMaxAge(TimeSpan.Zero);
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
            TripsFormVisible = true;

            if (!IsValidationGroupValid("TripsGroup"))
            {
                BindAll();
                return;
            }

            DateTime startDate;
            DateTime endDate;

            if (!TryValidateTripForm(out startDate, out endDate))
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

            if (!TryValidateTripBusinessRules(data, existingTrip.Id, TripsNumberTextBox.Text.Trim(), TripsFreightTextBox.Text.Trim(), TripsPrepaymentTextBox.Text.Trim()))
            {
                BindAll(data);
                return;
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
            existingTrip.StartDate = startDate.ToString("yyyy-MM-dd");
            existingTrip.EndDate = endDate.ToString("yyyy-MM-dd");
            existingTrip.Freight = TripsFreightTextBox.Text.Trim();
            existingTrip.Prepayment = TripsPrepaymentTextBox.Text.Trim();

            CrmRepository.Save(data);
            ClearTripsForm();
            ClearTripsEditState();
            TripsFormVisible = false;
            ShowMessage(isEditing ? "Рейс обновлен." : "Рейс успешно добавлен.", "success");
            BindAll();
        }

        protected void AddDriverButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            DriversFormVisible = true;

            if (!IsValidationGroupValid("DriversGroup"))
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
            var previousPassportPath = existingDriver.PassportScanPath;
            var previousLicensePath = existingDriver.LicenseScanPath;
            var passportPath = SaveUploadedFile(DriversPassportUpload, "Drivers/Passports", fullName + "_passport", CurrentCrmSettings.AllowedDocumentExtensions);
            var licensePath = SaveUploadedFile(DriversLicenseUpload, "Drivers/Licenses", fullName + "_license", CurrentCrmSettings.AllowedDocumentExtensions);

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
            DeleteStoredFileIfReplaced(previousPassportPath, existingDriver.PassportScanPath);
            DeleteStoredFileIfReplaced(previousLicensePath, existingDriver.LicenseScanPath);
            ClearDriversForm();
            ClearDriversEditState();
            DriversFormVisible = false;
            ShowMessage(isEditing ? "Водитель обновлен." : "Водитель успешно добавлен.", "success");
            BindAll();
        }

        protected void AddFleetButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            FleetFormVisible = true;

            if (!IsValidationGroupValid("FleetGroup"))
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
            var previousDocumentsPath = existingVehicle.DocumentsScanPath;
            var documentsPath = SaveUploadedFile(FleetDocumentsUpload, "Fleet/Documents", FleetLicensePlateTextBox.Text.Trim(), CurrentCrmSettings.AllowedDocumentExtensions);

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
            DeleteStoredFileIfReplaced(previousDocumentsPath, existingVehicle.DocumentsScanPath);
            ClearFleetForm();
            ClearFleetEditState();
            FleetFormVisible = false;
            ShowMessage(isEditing ? "Автомобиль обновлен." : "Автомобиль успешно добавлен в автопарк.", "success");
            BindAll();
        }

        protected void AddClientButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            ClientsFormVisible = true;

            if (!IsValidationGroupValid("ClientsGroup"))
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
            ClientsFormVisible = false;
            ShowMessage(isEditing ? "Клиент обновлен." : "Клиент успешно добавлен.", "success");
            BindAll();
        }

        protected void NewTripButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            ClearTripsEditState();
            ClearTripsForm();
            TripsFormVisible = true;
            BindAll();
        }

        protected void NewDriverButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            ClearDriversEditState();
            ClearDriversForm();
            DriversFormVisible = true;
            BindAll();
        }

        protected void NewFleetButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            ClearFleetEditState();
            ClearFleetForm();
            FleetFormVisible = true;
            BindAll();
        }

        protected void NewClientButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            ClearClientsEditState();
            ClearClientsForm();
            ClientsFormVisible = true;
            BindAll();
        }

        protected void TripsBulkEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(TripsSelectedIds, data.Trips.Select(item => item.Id));

            if (selectedIds.Count == 0)
            {
                ShowMessage("Выберите хотя бы один рейс для редактирования.", "warning");
                BindAll(data);
                return;
            }

            TripsEditingId = SerializeStoredIds(selectedIds);
            TripsFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        protected void TripsBulkDeleteButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(TripsSelectedIds, data.Trips.Select(item => item.Id));
            var trips = data.Trips.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();

            if (trips.Count == 0)
            {
                ShowMessage("Выберите хотя бы один рейс для удаления.", "warning");
                BindAll(data);
                return;
            }

            foreach (var trip in trips)
            {
                data.Trips.Remove(trip);
            }

            if (trips.Any(item => ContainsStoredId(TripsEditingId, item.Id)))
            {
                ClearTripsForm();
                TripsFormVisible = false;
            }

            TripsEditingId = RemoveStoredIds(TripsEditingId, trips.Select(item => item.Id));
            TripsSelectedIds = string.Empty;
            CrmRepository.Save(data);
            ShowMessage("Удалено рейсов: " + trips.Count.ToString(CultureInfo.CurrentCulture) + ".", "success");
            BindAll(data);
        }

        protected void TripsBulkStatusButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            var selectedStatus = GetSelectedValue(TripsBulkStatusDropDownList);
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(TripsSelectedIds, data.Trips.Select(item => item.Id));
            var trips = data.Trips.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();

            if (trips.Count == 0)
            {
                ShowMessage("Выберите хотя бы один рейс для изменения статуса.", "warning");
                BindAll(data);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedStatus))
            {
                ShowMessage("Выберите статус, который нужно установить для выбранных рейсов.", "warning");
                BindAll(data);
                return;
            }

            foreach (var trip in trips)
            {
                trip.Status = selectedStatus.Trim();
            }

            CrmRepository.Save(data);
            ShowMessage("Статус обновлен для рейсов: " + trips.Count.ToString(CultureInfo.CurrentCulture) + ".", "success");
            BindAll(data);
        }

        protected void DriversBulkEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(DriversSelectedIds, data.Drivers.Select(item => item.Id));

            if (selectedIds.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного водителя для редактирования.", "warning");
                BindAll(data);
                return;
            }

            DriversEditingId = SerializeStoredIds(selectedIds);
            DriversFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        protected void DriversBulkDeleteButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(DriversSelectedIds, data.Drivers.Select(item => item.Id));
            var drivers = data.Drivers.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            var storedPaths = new List<string>();

            if (drivers.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного водителя для удаления.", "warning");
                BindAll(data);
                return;
            }

            foreach (var driver in drivers)
            {
                storedPaths.Add(driver.PassportScanPath);
                storedPaths.Add(driver.LicenseScanPath);
                data.Drivers.Remove(driver);
                RemoveDriverReferences(data, driver);
            }

            if (drivers.Any(item => ContainsStoredId(DriversEditingId, item.Id)))
            {
                ClearDriversForm();
                DriversFormVisible = false;
            }

            DriversEditingId = RemoveStoredIds(DriversEditingId, drivers.Select(item => item.Id));
            DriversSelectedIds = string.Empty;
            CrmRepository.Save(data);
            DeleteStoredFiles(storedPaths.ToArray());
            ShowMessage("Удалено водителей: " + drivers.Count.ToString(CultureInfo.CurrentCulture) + ".", "success");
            BindAll(data);
        }

        protected void FleetBulkEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(FleetSelectedIds, data.FleetVehicles.Select(item => item.Id));

            if (selectedIds.Count == 0)
            {
                ShowMessage("Выберите хотя бы один автомобиль для редактирования.", "warning");
                BindAll(data);
                return;
            }

            FleetEditingId = SerializeStoredIds(selectedIds);
            FleetFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        protected void FleetBulkDeleteButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(FleetSelectedIds, data.FleetVehicles.Select(item => item.Id));
            var vehicles = data.FleetVehicles.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            var storedPaths = new List<string>();

            if (vehicles.Count == 0)
            {
                ShowMessage("Выберите хотя бы один автомобиль для удаления.", "warning");
                BindAll(data);
                return;
            }

            foreach (var vehicle in vehicles)
            {
                storedPaths.Add(vehicle.DocumentsScanPath);
                data.FleetVehicles.Remove(vehicle);
                RemoveVehicleReferences(data, vehicle);
            }

            if (vehicles.Any(item => ContainsStoredId(FleetEditingId, item.Id)))
            {
                ClearFleetForm();
                FleetFormVisible = false;
            }

            FleetEditingId = RemoveStoredIds(FleetEditingId, vehicles.Select(item => item.Id));
            FleetSelectedIds = string.Empty;
            CrmRepository.Save(data);
            DeleteStoredFiles(storedPaths.ToArray());
            ShowMessage("Удалено автомобилей: " + vehicles.Count.ToString(CultureInfo.CurrentCulture) + ".", "success");
            BindAll(data);
        }

        protected void ClientsBulkEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(ClientsSelectedIds, data.Clients.Select(item => item.Id));

            if (selectedIds.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного клиента для редактирования.", "warning");
                BindAll(data);
                return;
            }

            ClientsEditingId = SerializeStoredIds(selectedIds);
            ClientsFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        protected void ClientsBulkDeleteButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            var data = CrmRepository.Load();
            var selectedIds = GetAvailableStoredIds(ClientsSelectedIds, data.Clients.Select(item => item.Id));
            var clients = data.Clients.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();

            if (clients.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного клиента для удаления.", "warning");
                BindAll(data);
                return;
            }

            foreach (var client in clients)
            {
                data.Clients.Remove(client);
                RemoveClientReferences(data, client);
            }

            if (clients.Any(item => ContainsStoredId(ClientsEditingId, item.Id)))
            {
                ClearClientsForm();
                ClientsFormVisible = false;
            }

            ClientsEditingId = RemoveStoredIds(ClientsEditingId, clients.Select(item => item.Id));
            ClientsSelectedIds = string.Empty;
            CrmRepository.Save(data);
            ShowMessage("Удалено клиентов: " + clients.Count.ToString(CultureInfo.CurrentCulture) + ".", "success");
            BindAll(data);
        }

        protected void SaveSettingsButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";

            var existingSettings = CurrentCrmSettings;
            var homeContentLanguage = GetSelectedSettingsHomeContentLanguage();
            var candidateSettings = CrmSettingsService.Normalize(new CrmSettingsRecord
            {
                CompanyName = SettingsCompanyNameTextBox.Text.Trim(),
                LogoPath = existingSettings.LogoPath,
                DefaultCrmTab = GetSelectedValue(SettingsDefaultTabDropDownList),
                DefaultTripStatus = GetSelectedValue(SettingsDefaultTripStatusDropDownList),
                AllowedCrmRoles = new List<string>(GetEditableSettingsRoles(existingSettings)),
                TripStatuses = new List<string>(GetEditableTripStatuses(existingSettings)),
                AllowedDocumentExtensions = ParseSettingsValues(SettingsAllowedDocumentExtensionsTextBox.Text),
                AllowedLogoExtensions = ParseSettingsValues(SettingsAllowedLogoExtensionsTextBox.Text),
                HomePartnerNames = new List<string>(existingSettings.HomePartnerNames ?? Enumerable.Empty<string>()),
                HomePartnerNamesEn = new List<string>(existingSettings.HomePartnerNamesEn ?? Enumerable.Empty<string>()),
                HomePartnerNamesGe = new List<string>(existingSettings.HomePartnerNamesGe ?? Enumerable.Empty<string>()),
                HomeReviews = CloneReviewItems(existingSettings.HomeReviews),
                HomeReviewsEn = CloneReviewItems(existingSettings.HomeReviewsEn),
                HomeReviewsGe = CloneReviewItems(existingSettings.HomeReviewsGe),
                HomeContactAddress = existingSettings.HomeContactAddress,
                HomeContactAddressEn = existingSettings.HomeContactAddressEn,
                HomeContactAddressGe = existingSettings.HomeContactAddressGe,
                HomeContactPhone = SettingsHomeContactPhoneTextBox.Text.Trim(),
                HomeContactWorkingHours = existingSettings.HomeContactWorkingHours,
                HomeContactWorkingHoursEn = existingSettings.HomeContactWorkingHoursEn,
                HomeContactWorkingHoursGe = existingSettings.HomeContactWorkingHoursGe,
                HomeContactWhatsAppUrl = SettingsHomeContactWhatsAppUrlTextBox.Text.Trim(),
                RequireUniqueTripNumbers = SettingsRequireUniqueTripNumbersCheckBox.Checked,
                ValidatePrepaymentAgainstFreight = SettingsValidatePrepaymentCheckBox.Checked
            });

            ApplyLocalizedHomeContentInputs(candidateSettings, homeContentLanguage);

            if (Context != null
                && Context.User != null
                && !candidateSettings.AllowedCrmRoles.Any(role => Context.User.IsInRole(role)))
            {
                ShowMessage("В списке ролей должна остаться хотя бы одна ваша текущая роль, иначе вы потеряете доступ к CRM.", "warning");
                BindAll();
                return;
            }

            try
            {
                if (SettingsLogoUpload != null && SettingsLogoUpload.HasFile)
                {
                    candidateSettings.LogoPath = SaveLogoFile(SettingsLogoUpload, "Branding", "company_logo", candidateSettings.AllowedLogoExtensions);
                }

                var savedSettings = CrmSettingsService.Save(candidateSettings);
                DeleteStoredFileIfReplaced(existingSettings.LogoPath, savedSettings.LogoPath);
                ResetCrmSettings();
                SyncSettingsEditorState(savedSettings);
                SettingsInputsNeedRefresh = true;
                ShowMessage("Настройки CRM обновлены.", "success");
            }
            catch (InvalidOperationException exception)
            {
                ShowMessage(exception.Message, "warning");
            }

            BindAll();
        }

        protected void SettingsHomeContentLanguageDropDownList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            SettingsInputsNeedRefresh = true;
            BindAll();
        }

        protected void AddSettingsRoleButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            AddSettingsListValue(SettingsNewRoleTextBox, GetEditableSettingsRoles(CurrentCrmSettings), "Укажите роль для добавления.");
            BindAll();
        }

        protected void SettingsRolesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "settings";

            if (!string.Equals(e.CommandName, "RemoveItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveSettingsListValue(GetEditableSettingsRoles(CurrentCrmSettings), Convert.ToString(e.CommandArgument), "Нельзя удалить последнюю роль с доступом в CRM.");
            BindAll();
        }

        protected void AddSettingsTripStatusButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            AddSettingsListValue(SettingsNewTripStatusTextBox, GetEditableTripStatuses(CurrentCrmSettings), "Укажите статус рейса для добавления.");
            BindAll();
        }

        protected void SettingsTripStatusesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "settings";

            if (!string.Equals(e.CommandName, "RemoveItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveSettingsListValue(GetEditableTripStatuses(CurrentCrmSettings), Convert.ToString(e.CommandArgument), "Нельзя удалить последний статус рейса.");
            BindAll();
        }

        protected void ImportSettingsButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";

            if (SettingsImportJsonUpload == null || !SettingsImportJsonUpload.HasFile)
            {
                ShowMessage("Выберите JSON-файл с настройками CRM для импорта.", "warning");
                BindAll();
                return;
            }

            try
            {
                string json;

                using (var reader = new StreamReader(SettingsImportJsonUpload.FileContent))
                {
                    json = reader.ReadToEnd();
                }

                var existingSettings = CurrentCrmSettings;
                var importedSettings = CrmSettingsService.ImportFromJson(json, existingSettings);

                if (Context != null
                    && Context.User != null
                    && !importedSettings.AllowedCrmRoles.Any(role => Context.User.IsInRole(role)))
                {
                    ShowMessage("Импортируемый список ролей должен содержать хотя бы одну вашу текущую роль, иначе вы потеряете доступ к CRM.", "warning");
                    BindAll();
                    return;
                }

                var savedSettings = CrmSettingsService.Save(importedSettings);
                ResetCrmSettings();
                SyncSettingsEditorState(savedSettings);
                SettingsInputsNeedRefresh = true;
                ShowMessage("Настройки CRM импортированы. Текущий логотип оставлен без изменений.", "success");
            }
            catch (InvalidOperationException exception)
            {
                ShowMessage(exception.Message, "warning");
            }

            BindAll();
        }

        protected void ImportCrmExcelButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            CrmExcelImportReportHtml = string.Empty;

            if (SettingsImportExcelUpload == null || !SettingsImportExcelUpload.HasFile)
            {
                ShowMessage("Выберите Excel-файл .xlsx с CRM-данными для импорта.", "warning");
                BindAll();
                return;
            }

            var extension = (Path.GetExtension(SettingsImportExcelUpload.FileName) ?? string.Empty).Trim();

            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Поддерживается только формат Excel .xlsx.", "warning");
                BindAll();
                return;
            }

            try
            {
                var data = CrmRepository.Load();
                CrmExcelImportResult importResult;

                using (var stream = SettingsImportExcelUpload.FileContent)
                {
                    importResult = CrmExcelImportService.Import(stream, data);
                }

                CrmRepository.Save(data);
                CrmExcelImportReportHtml = BuildCrmExcelImportReportHtml(importResult);
                ShowMessage(importResult.BuildSummaryMessage(), importResult.HasChanges ? "success" : "warning");
            }
            catch (InvalidOperationException exception)
            {
                ShowMessage(exception.Message, "warning");
            }

            BindAll();
        }

        protected void ExportCrmExcelTemplateButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";

            var fileName = "crm-import-template-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".xlsx";
            var content = CrmExcelImportService.BuildTemplateWorkbook();

            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            Response.BinaryWrite(content);
            Response.Flush();
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void AddSettingsHomePartnerButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            AddSettingsListValue(SettingsNewHomePartnerTextBox, GetEditableHomePartners(CurrentCrmSettings), "Укажите партнера для добавления.");
            BindAll();
        }

        protected void SettingsHomePartnersRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "settings";

            if (!string.Equals(e.CommandName, "RemoveItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RemoveSettingsListValue(GetEditableHomePartners(CurrentCrmSettings), Convert.ToString(e.CommandArgument), "Нельзя удалить последнего партнера на главной странице.");
            BindAll();
        }

        protected void AddSettingsHomeReviewButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";
            AddSettingsReviewItem();
            BindAll();
        }

        protected void ApplySettingsLeadFiltersButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "inquiries";
            BindAll();
        }

        protected void ResetSettingsLeadFiltersButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "inquiries";
            SettingsLeadFilterPhoneTextBox.Text = string.Empty;
            SettingsLeadFilterMessengerTextBox.Text = string.Empty;
            SettingsLeadFilterDirectionTextBox.Text = string.Empty;
            SettingsLeadFilterCommentTextBox.Text = string.Empty;

            if (SettingsLeadFilterPanelStateHiddenField != null)
            {
                SettingsLeadFilterPanelStateHiddenField.Value = "expanded";
            }

            if (SettingsLeadFilterCargoTypeDropDownList != null)
            {
                SettingsLeadFilterCargoTypeDropDownList.ClearSelection();
            }

            BindAll();
        }

        protected void SettingsInquiryBulkStatusButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "inquiries";

            var selectedStatus = GetSelectedValue(SettingsInquiryBulkStatusDropDownList);
            var allInquiries = GetInquirySourceRecords();
            var inquiries = ApplyLeadFilters(allInquiries);
            var selectedIds = GetAvailableStoredIds(InquiriesSelectedIds, inquiries.Select(item => item.Id));
            var selectedInquiries = inquiries.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            var syncedCount = 0;

            if (selectedInquiries.Count == 0)
            {
                ShowMessage("Выберите хотя бы одну заявку для изменения статуса.", "warning");
                BindAll();
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedStatus))
            {
                ShowMessage("Выберите статус для выбранных заявок.", "warning");
                BindAll();
                return;
            }

            foreach (var inquiry in selectedInquiries)
            {
                var updatedInquiry = HomeInquiryService.UpdateManagementFields(inquiry.Id, inquiry.Source, selectedStatus, inquiry.AssignedManager);

                if (SyncInquiryToCrm(updatedInquiry))
                {
                    syncedCount++;
                }
            }

            ShowMessage(
                syncedCount > 0
                    ? string.Format(CultureInfo.CurrentCulture, "Статус обновлен для {0} заявок. CRM-данные синхронизированы для {1} из них.", selectedInquiries.Count, syncedCount)
                    : string.Format(CultureInfo.CurrentCulture, "Статус обновлен для {0} заявок.", selectedInquiries.Count),
                "success");

            BindAll();
        }

        protected void SettingsHomeReviewsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "settings";

            if (!string.Equals(e.CommandName, "RemoveItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var reviews = GetEditableHomeReviews(CurrentCrmSettings);

            if (reviews.Count <= 1)
            {
                ShowMessage("Нельзя удалить последний отзыв на главной странице.", "warning");
                BindAll();
                return;
            }

            int index;

            if (int.TryParse(Convert.ToString(e.CommandArgument), out index)
                && index >= 0
                && index < reviews.Count)
            {
                reviews.RemoveAt(index);
            }

            BindAll();
        }

        protected void SettingsRecentInquiriesRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "inquiries";

            if (string.Equals(e.CommandName, "AddLeadComment", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    HomeInquiryService.AddComment(
                        Convert.ToString(e.CommandArgument),
                        ResolveLeadCommentAuthor(),
                        GetRepeaterTextBoxValue(e.Item, "SettingsLeadNewCommentTextBox"));

                    ShowMessage("Комментарий к лиду добавлен.", "success");
                }
                catch (InvalidOperationException exception)
                {
                    ShowMessage(exception.Message, "warning");
                }

                BindAll();
                return;
            }

            if (!string.Equals(e.CommandName, "SaveLead", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var inquiry = HomeInquiryService.UpdateManagementFields(
                    Convert.ToString(e.CommandArgument),
                    GetRepeaterTextBoxValue(e.Item, "SettingsLeadSourceTextBox"),
                    GetRepeaterSelectedValue(e.Item, "SettingsLeadStatusDropDownList"),
                    GetRepeaterTextBoxValue(e.Item, "SettingsLeadAssignedManagerTextBox"));

                var crmDataWasSynced = SyncInquiryToCrm(inquiry);

                ShowMessage(
                    crmDataWasSynced
                        ? "Параметры лида обновлены. CRM-данные синхронизированы."
                        : "Параметры лида обновлены.",
                    "success");
            }
            catch (InvalidOperationException exception)
            {
                ShowMessage(exception.Message, "warning");
            }

            BindAll();
        }

        protected void SettingsInquiryListRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "inquiries";

            if (!string.Equals(e.CommandName, "SelectItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedInquiryId = Convert.ToString(e.CommandArgument);
            BindAll();
        }

        protected void CloseSettingsInquiryDetailButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "inquiries";
            SelectedInquiryId = string.Empty;
            BindAll();
        }

        protected void ExportSettingsButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "settings";

            var fileName = "crm-settings-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
            var json = CrmSettingsService.ExportToJson(CurrentCrmSettings);

            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = "application/json";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            Response.Write(json);
            Response.Flush();
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void TripsCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "trips";
            ClearTripsForm();
            ClearTripsEditState();
            TripsFormVisible = false;
            BindAll();
        }

        protected void DriversCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "drivers";
            ClearDriversForm();
            ClearDriversEditState();
            DriversFormVisible = false;
            BindAll();
        }

        protected void FleetCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "fleet";
            ClearFleetForm();
            ClearFleetEditState();
            FleetFormVisible = false;
            BindAll();
        }

        protected void ClientsCancelEditButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "clients";
            ClearClientsForm();
            ClearClientsEditState();
            ClientsFormVisible = false;
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

            if (string.Equals(e.CommandName, "SaveInlineItem", StringComparison.OrdinalIgnoreCase))
            {
                SaveInlineTrip(e.Item, tripId);
                return;
            }

            if (string.Equals(e.CommandName, "CancelInlineEdit", StringComparison.OrdinalIgnoreCase))
            {
                TripsEditingId = RemoveStoredIds(TripsEditingId, new[] { tripId });
                BindAll();
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

            if (string.Equals(e.CommandName, "SaveInlineItem", StringComparison.OrdinalIgnoreCase))
            {
                SaveInlineDriver(e.Item, driverId);
                return;
            }

            if (string.Equals(e.CommandName, "CancelInlineEdit", StringComparison.OrdinalIgnoreCase))
            {
                DriversEditingId = RemoveStoredIds(DriversEditingId, new[] { driverId });
                BindAll();
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

            if (string.Equals(e.CommandName, "SaveInlineItem", StringComparison.OrdinalIgnoreCase))
            {
                SaveInlineFleet(e.Item, vehicleId);
                return;
            }

            if (string.Equals(e.CommandName, "CancelInlineEdit", StringComparison.OrdinalIgnoreCase))
            {
                FleetEditingId = RemoveStoredIds(FleetEditingId, new[] { vehicleId });
                BindAll();
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

            if (string.Equals(e.CommandName, "SaveInlineItem", StringComparison.OrdinalIgnoreCase))
            {
                SaveInlineClient(e.Item, clientId);
                return;
            }

            if (string.Equals(e.CommandName, "CancelInlineEdit", StringComparison.OrdinalIgnoreCase))
            {
                ClientsEditingId = RemoveStoredIds(ClientsEditingId, new[] { clientId });
                BindAll();
                return;
            }

            if (string.Equals(e.CommandName, "DeleteItem", StringComparison.OrdinalIgnoreCase))
            {
                DeleteClient(clientId);
            }
        }

        protected void CarsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ActiveTab = "cars";

            if (!string.Equals(e.CommandName, "SelectItem", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedCarId = Convert.ToString(e.CommandArgument);
            CarsInputsNeedRefresh = true;
            BindAll();
        }

        protected void CloseCarsDetailButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "cars";
            SelectedCarId = string.Empty;
            CarsInputsNeedRefresh = true;
            BindAll();
        }

        protected void SaveCarsDetailButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "cars";

            var data = CrmRepository.Load();
            var selectedCar = FindById(data.Cars, SelectedCarId);
            var newTripNumber = NormalizeCrmText(GetSelectedValue(CarsDetailTripDropDownList));
            var newForwarder = NormalizeCrmText(CarsDetailForwarderTextBox.Text);
            var newDealer = NormalizeCrmText(CarsDetailDealerTextBox.Text);
            var newStatus = NormalizeCrmText(CarsDetailStatusTextBox.Text);
            var newStartPrice = NormalizeCrmText(CarsDetailStartPriceTextBox.Text);
            var newInvoice = NormalizeCrmText(CarsDetailInvoiceTextBox.Text);
            var newYear = NormalizeCrmText(CarsDetailYearTextBox.Text);
            var newBrand = NormalizeCrmText(CarsDetailBrandTextBox.Text);
            var newModel = NormalizeCrmText(CarsDetailModelTextBox.Text);
            var newVin = NormalizeCrmText(CarsDetailVinTextBox.Text);
            var newLocation = NormalizeCrmText(CarsDetailLocationTextBox.Text);
            var newTitle = NormalizeCrmText(CarsDetailTitleTextBox.Text);
            var newKey = NormalizeCrmText(CarsDetailKeyTextBox.Text);
            var newInspection = NormalizeCrmText(CarsDetailInspectionTextBox.Text);
            var newReExport = NormalizeCrmText(CarsDetailReExportTextBox.Text);
            var newVolume = NormalizeCrmText(CarsDetailVolumeTextBox.Text);
            var newPower = NormalizeCrmText(CarsDetailPowerTextBox.Text);
            var newPortCost = NormalizeCrmText(CarsDetailPortCostTextBox.Text);
            var newLoadingCost = NormalizeCrmText(CarsDetailLoadingCostTextBox.Text);
            var newTowTruckCost = NormalizeCrmText(CarsDetailTowTruckCostTextBox.Text);
            var newParkingCost = NormalizeCrmText(CarsDetailParkingCostTextBox.Text);
            var newInspectionCost = NormalizeCrmText(CarsDetailInspectionCostTextBox.Text);
            var newReExportCost = NormalizeCrmText(CarsDetailReExportCostTextBox.Text);
            var newExpertiseCost = NormalizeCrmText(CarsDetailExpertiseCostTextBox.Text);
            var newDeliveryCost = NormalizeCrmText(CarsDetailDeliveryCostTextBox.Text);
            var newFirstName = NormalizeCrmText(CarsDetailFirstNameTextBox.Text);
            var newLastName = NormalizeCrmText(CarsDetailLastNameTextBox.Text);
            var newPassport = NormalizeCrmText(CarsDetailPassportTextBox.Text);
            var newAddress = NormalizeCrmText(CarsDetailAddressTextBox.Text);
            var newComment = NormalizeCrmText(CarsDetailCommentTextBox.Text);

            if (selectedCar == null)
            {
                SelectedCarId = string.Empty;
                CarsInputsNeedRefresh = true;
                ShowMessage("Карточка автомобиля не найдена.", "warning");
                BindAll(data);
                return;
            }

            if (!TryValidateCarBusinessRules(data, selectedCar.Id, newVin))
            {
                CarsInputsNeedRefresh = false;
                BindAll(data);
                return;
            }

            var changedBy = ResolveLeadCommentAuthor();

            AppendCarChangeHistory(selectedCar, "Рейс", selectedCar.TripNumber, newTripNumber, changedBy);
            AppendCarChangeHistory(selectedCar, "VIN", selectedCar.Vin, newVin, changedBy);
            AppendCarChangeHistory(selectedCar, "Дилер", selectedCar.Dealer, newDealer, changedBy);
            AppendCarChangeHistory(selectedCar, "Статус", selectedCar.Status, newStatus, changedBy);

            selectedCar.TripNumber = newTripNumber;
            selectedCar.Forwarder = newForwarder;
            selectedCar.Dealer = newDealer;
            selectedCar.Status = newStatus;
            selectedCar.StartPrice = newStartPrice;
            selectedCar.Invoice = newInvoice;
            selectedCar.Year = newYear;
            selectedCar.Brand = newBrand;
            selectedCar.Model = newModel;
            selectedCar.Vin = newVin;
            selectedCar.Location = newLocation;
            selectedCar.Title = newTitle;
            selectedCar.Key = newKey;
            selectedCar.Inspection = newInspection;
            selectedCar.ReExport = newReExport;
            selectedCar.Volume = newVolume;
            selectedCar.Power = newPower;
            selectedCar.PortCost = newPortCost;
            selectedCar.LoadingCost = newLoadingCost;
            selectedCar.TowTruckCost = newTowTruckCost;
            selectedCar.ParkingCost = newParkingCost;
            selectedCar.InspectionCost = newInspectionCost;
            selectedCar.ReExportCost = newReExportCost;
            selectedCar.ExpertiseCost = newExpertiseCost;
            selectedCar.DeliveryCost = newDeliveryCost;
            selectedCar.FirstName = newFirstName;
            selectedCar.LastName = newLastName;
            selectedCar.Passport = newPassport;
            selectedCar.Address = newAddress;
            selectedCar.Comment = newComment;

            CarDealerDirectoryService.EnsureRegistered(newDealer);

            CrmRepository.Save(data);
            CarsInputsNeedRefresh = true;
            ShowMessage("Карточка автомобиля обновлена.", "success");
            BindAll(data);
        }

        protected void CarsBulkAssignTripButton_Click(object sender, EventArgs e)
        {
            ActiveTab = "cars";

            var data = CrmRepository.Load();
            var cars = ApplyDashboardCarScopeFilters(data.Cars)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();
            var selectedIds = GetAvailableStoredIds(CarsSelectedIds, cars.Select(item => item.Id));
            var selectedCars = cars.Where(item => item != null && selectedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            var selectedTripNumber = GetSelectedValue(CarsBulkTripDropDownList);
            var trip = data.Trips.FirstOrDefault(item => item != null && StringEquals(item.Number, selectedTripNumber));

            if (selectedCars.Count == 0)
            {
                ShowMessage("Выберите хотя бы один автомобиль для привязки к рейсу.", "warning");
                BindAll(data);
                return;
            }

            if (trip == null)
            {
                ShowMessage("Выберите существующий рейс для выбранных автомобилей.", "warning");
                BindAll(data);
                return;
            }

            var changedBy = ResolveLeadCommentAuthor();

            foreach (var car in selectedCars)
            {
                AppendCarChangeHistory(car, "Рейс", car.TripNumber, trip.Number, changedBy);
                car.TripNumber = trip.Number;
            }

            CrmRepository.Save(data);
            CarsInputsNeedRefresh = true;
            ShowMessage(string.Format(CultureInfo.CurrentCulture, "К рейсу {0} привязано автомобилей: {1}.", trip.Number, selectedCars.Count), "success");
            BindAll(data);
        }

        protected void TripsRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var trip = e.Item.DataItem as TripRecord;

            if (trip == null || !IsTripsInlineEditRow(trip.Id))
            {
                return;
            }

            var data = CrmRepository.Load();
            BindClientsDropDownList(e.Item.FindControl("InlineTripClientDropDownList") as DropDownList, data.Clients, ResolveClientId(trip, data.Clients));
            BindDriversDropDownList(e.Item.FindControl("InlineTripDriverDropDownList") as DropDownList, data.Drivers, ResolveDriverId(trip, data.Drivers));
            BindVehiclesDropDownList(e.Item.FindControl("InlineTripVehicleDropDownList") as DropDownList, data.FleetVehicles, ResolveVehicleId(trip, data.FleetVehicles));
            BindTripStatusesDropDownList(e.Item.FindControl("InlineTripStatusDropDownList") as ListControl, CurrentCrmSettings.TripStatuses, trip.Status);
        }

        protected void DriversRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
        }

        protected void FleetRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var vehicle = e.Item.DataItem as FleetVehicleRecord;

            if (vehicle == null || !IsFleetInlineEditRow(vehicle.Id))
            {
                return;
            }

            var data = CrmRepository.Load();
            BindFleetDriversCheckBoxList(e.Item.FindControl("InlineFleetDriversCheckBoxList") as CheckBoxList, data.Drivers, ResolveDriverIds(vehicle, data.Drivers));
        }

        protected string GetTabCss(string tabName)
        {
            var classes = new List<string>();

            if (string.Equals(ActiveTab, tabName, StringComparison.OrdinalIgnoreCase))
            {
                classes.Add("active");
            }

            if (string.Equals(tabName, "inquiries", StringComparison.OrdinalIgnoreCase) && HasNewInquiries)
            {
                classes.Add("crm-tab-has-new");
            }

            return string.Join(" ", classes);
        }

        protected string GetPaneCss(string tabName)
        {
            return string.Equals(ActiveTab, tabName, StringComparison.OrdinalIgnoreCase)
                ? "tab-pane fade in active"
                : "tab-pane fade";
        }

        protected string GetDashboardPeriodLabel()
        {
            return GetDashboardPeriodLabel(GetDashboardPeriodDays());
        }

        protected string GetDashboardPeriodButtonCss(int days)
        {
            return GetDashboardPeriodDays() == days ? "btn btn-default active" : "btn btn-default";
        }

        protected string GetDashboardPeriodUrl(int days)
        {
            return BuildCrmTabUrl("dashboard", new[]
            {
                new KeyValuePair<string, string>(DashboardPeriodQueryKey, NormalizeDashboardPeriodDays(days).ToString(CultureInfo.InvariantCulture))
            });
        }

        protected string GetInquiryFilterPanelCss()
        {
            return IsInquiryFilterPanelCollapsed()
                ? "panel panel-default crm-inquiry-filter-panel crm-inquiry-filter-panel-collapsed"
                : "panel panel-default crm-inquiry-filter-panel";
        }

        protected string GetInquiryFilterToggleText()
        {
            return IsInquiryFilterPanelCollapsed() ? "Развернуть фильтры" : "Свернуть фильтры";
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
            var url = BuildDocumentViewerUrl(path);

            if (string.IsNullOrWhiteSpace(url))
            {
                return "<span class=\"text-muted\">Файл недоступен</span>";
            }

            return string.Format("<a href=\"{0}\" target=\"_blank\" rel=\"noopener\">{1}</a>", url, fileName);
        }

        protected string FormatInquiryTimestamp(object value)
        {
            var dateTime = value is DateTime ? (DateTime)value : DateTime.MinValue;

            if (dateTime == DateTime.MinValue)
            {
                return string.Empty;
            }

            return dateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }

        protected string FormatLeadManager(object value)
        {
            var manager = Convert.ToString(value);
            return string.IsNullOrWhiteSpace(manager) ? "Не назначен" : manager.Trim();
        }

        protected string FormatLeadStatus(object value)
        {
            var status = Convert.ToString(value);
            return string.IsNullOrWhiteSpace(status) ? "Новая" : status.Trim();
        }

        protected string FormatLeadText(object value, string emptyText)
        {
            var text = Convert.ToString(value);
            return HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(text) ? emptyText : text.Trim());
        }

        protected string FormatCarGridText(object value)
        {
            var text = Convert.ToString(value);
            return string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
        }

        protected string FormatLeadMultilineText(object value, string emptyText)
        {
            var text = Convert.ToString(value);
            var normalizedText = string.IsNullOrWhiteSpace(text) ? emptyText : text.Trim();
            return HttpUtility.HtmlEncode(normalizedText).Replace("\r\n", "<br />").Replace("\n", "<br />");
        }

        protected string FormatLeadComments(object value)
        {
            var comments = value as IEnumerable<HomeInquiryCommentRecord>;

            if (comments == null)
            {
                return "<span class=\"text-muted\">Комментариев пока нет.</span>";
            }

            var items = comments
                .Where(comment => comment != null && !string.IsNullOrWhiteSpace(comment.Text))
                .OrderByDescending(comment => comment.CreatedAtUtc)
                .ToList();

            if (items.Count == 0)
            {
                return "<span class=\"text-muted\">Комментариев пока нет.</span>";
            }

            var builder = new System.Text.StringBuilder();

            foreach (var comment in items)
            {
                builder.Append("<div class=\"well well-sm\" style=\"margin-bottom: 8px;\">");
                builder.AppendFormat(
                    "<div><strong>{0}</strong> <span class=\"text-muted\">{1}</span></div>",
                    HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(comment.Author) ? "CRM" : comment.Author.Trim()),
                    HttpUtility.HtmlEncode(comment.CreatedAtUtc == DateTime.MinValue
                        ? string.Empty
                        : comment.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")));
                builder.AppendFormat(
                    "<div style=\"white-space: pre-line;\">{0}</div>",
                    HttpUtility.HtmlEncode(comment.Text.Trim()).Replace("\r\n", "<br />").Replace("\n", "<br />"));
                builder.Append("</div>");
            }

            return builder.ToString();
        }

        protected string GetCurrentUserRolesLabel()
        {
            if (Context == null || Context.User == null)
            {
                return "Без ролей";
            }

            var roles = CurrentCrmSettings.AllowedCrmRoles
                .Where(role => Context.User.IsInRole(role))
                .ToList();

            return roles.Count == 0
                ? "Без ролей"
                : string.Join(", ", roles);
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

        private bool HasNewInquiries
        {
            get
            {
                if (!_hasNewInquiries.HasValue)
                {
                    _hasNewInquiries = HomeInquiryService.HasNewInquiries();
                }

                return _hasNewInquiries.Value;
            }
        }

        private CrmSettingsRecord CurrentCrmSettings
        {
            get
            {
                return _crmSettings ?? (_crmSettings = CrmSettingsService.GetCurrent());
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
                TripsEditingIdHiddenField.Value = NormalizeStoredIds(value);
            }
        }

        private string TripsSelectedIds
        {
            get
            {
                return TripsSelectedIdsHiddenField == null ? string.Empty : TripsSelectedIdsHiddenField.Value;
            }
            set
            {
                if (TripsSelectedIdsHiddenField != null)
                {
                    TripsSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool TripsFormVisible
        {
            get
            {
                return GetViewStateBoolean("TripsFormVisible");
            }
            set
            {
                ViewState["TripsFormVisible"] = value;
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
                DriversEditingIdHiddenField.Value = NormalizeStoredIds(value);
            }
        }

        private string DriversSelectedIds
        {
            get
            {
                return DriversSelectedIdsHiddenField == null ? string.Empty : DriversSelectedIdsHiddenField.Value;
            }
            set
            {
                if (DriversSelectedIdsHiddenField != null)
                {
                    DriversSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool DriversFormVisible
        {
            get
            {
                return GetViewStateBoolean("DriversFormVisible");
            }
            set
            {
                ViewState["DriversFormVisible"] = value;
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
                FleetEditingIdHiddenField.Value = NormalizeStoredIds(value);
            }
        }

        private string FleetSelectedIds
        {
            get
            {
                return FleetSelectedIdsHiddenField == null ? string.Empty : FleetSelectedIdsHiddenField.Value;
            }
            set
            {
                if (FleetSelectedIdsHiddenField != null)
                {
                    FleetSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool FleetFormVisible
        {
            get
            {
                return GetViewStateBoolean("FleetFormVisible");
            }
            set
            {
                ViewState["FleetFormVisible"] = value;
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
                ClientsEditingIdHiddenField.Value = NormalizeStoredIds(value);
            }
        }

        private string ClientsSelectedIds
        {
            get
            {
                return ClientsSelectedIdsHiddenField == null ? string.Empty : ClientsSelectedIdsHiddenField.Value;
            }
            set
            {
                if (ClientsSelectedIdsHiddenField != null)
                {
                    ClientsSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool ClientsFormVisible
        {
            get
            {
                return GetViewStateBoolean("ClientsFormVisible");
            }
            set
            {
                ViewState["ClientsFormVisible"] = value;
            }
        }

        private string CarsSelectedIds
        {
            get
            {
                return CarsSelectedIdsHiddenField == null ? string.Empty : CarsSelectedIdsHiddenField.Value;
            }
            set
            {
                if (CarsSelectedIdsHiddenField != null)
                {
                    CarsSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool SettingsInputsNeedRefresh
        {
            get
            {
                return GetViewStateBoolean("SettingsInputsNeedRefresh");
            }
            set
            {
                ViewState["SettingsInputsNeedRefresh"] = value;
            }
        }

        private bool CarsInputsNeedRefresh
        {
            get
            {
                return GetViewStateBoolean("CarsInputsNeedRefresh");
            }
            set
            {
                ViewState["CarsInputsNeedRefresh"] = value;
            }
        }

        private string CrmExcelImportReportHtml
        {
            get
            {
                return Convert.ToString(ViewState["CrmExcelImportReportHtml"]) ?? string.Empty;
            }
            set
            {
                ViewState["CrmExcelImportReportHtml"] = value ?? string.Empty;
            }
        }

        private string SelectedCarId
        {
            get
            {
                return Convert.ToString(ViewState["SelectedCarId"]) ?? string.Empty;
            }
            set
            {
                ViewState["SelectedCarId"] = value ?? string.Empty;
            }
        }

        private string SelectedInquiryId
        {
            get
            {
                if (SettingsSelectedInquiryIdHiddenField != null)
                {
                    var hiddenValue = (SettingsSelectedInquiryIdHiddenField.Value ?? string.Empty).Trim();

                    if (!string.IsNullOrWhiteSpace(hiddenValue))
                    {
                        return hiddenValue;
                    }
                }

                return (Convert.ToString(ViewState["SelectedInquiryId"]) ?? string.Empty).Trim();
            }
            set
            {
                var normalizedValue = (value ?? string.Empty).Trim();

                ViewState["SelectedInquiryId"] = normalizedValue;

                if (SettingsSelectedInquiryIdHiddenField != null)
                {
                    SettingsSelectedInquiryIdHiddenField.Value = normalizedValue;
                }
            }
        }

        private string InquiriesSelectedIds
        {
            get
            {
                return InquiriesSelectedIdsHiddenField == null ? string.Empty : InquiriesSelectedIdsHiddenField.Value;
            }
            set
            {
                if (InquiriesSelectedIdsHiddenField != null)
                {
                    InquiriesSelectedIdsHiddenField.Value = NormalizeStoredIds(value);
                }
            }
        }

        private bool EnsureAuthenticated()
        {
            if (!Request.IsAuthenticated)
            {
                Response.Redirect(FormsAuthentication.LoginUrl + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            return true;
        }

        private bool EnsureCrmAccess()
        {
            if (Context != null
                && Context.User != null
                && CurrentCrmSettings.AllowedCrmRoles.Any(role => Context.User.IsInRole(role)))
            {
                return true;
            }

            Response.Redirect("~/AccessDenied.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }

        private void BindAll()
        {
            BindAll(CrmRepository.Load());
        }

        private void BindAll(CrmDataStore data)
        {
            BindReferenceData(data);
            BindDashboard(data);
            BindTrips(data);
            BindDrivers(data);
            BindFleet(data);
            BindCars(data);
            BindClients(data);
            BindCrmSettings();
            ApplyFormState();
        }

        private void BindCrmSettings()
        {
            var settings = CurrentCrmSettings;
            var homeContentLanguage = GetSelectedSettingsHomeContentLanguage();
            var hasLogo = settings != null && !string.IsNullOrWhiteSpace(settings.LogoPath);
            var editableRoles = GetEditableSettingsRoles(settings);
            var editableStatuses = GetEditableTripStatuses(settings);
            var editablePartners = GetEditableHomePartners(settings);
            var editableReviews = GetEditableHomeReviews(settings);
            var allRecentInquiries = GetInquirySourceRecords();
            var dashboardScopedInquiries = ApplyDashboardInquiryScopeFilters(allRecentInquiries);
            var recentInquiries = ApplyLeadFilters(allRecentInquiries);
            var selectedBulkInquiryStatus = GetSelectedValue(SettingsInquiryBulkStatusDropDownList);
            var selectedInquiry = FindById(recentInquiries, SelectedInquiryId);
            var shouldRefreshInputs = !IsPostBack || SettingsInputsNeedRefresh;

            _hasNewInquiries = HomeInquiryService.HasNewInquiries();

            if (shouldRefreshInputs)
            {
                SettingsCompanyNameTextBox.Text = settings.CompanyName;
                SettingsNewRoleTextBox.Text = string.Empty;
                SettingsNewTripStatusTextBox.Text = string.Empty;
                SettingsNewHomePartnerTextBox.Text = string.Empty;
                SettingsNewHomeReviewQuoteTextBox.Text = string.Empty;
                SettingsNewHomeReviewAuthorTextBox.Text = string.Empty;
                SetSelectedValue(SettingsDefaultTabDropDownList, settings.DefaultCrmTab);
                SetSelectedValue(SettingsHomeContentLanguageDropDownList, homeContentLanguage);
                SettingsAllowedDocumentExtensionsTextBox.Text = FormatSettingsValues(settings.AllowedDocumentExtensions);
                SettingsAllowedLogoExtensionsTextBox.Text = FormatSettingsValues(settings.AllowedLogoExtensions);
                SettingsHomeContactAddressTextBox.Text = CrmSettingsService.GetLocalizedHomeContactAddress(settings, homeContentLanguage);
                SettingsHomeContactPhoneTextBox.Text = settings.HomeContactPhone;
                SettingsHomeContactHoursTextBox.Text = CrmSettingsService.GetLocalizedHomeContactWorkingHours(settings, homeContentLanguage);
                SettingsHomeContactWhatsAppUrlTextBox.Text = settings.HomeContactWhatsAppUrl;
                SettingsRequireUniqueTripNumbersCheckBox.Checked = settings.RequireUniqueTripNumbers;
                SettingsValidatePrepaymentCheckBox.Checked = settings.ValidatePrepaymentAgainstFreight;
            }

            SetSelectedValue(SettingsHomeContentLanguageDropDownList, homeContentLanguage);

            if (selectedInquiry == null)
            {
                SelectedInquiryId = string.Empty;
            }

            InquiriesSelectedIds = KeepStoredIds(InquiriesSelectedIds, recentInquiries.Select(item => item.Id));

            SettingsRolesRepeater.DataSource = editableRoles;
            SettingsRolesRepeater.DataBind();
            SettingsTripStatusesRepeater.DataSource = editableStatuses;
            SettingsTripStatusesRepeater.DataBind();
            SettingsHomePartnersRepeater.DataSource = editablePartners;
            SettingsHomePartnersRepeater.DataBind();
            SettingsHomeReviewsRepeater.DataSource = editableReviews;
            SettingsHomeReviewsRepeater.DataBind();
            CrmExcelImportReportLiteral.Text = CrmExcelImportReportHtml;
            CrmExcelImportReportPanel.Visible = !string.IsNullOrWhiteSpace(CrmExcelImportReportHtml);
            DashboardInquiryFilterNoticeLiteral.Text = BuildDashboardInquiryFilterNoticeHtml();
            BindInquiryBulkStatusDropDownList(selectedBulkInquiryStatus);
            SetInquiryFilterSummary(recentInquiries.Count, dashboardScopedInquiries.Count);
            SettingsInquiryListRepeater.DataSource = recentInquiries;
            SettingsInquiryListRepeater.DataBind();
            SettingsInquiryListRepeater.Visible = recentInquiries.Count > 0;
            var isInquiryDrawerOpen = selectedInquiry != null && string.Equals(ActiveTab, "inquiries", StringComparison.OrdinalIgnoreCase);

            SettingsRecentInquiriesRepeater.DataSource = selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry };
            SettingsRecentInquiriesRepeater.DataBind();
            SettingsRecentInquiriesRepeater.Visible = isInquiryDrawerOpen;
            SettingsInquiryDetailPanel.Visible = isInquiryDrawerOpen;
            SettingsRecentInquiriesEmptyPanel.Visible = recentInquiries.Count == 0;
            BindInquiryEditors(selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry });
            BindTripStatusesDropDownList(SettingsDefaultTripStatusDropDownList, editableStatuses, shouldRefreshInputs ? settings.DefaultTripStatus : GetSelectedValue(SettingsDefaultTripStatusDropDownList), false);
            SettingsCurrentCompanyNameLiteral.Text = HttpUtility.HtmlEncode(settings.CompanyName);
            SettingsInputsNeedRefresh = false;

            SettingsCurrentLogoPanel.Visible = hasLogo;
            SettingsNoLogoPanel.Visible = !hasLogo;

            if (!hasLogo)
            {
                SettingsLogoPreviewImage.ImageUrl = string.Empty;
                SettingsCurrentLogoLink.HRef = string.Empty;
                return;
            }

            var logoUrl = BuildStoredFileUrl(settings.LogoPath, StoredFileService.LogoKind, false);

            if (string.IsNullOrWhiteSpace(logoUrl))
            {
                SettingsCurrentLogoPanel.Visible = false;
                SettingsNoLogoPanel.Visible = true;
                SettingsLogoPreviewImage.ImageUrl = string.Empty;
                SettingsCurrentLogoLink.HRef = string.Empty;
                return;
            }

            SettingsLogoPreviewImage.ImageUrl = logoUrl;
            SettingsCurrentLogoLink.HRef = logoUrl;
        }

        private void BindInquiryPostbackControls()
        {
            var allInquiries = GetInquirySourceRecords();
            var dashboardScopedInquiries = ApplyDashboardInquiryScopeFilters(allInquiries);
            var inquiries = ApplyLeadFilters(allInquiries);
            var selectedInquiryId = GetPostedSelectedInquiryId();
            var selectedBulkInquiryStatus = GetPostedListControlValue(SettingsInquiryBulkStatusDropDownList);

            if (!string.IsNullOrWhiteSpace(selectedInquiryId))
            {
                SelectedInquiryId = selectedInquiryId;
            }

            var selectedInquiry = FindById(inquiries, SelectedInquiryId);

            if (selectedInquiry == null)
            {
                SelectedInquiryId = string.Empty;
            }

            InquiriesSelectedIds = KeepStoredIds(InquiriesSelectedIds, inquiries.Select(item => item.Id));

            DashboardInquiryFilterNoticeLiteral.Text = BuildDashboardInquiryFilterNoticeHtml();
            BindInquiryBulkStatusDropDownList(selectedBulkInquiryStatus);
            SetInquiryFilterSummary(inquiries.Count, dashboardScopedInquiries.Count);
            SettingsInquiryListRepeater.DataSource = inquiries;
            SettingsInquiryListRepeater.DataBind();
            SettingsInquiryListRepeater.Visible = inquiries.Count > 0;
            var isInquiryDrawerOpen = selectedInquiry != null && string.Equals(ActiveTab, "inquiries", StringComparison.OrdinalIgnoreCase);

            SettingsRecentInquiriesRepeater.DataSource = selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry };
            SettingsRecentInquiriesRepeater.DataBind();
            SettingsRecentInquiriesRepeater.Visible = isInquiryDrawerOpen;
            SettingsInquiryDetailPanel.Visible = isInquiryDrawerOpen;
            SettingsRecentInquiriesEmptyPanel.Visible = inquiries.Count == 0;
            BindInquiryEditors(selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry });
        }

        private void BindPostbackListControls()
        {
            var data = CrmRepository.Load();
            var settings = CurrentCrmSettings;

            BindClientsDropDownList(data.Clients, GetPostedListControlValue(TripsClientDropDownList));
            BindDriversDropDownList(data.Drivers, GetPostedListControlValue(TripsDriverDropDownList));
            BindVehiclesDropDownList(data.FleetVehicles, GetPostedListControlValue(TripsVehicleDropDownList));
            BindTripStatusesDropDownList(
                TripsStatusDropDownList,
                settings.TripStatuses,
                string.IsNullOrWhiteSpace(GetPostedListControlValue(TripsStatusDropDownList)) ? settings.DefaultTripStatus : GetPostedListControlValue(TripsStatusDropDownList));
            BindTripsBulkStatusDropDownList(GetPostedListControlValue(TripsBulkStatusDropDownList));
            BindCarsBulkTripDropDownList(data.Trips, GetPostedListControlValue(CarsBulkTripDropDownList));
            BindCarsDetailTripDropDownList(data.Trips, GetPostedListControlValue(CarsDetailTripDropDownList));
            BindFleetDriversCheckBoxList(data.Drivers, GetPostedListControlValues(FleetDriversCheckBoxList));
            BindInquiryBulkStatusDropDownList(GetPostedListControlValue(SettingsInquiryBulkStatusDropDownList));
            BindTripStatusesDropDownList(
                SettingsDefaultTripStatusDropDownList,
                GetEditableTripStatuses(settings),
                string.IsNullOrWhiteSpace(GetPostedListControlValue(SettingsDefaultTripStatusDropDownList)) ? settings.DefaultTripStatus : GetPostedListControlValue(SettingsDefaultTripStatusDropDownList),
                false);
        }

        private void BindReferenceData(CrmDataStore data)
        {
            var selectedClientId = GetSelectedValue(TripsClientDropDownList);
            var selectedDriverId = GetSelectedValue(TripsDriverDropDownList);
            var selectedVehicleId = GetSelectedValue(TripsVehicleDropDownList);
            var selectedStatus = GetSelectedValue(TripsStatusDropDownList);
            var selectedBulkTripStatus = GetSelectedValue(TripsBulkStatusDropDownList);
            var selectedFleetDriverIds = GetSelectedValues(FleetDriversCheckBoxList);

            BindClientsDropDownList(data.Clients, selectedClientId);
            BindDriversDropDownList(data.Drivers, selectedDriverId);
            BindVehiclesDropDownList(data.FleetVehicles, selectedVehicleId);
            BindTripStatusesDropDownList(TripsStatusDropDownList, CurrentCrmSettings.TripStatuses, string.IsNullOrWhiteSpace(selectedStatus) ? CurrentCrmSettings.DefaultTripStatus : selectedStatus);
            BindTripsBulkStatusDropDownList(selectedBulkTripStatus);
            BindFleetDriversCheckBoxList(data.Drivers, selectedFleetDriverIds);
        }

        private void BindTrips(CrmDataStore data)
        {
            var trips = data.Trips
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            TripsEditingId = KeepStoredIds(TripsEditingId, trips.Select(item => item.Id));
            TripsSelectedIds = KeepStoredIds(TripsSelectedIds, trips.Select(item => item.Id));

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

            DriversEditingId = KeepStoredIds(DriversEditingId, drivers.Select(item => item.Id));
            DriversSelectedIds = KeepStoredIds(DriversSelectedIds, drivers.Select(item => item.Id));

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

            FleetEditingId = KeepStoredIds(FleetEditingId, vehicles.Select(item => item.Id));
            FleetSelectedIds = KeepStoredIds(FleetSelectedIds, vehicles.Select(item => item.Id));

            FleetRepeater.DataSource = vehicles;
            FleetRepeater.DataBind();
            FleetRepeater.Visible = vehicles.Count > 0;
            FleetEmptyPanel.Visible = vehicles.Count == 0;
        }

        private void BindCars(CrmDataStore data)
        {
            DashboardCarsFilterNoticeLiteral.Text = BuildDashboardCarsFilterNoticeHtml();

            var cars = ApplyDashboardCarScopeFilters(data.Cars)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();
            var selectedBulkTripNumber = GetSelectedValue(CarsBulkTripDropDownList);
            var shouldRefreshCarInputs = !IsPostBack || CarsInputsNeedRefresh;

            var selectedCar = FindById(cars, SelectedCarId);

            if (selectedCar == null)
            {
                SelectedCarId = string.Empty;
            }

            CarsSelectedIds = KeepStoredIds(CarsSelectedIds, cars.Select(item => item.Id));
            _selectedCar = selectedCar;

            BindCarsBulkTripDropDownList(data.Trips, selectedBulkTripNumber);
            CarsRepeater.DataSource = cars;
            CarsRepeater.DataBind();
            CarsRepeater.Visible = cars.Count > 0;
            CarsEmptyPanel.Visible = cars.Count == 0;
            CarsDetailPanel.Visible = selectedCar != null && string.Equals(ActiveTab, "cars", StringComparison.OrdinalIgnoreCase);

            CarDealerDirectoryService.EnsureRegistered(cars.Select(item => item.Dealer));
            CarsDealerSuggestionsLiteral.Text = BuildDatalistOptionsHtml(CarDealerDirectoryService.GetDealerNames().Concat(cars.Select(item => item.Dealer)));

            if (selectedCar == null)
            {
                CarsDetailTitleLiteral.Text = string.Empty;
                CarsDetailMetaLiteral.Text = string.Empty;
                CarsDetailHistoryLiteral.Text = string.Empty;
                BindCarsDetailTripDropDownList(data.Trips, string.Empty);

                if (shouldRefreshCarInputs)
                {
                    ClearCarDetailInputs();
                }
            }
            else
            {
                CarsDetailTitleLiteral.Text = HttpUtility.HtmlEncode(BuildCarCardTitle(selectedCar));
                CarsDetailMetaLiteral.Text = HttpUtility.HtmlEncode(BuildCarDetailMetaText(selectedCar));
                CarsDetailHistoryLiteral.Text = BuildCarChangeHistoryHtml(selectedCar.ChangeHistory);
                BindCarsDetailTripDropDownList(data.Trips, shouldRefreshCarInputs ? selectedCar.TripNumber : GetPostedListControlValue(CarsDetailTripDropDownList));

                if (shouldRefreshCarInputs)
                {
                    BindCarDetailInputs(selectedCar);
                }
            }

            CarsInputsNeedRefresh = true;
        }

        private void BindClients(CrmDataStore data)
        {
            var clients = data.Clients
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            ClientsEditingId = KeepStoredIds(ClientsEditingId, clients.Select(item => item.Id));
            ClientsSelectedIds = KeepStoredIds(ClientsSelectedIds, clients.Select(item => item.Id));

            ClientsRepeater.DataSource = clients;
            ClientsRepeater.DataBind();
            ClientsRepeater.Visible = clients.Count > 0;
            ClientsEmptyPanel.Visible = clients.Count == 0;
        }

        private void BindClientsDropDownList(IEnumerable<ClientRecord> clients, string selectedValue)
        {
            BindClientsDropDownList(TripsClientDropDownList, clients, selectedValue);
        }

        private static void BindClientsDropDownList(DropDownList control, IEnumerable<ClientRecord> clients, string selectedValue)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();
            control.Items.Add(new ListItem("Выберите клиента", string.Empty));

            foreach (var client in clients.OrderBy(item => item.Name))
            {
                control.Items.Add(new ListItem(client.Name, client.Id));
            }

            SetSelectedValue(control, selectedValue);
        }

        private void BindDriversDropDownList(IEnumerable<DriverRecord> drivers, string selectedValue)
        {
            BindDriversDropDownList(TripsDriverDropDownList, drivers, selectedValue);
        }

        private static void BindDriversDropDownList(DropDownList control, IEnumerable<DriverRecord> drivers, string selectedValue)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();
            control.Items.Add(new ListItem("Выберите водителя", string.Empty));

            foreach (var driver in drivers.OrderBy(item => item.FullName))
            {
                control.Items.Add(new ListItem(driver.FullName, driver.Id));
            }

            SetSelectedValue(control, selectedValue);
        }

        private void BindVehiclesDropDownList(IEnumerable<FleetVehicleRecord> vehicles, string selectedValue)
        {
            BindVehiclesDropDownList(TripsVehicleDropDownList, vehicles, selectedValue);
        }

        private static void BindTripStatusesDropDownList(ListControl control, IEnumerable<string> statuses, string selectedValue, bool preserveMissingSelectedValue = true)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();

            foreach (var status in (statuses ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                control.Items.Add(new ListItem(status, status));
            }

            if (preserveMissingSelectedValue && !string.IsNullOrWhiteSpace(selectedValue) && control.Items.FindByValue(selectedValue) == null)
            {
                control.Items.Add(new ListItem(selectedValue, selectedValue));
            }

            SetSelectedValue(control, selectedValue);
        }

        private void BindTripsBulkStatusDropDownList(string selectedValue)
        {
            if (TripsBulkStatusDropDownList == null)
            {
                return;
            }

            BindTripStatusesDropDownList(TripsBulkStatusDropDownList, CurrentCrmSettings.TripStatuses, selectedValue);
            TripsBulkStatusDropDownList.Items.Insert(0, new ListItem("Изменить статус...", string.Empty));

            if (string.IsNullOrWhiteSpace(selectedValue))
            {
                TripsBulkStatusDropDownList.SelectedIndex = 0;
            }
        }

        private void BindInquiryBulkStatusDropDownList(string selectedValue)
        {
            if (SettingsInquiryBulkStatusDropDownList == null)
            {
                return;
            }

            BindLeadStatusesDropDownList(SettingsInquiryBulkStatusDropDownList, selectedValue);
            SettingsInquiryBulkStatusDropDownList.Items.Insert(0, new ListItem("Изменить статус...", string.Empty));

            if (string.IsNullOrWhiteSpace(selectedValue))
            {
                SettingsInquiryBulkStatusDropDownList.SelectedIndex = 0;
            }
        }

        private void BindCarsBulkTripDropDownList(IEnumerable<TripRecord> trips, string selectedValue)
        {
            if (CarsBulkTripDropDownList == null)
            {
                return;
            }

            CarsBulkTripDropDownList.Items.Clear();
            CarsBulkTripDropDownList.Items.Add(new ListItem("Выберите рейс...", string.Empty));

            foreach (var trip in (trips ?? Enumerable.Empty<TripRecord>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Number))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.Number))
            {
                CarsBulkTripDropDownList.Items.Add(new ListItem(BuildTripListLabel(trip), trip.Number));
            }

            SetSelectedValue(CarsBulkTripDropDownList, selectedValue);

            if (string.IsNullOrWhiteSpace(selectedValue))
            {
                CarsBulkTripDropDownList.SelectedIndex = 0;
            }
        }

        private void BindCarsDetailTripDropDownList(IEnumerable<TripRecord> trips, string selectedValue)
        {
            if (CarsDetailTripDropDownList == null)
            {
                return;
            }

            CarsDetailTripDropDownList.Items.Clear();
            CarsDetailTripDropDownList.Items.Add(new ListItem("Без рейса", string.Empty));

            foreach (var trip in (trips ?? Enumerable.Empty<TripRecord>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Number))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.Number))
            {
                CarsDetailTripDropDownList.Items.Add(new ListItem(BuildTripListLabel(trip), trip.Number));
            }

            var normalizedSelectedValue = NormalizeCrmText(selectedValue);

            if (!string.IsNullOrWhiteSpace(normalizedSelectedValue)
                && CarsDetailTripDropDownList.Items.FindByValue(normalizedSelectedValue) == null)
            {
                CarsDetailTripDropDownList.Items.Insert(1, new ListItem(normalizedSelectedValue + " (текущий)", normalizedSelectedValue));
            }

            SetSelectedValue(CarsDetailTripDropDownList, normalizedSelectedValue);
        }

        private void BindCarDetailInputs(CarRecord car)
        {
            if (car == null)
            {
                ClearCarDetailInputs();
                return;
            }

            CarsDetailForwarderTextBox.Text = car.Forwarder;
            CarsDetailDealerTextBox.Text = car.Dealer;
            CarsDetailStatusTextBox.Text = car.Status;
            CarsDetailStartPriceTextBox.Text = car.StartPrice;
            CarsDetailInvoiceTextBox.Text = car.Invoice;
            CarsDetailYearTextBox.Text = car.Year;
            CarsDetailBrandTextBox.Text = car.Brand;
            CarsDetailModelTextBox.Text = car.Model;
            CarsDetailVinTextBox.Text = car.Vin;
            CarsDetailLocationTextBox.Text = car.Location;
            CarsDetailTitleTextBox.Text = car.Title;
            CarsDetailKeyTextBox.Text = car.Key;
            CarsDetailInspectionTextBox.Text = car.Inspection;
            CarsDetailReExportTextBox.Text = car.ReExport;
            CarsDetailVolumeTextBox.Text = car.Volume;
            CarsDetailPowerTextBox.Text = car.Power;
            CarsDetailPortCostTextBox.Text = car.PortCost;
            CarsDetailLoadingCostTextBox.Text = car.LoadingCost;
            CarsDetailTowTruckCostTextBox.Text = car.TowTruckCost;
            CarsDetailParkingCostTextBox.Text = car.ParkingCost;
            CarsDetailInspectionCostTextBox.Text = car.InspectionCost;
            CarsDetailReExportCostTextBox.Text = car.ReExportCost;
            CarsDetailExpertiseCostTextBox.Text = car.ExpertiseCost;
            CarsDetailDeliveryCostTextBox.Text = car.DeliveryCost;
            CarsDetailFirstNameTextBox.Text = car.FirstName;
            CarsDetailLastNameTextBox.Text = car.LastName;
            CarsDetailPassportTextBox.Text = car.Passport;
            CarsDetailAddressTextBox.Text = car.Address;
            CarsDetailCommentTextBox.Text = car.Comment;
        }

        private void ClearCarDetailInputs()
        {
            CarsDetailForwarderTextBox.Text = string.Empty;
            CarsDetailDealerTextBox.Text = string.Empty;
            CarsDetailStatusTextBox.Text = string.Empty;
            CarsDetailStartPriceTextBox.Text = string.Empty;
            CarsDetailInvoiceTextBox.Text = string.Empty;
            CarsDetailYearTextBox.Text = string.Empty;
            CarsDetailBrandTextBox.Text = string.Empty;
            CarsDetailModelTextBox.Text = string.Empty;
            CarsDetailVinTextBox.Text = string.Empty;
            CarsDetailLocationTextBox.Text = string.Empty;
            CarsDetailTitleTextBox.Text = string.Empty;
            CarsDetailKeyTextBox.Text = string.Empty;
            CarsDetailInspectionTextBox.Text = string.Empty;
            CarsDetailReExportTextBox.Text = string.Empty;
            CarsDetailVolumeTextBox.Text = string.Empty;
            CarsDetailPowerTextBox.Text = string.Empty;
            CarsDetailPortCostTextBox.Text = string.Empty;
            CarsDetailLoadingCostTextBox.Text = string.Empty;
            CarsDetailTowTruckCostTextBox.Text = string.Empty;
            CarsDetailParkingCostTextBox.Text = string.Empty;
            CarsDetailInspectionCostTextBox.Text = string.Empty;
            CarsDetailReExportCostTextBox.Text = string.Empty;
            CarsDetailExpertiseCostTextBox.Text = string.Empty;
            CarsDetailDeliveryCostTextBox.Text = string.Empty;
            CarsDetailFirstNameTextBox.Text = string.Empty;
            CarsDetailLastNameTextBox.Text = string.Empty;
            CarsDetailPassportTextBox.Text = string.Empty;
            CarsDetailAddressTextBox.Text = string.Empty;
            CarsDetailCommentTextBox.Text = string.Empty;
        }

        private static void BindVehiclesDropDownList(DropDownList control, IEnumerable<FleetVehicleRecord> vehicles, string selectedValue)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();
            control.Items.Add(new ListItem("Выберите автомобиль", string.Empty));

            foreach (var vehicle in vehicles.OrderBy(item => item.CarBrand).ThenBy(item => item.CarModel))
            {
                control.Items.Add(new ListItem(BuildVehicleName(vehicle), vehicle.Id));
            }

            SetSelectedValue(control, selectedValue);
        }

        private void BindFleetDriversCheckBoxList(IEnumerable<DriverRecord> drivers, IEnumerable<string> selectedValues)
        {
            BindFleetDriversCheckBoxList(FleetDriversCheckBoxList, drivers, selectedValues);
        }

        private static void BindFleetDriversCheckBoxList(CheckBoxList control, IEnumerable<DriverRecord> drivers, IEnumerable<string> selectedValues)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();

            foreach (var driver in drivers.OrderBy(item => item.FullName))
            {
                control.Items.Add(new ListItem(driver.FullName, driver.Id));
            }

            SetSelectedValues(control, selectedValues);
        }

        private void ApplyFormState()
        {
            var isTripsEditing = IsEditing(TripsEditingId);
            var isDriversEditing = IsEditing(DriversEditingId);
            var isFleetEditing = IsEditing(FleetEditingId);
            var isClientsEditing = IsEditing(ClientsEditingId);
            var isTripsFormVisible = TripsFormVisible;
            var isDriversFormVisible = DriversFormVisible;
            var isFleetFormVisible = FleetFormVisible;
            var isClientsFormVisible = ClientsFormVisible;

            TripsFormTitleLiteral.Text = "Новый рейс";
            AddTripButton.Text = "Добавить рейс";
            TripsCancelEditButton.Text = "Скрыть форму";
            TripsCancelEditButton.Visible = isTripsFormVisible;

            DriversFormTitleLiteral.Text = "Новый водитель";
            AddDriverButton.Text = "Добавить водителя";
            DriversCancelEditButton.Text = "Скрыть форму";
            DriversCancelEditButton.Visible = isDriversFormVisible;

            FleetFormTitleLiteral.Text = "Новый автомобиль";
            AddFleetButton.Text = "Добавить автомобиль";
            FleetCancelEditButton.Text = "Скрыть форму";
            FleetCancelEditButton.Visible = isFleetFormVisible;

            ClientsFormTitleLiteral.Text = "Новый клиент";
            AddClientButton.Text = "Добавить клиента";
            ClientsCancelEditButton.Text = "Скрыть форму";
            ClientsCancelEditButton.Visible = isClientsFormVisible;
        }

        protected string GetFormColumnCss(string section)
        {
            if (string.Equals(section, "trips", StringComparison.OrdinalIgnoreCase))
            {
                return TripsFormVisible ? "col-md-4" : "hidden";
            }

            if (string.Equals(section, "drivers", StringComparison.OrdinalIgnoreCase))
            {
                return DriversFormVisible ? "col-md-5" : "hidden";
            }

            if (string.Equals(section, "fleet", StringComparison.OrdinalIgnoreCase))
            {
                return FleetFormVisible ? "col-md-5" : "hidden";
            }

            if (string.Equals(section, "clients", StringComparison.OrdinalIgnoreCase))
            {
                return ClientsFormVisible ? "col-md-4" : "hidden";
            }

            return string.Empty;
        }

        protected string GetListColumnCss(string section)
        {
            if (string.Equals(section, "trips", StringComparison.OrdinalIgnoreCase))
            {
                return TripsFormVisible ? "col-md-8" : "col-md-12";
            }

            if (string.Equals(section, "drivers", StringComparison.OrdinalIgnoreCase))
            {
                return DriversFormVisible ? "col-md-7" : "col-md-12";
            }

            if (string.Equals(section, "fleet", StringComparison.OrdinalIgnoreCase))
            {
                return FleetFormVisible ? "col-md-7" : "col-md-12";
            }

            if (string.Equals(section, "clients", StringComparison.OrdinalIgnoreCase))
            {
                return ClientsFormVisible ? "col-md-8" : "col-md-12";
            }

            return string.Empty;
        }

        private void ShowMessage(string message, string type)
        {
            PageAlertPanel.Visible = true;
            PageAlertPanel.CssClass = "alert alert-" + type;
            PageAlertLiteral.Text = HttpUtility.HtmlEncode(message);
        }

        private string BuildCrmExcelImportReportHtml(CrmExcelImportResult importResult)
        {
            if (importResult == null || importResult.SheetResults == null || importResult.SheetResults.Count == 0)
            {
                return string.Empty;
            }

            const int maxWarningsPerSheet = 12;
            var sheetResults = importResult.SheetResults.Where(item => item != null).ToList();

            if (sheetResults.Count == 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("<div class=\"crm-excel-import-report-summary\">");
            builder.AppendFormat(
                CultureInfo.CurrentCulture,
                "Распознано листов: <strong>{0}</strong>. Всего пропущено строк: <strong>{1}</strong>.",
                importResult.RecognizedSheetCount.ToString(CultureInfo.CurrentCulture),
                importResult.SkippedRows.ToString(CultureInfo.CurrentCulture));
            builder.Append("</div>");
            builder.Append("<div class=\"table-responsive\"><table class=\"table table-bordered table-striped crm-excel-import-report-table\"><thead><tr><th>Лист</th><th>Обработано</th><th>Добавлено</th><th>Обновлено</th><th>Без изменений</th><th>Пропущено</th></tr></thead><tbody>");

            foreach (var sheetResult in sheetResults)
            {
                builder.Append("<tr>");
                builder.AppendFormat("<td>{0}</td>", HttpUtility.HtmlEncode(sheetResult.DisplayName));
                builder.AppendFormat("<td>{0}</td>", sheetResult.ProcessedRows.ToString(CultureInfo.CurrentCulture));
                builder.AppendFormat("<td>{0}</td>", sheetResult.AddedRows.ToString(CultureInfo.CurrentCulture));
                builder.AppendFormat("<td>{0}</td>", sheetResult.UpdatedRows.ToString(CultureInfo.CurrentCulture));
                builder.AppendFormat("<td>{0}</td>", sheetResult.UnchangedRows.ToString(CultureInfo.CurrentCulture));
                builder.AppendFormat("<td>{0}</td>", sheetResult.SkippedRows.ToString(CultureInfo.CurrentCulture));
                builder.Append("</tr>");
            }

            builder.Append("</tbody></table></div>");

            var sheetsWithWarnings = sheetResults.Where(item => item.RowWarnings != null && item.RowWarnings.Count > 0).ToList();

            if (sheetsWithWarnings.Count > 0)
            {
                builder.Append("<div class=\"crm-excel-import-report-warnings\">");

                foreach (var sheetResult in sheetsWithWarnings)
                {
                    builder.Append("<div class=\"crm-excel-import-report-sheet\">");
                    builder.AppendFormat("<div class=\"crm-excel-import-report-sheet-title\">{0}</div>", HttpUtility.HtmlEncode(sheetResult.DisplayName));
                    builder.Append("<ul class=\"crm-excel-import-report-warning-list\">");

                    foreach (var warning in sheetResult.RowWarnings.Take(maxWarningsPerSheet))
                    {
                        builder.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(warning));
                    }

                    if (sheetResult.RowWarnings.Count > maxWarningsPerSheet)
                    {
                        builder.AppendFormat(
                            CultureInfo.CurrentCulture,
                            "<li>Еще предупреждений: {0}</li>",
                            HttpUtility.HtmlEncode((sheetResult.RowWarnings.Count - maxWarningsPerSheet).ToString(CultureInfo.CurrentCulture)));
                    }

                    builder.Append("</ul></div>");
                }

                builder.Append("</div>");
            }

            return builder.ToString();
        }

        private void HideMessage()
        {
            PageAlertPanel.Visible = false;
            PageAlertLiteral.Text = string.Empty;
        }

        private bool IsValidationGroupValid(string validationGroup)
        {
            Page.Validate(validationGroup);

            foreach (BaseValidator validator in Page.GetValidators(validationGroup))
            {
                if (!validator.IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartTripEdit(string tripId)
        {
            var data = CrmRepository.Load();
            var trip = FindById(data.Trips, tripId);

            if (trip == null)
            {
                TripsFormVisible = false;
                ShowMessage("Рейс не найден.", "warning");
                BindAll(data);
                return;
            }

            TripsEditingId = SerializeStoredIds(new[] { trip.Id });
            TripsFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        private void StartDriverEdit(string driverId)
        {
            var data = CrmRepository.Load();
            var driver = FindById(data.Drivers, driverId);

            if (driver == null)
            {
                DriversFormVisible = false;
                ShowMessage("Водитель не найден.", "warning");
                BindAll(data);
                return;
            }

            DriversEditingId = SerializeStoredIds(new[] { driver.Id });
            DriversFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        private void StartFleetEdit(string vehicleId)
        {
            var data = CrmRepository.Load();
            var vehicle = FindById(data.FleetVehicles, vehicleId);

            if (vehicle == null)
            {
                FleetFormVisible = false;
                ShowMessage("Автомобиль не найден.", "warning");
                BindAll(data);
                return;
            }

            FleetEditingId = SerializeStoredIds(new[] { vehicle.Id });
            FleetFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        private void StartClientEdit(string clientId)
        {
            var data = CrmRepository.Load();
            var client = FindById(data.Clients, clientId);

            if (client == null)
            {
                ClientsFormVisible = false;
                ShowMessage("Клиент не найден.", "warning");
                BindAll(data);
                return;
            }

            ClientsEditingId = SerializeStoredIds(new[] { client.Id });
            ClientsFormVisible = false;
            BindAll(data);
            ApplyFormState();
        }

        private void SaveInlineTrip(RepeaterItem item, string tripId)
        {
            var number = GetRepeaterTextBoxValue(item, "InlineTripNumberTextBox");
            var country = GetRepeaterTextBoxValue(item, "InlineTripCountryTextBox");
            var freight = GetRepeaterTextBoxValue(item, "InlineTripFreightTextBox");
            var prepayment = GetRepeaterTextBoxValue(item, "InlineTripPrepaymentTextBox");
            var startDateText = GetRepeaterTextBoxValue(item, "InlineTripStartDateTextBox");
            var endDateText = GetRepeaterTextBoxValue(item, "InlineTripEndDateTextBox");
            var clientId = GetRepeaterSelectedValue(item, "InlineTripClientDropDownList");
            var driverId = GetRepeaterSelectedValue(item, "InlineTripDriverDropDownList");
            var vehicleId = GetRepeaterSelectedValue(item, "InlineTripVehicleDropDownList");
            var status = GetRepeaterSelectedValue(item, "InlineTripStatusDropDownList");

            if (string.IsNullOrWhiteSpace(number)
                || string.IsNullOrWhiteSpace(country)
                || string.IsNullOrWhiteSpace(startDateText)
                || string.IsNullOrWhiteSpace(endDateText)
                || string.IsNullOrWhiteSpace(freight)
                || string.IsNullOrWhiteSpace(prepayment)
                || string.IsNullOrWhiteSpace(clientId)
                || string.IsNullOrWhiteSpace(driverId)
                || string.IsNullOrWhiteSpace(vehicleId))
            {
                ShowMessage("Заполните все поля рейса прямо в строке таблицы.", "warning");
                return;
            }

            DateTime startDate;
            DateTime endDate;

            if (!TryValidateTripValues(startDateText, endDateText, freight, prepayment, out startDate, out endDate))
            {
                return;
            }

            var data = CrmRepository.Load();
            var existingTrip = FindById(data.Trips, tripId);

            if (existingTrip == null)
            {
                ClearTripsEditState();
                ShowMessage("Редактируемый рейс не найден.", "warning");
                BindAll(data);
                return;
            }

            var client = data.Clients.FirstOrDefault(record => StringEquals(record.Id, clientId));
            var driver = data.Drivers.FirstOrDefault(record => StringEquals(record.Id, driverId));
            var vehicle = data.FleetVehicles.FirstOrDefault(record => StringEquals(record.Id, vehicleId));

            if (client == null || driver == null || vehicle == null)
            {
                ShowMessage("Для сохранения рейса выберите существующего клиента, водителя и автомобиль.", "warning");
                return;
            }

            existingTrip.Number = number;
            existingTrip.ClientId = client.Id;
            existingTrip.ClientName = client.Name;
            existingTrip.Status = status;
            existingTrip.Country = country;
            existingTrip.VehicleId = vehicle.Id;
            existingTrip.VehicleName = BuildVehicleName(vehicle);
            existingTrip.DriverId = driver.Id;
            existingTrip.DriverName = driver.FullName;
            existingTrip.StartDate = startDate.ToString("yyyy-MM-dd");
            existingTrip.EndDate = endDate.ToString("yyyy-MM-dd");
            existingTrip.Freight = freight;
            existingTrip.Prepayment = prepayment;

            CrmRepository.Save(data);
            TripsEditingId = RemoveStoredIds(TripsEditingId, new[] { tripId });
            ShowMessage("Рейс обновлен.", "success");
            BindAll();
        }

        private void SaveInlineDriver(RepeaterItem item, string driverId)
        {
            var fullName = GetRepeaterTextBoxValue(item, "InlineDriverFullNameTextBox");
            var birthDate = GetRepeaterTextBoxValue(item, "InlineDriverBirthDateTextBox");
            var phone = GetRepeaterTextBoxValue(item, "InlineDriverPhoneTextBox");
            var address = GetRepeaterTextBoxValue(item, "InlineDriverAddressTextBox");

            if (string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(birthDate)
                || string.IsNullOrWhiteSpace(phone)
                || string.IsNullOrWhiteSpace(address))
            {
                ShowMessage("Заполните все поля водителя прямо в строке таблицы.", "warning");
                return;
            }

            var data = CrmRepository.Load();
            var existingDriver = FindById(data.Drivers, driverId);

            if (existingDriver == null)
            {
                ClearDriversEditState();
                ShowMessage("Редактируемый водитель не найден.", "warning");
                BindAll(data);
                return;
            }

            var previousName = existingDriver.FullName;
            var previousPassportPath = existingDriver.PassportScanPath;
            var previousLicensePath = existingDriver.LicenseScanPath;
            var passportPath = SaveUploadedFile(GetRepeaterFileUpload(item, "InlineDriverPassportUpload"), "Drivers/Passports", fullName + "_passport", CurrentCrmSettings.AllowedDocumentExtensions);
            var licensePath = SaveUploadedFile(GetRepeaterFileUpload(item, "InlineDriverLicenseUpload"), "Drivers/Licenses", fullName + "_license", CurrentCrmSettings.AllowedDocumentExtensions);

            existingDriver.FullName = fullName;
            existingDriver.BirthDate = birthDate;
            existingDriver.PhoneNumber = phone;
            existingDriver.Address = address;
            existingDriver.PassportScanPath = string.IsNullOrWhiteSpace(passportPath) ? existingDriver.PassportScanPath : passportPath;
            existingDriver.LicenseScanPath = string.IsNullOrWhiteSpace(licensePath) ? existingDriver.LicenseScanPath : licensePath;

            SyncDriverReferences(data, existingDriver, previousName);
            CrmRepository.Save(data);
            DeleteStoredFileIfReplaced(previousPassportPath, existingDriver.PassportScanPath);
            DeleteStoredFileIfReplaced(previousLicensePath, existingDriver.LicenseScanPath);
            DriversEditingId = RemoveStoredIds(DriversEditingId, new[] { driverId });
            ShowMessage("Водитель обновлен.", "success");
            BindAll();
        }

        private void SaveInlineFleet(RepeaterItem item, string vehicleId)
        {
            var carBrand = GetRepeaterTextBoxValue(item, "InlineFleetCarBrandTextBox");
            var carModel = GetRepeaterTextBoxValue(item, "InlineFleetCarModelTextBox");
            var licensePlate = GetRepeaterTextBoxValue(item, "InlineFleetLicensePlateTextBox");
            var vinCode = GetRepeaterTextBoxValue(item, "InlineFleetVinTextBox");
            var trailerBrand = GetRepeaterTextBoxValue(item, "InlineFleetTrailerBrandTextBox");
            var trailerModel = GetRepeaterTextBoxValue(item, "InlineFleetTrailerModelTextBox");
            var trailerLicensePlate = GetRepeaterTextBoxValue(item, "InlineFleetTrailerLicensePlateTextBox");

            if (string.IsNullOrWhiteSpace(carBrand)
                || string.IsNullOrWhiteSpace(carModel)
                || string.IsNullOrWhiteSpace(licensePlate)
                || string.IsNullOrWhiteSpace(vinCode))
            {
                ShowMessage("Заполните обязательные поля автомобиля прямо в строке таблицы.", "warning");
                return;
            }

            var data = CrmRepository.Load();
            var existingVehicle = FindById(data.FleetVehicles, vehicleId);

            if (existingVehicle == null)
            {
                ClearFleetEditState();
                ShowMessage("Редактируемый автомобиль не найден.", "warning");
                BindAll(data);
                return;
            }

            var selectedDriverIds = GetRepeaterSelectedValues(item, "InlineFleetDriversCheckBoxList");
            var selectedDrivers = data.Drivers
                .Where(driver => selectedDriverIds.Contains(driver.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var previousVehicleName = BuildVehicleName(existingVehicle);
            var previousDocumentsPath = existingVehicle.DocumentsScanPath;
            var documentsPath = SaveUploadedFile(GetRepeaterFileUpload(item, "InlineFleetDocumentsUpload"), "Fleet/Documents", licensePlate, CurrentCrmSettings.AllowedDocumentExtensions);

            existingVehicle.CarBrand = carBrand;
            existingVehicle.CarModel = carModel;
            existingVehicle.LicensePlate = licensePlate;
            existingVehicle.VinCode = vinCode;
            existingVehicle.TrailerBrand = trailerBrand;
            existingVehicle.TrailerModel = trailerModel;
            existingVehicle.TrailerLicensePlate = trailerLicensePlate;
            existingVehicle.AssignedDriverIds = selectedDrivers.Select(driver => driver.Id).ToList();
            existingVehicle.AssignedDriverNames = selectedDrivers.Select(driver => driver.FullName).ToList();
            existingVehicle.DocumentsScanPath = string.IsNullOrWhiteSpace(documentsPath) ? existingVehicle.DocumentsScanPath : documentsPath;

            SyncVehicleReferences(data, existingVehicle, previousVehicleName);
            CrmRepository.Save(data);
            DeleteStoredFileIfReplaced(previousDocumentsPath, existingVehicle.DocumentsScanPath);
            FleetEditingId = RemoveStoredIds(FleetEditingId, new[] { vehicleId });
            ShowMessage("Автомобиль обновлен.", "success");
            BindAll();
        }

        private void SaveInlineClient(RepeaterItem item, string clientId)
        {
            var name = GetRepeaterTextBoxValue(item, "InlineClientNameTextBox");
            var direction = GetRepeaterTextBoxValue(item, "InlineClientDirectionTextBox");
            var manager = GetRepeaterTextBoxValue(item, "InlineClientManagerTextBox");
            var phone = GetRepeaterTextBoxValue(item, "InlineClientPhoneTextBox");
            var email = GetRepeaterTextBoxValue(item, "InlineClientEmailTextBox");

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(direction)
                || string.IsNullOrWhiteSpace(phone))
            {
                ShowMessage("Заполните обязательные поля клиента прямо в строке таблицы.", "warning");
                return;
            }

            var data = CrmRepository.Load();
            var existingClient = FindById(data.Clients, clientId);

            if (existingClient == null)
            {
                ClearClientsEditState();
                ShowMessage("Редактируемый клиент не найден.", "warning");
                BindAll(data);
                return;
            }

            var previousName = existingClient.Name;

            existingClient.Name = name;
            existingClient.Direction = direction;
            existingClient.Manager = manager;
            existingClient.PhoneNumber = phone;
            existingClient.Email = email;

            SyncClientReferences(data, existingClient, previousName);
            CrmRepository.Save(data);
            ClientsEditingId = RemoveStoredIds(ClientsEditingId, new[] { clientId });
            ShowMessage("Клиент обновлен.", "success");
            BindAll();
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

            if (ContainsStoredId(TripsEditingId, tripId))
            {
                ClearTripsForm();
                TripsEditingId = RemoveStoredIds(TripsEditingId, new[] { tripId });
                TripsFormVisible = false;
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

            var passportPath = driver.PassportScanPath;
            var licensePath = driver.LicenseScanPath;
            data.Drivers.Remove(driver);
            RemoveDriverReferences(data, driver);

            if (ContainsStoredId(DriversEditingId, driverId))
            {
                ClearDriversForm();
                DriversEditingId = RemoveStoredIds(DriversEditingId, new[] { driverId });
                DriversFormVisible = false;
            }

            CrmRepository.Save(data);
            DeleteStoredFiles(passportPath, licensePath);
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

            var documentsPath = vehicle.DocumentsScanPath;
            data.FleetVehicles.Remove(vehicle);
            RemoveVehicleReferences(data, vehicle);

            if (ContainsStoredId(FleetEditingId, vehicleId))
            {
                ClearFleetForm();
                FleetEditingId = RemoveStoredIds(FleetEditingId, new[] { vehicleId });
                FleetFormVisible = false;
            }

            CrmRepository.Save(data);
            DeleteStoredFiles(documentsPath);
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

            if (ContainsStoredId(ClientsEditingId, clientId))
            {
                ClearClientsForm();
                ClientsEditingId = RemoveStoredIds(ClientsEditingId, new[] { clientId });
                ClientsFormVisible = false;
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
            SetSelectedValue(TripsStatusDropDownList, CurrentCrmSettings.DefaultTripStatus);
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

        private bool GetViewStateBoolean(string key)
        {
            var value = ViewState[key];
            return value is bool && (bool)value;
        }

        private string SaveUploadedFile(FileUpload upload, string relativeFolder, string filePrefix, IEnumerable<string> allowedExtensions = null)
        {
            if (upload == null || !upload.HasFile)
            {
                return string.Empty;
            }

            var extension = GetValidatedFileExtension(upload.FileName, allowedExtensions);

            var safePrefix = MakeSafeFileName(string.IsNullOrWhiteSpace(filePrefix) ? "document" : filePrefix);
            var fileName = safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
            var storedPath = StoredFileService.BuildProtectedAppRelativePath(relativeFolder, fileName);
            var physicalPath = Server.MapPath(storedPath);
            var physicalFolder = Path.GetDirectoryName(physicalPath);

            Directory.CreateDirectory(physicalFolder);

            upload.SaveAs(physicalPath);

            return storedPath;
        }

        private string SaveLogoFile(FileUpload upload, string relativeFolder, string filePrefix)
        {
            return SaveLogoFile(upload, relativeFolder, filePrefix, CurrentCrmSettings.AllowedLogoExtensions);
        }

        private string SaveLogoFile(FileUpload upload, string relativeFolder, string filePrefix, IEnumerable<string> allowedLogoExtensions)
        {
            if (upload == null || !upload.HasFile)
            {
                return string.Empty;
            }

            var extension = GetValidatedFileExtension(upload.FileName, allowedLogoExtensions);

            if (!CanResizeLogo(extension))
            {
                return SaveUploadedFile(upload, relativeFolder, filePrefix, allowedLogoExtensions);
            }

            return SaveScaledImageFile(upload, relativeFolder, filePrefix, extension, MaxStoredLogoWidth, MaxStoredLogoHeight);
        }

        private string SaveScaledImageFile(FileUpload upload, string relativeFolder, string filePrefix, string extension, int maxWidth, int maxHeight)
        {
            var safePrefix = MakeSafeFileName(string.IsNullOrWhiteSpace(filePrefix) ? "document" : filePrefix);
            var fileName = safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
            var storedPath = StoredFileService.BuildProtectedAppRelativePath(relativeFolder, fileName);
            var physicalPath = Server.MapPath(storedPath);
            var physicalFolder = Path.GetDirectoryName(physicalPath);

            Directory.CreateDirectory(physicalFolder);

            try
            {
                using (var sourceImage = System.Drawing.Image.FromStream(upload.PostedFile.InputStream))
                {
                    var targetSize = CalculateScaledSize(sourceImage.Width, sourceImage.Height, maxWidth, maxHeight);

                    using (var resizedImage = ResizeImage(sourceImage, targetSize.Width, targetSize.Height, extension))
                    {
                        SaveRasterImage(resizedImage, physicalPath, extension);
                    }
                }
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Не удалось обработать файл как изображение.");
            }

            return storedPath;
        }

        private string BuildStoredFileUrl(string storedPath, string kind, bool download)
        {
            var appRelativeUrl = StoredFileService.BuildAccessUrl(storedPath, kind, download);
            return string.IsNullOrWhiteSpace(appRelativeUrl) ? string.Empty : ResolveUrl(appRelativeUrl);
        }

        private string BuildDocumentViewerUrl(string storedPath)
        {
            var appRelativeUrl = StoredFileService.BuildDocumentViewerUrl(storedPath);
            return string.IsNullOrWhiteSpace(appRelativeUrl) ? string.Empty : ResolveUrl(appRelativeUrl);
        }

        private bool TryValidateTripForm(out DateTime startDate, out DateTime endDate)
        {
            return TryValidateTripValues(TripsStartDateTextBox.Text, TripsEndDateTextBox.Text, TripsFreightTextBox.Text, TripsPrepaymentTextBox.Text, out startDate, out endDate);
        }

        private bool TryValidateTripBusinessRules(CrmDataStore data, string currentTripId, string tripNumber, string freightText, string prepaymentText)
        {
            if (CurrentCrmSettings.RequireUniqueTripNumbers
                && data.Trips.Any(item => !StringEquals(item.Id, currentTripId)
                    && string.Equals((item.Number ?? string.Empty).Trim(), (tripNumber ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage("Рейс с таким номером уже существует.", "warning");
                return false;
            }

            if (CurrentCrmSettings.ValidatePrepaymentAgainstFreight)
            {
                decimal freight;
                decimal prepayment;

                if (TryParseAmount(freightText, out freight)
                    && TryParseAmount(prepaymentText, out prepayment)
                    && prepayment > freight)
                {
                    ShowMessage("Предоплата не может быть больше фрахта.", "warning");
                    return false;
                }
            }

            return true;
        }

        private bool TryValidateCarBusinessRules(CrmDataStore data, string currentCarId, string vin)
        {
            var normalizedVin = NormalizeCrmText(vin);

            if (!string.IsNullOrWhiteSpace(normalizedVin)
                && data.Cars.Any(item => item != null
                    && !StringEquals(item.Id, currentCarId)
                    && string.Equals(NormalizeCrmText(item.Vin), normalizedVin, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage("Автомобиль с таким VIN уже существует.", "warning");
                return false;
            }

            return true;
        }

        private bool TryValidateTripValues(string startDateText, string endDateText, string freightText, string prepaymentText, out DateTime startDate, out DateTime endDate)
        {
            startDate = DateTime.MinValue;
            endDate = DateTime.MinValue;

            if (!DateTime.TryParse(startDateText, out startDate)
                || !DateTime.TryParse(endDateText, out endDate))
            {
                ShowMessage("Укажите корректные даты рейса.", "warning");
                return false;
            }

            if (endDate < startDate)
            {
                ShowMessage("Дата окончания не может быть раньше даты начала.", "warning");
                return false;
            }

            decimal ignoredValue;

            if (!TryParseAmount(freightText, out ignoredValue))
            {
                ShowMessage("Фрахт должен быть числом.", "warning");
                return false;
            }

            if (!TryParseAmount(prepaymentText, out ignoredValue))
            {
                ShowMessage("Предоплата должна быть числом.", "warning");
                return false;
            }

            return true;
        }

        private void DeleteStoredFileIfReplaced(string previousPath, string currentPath)
        {
            var normalizedPreviousPath = StoredFileService.NormalizeStoredPath(previousPath);
            var normalizedCurrentPath = StoredFileService.NormalizeStoredPath(currentPath);

            if (StringEquals(normalizedPreviousPath, normalizedCurrentPath))
            {
                return;
            }

            DeleteStoredFiles(previousPath);
        }

        private void DeleteStoredFiles(params string[] storedPaths)
        {
            foreach (var storedPath in storedPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                string physicalPath;

                if (!StoredFileService.TryMapStoredPath(Server, storedPath, out physicalPath))
                {
                    continue;
                }

                try
                {
                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private static bool TryParseAmount(string rawValue, out decimal value)
        {
            var normalizedValue = (rawValue ?? string.Empty).Trim();

            return decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string GetValidatedFileExtension(string fileName, IEnumerable<string> allowedExtensions)
        {
            var extension = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();
            var normalizedExtensions = (allowedExtensions ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedExtensions.Count > 0 && !normalizedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Разрешены только файлы: " + string.Join(", ", normalizedExtensions));
            }

            return extension;
        }

        private string ResolveDefaultCrmTab()
        {
            var requestedTab = Request == null ? string.Empty : Convert.ToString(Request.QueryString["tab"]);

            if (IsSupportedCrmTab(requestedTab))
            {
                return requestedTab.Trim().ToLowerInvariant();
            }

            return string.IsNullOrWhiteSpace(CurrentCrmSettings.DefaultCrmTab) ? DefaultTab : CurrentCrmSettings.DefaultCrmTab;
        }

        private static bool IsSupportedCrmTab(string tabName)
        {
            var normalizedTab = (tabName ?? string.Empty).Trim().ToLowerInvariant();

            switch (normalizedTab)
            {
                case "dashboard":
                case "trips":
                case "drivers":
                case "fleet":
                case "cars":
                case "clients":
                case "inquiries":
                case "settings":
                    return true;
                default:
                    return false;
            }
        }

        private void BindDashboard(CrmDataStore data)
        {
            var dashboardPeriodDays = GetDashboardPeriodDays();
            var periodStartUtc = GetDashboardPeriodStartUtc(dashboardPeriodDays);
            var inquiries = HomeInquiryService.GetRecent(500)
                .Where(item => item != null && item.CreatedAtUtc >= periodStartUtc)
                .ToList();
            var cars = (data == null || data.Cars == null ? Enumerable.Empty<CarRecord>() : data.Cars)
                .Where(item => item != null && item.CreatedAtUtc >= periodStartUtc)
                .ToList();
            var trips = (data == null || data.Trips == null ? Enumerable.Empty<TripRecord>() : data.Trips)
                .Where(item => item != null && item.CreatedAtUtc >= periodStartUtc)
                .ToList();
            var inquiryStatusCounts = GetInquiryStatusCounts(inquiries);
            var carBrandCounts = GetCarBrandCounts(cars);
            var inquiryTrend = GetInquiryTrendPoints(inquiries, dashboardPeriodDays);
            var tripTrend = GetTripTrendPoints(trips, dashboardPeriodDays);
            var inquiriesInPeriod = inquiries.Count;
            var tripsInPeriod = trips.Count;
            var newInquiriesCount = inquiries.Count(item => item != null && string.Equals(FormatLeadStatus(item.Status), "Новая", StringComparison.OrdinalIgnoreCase));
            var inWorkInquiriesCount = inquiries.Count(item => item != null && string.Equals(FormatLeadStatus(item.Status), "В работе", StringComparison.OrdinalIgnoreCase));
            var waitingDocumentsCount = inquiries.Count(item => item != null && FormatLeadStatus(item.Status).IndexOf("документ", StringComparison.OrdinalIgnoreCase) >= 0);

            DashboardKpiLiteral.Text = BuildDashboardKpiCardsHtml(new[]
            {
                new KeyValuePair<string, string>("Новые заявки", newInquiriesCount.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, string>("В работе", inWorkInquiriesCount.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, string>("Ждут документы", waitingDocumentsCount.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, string>("Заявок за период", inquiriesInPeriod.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, string>("Рейсов за период", tripsInPeriod.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, string>("Клиенты", (data == null || data.Clients == null ? 0 : data.Clients.Count).ToString(CultureInfo.CurrentCulture))
            });
            DashboardInquiryTrendLiteral.Text = BuildLineChartHtml(inquiryTrend, "заявок", "Динамика заявок", delegate(int index, KeyValuePair<string, int> item)
            {
                return item.Value <= 0
                    ? string.Empty
                    : BuildCrmTabUrl("inquiries", new[]
                    {
                        new KeyValuePair<string, string>(DashboardPeriodQueryKey, dashboardPeriodDays.ToString(CultureInfo.InvariantCulture)),
                        new KeyValuePair<string, string>(DashboardInquiryDateQueryKey, periodStartUtc.AddDays(index).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    });
            });
            DashboardInquiryStatusLiteral.Text = BuildDonutChartHtml(inquiryStatusCounts, "Статусы заявок", delegate(KeyValuePair<string, int> item)
            {
                return BuildCrmTabUrl("inquiries", new[]
                {
                    new KeyValuePair<string, string>(DashboardPeriodQueryKey, dashboardPeriodDays.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>(DashboardInquiryStatusQueryKey, item.Key)
                });
            });
            DashboardCarBrandLiteral.Text = BuildBarChartHtml(carBrandCounts, "автомобилей", true, delegate(KeyValuePair<string, int> item)
            {
                return BuildCrmTabUrl("cars", new[]
                {
                    new KeyValuePair<string, string>(DashboardPeriodQueryKey, dashboardPeriodDays.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>(DashboardCarBrandQueryKey, item.Key)
                });
            });
            DashboardTripTrendLiteral.Text = BuildLineChartHtml(tripTrend, "рейсов", "Динамика рейсов", null);
        }

        private static List<KeyValuePair<string, int>> GetInquiryStatusCounts(IEnumerable<HomeInquiryRecord> inquiries)
        {
            var items = (inquiries ?? Enumerable.Empty<HomeInquiryRecord>())
                .Where(item => item != null)
                .GroupBy(item => string.IsNullOrWhiteSpace(item.Status) ? "Новая" : item.Status.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, int>(group.First().Status == null ? group.Key : (string.IsNullOrWhiteSpace(group.First().Status) ? "Новая" : group.First().Status.Trim()), group.Count()))
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .ToList();

            return items;
        }

        private static List<KeyValuePair<string, int>> GetCarBrandCounts(IEnumerable<CarRecord> cars)
        {
            return (cars ?? Enumerable.Empty<CarRecord>())
                .Where(item => item != null)
                .GroupBy(item => string.IsNullOrWhiteSpace(item.Brand) ? "Без марки" : item.Brand.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(8)
                .ToList();
        }

        private static List<KeyValuePair<string, int>> GetTripTrendPoints(IEnumerable<TripRecord> trips, int days)
        {
            var safeDays = days <= 0 ? DefaultDashboardPeriodDays : days;
            var today = DateTime.UtcNow.Date;
            var start = today.AddDays(-(safeDays - 1));
            var grouped = (trips ?? Enumerable.Empty<TripRecord>())
                .Where(item => item != null)
                .GroupBy(item => item.CreatedAtUtc.Date)
                .ToDictionary(group => group.Key, group => group.Count());
            var points = new List<KeyValuePair<string, int>>();

            for (var index = 0; index < safeDays; index++)
            {
                var date = start.AddDays(index);
                int count;

                if (!grouped.TryGetValue(date, out count))
                {
                    count = 0;
                }

                points.Add(new KeyValuePair<string, int>(date.ToLocalTime().ToString("dd.MM", CultureInfo.CurrentCulture), count));
            }

            return points;
        }

        private static List<KeyValuePair<string, int>> GetInquiryTrendPoints(IEnumerable<HomeInquiryRecord> inquiries, int days)
        {
            var safeDays = days <= 0 ? 30 : days;
            var today = DateTime.UtcNow.Date;
            var start = today.AddDays(-(safeDays - 1));
            var grouped = (inquiries ?? Enumerable.Empty<HomeInquiryRecord>())
                .Where(item => item != null)
                .GroupBy(item => item.CreatedAtUtc.Date)
                .ToDictionary(group => group.Key, group => group.Count());
            var points = new List<KeyValuePair<string, int>>();

            for (var index = 0; index < safeDays; index++)
            {
                var date = start.AddDays(index);
                int count;

                if (!grouped.TryGetValue(date, out count))
                {
                    count = 0;
                }

                points.Add(new KeyValuePair<string, int>(date.ToLocalTime().ToString("dd.MM", CultureInfo.CurrentCulture), count));
            }

            return points;
        }

        private string BuildDashboardKpiCardsHtml(IEnumerable<KeyValuePair<string, string>> cards)
        {
            var items = (cards ?? Enumerable.Empty<KeyValuePair<string, string>>()).ToList();

            if (items.Count == 0)
            {
                return "<div class=\"crm-dashboard-empty\">Недостаточно данных для сводки.</div>";
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("<div class=\"crm-dashboard-kpi-grid\">");

            foreach (var card in items)
            {
                builder.Append("<div class=\"crm-dashboard-kpi-card\">");
                builder.AppendFormat("<div class=\"crm-dashboard-kpi-label\">{0}</div>", HttpUtility.HtmlEncode(card.Key));
                builder.AppendFormat("<div class=\"crm-dashboard-kpi-value\">{0}</div>", HttpUtility.HtmlEncode(card.Value));
                builder.Append("</div>");
            }

            builder.Append("</div>");
            return builder.ToString();
        }

        private string BuildLineChartHtml(IList<KeyValuePair<string, int>> points, string valueSuffix, string chartTitle, Func<int, KeyValuePair<string, int>, string> itemUrlBuilder)
        {
            var items = points == null ? new List<KeyValuePair<string, int>>() : points.ToList();

            if (items.Count == 0 || items.All(item => item.Value <= 0))
            {
                return "<div class=\"crm-dashboard-empty\">Пока недостаточно данных для графика.</div>";
            }

            const decimal width = 760m;
            const decimal height = 260m;
            const decimal paddingLeft = 24m;
            const decimal paddingRight = 24m;
            const decimal paddingTop = 20m;
            const decimal paddingBottom = 34m;
            var maxValue = Math.Max(1, items.Max(item => item.Value));
            var chartWidth = width - paddingLeft - paddingRight;
            var chartHeight = height - paddingTop - paddingBottom;
            var stepX = items.Count <= 1 ? 0 : chartWidth / (items.Count - 1);
            var linePoints = new List<string>();
            var areaPoints = new List<string>();
            var builder = new System.Text.StringBuilder();

            for (var index = 0; index < items.Count; index++)
            {
                var x = paddingLeft + stepX * index;
                var y = paddingTop + chartHeight - (items[index].Value / (decimal)maxValue) * chartHeight;
                linePoints.Add(x.ToString("0.##", CultureInfo.InvariantCulture) + "," + y.ToString("0.##", CultureInfo.InvariantCulture));
                areaPoints.Add(x.ToString("0.##", CultureInfo.InvariantCulture) + "," + y.ToString("0.##", CultureInfo.InvariantCulture));
            }

            areaPoints.Insert(0, paddingLeft.ToString("0.##", CultureInfo.InvariantCulture) + "," + (paddingTop + chartHeight).ToString("0.##", CultureInfo.InvariantCulture));
            areaPoints.Add((paddingLeft + chartWidth).ToString("0.##", CultureInfo.InvariantCulture) + "," + (paddingTop + chartHeight).ToString("0.##", CultureInfo.InvariantCulture));

            builder.Append("<div class=\"crm-dashboard-line-chart\">");
            builder.AppendFormat("<svg viewBox=\"0 0 {0} {1}\" role=\"img\" aria-label=\"{2}\">", width.ToString("0", CultureInfo.InvariantCulture), height.ToString("0", CultureInfo.InvariantCulture), HttpUtility.HtmlAttributeEncode(chartTitle));

            for (var level = 0; level <= 4; level++)
            {
                var currentY = paddingTop + (chartHeight / 4m) * level;
                builder.AppendFormat("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{1}\" class=\"crm-dashboard-grid-line\"></line>", paddingLeft.ToString("0.##", CultureInfo.InvariantCulture), currentY.ToString("0.##", CultureInfo.InvariantCulture), (paddingLeft + chartWidth).ToString("0.##", CultureInfo.InvariantCulture));
            }

            builder.AppendFormat("<polygon points=\"{0}\" class=\"crm-dashboard-line-area\"></polygon>", string.Join(" ", areaPoints));
            builder.AppendFormat("<polyline points=\"{0}\" class=\"crm-dashboard-line-path\"></polyline>", string.Join(" ", linePoints));

            for (var pointIndex = 0; pointIndex < items.Count; pointIndex++)
            {
                var x = paddingLeft + stepX * pointIndex;
                var y = paddingTop + chartHeight - (items[pointIndex].Value / (decimal)maxValue) * chartHeight;
                var itemUrl = itemUrlBuilder == null ? string.Empty : itemUrlBuilder(pointIndex, items[pointIndex]);

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.AppendFormat("<a href=\"{0}\" class=\"crm-dashboard-svg-link\">", HttpUtility.HtmlAttributeEncode(itemUrl));
                    builder.AppendFormat("<circle cx=\"{0}\" cy=\"{1}\" r=\"11\" class=\"crm-dashboard-line-hit\"></circle>", x.ToString("0.##", CultureInfo.InvariantCulture), y.ToString("0.##", CultureInfo.InvariantCulture));
                }

                builder.AppendFormat("<circle cx=\"{0}\" cy=\"{1}\" r=\"4\" class=\"crm-dashboard-line-point\"><title>{2}: {3} {4}</title></circle>", x.ToString("0.##", CultureInfo.InvariantCulture), y.ToString("0.##", CultureInfo.InvariantCulture), HttpUtility.HtmlEncode(items[pointIndex].Key), items[pointIndex].Value.ToString(CultureInfo.CurrentCulture), HttpUtility.HtmlEncode(valueSuffix));

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.Append("</a>");
                }

                if (pointIndex == 0 || pointIndex == items.Count - 1 || pointIndex % Math.Max(1, items.Count / 5) == 0)
                {
                    builder.AppendFormat("<text x=\"{0}\" y=\"{1}\" text-anchor=\"middle\" class=\"crm-dashboard-axis-label\">{2}</text>", x.ToString("0.##", CultureInfo.InvariantCulture), (height - 8m).ToString("0.##", CultureInfo.InvariantCulture), HttpUtility.HtmlEncode(items[pointIndex].Key));
                }
            }

            builder.Append("</svg>");
            builder.Append("</div>");
            return builder.ToString();
        }

        private string BuildDonutChartHtml(IList<KeyValuePair<string, int>> points, string chartTitle, Func<KeyValuePair<string, int>, string> itemUrlBuilder)
        {
            var items = points == null ? new List<KeyValuePair<string, int>>() : points.Where(item => item.Value > 0).ToList();

            if (items.Count == 0)
            {
                return "<div class=\"crm-dashboard-empty\">Пока нет данных для распределения.</div>";
            }

            var total = items.Sum(item => item.Value);
            var builder = new System.Text.StringBuilder();
            var cumulative = 0m;

            builder.Append("<div class=\"crm-dashboard-donut-layout\">");
            builder.Append("<svg viewBox=\"0 0 180 180\" class=\"crm-dashboard-donut-chart\" role=\"img\" aria-label=\"");
            builder.Append(HttpUtility.HtmlEncode(chartTitle));
            builder.Append("\">");
            builder.Append("<circle cx=\"90\" cy=\"90\" r=\"56\" class=\"crm-dashboard-donut-base\"></circle>");

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var length = total == 0 ? 0m : (item.Value / (decimal)total) * 351.86m;
                var itemUrl = itemUrlBuilder == null ? string.Empty : itemUrlBuilder(item);

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.AppendFormat("<a href=\"{0}\" class=\"crm-dashboard-svg-link\">", HttpUtility.HtmlAttributeEncode(itemUrl));
                }

                builder.AppendFormat(CultureInfo.InvariantCulture, "<circle cx=\"90\" cy=\"90\" r=\"56\" class=\"crm-dashboard-donut-segment\" stroke=\"{0}\" stroke-dasharray=\"{1:0.##} 351.86\" stroke-dashoffset=\"-{2:0.##}\"><title>{3}: {4}</title></circle>", DashboardPalette[index % DashboardPalette.Length], length, cumulative, HttpUtility.HtmlEncode(item.Key), item.Value.ToString(CultureInfo.CurrentCulture));

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.Append("</a>");
                }

                cumulative += length;
            }

            builder.AppendFormat("<text x=\"90\" y=\"84\" text-anchor=\"middle\" class=\"crm-dashboard-donut-total-label\">Всего</text><text x=\"90\" y=\"104\" text-anchor=\"middle\" class=\"crm-dashboard-donut-total-value\">{0}</text>", total.ToString(CultureInfo.CurrentCulture));
            builder.Append("</svg>");
            builder.Append(BuildLegendHtml(items, itemUrlBuilder));
            builder.Append("</div>");
            return builder.ToString();
        }

        private string BuildBarChartHtml(IList<KeyValuePair<string, int>> points, string valueSuffix, bool compact, Func<KeyValuePair<string, int>, string> itemUrlBuilder)
        {
            var items = points == null ? new List<KeyValuePair<string, int>>() : points.Where(item => item.Value > 0).ToList();

            if (items.Count == 0)
            {
                return "<div class=\"crm-dashboard-empty\">Пока нет данных для сравнения.</div>";
            }

            var maxValue = Math.Max(1, items.Max(item => item.Value));
            var builder = new System.Text.StringBuilder();
            builder.AppendFormat("<div class=\"crm-dashboard-bar-list{0}\">", compact ? " crm-dashboard-bar-list-compact" : string.Empty);

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var percent = item.Value / (decimal)maxValue * 100m;
                var itemUrl = itemUrlBuilder == null ? string.Empty : itemUrlBuilder(item);

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.AppendFormat("<a class=\"crm-dashboard-bar-row crm-dashboard-bar-row-link\" href=\"{0}\">", HttpUtility.HtmlAttributeEncode(itemUrl));
                }
                else
                {
                    builder.Append("<div class=\"crm-dashboard-bar-row\">");
                }

                builder.AppendFormat("<div class=\"crm-dashboard-bar-label\">{0}</div>", HttpUtility.HtmlEncode(item.Key));
                builder.Append("<div class=\"crm-dashboard-bar-track\">");
                builder.AppendFormat(CultureInfo.InvariantCulture, "<div class=\"crm-dashboard-bar-fill\" style=\"width:{0:0.##}%; background:{1};\"></div>", percent, DashboardPalette[index % DashboardPalette.Length]);
                builder.Append("</div>");
                builder.AppendFormat("<div class=\"crm-dashboard-bar-value\">{0} {1}</div>", item.Value.ToString(CultureInfo.CurrentCulture), HttpUtility.HtmlEncode(valueSuffix));

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.Append("</a>");
                }
                else
                {
                    builder.Append("</div>");
                }
            }

            builder.Append("</div>");
            return builder.ToString();
        }

        private string BuildLegendHtml(IList<KeyValuePair<string, int>> items, Func<KeyValuePair<string, int>, string> itemUrlBuilder)
        {
            var builder = new System.Text.StringBuilder();
            var total = items.Sum(item => item.Value);
            builder.Append("<div class=\"crm-dashboard-legend\">");

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var percentage = total == 0 ? 0 : (item.Value * 100m / total);
                var itemUrl = itemUrlBuilder == null ? string.Empty : itemUrlBuilder(item);

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.AppendFormat("<a class=\"crm-dashboard-legend-item crm-dashboard-legend-link\" href=\"{0}\">", HttpUtility.HtmlAttributeEncode(itemUrl));
                }
                else
                {
                    builder.Append("<div class=\"crm-dashboard-legend-item\">");
                }

                builder.AppendFormat("<span class=\"crm-dashboard-legend-swatch\" style=\"background:{0};\"></span>", DashboardPalette[index % DashboardPalette.Length]);
                builder.AppendFormat("<span class=\"crm-dashboard-legend-label\">{0}</span>", HttpUtility.HtmlEncode(item.Key));
                builder.AppendFormat("<span class=\"crm-dashboard-legend-value\">{0} ({1:0.#}%)</span>", item.Value.ToString(CultureInfo.CurrentCulture), percentage);

                if (!string.IsNullOrWhiteSpace(itemUrl))
                {
                    builder.Append("</a>");
                }
                else
                {
                    builder.Append("</div>");
                }
            }

            builder.Append("</div>");
            return builder.ToString();
        }

        private string BuildCrmTabUrl(string tabName, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var queryParts = new List<string> { "tab=" + HttpUtility.UrlEncode(tabName ?? string.Empty) };

            foreach (var parameter in parameters ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                if (string.IsNullOrWhiteSpace(parameter.Key) || string.IsNullOrWhiteSpace(parameter.Value))
                {
                    continue;
                }

                queryParts.Add(HttpUtility.UrlEncode(parameter.Key) + "=" + HttpUtility.UrlEncode(parameter.Value));
            }

            return ResolveUrl("~/orders") + "?" + string.Join("&", queryParts);
        }

        private static int NormalizeDashboardPeriodDays(int days)
        {
            return SupportedDashboardPeriods.Contains(days) ? days : DefaultDashboardPeriodDays;
        }

        private int GetDashboardPeriodDays()
        {
            var rawValue = Request == null ? string.Empty : Convert.ToString(Request.QueryString[DashboardPeriodQueryKey]);
            int parsedValue;

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return NormalizeDashboardPeriodDays(parsedValue);
            }

            return DefaultDashboardPeriodDays;
        }

        private static string GetDashboardPeriodLabel(int days)
        {
            switch (NormalizeDashboardPeriodDays(days))
            {
                case 7:
                    return "7 дней";
                case 30:
                    return "30 дней";
                case 90:
                    return "90 дней";
                case 365:
                    return "год";
                default:
                    return "30 дней";
            }
        }

        private static DateTime GetDashboardPeriodStartUtc(int days)
        {
            var safeDays = NormalizeDashboardPeriodDays(days);
            return DateTime.UtcNow.Date.AddDays(-(safeDays - 1));
        }

        private string GetDashboardInquiryStatusFilter()
        {
            return (Request == null ? string.Empty : Convert.ToString(Request.QueryString[DashboardInquiryStatusQueryKey]) ?? string.Empty).Trim();
        }

        private DateTime? GetDashboardInquiryDateFilter()
        {
            var rawValue = (Request == null ? string.Empty : Convert.ToString(Request.QueryString[DashboardInquiryDateQueryKey]) ?? string.Empty).Trim();
            DateTime parsedValue;

            if (DateTime.TryParseExact(rawValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedValue))
            {
                return parsedValue.Date;
            }

            return null;
        }

        private string GetDashboardCarBrandFilter()
        {
            return (Request == null ? string.Empty : Convert.ToString(Request.QueryString[DashboardCarBrandQueryKey]) ?? string.Empty).Trim();
        }

        private bool HasDashboardInquiryScopeFilter()
        {
            return !string.IsNullOrWhiteSpace(GetDashboardInquiryStatusFilter()) || GetDashboardInquiryDateFilter().HasValue;
        }

        private List<HomeInquiryRecord> GetInquirySourceRecords()
        {
            return HomeInquiryService.GetRecent(HasDashboardInquiryScopeFilter() ? 500 : 20);
        }

        private List<HomeInquiryRecord> ApplyDashboardInquiryScopeFilters(IEnumerable<HomeInquiryRecord> inquiries)
        {
            var items = (inquiries ?? Enumerable.Empty<HomeInquiryRecord>()).Where(item => item != null).ToList();
            var inquiryDate = GetDashboardInquiryDateFilter();
            var inquiryStatus = GetDashboardInquiryStatusFilter();

            if (!HasDashboardInquiryScopeFilter())
            {
                return items;
            }

            if (inquiryDate.HasValue)
            {
                items = items.Where(item => item.CreatedAtUtc.Date == inquiryDate.Value.Date).ToList();
            }
            else
            {
                var periodStartUtc = GetDashboardPeriodStartUtc(GetDashboardPeriodDays());
                items = items.Where(item => item.CreatedAtUtc >= periodStartUtc).ToList();
            }

            if (!string.IsNullOrWhiteSpace(inquiryStatus))
            {
                items = items.Where(item => string.Equals(FormatLeadStatus(item.Status), inquiryStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return items;
        }

        private List<CarRecord> ApplyDashboardCarScopeFilters(IEnumerable<CarRecord> cars)
        {
            var items = (cars ?? Enumerable.Empty<CarRecord>()).Where(item => item != null).ToList();
            var carBrand = GetDashboardCarBrandFilter();

            if (string.IsNullOrWhiteSpace(carBrand))
            {
                return items;
            }

            var periodStartUtc = GetDashboardPeriodStartUtc(GetDashboardPeriodDays());
            return items
                .Where(item => item.CreatedAtUtc >= periodStartUtc)
                .Where(item => string.Equals(string.IsNullOrWhiteSpace(item.Brand) ? "Без марки" : item.Brand.Trim(), carBrand, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private string BuildDashboardInquiryFilterNoticeHtml()
        {
            if (!HasDashboardInquiryScopeFilter())
            {
                return string.Empty;
            }

            var status = GetDashboardInquiryStatusFilter();
            var date = GetDashboardInquiryDateFilter();
            var description = date.HasValue
                ? "за " + date.Value.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
                : string.IsNullOrWhiteSpace(status)
                    ? "за период " + GetDashboardPeriodLabel()
                    : string.Format(CultureInfo.CurrentCulture, "со статусом \"{0}\" за период {1}", status, GetDashboardPeriodLabel());

            return string.Format(
                CultureInfo.CurrentCulture,
                "<div class=\"alert alert-info crm-dashboard-scope-alert\">Показаны заявки из дашборда {0}. <a href=\"{1}\">Показать все заявки</a></div>",
                HttpUtility.HtmlEncode(description),
                HttpUtility.HtmlAttributeEncode(BuildCrmTabUrl("inquiries", Enumerable.Empty<KeyValuePair<string, string>>())));
        }

        private string BuildDashboardCarsFilterNoticeHtml()
        {
            var brand = GetDashboardCarBrandFilter();

            if (string.IsNullOrWhiteSpace(brand))
            {
                return string.Empty;
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                "<div class=\"alert alert-info crm-dashboard-scope-alert\">Показаны автомобили марки \"{0}\" за период {1}. <a href=\"{2}\">Показать все автомобили</a></div>",
                HttpUtility.HtmlEncode(brand),
                HttpUtility.HtmlEncode(GetDashboardPeriodLabel()),
                HttpUtility.HtmlAttributeEncode(BuildCrmTabUrl("cars", Enumerable.Empty<KeyValuePair<string, string>>())));
        }

        private void ResetCrmSettings()
        {
            _crmSettings = null;
        }

        private static List<string> ParseSettingsValues(string rawValue)
        {
            return (rawValue ?? string.Empty)
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string FormatSettingsValues(IEnumerable<string> values)
        {
            return string.Join(Environment.NewLine, (values ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private List<string> GetEditableHomePartners(CrmSettingsRecord settings)
        {
            var languageCode = GetSelectedSettingsHomeContentLanguage();
            return EnsureSettingsEditorList("SettingsHomePartnerEditorValues_" + languageCode, CrmSettingsService.GetLocalizedHomePartnerNames(settings, languageCode));
        }

        private List<LandingReviewRecord> GetEditableHomeReviews(CrmSettingsRecord settings)
        {
            var languageCode = GetSelectedSettingsHomeContentLanguage();
            return EnsureSettingsReviewList("SettingsHomeReviewEditorValues_" + languageCode, CrmSettingsService.GetLocalizedHomeReviews(settings, languageCode));
        }

        private List<string> GetEditableSettingsRoles(CrmSettingsRecord settings)
        {
            return EnsureSettingsEditorList("SettingsAllowedRolesEditorValues", settings == null ? null : settings.AllowedCrmRoles);
        }

        private List<string> GetEditableTripStatuses(CrmSettingsRecord settings)
        {
            return EnsureSettingsEditorList("SettingsTripStatusesEditorValues", settings == null ? null : settings.TripStatuses);
        }

        private void BindInquiryEditors(IEnumerable<HomeInquiryRecord> inquiries)
        {
            if (SettingsRecentInquiriesRepeater == null)
            {
                return;
            }

            var items = inquiries == null ? new List<HomeInquiryRecord>() : inquiries.ToList();

            for (var index = 0; index < SettingsRecentInquiriesRepeater.Items.Count && index < items.Count; index++)
            {
                var inquiry = items[index];
                var item = SettingsRecentInquiriesRepeater.Items[index];
                BindLeadStatusesDropDownList(item.FindControl("SettingsLeadStatusDropDownList") as ListControl, inquiry == null ? string.Empty : inquiry.Status);
            }
        }

        private List<HomeInquiryRecord> ApplyLeadFilters(IEnumerable<HomeInquiryRecord> inquiries)
        {
            var items = ApplyDashboardInquiryScopeFilters(inquiries);

            var phoneFilter = GetPostedFilterTextValue(SettingsLeadFilterPhoneTextBox);
            var messengerFilter = GetPostedFilterTextValue(SettingsLeadFilterMessengerTextBox);
            var directionFilter = GetPostedFilterTextValue(SettingsLeadFilterDirectionTextBox);
            var cargoTypeFilter = GetPostedFilterSelectedValue(SettingsLeadFilterCargoTypeDropDownList);
            var commentFilter = GetPostedFilterTextValue(SettingsLeadFilterCommentTextBox);

            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                items = items.Where(item => ContainsIgnoreCase(item == null ? string.Empty : item.Phone, phoneFilter)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(messengerFilter))
            {
                items = items.Where(item => ContainsIgnoreCase(item == null ? string.Empty : item.Messenger, messengerFilter)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(directionFilter))
            {
                items = items.Where(item => ContainsIgnoreCase(item == null ? string.Empty : item.Direction, directionFilter)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(cargoTypeFilter))
            {
                items = items.Where(item => string.Equals(item == null ? string.Empty : item.CargoType, cargoTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(commentFilter))
            {
                items = items.Where(item => ContainsIgnoreCase(item == null ? string.Empty : item.ClientComment, commentFilter)).ToList();
            }

            return items;
        }

        private void SetInquiryFilterSummary(int filteredCount, int totalCount)
        {
            if (SettingsLeadFilterSummaryLiteral == null)
            {
                return;
            }

            var summary = totalCount <= 0
                ? "Заявок пока нет"
                : filteredCount == totalCount
                    ? string.Format(CultureInfo.CurrentCulture, "Найдено заявок: {0}", filteredCount)
                    : string.Format(CultureInfo.CurrentCulture, "Найдено заявок: {0} из {1}", filteredCount, totalCount);

            SettingsLeadFilterSummaryLiteral.Text = HttpUtility.HtmlEncode(summary);
        }

        protected bool IsInquiryFilterPanelCollapsed()
        {
            return SettingsLeadFilterPanelStateHiddenField != null
                && string.Equals(SettingsLeadFilterPanelStateHiddenField.Value, "collapsed", StringComparison.OrdinalIgnoreCase);
        }

        private string GetPostedFilterTextValue(TextBox control)
        {
            if (control == null)
            {
                return string.Empty;
            }

            var currentValue = control.Text.Trim();

            if (!string.IsNullOrWhiteSpace(currentValue) || !IsPostBack || Request == null)
            {
                return currentValue;
            }

            return (Request.Form[control.UniqueID] ?? string.Empty).Trim();
        }

        private string GetPostedFilterSelectedValue(ListControl control)
        {
            if (control == null)
            {
                return string.Empty;
            }

            var currentValue = GetSelectedValue(control);

            if (!string.IsNullOrWhiteSpace(currentValue) || !IsPostBack || Request == null)
            {
                return currentValue;
            }

            return Convert.ToString(Request.Form[control.UniqueID]) ?? string.Empty;
        }

        private string GetPostedSelectedInquiryId()
        {
            var currentValue = SelectedInquiryId;

            if (!string.IsNullOrWhiteSpace(currentValue)
                || !IsPostBack
                || Request == null
                || SettingsSelectedInquiryIdHiddenField == null)
            {
                return currentValue;
            }

            return (Request.Form[SettingsSelectedInquiryIdHiddenField.UniqueID] ?? string.Empty).Trim();
        }

        private string GetPostedListControlValue(ListControl control)
        {
            if (control == null)
            {
                return string.Empty;
            }

            if (Request != null)
            {
                var postedValue = Convert.ToString(Request.Form[control.UniqueID]);

                if (!string.IsNullOrWhiteSpace(postedValue))
                {
                    return postedValue;
                }
            }

            return GetSelectedValue(control);
        }

        private IEnumerable<string> GetPostedListControlValues(ListControl control)
        {
            if (control == null)
            {
                return Enumerable.Empty<string>();
            }

            if (Request != null)
            {
                var postedValues = Request.Form.GetValues(control.UniqueID);

                if (postedValues != null && postedValues.Length > 0)
                {
                    return postedValues;
                }
            }

            return GetSelectedValues(control);
        }

        private bool RequiresEarlyInquiryBinding()
        {
            if (Request == null || Request.Form == null)
            {
                return false;
            }

            return Request.Form.AllKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Any(key => key.IndexOf("SettingsRecentInquiriesRepeater", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("SettingsInquiryListRepeater", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("SettingsLeadFilter", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("ApplySettingsLeadFiltersButton", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("ResetSettingsLeadFiltersButton", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("CloseSettingsInquiryDetailButton", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("InquiriesTabButton", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrWhiteSpace(source)
                && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void BindLeadStatusesDropDownList(ListControl control, string selectedValue)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();

            foreach (var status in HomeInquiryService.GetAvailableStatuses())
            {
                control.Items.Add(new ListItem(status, status));
            }

            SetSelectedValue(control, selectedValue);
        }

        private string ResolveLeadCommentAuthor()
        {
            if (Context == null || Context.User == null || Context.User.Identity == null)
            {
                return "CRM";
            }

            return string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "CRM"
                : Context.User.Identity.Name.Trim();
        }

        private List<string> EnsureSettingsEditorList(string key, IEnumerable<string> fallbackValues)
        {
            var values = ViewState[key] as List<string>;

            if (values != null)
            {
                return values;
            }

            values = (fallbackValues ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ViewState[key] = values;
            return values;
        }

        private List<LandingReviewRecord> EnsureSettingsReviewList(string key, IEnumerable<LandingReviewRecord> fallbackValues)
        {
            var values = ViewState[key] as List<LandingReviewRecord>;

            if (values != null)
            {
                return values;
            }

            values = CloneReviewItems(fallbackValues);
            ViewState[key] = values;
            return values;
        }

        private void SyncSettingsEditorState(CrmSettingsRecord settings)
        {
            ViewState["SettingsAllowedRolesEditorValues"] = new List<string>((settings == null ? Enumerable.Empty<string>() : settings.AllowedCrmRoles ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
            ViewState["SettingsTripStatusesEditorValues"] = new List<string>((settings == null ? Enumerable.Empty<string>() : settings.TripStatuses ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
            SyncLocalizedHomeEditorState(settings, "ru");
            SyncLocalizedHomeEditorState(settings, "en");
            SyncLocalizedHomeEditorState(settings, "ge");
        }

        private void SyncLocalizedHomeEditorState(CrmSettingsRecord settings, string languageCode)
        {
            ViewState["SettingsHomePartnerEditorValues_" + languageCode] = new List<string>(CrmSettingsService.GetLocalizedHomePartnerNames(settings, languageCode)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
            ViewState["SettingsHomeReviewEditorValues_" + languageCode] = CloneReviewItems(CrmSettingsService.GetLocalizedHomeReviews(settings, languageCode));
        }

        private void ApplyLocalizedHomeContentInputs(CrmSettingsRecord settings, string languageCode)
        {
            var normalizedLanguageCode = NormalizeSettingsHomeContentLanguage(languageCode);
            var partnerValues = new List<string>(GetEditableHomePartners(settings));
            var reviewValues = CloneReviewItems(GetEditableHomeReviews(settings));
            var address = SettingsHomeContactAddressTextBox.Text.Trim();
            var workingHours = SettingsHomeContactHoursTextBox.Text.Trim();

            switch (normalizedLanguageCode)
            {
                case "en":
                    settings.HomePartnerNamesEn = partnerValues;
                    settings.HomeReviewsEn = reviewValues;
                    settings.HomeContactAddressEn = address;
                    settings.HomeContactWorkingHoursEn = workingHours;
                    break;
                case "ge":
                    settings.HomePartnerNamesGe = partnerValues;
                    settings.HomeReviewsGe = reviewValues;
                    settings.HomeContactAddressGe = address;
                    settings.HomeContactWorkingHoursGe = workingHours;
                    break;
                default:
                    settings.HomePartnerNames = partnerValues;
                    settings.HomeReviews = reviewValues;
                    settings.HomeContactAddress = address;
                    settings.HomeContactWorkingHours = workingHours;
                    break;
            }
        }

        private string GetSelectedSettingsHomeContentLanguage()
        {
            return NormalizeSettingsHomeContentLanguage(GetPostedListControlValue(SettingsHomeContentLanguageDropDownList));
        }

        private static string NormalizeSettingsHomeContentLanguage(string languageCode)
        {
            var normalizedLanguageCode = PublicSiteLocalizationService.NormalizeLanguageCode(languageCode);
            return string.IsNullOrWhiteSpace(normalizedLanguageCode)
                ? PublicSiteLocalizationService.DefaultLanguageCode
                : normalizedLanguageCode;
        }

        private static List<LandingReviewRecord> CloneReviewItems(IEnumerable<LandingReviewRecord> reviews)
        {
            return (reviews ?? Enumerable.Empty<LandingReviewRecord>())
                .Where(item => item != null)
                .Select(item => new LandingReviewRecord
                {
                    Quote = (item.Quote ?? string.Empty).Trim(),
                    Author = (item.Author ?? string.Empty).Trim()
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Quote) && !string.IsNullOrWhiteSpace(item.Author))
                .ToList();
        }

        private void AddSettingsListValue(TextBox input, List<string> targetValues, string emptyMessage)
        {
            var rawValue = input == null ? string.Empty : input.Text;
            var valuesToAdd = ParseSettingsValues(rawValue);

            if (valuesToAdd.Count == 0)
            {
                ShowMessage(emptyMessage, "warning");
                return;
            }

            var hasNewValue = false;

            foreach (var value in valuesToAdd)
            {
                if (targetValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                targetValues.Add(value);
                hasNewValue = true;
            }

            input.Text = string.Empty;

            if (!hasNewValue)
            {
                ShowMessage("Такое значение уже есть в списке.", "warning");
            }
        }

        private void AddSettingsReviewItem()
        {
            var quote = (SettingsNewHomeReviewQuoteTextBox.Text ?? string.Empty).Trim();
            var author = (SettingsNewHomeReviewAuthorTextBox.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(quote) || string.IsNullOrWhiteSpace(author))
            {
                ShowMessage("Для отзыва нужно заполнить и текст, и автора.", "warning");
                return;
            }

            var reviews = GetEditableHomeReviews(CurrentCrmSettings);

            if (reviews.Any(item => string.Equals(item.Quote, quote, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Author, author, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage("Такой отзыв уже есть в списке.", "warning");
                return;
            }

            reviews.Add(new LandingReviewRecord
            {
                Quote = quote,
                Author = author
            });

            SettingsNewHomeReviewQuoteTextBox.Text = string.Empty;
            SettingsNewHomeReviewAuthorTextBox.Text = string.Empty;
        }

        private void RemoveSettingsListValue(List<string> targetValues, string valueToRemove, string lastItemMessage)
        {
            if (targetValues.Count <= 1)
            {
                ShowMessage(lastItemMessage, "warning");
                return;
            }

            var index = targetValues.FindIndex(item => string.Equals(item, valueToRemove, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                targetValues.RemoveAt(index);
            }
        }

        private static bool CanResizeLogo(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase);
        }

        private static Size CalculateScaledSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                return new Size(maxWidth, maxHeight);
            }

            var scale = Math.Min((double)maxWidth / originalWidth, (double)maxHeight / originalHeight);

            if (scale > 1d)
            {
                scale = 1d;
            }

            return new Size(
                Math.Max(1, (int)Math.Round(originalWidth * scale)),
                Math.Max(1, (int)Math.Round(originalHeight * scale)));
        }

        private static Bitmap ResizeImage(System.Drawing.Image sourceImage, int width, int height, string extension)
        {
            var useTransparentBackground = !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);

            var bitmap = new Bitmap(width, height, useTransparentBackground ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            var horizontalResolution = sourceImage.HorizontalResolution > 0 ? sourceImage.HorizontalResolution : 96f;
            var verticalResolution = sourceImage.VerticalResolution > 0 ? sourceImage.VerticalResolution : 96f;

            bitmap.SetResolution(horizontalResolution, verticalResolution);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(useTransparentBackground ? Color.Transparent : Color.White);
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(sourceImage, 0, 0, width, height);
            }

            return bitmap;
        }

        private static void SaveRasterImage(Bitmap image, string physicalPath, string extension)
        {
            if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var encoder = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(item => item.FormatID == ImageFormat.Jpeg.Guid);

                if (encoder == null)
                {
                    image.Save(physicalPath, ImageFormat.Jpeg);
                    return;
                }

                using (var parameters = new EncoderParameters(1))
                {
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                    image.Save(physicalPath, encoder, parameters);
                    return;
                }
            }

            if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(physicalPath, ImageFormat.Gif);
                return;
            }

            image.Save(physicalPath, ImageFormat.Png);
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

        private static string BuildTripListLabel(TripRecord trip)
        {
            if (trip == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(trip.Number))
            {
                parts.Add(trip.Number.Trim());
            }

            if (!string.IsNullOrWhiteSpace(trip.ClientName))
            {
                parts.Add(trip.ClientName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(trip.Status))
            {
                parts.Add(trip.Status.Trim());
            }

            return parts.Count == 0 ? string.Empty : string.Join(" - ", parts);
        }

        private static string BuildCarDetailMetaText(CarRecord car)
        {
            if (car == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            parts.Add("Дата создания: " + (car.CreatedAtUtc == DateTime.MinValue ? "Не указана" : car.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")));

            if (!string.IsNullOrWhiteSpace(car.ClientName))
            {
                parts.Add("Клиент: " + car.ClientName.Trim());
            }

            return string.Join(" | ", parts);
        }

        private static string BuildDatalistOptionsHtml(IEnumerable<string> values)
        {
            var normalizedValues = (values ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToList();

            if (normalizedValues.Count == 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();

            foreach (var value in normalizedValues)
            {
                builder.Append("<option value=\"");
                builder.Append(HttpUtility.HtmlAttributeEncode(value));
                builder.Append("\"></option>");
            }

            return builder.ToString();
        }

        private static void AppendCarChangeHistory(CarRecord car, string fieldName, string previousValue, string newValue, string changedBy)
        {
            if (car == null)
            {
                return;
            }

            var normalizedPreviousValue = NormalizeCrmText(previousValue);
            var normalizedNewValue = NormalizeCrmText(newValue);

            if (string.Equals(normalizedPreviousValue, normalizedNewValue, StringComparison.Ordinal))
            {
                return;
            }

            car.ChangeHistory = car.ChangeHistory ?? new List<CarChangeLogRecord>();
            car.ChangeHistory.Insert(0, new CarChangeLogRecord
            {
                FieldName = NormalizeCrmText(fieldName),
                PreviousValue = normalizedPreviousValue,
                NewValue = normalizedNewValue,
                ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "CRM" : changedBy.Trim(),
                ChangedAtUtc = DateTime.UtcNow
            });
        }

        private static string BuildCarChangeHistoryHtml(IEnumerable<CarChangeLogRecord> history)
        {
            var items = (history ?? Enumerable.Empty<CarChangeLogRecord>())
                .Where(item => item != null)
                .OrderByDescending(item => item.ChangedAtUtc)
                .ToList();

            if (items.Count == 0)
            {
                return "<p class=\"text-muted\">Изменений пока нет.</p>";
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("<div class=\"crm-car-history-list\">");

            foreach (var item in items)
            {
                builder.Append("<div class=\"crm-car-history-item\">");
                builder.Append("<div class=\"crm-car-history-meta\">");
                builder.Append(HttpUtility.HtmlEncode(item.ChangedAtUtc == DateTime.MinValue
                    ? "Дата не указана"
                    : item.ChangedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")));
                builder.Append(" | ");
                builder.Append(HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.ChangedBy) ? "CRM" : item.ChangedBy.Trim()));
                builder.Append("</div>");
                builder.Append("<div class=\"crm-car-history-change\"><strong>");
                builder.Append(HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.FieldName) ? "Поле" : item.FieldName.Trim()));
                builder.Append("</strong>: ");
                builder.Append(HttpUtility.HtmlEncode(FormatCarHistoryValue(item.PreviousValue)));
                builder.Append(" &rarr; ");
                builder.Append(HttpUtility.HtmlEncode(FormatCarHistoryValue(item.NewValue)));
                builder.Append("</div></div>");
            }

            builder.Append("</div>");
            return builder.ToString();
        }

        private static string FormatCarHistoryValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
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

        private static List<string> ParseStoredIds(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item == null ? string.Empty : item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string SerializeStoredIds(IEnumerable<string> ids)
        {
            return string.Join(",", (ids ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string NormalizeStoredIds(string value)
        {
            return SerializeStoredIds(ParseStoredIds(value));
        }

        private static bool ContainsStoredId(string storedIds, string id)
        {
            return ParseStoredIds(storedIds).Contains((id ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static string RemoveStoredIds(string storedIds, IEnumerable<string> idsToRemove)
        {
            var removalSet = new HashSet<string>((idsToRemove ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()), StringComparer.OrdinalIgnoreCase);

            return SerializeStoredIds(ParseStoredIds(storedIds).Where(item => !removalSet.Contains(item)));
        }

        private static string KeepStoredIds(string storedIds, IEnumerable<string> availableIds)
        {
            var availableSet = new HashSet<string>((availableIds ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()), StringComparer.OrdinalIgnoreCase);

            return SerializeStoredIds(ParseStoredIds(storedIds).Where(item => availableSet.Contains(item)));
        }

        private static List<string> GetAvailableStoredIds(string storedIds, IEnumerable<string> availableIds)
        {
            var availableSet = new HashSet<string>((availableIds ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()), StringComparer.OrdinalIgnoreCase);

            return ParseStoredIds(storedIds).Where(item => availableSet.Contains(item)).ToList();
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSelectedValue(ListControl control)
        {
            return control == null ? string.Empty : Convert.ToString(control.SelectedValue);
        }

        protected bool IsClientsInlineEditRow(object recordId)
        {
            return ContainsStoredId(ClientsEditingId, Convert.ToString(recordId));
        }

        protected bool IsTripsInlineEditRow(object recordId)
        {
            return ContainsStoredId(TripsEditingId, Convert.ToString(recordId));
        }

        protected bool IsDriversInlineEditRow(object recordId)
        {
            return ContainsStoredId(DriversEditingId, Convert.ToString(recordId));
        }

        protected bool IsFleetInlineEditRow(object recordId)
        {
            return ContainsStoredId(FleetEditingId, Convert.ToString(recordId));
        }

        protected bool IsTripsSelected(object recordId)
        {
            return ContainsStoredId(TripsSelectedIds, Convert.ToString(recordId));
        }

        protected bool IsDriversSelected(object recordId)
        {
            return ContainsStoredId(DriversSelectedIds, Convert.ToString(recordId));
        }

        protected bool IsFleetSelected(object recordId)
        {
            return ContainsStoredId(FleetSelectedIds, Convert.ToString(recordId));
        }

        protected bool IsClientsSelected(object recordId)
        {
            return ContainsStoredId(ClientsSelectedIds, Convert.ToString(recordId));
        }

        protected bool IsCarsSelected(object recordId)
        {
            return ContainsStoredId(CarsSelectedIds, Convert.ToString(recordId));
        }

        protected bool IsInquiriesSelected(object recordId)
        {
            return ContainsStoredId(InquiriesSelectedIds, Convert.ToString(recordId));
        }

        protected string GetCarRowCss(object recordId)
        {
            var classes = new List<string> { "crm-clickable-row" };

            if (StringEquals(SelectedCarId, Convert.ToString(recordId)))
            {
                classes.Add("crm-car-row-selected");
            }

            return string.Join(" ", classes);
        }

        protected string GetInquiryRowCss(object recordId)
        {
            var classes = new List<string> { "crm-clickable-row" };

            if (StringEquals(SelectedInquiryId, Convert.ToString(recordId)))
            {
                classes.Add("crm-inquiry-row-selected");
            }

            return string.Join(" ", classes);
        }

        private static string GetRepeaterTextBoxValue(RepeaterItem item, string controlId)
        {
            var textBox = item == null ? null : item.FindControl(controlId) as TextBox;
            return textBox == null ? string.Empty : textBox.Text.Trim();
        }

        private static string GetRepeaterSelectedValue(RepeaterItem item, string controlId)
        {
            var control = item == null ? null : item.FindControl(controlId) as ListControl;
            return control == null ? string.Empty : Convert.ToString(control.SelectedValue);
        }

        private static List<string> GetRepeaterSelectedValues(RepeaterItem item, string controlId)
        {
            return GetSelectedValues(item == null ? null : item.FindControl(controlId) as ListControl);
        }

        private static FileUpload GetRepeaterFileUpload(RepeaterItem item, string controlId)
        {
            return item == null ? null : item.FindControl(controlId) as FileUpload;
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

        private bool SyncInquiryToCrm(HomeInquiryRecord inquiry)
        {
            if (inquiry == null)
            {
                return false;
            }

            var shouldSyncCars = IsCarsInquiryStatus(inquiry.Status);
            var shouldSyncClients = IsPositiveClosedInquiryStatus(inquiry.Status);

            if (!shouldSyncCars && !shouldSyncClients)
            {
                return false;
            }

            var data = CrmRepository.Load();
            ClientRecord client = null;
            var clientChanged = false;
            var carChanged = false;

            if (shouldSyncClients)
            {
                clientChanged = SyncClientFromInquiry(data, inquiry, out client);
            }
            else
            {
                client = FindClientForInquiry(data, inquiry);
            }

            if (shouldSyncCars)
            {
                carChanged = SyncCarFromInquiry(data, inquiry, client);
            }

            if (!clientChanged && !carChanged)
            {
                return false;
            }

            CrmRepository.Save(data);
            return true;
        }

        private static bool SyncClientFromInquiry(CrmDataStore data, HomeInquiryRecord inquiry, out ClientRecord client)
        {
            client = FindClientForInquiry(data, inquiry);

            var isNew = client == null;

            if (isNew)
            {
                client = new ClientRecord
                {
                    CreatedAtUtc = inquiry.CreatedAtUtc == DateTime.MinValue ? DateTime.UtcNow : inquiry.CreatedAtUtc
                };

                data.Clients.Insert(0, client);
            }

            var previousName = client.Name;
            var hasChanged = isNew;
            var normalizedManager = string.IsNullOrWhiteSpace(inquiry.AssignedManager) ? ResolveClientManagerFromInquiry(inquiry) : inquiry.AssignedManager.Trim();

            var normalizedSourceInquiryId = NormalizeCrmText(inquiry.Id);
            var normalizedName = NormalizeCrmText(inquiry.Name);
            var normalizedDirection = NormalizeCrmText(inquiry.Direction);
            var normalizedPhone = NormalizeCrmText(inquiry.Phone);
            var normalizedEmail = NormalizeCrmText(inquiry.Email);

            if (!string.Equals(NormalizeCrmText(client.SourceInquiryId), normalizedSourceInquiryId, StringComparison.Ordinal))
            {
                client.SourceInquiryId = normalizedSourceInquiryId;
                hasChanged = true;
            }

            if (!string.Equals(NormalizeCrmText(client.Name), normalizedName, StringComparison.Ordinal))
            {
                client.Name = normalizedName;
                hasChanged = true;
            }

            if (!string.Equals(NormalizeCrmText(client.Direction), normalizedDirection, StringComparison.Ordinal))
            {
                client.Direction = normalizedDirection;
                hasChanged = true;
            }

            if (!string.Equals(NormalizeCrmText(client.Manager), NormalizeCrmText(normalizedManager), StringComparison.Ordinal))
            {
                client.Manager = NormalizeCrmText(normalizedManager);
                hasChanged = true;
            }

            if (!string.Equals(NormalizeCrmText(client.PhoneNumber), normalizedPhone, StringComparison.Ordinal))
            {
                client.PhoneNumber = normalizedPhone;
                hasChanged = true;
            }

            if (!string.Equals(NormalizeCrmText(client.Email), normalizedEmail, StringComparison.Ordinal))
            {
                client.Email = normalizedEmail;
                hasChanged = true;
            }

            if (hasChanged && !StringEquals(previousName, client.Name) && !string.IsNullOrWhiteSpace(previousName))
            {
                SyncClientReferences(data, client, previousName);
            }

            return hasChanged;
        }

        private static bool SyncCarFromInquiry(CrmDataStore data, HomeInquiryRecord inquiry, ClientRecord client)
        {
            var car = data.Cars.FirstOrDefault(item => StringEquals(item.SourceInquiryId, inquiry.Id));
            var isNew = car == null;

            if (isNew)
            {
                car = new CarRecord
                {
                    CreatedAtUtc = inquiry.CreatedAtUtc == DateTime.MinValue ? DateTime.UtcNow : inquiry.CreatedAtUtc
                };

                data.Cars.Insert(0, car);
            }

            string firstName;
            string lastName;
            SplitFullName(inquiry.Name, out firstName, out lastName);

            var hasChanged = isNew;
            var synchronizedDealer = NormalizeCrmText(inquiry.Source);
            var synchronizedStatus = NormalizeCrmText(inquiry.Status);

            hasChanged |= AssignStringIfDifferent(car.SourceInquiryId, inquiry.Id, value => car.SourceInquiryId = value);
            hasChanged |= AssignStringIfDifferent(car.ClientId, client == null ? string.Empty : client.Id, value => car.ClientId = value);
            hasChanged |= AssignStringIfDifferent(car.ClientName, client == null ? NormalizeCrmText(inquiry.Name) : client.Name, value => car.ClientName = value);
            hasChanged |= AssignStringIfDifferent(car.Forwarder, NormalizeCrmText(inquiry.AssignedManager), value => car.Forwarder = value);
            AppendCarChangeHistory(car, "Дилер", car.Dealer, synchronizedDealer, "CRM sync");
            hasChanged |= AssignStringIfDifferent(car.Dealer, synchronizedDealer, value => car.Dealer = value);
            hasChanged |= AssignStringIfDifferent(car.Location, NormalizeCrmText(inquiry.Direction), value => car.Location = value);
            hasChanged |= AssignStringIfDifferent(car.ReExport, NormalizeCrmText(inquiry.CargoType), value => car.ReExport = value);
            AppendCarChangeHistory(car, "Статус", car.Status, synchronizedStatus, "CRM sync");
            hasChanged |= AssignStringIfDifferent(car.Status, synchronizedStatus, value => car.Status = value);
            hasChanged |= AssignStringIfDifferent(car.Comment, NormalizeCrmText(inquiry.ClientComment), value => car.Comment = value);
            hasChanged |= AssignStringIfDifferent(car.FirstName, firstName, value => car.FirstName = value);
            hasChanged |= AssignStringIfDifferent(car.LastName, lastName, value => car.LastName = value);

            return hasChanged;
        }

        private static ClientRecord FindClientForInquiry(CrmDataStore data, HomeInquiryRecord inquiry)
        {
            var byInquiryId = data.Clients.FirstOrDefault(item => StringEquals(item.SourceInquiryId, inquiry.Id));

            if (byInquiryId != null)
            {
                return byInquiryId;
            }

            if (!string.IsNullOrWhiteSpace(inquiry.Email))
            {
                var normalizedEmail = inquiry.Email.Trim();
                var byEmail = data.Clients.FirstOrDefault(item => StringEquals(item.Email, normalizedEmail));

                if (byEmail != null)
                {
                    return byEmail;
                }
            }

            return null;
        }

        private static bool IsPositiveClosedInquiryStatus(string status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                return false;
            }

            if (normalizedStatus.Contains("отмен")
                || normalizedStatus.Contains("отказ")
                || normalizedStatus.Contains("cancel")
                || normalizedStatus.Contains("refus"))
            {
                return false;
            }

            return normalizedStatus.Contains("полож")
                || normalizedStatus.Contains("успеш")
                || normalizedStatus.Contains("закры");
        }

        private static bool IsCarsInquiryStatus(string status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                return false;
            }

            var waitingForDocuments = normalizedStatus.Contains("ожида")
                && normalizedStatus.Contains("документ");

            return waitingForDocuments
                || normalizedStatus.Contains("в работе")
                || normalizedStatus.Contains("оплач")
                || IsPositiveClosedInquiryStatus(status);
        }

        private static string ResolveClientManagerFromInquiry(HomeInquiryRecord inquiry)
        {
            if (!string.IsNullOrWhiteSpace(inquiry.AssignedManager))
            {
                return inquiry.AssignedManager.Trim();
            }

            if (!string.IsNullOrWhiteSpace(inquiry.Source))
            {
                return inquiry.Source.Trim();
            }

            return "CRM";
        }

        private static string NormalizeCrmText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static void SplitFullName(string fullName, out string firstName, out string lastName)
        {
            var parts = (fullName ?? string.Empty)
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                firstName = string.Empty;
                lastName = string.Empty;
                return;
            }

            firstName = parts[0];
            lastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1).ToArray()) : string.Empty;
        }

        private static bool AssignStringIfDifferent(string currentValue, string newValue, Action<string> assign)
        {
            var normalizedCurrent = NormalizeCrmText(currentValue);
            var normalizedNew = NormalizeCrmText(newValue);

            if (string.Equals(normalizedCurrent, normalizedNew, StringComparison.Ordinal))
            {
                return false;
            }

            assign(normalizedNew);
            return true;
        }

        private static string BuildCarDetailHtml(CarRecord car)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("<div class=\"panel panel-default\">");
            builder.Append("<div class=\"panel-heading\"><h3 class=\"panel-title\">");
            builder.Append(HttpUtility.HtmlEncode(BuildCarCardTitle(car)));
            builder.Append("</h3></div>");
            builder.Append("<div class=\"panel-body\">");
            builder.Append("<p class=\"text-muted crm-car-detail-meta\">");
            builder.Append("Дата создания: ");
            builder.Append(HttpUtility.HtmlEncode(car.CreatedAtUtc == DateTime.MinValue ? "Не указана" : car.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")));

            if (!string.IsNullOrWhiteSpace(car.ClientName))
            {
                builder.Append(" | Клиент: ");
                builder.Append(HttpUtility.HtmlEncode(car.ClientName));
            }

            builder.Append("</p>");
            builder.Append("<div class=\"row\">");
            AppendCarDetailSection(builder, "Сделка", new[]
            {
                CreateCarDetail("Рейс", car.TripNumber),
                CreateCarDetail("Форвардер", car.Forwarder),
                CreateCarDetail("Дилер", car.Dealer),
                CreateCarDetail("Статус", car.Status),
                CreateCarDetail("Стартовая цена", car.StartPrice),
                CreateCarDetail("Инвойс", car.Invoice)
            });
            AppendCarDetailSection(builder, "Автомобиль", new[]
            {
                CreateCarDetail("Год", car.Year),
                CreateCarDetail("Марка", car.Brand),
                CreateCarDetail("Модель", car.Model),
                CreateCarDetail("Вин", car.Vin),
                CreateCarDetail("Локация", car.Location),
                CreateCarDetail("Тайтл", car.Title),
                CreateCarDetail("Ключ", car.Key),
                CreateCarDetail("Инспекция", car.Inspection),
                CreateCarDetail("Ре-экспорт", car.ReExport),
                CreateCarDetail("Объем", car.Volume),
                CreateCarDetail("Мощность", car.Power)
            });
            AppendCarDetailSection(builder, "Траты", new[]
            {
                CreateCarDetail("Портовые", car.PortCost),
                CreateCarDetail("Погрузка", car.LoadingCost),
                CreateCarDetail("Эвакуатор", car.TowTruckCost),
                CreateCarDetail("Паркинг", car.ParkingCost),
                CreateCarDetail("Досмотр", car.InspectionCost),
                CreateCarDetail("Реэкспорт", car.ReExportCost),
                CreateCarDetail("Экспертиза", car.ExpertiseCost),
                CreateCarDetail("Стоимость доставки", car.DeliveryCost)
            });
            AppendCarDetailSection(builder, "Владелец", new[]
            {
                CreateCarDetail("Имя", car.FirstName),
                CreateCarDetail("Фамилия", car.LastName),
                CreateCarDetail("Паспорт", car.Passport),
                CreateCarDetail("Адрес", car.Address)
            });
            AppendCarDetailSection(builder, "Комментарий", new[]
            {
                CreateCarDetail("Комментарий", car.Comment)
            });
            builder.Append("</div>");
            builder.Append("</div></div>");
            return builder.ToString();
        }

        private static string BuildCarCardTitle(CarRecord car)
        {
            var titleParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(car.Year))
            {
                titleParts.Add(car.Year.Trim());
            }

            if (!string.IsNullOrWhiteSpace(car.Brand))
            {
                titleParts.Add(car.Brand.Trim());
            }

            if (!string.IsNullOrWhiteSpace(car.Model))
            {
                titleParts.Add(car.Model.Trim());
            }

            if (titleParts.Count > 0)
            {
                return string.Join(" ", titleParts);
            }

            if (!string.IsNullOrWhiteSpace(car.ClientName))
            {
                return "Карточка автомобиля: " + car.ClientName.Trim();
            }

            return "Карточка автомобиля";
        }

        private static KeyValuePair<string, string> CreateCarDetail(string label, string value)
        {
            return new KeyValuePair<string, string>(label, value);
        }

        private static void AppendCarDetailSection(System.Text.StringBuilder builder, string title, IEnumerable<KeyValuePair<string, string>> values)
        {
            builder.Append("<div class=\"col-md-6\"><div class=\"panel panel-default crm-car-detail-section\">");
            builder.Append("<div class=\"panel-heading\"><h4 class=\"panel-title\">");
            builder.Append(HttpUtility.HtmlEncode(title));
            builder.Append("</h4></div><div class=\"panel-body\"><dl class=\"dl-horizontal crm-car-detail-list\">");

            foreach (var entry in values)
            {
                builder.Append("<dt>");
                builder.Append(HttpUtility.HtmlEncode(entry.Key));
                builder.Append("</dt><dd>");
                builder.Append(HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(entry.Value) ? "Не указано" : entry.Value.Trim()).Replace("\r\n", "<br />").Replace("\n", "<br />"));
                builder.Append("</dd>");
            }

            builder.Append("</dl></div></div></div>");
        }
    }
}
