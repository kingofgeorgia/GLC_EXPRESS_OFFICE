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

            HideMessage();

            if (!IsPostBack)
            {
                ActiveTab = ResolveDefaultCrmTab();
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

            if (SettingsLeadFilterCargoTypeDropDownList != null)
            {
                SettingsLeadFilterCargoTypeDropDownList.ClearSelection();
            }

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
                ClearTripsEditState();
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
                ClearDriversEditState();
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
                ClearFleetEditState();
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
                ClearClientsEditState();
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
            BindAll();
        }

        protected void TripsRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var trip = e.Item.DataItem as TripRecord;

            if (trip == null || !StringEquals(TripsEditingId, trip.Id))
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

            if (vehicle == null || !StringEquals(FleetEditingId, vehicle.Id))
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
                TripsEditingIdHiddenField.Value = value ?? string.Empty;
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
                DriversEditingIdHiddenField.Value = value ?? string.Empty;
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
                FleetEditingIdHiddenField.Value = value ?? string.Empty;
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
                ClientsEditingIdHiddenField.Value = value ?? string.Empty;
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
                return Convert.ToString(ViewState["SelectedInquiryId"]) ?? string.Empty;
            }
            set
            {
                ViewState["SelectedInquiryId"] = value ?? string.Empty;
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
            var recentInquiries = ApplyLeadFilters(HomeInquiryService.GetRecent(20));
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

            SettingsRolesRepeater.DataSource = editableRoles;
            SettingsRolesRepeater.DataBind();
            SettingsTripStatusesRepeater.DataSource = editableStatuses;
            SettingsTripStatusesRepeater.DataBind();
            SettingsHomePartnersRepeater.DataSource = editablePartners;
            SettingsHomePartnersRepeater.DataBind();
            SettingsHomeReviewsRepeater.DataSource = editableReviews;
            SettingsHomeReviewsRepeater.DataBind();
            SettingsInquiryListRepeater.DataSource = recentInquiries;
            SettingsInquiryListRepeater.DataBind();
            SettingsInquiryListRepeater.Visible = recentInquiries.Count > 0;
            SettingsRecentInquiriesRepeater.DataSource = selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry };
            SettingsRecentInquiriesRepeater.DataBind();
            SettingsRecentInquiriesRepeater.Visible = selectedInquiry != null;
            SettingsInquiryDetailPanel.Visible = selectedInquiry != null;
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
            var inquiries = ApplyLeadFilters(HomeInquiryService.GetRecent(20));
            var selectedInquiry = FindById(inquiries, SelectedInquiryId);

            if (selectedInquiry == null)
            {
                SelectedInquiryId = string.Empty;
            }

            SettingsInquiryListRepeater.DataSource = inquiries;
            SettingsInquiryListRepeater.DataBind();
            SettingsRecentInquiriesRepeater.DataSource = selectedInquiry == null ? new List<HomeInquiryRecord>() : new List<HomeInquiryRecord> { selectedInquiry };
            SettingsRecentInquiriesRepeater.DataBind();
            SettingsInquiryDetailPanel.Visible = selectedInquiry != null;
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
            BindFleetDriversCheckBoxList(data.Drivers, GetPostedListControlValues(FleetDriversCheckBoxList));
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
            var selectedFleetDriverIds = GetSelectedValues(FleetDriversCheckBoxList);

            BindClientsDropDownList(data.Clients, selectedClientId);
            BindDriversDropDownList(data.Drivers, selectedDriverId);
            BindVehiclesDropDownList(data.FleetVehicles, selectedVehicleId);
            BindTripStatusesDropDownList(TripsStatusDropDownList, CurrentCrmSettings.TripStatuses, string.IsNullOrWhiteSpace(selectedStatus) ? CurrentCrmSettings.DefaultTripStatus : selectedStatus);
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

        private void BindCars(CrmDataStore data)
        {
            var cars = data.Cars
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToList();

            var selectedCar = FindById(cars, SelectedCarId);

            if (selectedCar == null && cars.Count > 0)
            {
                selectedCar = cars[0];
                SelectedCarId = selectedCar.Id;
            }
            else if (selectedCar == null)
            {
                SelectedCarId = string.Empty;
            }

            _selectedCar = selectedCar;

            CarsRepeater.DataSource = cars;
            CarsRepeater.DataBind();
            CarsRepeater.Visible = cars.Count > 0;
            CarsEmptyPanel.Visible = cars.Count == 0;
            CarsDetailPanel.Visible = selectedCar != null;
            CarsDetailLiteral.Text = selectedCar == null ? string.Empty : BuildCarDetailHtml(selectedCar);
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

            TripsEditingId = trip.Id;
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

            DriversEditingId = driver.Id;
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

            FleetEditingId = vehicle.Id;
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

            ClientsEditingId = client.Id;
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
            ClearTripsEditState();
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
            ClearDriversEditState();
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
            ClearFleetEditState();
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
                || string.IsNullOrWhiteSpace(manager)
                || string.IsNullOrWhiteSpace(phone)
                || string.IsNullOrWhiteSpace(email))
            {
                ShowMessage("Заполните все поля клиента прямо в строке таблицы.", "warning");
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
            ClearClientsEditState();
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

            if (StringEquals(TripsEditingId, tripId))
            {
                ClearTripsForm();
                ClearTripsEditState();
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

            if (StringEquals(DriversEditingId, driverId))
            {
                ClearDriversForm();
                ClearDriversEditState();
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

            if (StringEquals(FleetEditingId, vehicleId))
            {
                ClearFleetForm();
                ClearFleetEditState();
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

            if (StringEquals(ClientsEditingId, clientId))
            {
                ClearClientsForm();
                ClearClientsEditState();
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
            var items = (inquiries ?? Enumerable.Empty<HomeInquiryRecord>()).ToList();

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

        protected bool IsClientsInlineEditRow(object recordId)
        {
            return StringEquals(ClientsEditingId, Convert.ToString(recordId));
        }

        protected bool IsTripsInlineEditRow(object recordId)
        {
            return StringEquals(TripsEditingId, Convert.ToString(recordId));
        }

        protected bool IsDriversInlineEditRow(object recordId)
        {
            return StringEquals(DriversEditingId, Convert.ToString(recordId));
        }

        protected bool IsFleetInlineEditRow(object recordId)
        {
            return StringEquals(FleetEditingId, Convert.ToString(recordId));
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

            hasChanged |= AssignStringIfDifferent(car.SourceInquiryId, inquiry.Id, value => car.SourceInquiryId = value);
            hasChanged |= AssignStringIfDifferent(car.ClientId, client == null ? string.Empty : client.Id, value => car.ClientId = value);
            hasChanged |= AssignStringIfDifferent(car.ClientName, client == null ? NormalizeCrmText(inquiry.Name) : client.Name, value => car.ClientName = value);
            hasChanged |= AssignStringIfDifferent(car.Forwarder, NormalizeCrmText(inquiry.AssignedManager), value => car.Forwarder = value);
            hasChanged |= AssignStringIfDifferent(car.Dealer, NormalizeCrmText(inquiry.Source), value => car.Dealer = value);
            hasChanged |= AssignStringIfDifferent(car.Location, NormalizeCrmText(inquiry.Direction), value => car.Location = value);
            hasChanged |= AssignStringIfDifferent(car.ReExport, NormalizeCrmText(inquiry.CargoType), value => car.ReExport = value);
            hasChanged |= AssignStringIfDifferent(car.Status, NormalizeCrmText(inquiry.Status), value => car.Status = value);
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

            return normalizedStatus.Contains("ожидаем документ")
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
