using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using GLC_EXPRESS.Models;

namespace GLC_EXPRESS.Services
{
    public sealed class CrmExcelImportSheetResult
    {
        public CrmExcelImportSheetResult()
        {
            RowWarnings = new List<string>();
        }

        public string SheetName { get; set; }

        public string DisplayName { get; set; }

        public int ProcessedRows { get; set; }

        public int AddedRows { get; set; }

        public int UpdatedRows { get; set; }

        public int SkippedRows { get; set; }

        public List<string> RowWarnings { get; private set; }

        public int UnchangedRows
        {
            get
            {
                var unchangedRows = ProcessedRows - AddedRows - UpdatedRows;
                return unchangedRows < 0 ? 0 : unchangedRows;
            }
        }
    }

    public sealed class CrmExcelImportResult
    {
        public CrmExcelImportResult()
        {
            SheetResults = new List<CrmExcelImportSheetResult>();
            Warnings = new List<string>();
        }

        public int RecognizedSheetCount { get; set; }

        public int AddedTrips { get; set; }

        public int UpdatedTrips { get; set; }

        public int AddedDrivers { get; set; }

        public int UpdatedDrivers { get; set; }

        public int AddedFleetVehicles { get; set; }

        public int UpdatedFleetVehicles { get; set; }

        public int AddedClients { get; set; }

        public int UpdatedClients { get; set; }

        public int AddedCars { get; set; }

        public int UpdatedCars { get; set; }

        public int SkippedRows { get; set; }

        public List<CrmExcelImportSheetResult> SheetResults { get; private set; }

        public List<string> Warnings { get; private set; }

        public bool HasChanges
        {
            get
            {
                return AddedTrips + UpdatedTrips
                    + AddedDrivers + UpdatedDrivers
                    + AddedFleetVehicles + UpdatedFleetVehicles
                    + AddedClients + UpdatedClients
                    + AddedCars + UpdatedCars > 0;
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Warnings.Add(message.Trim());
            }
        }

        public CrmExcelImportSheetResult EnsureSheetResult(string sheetName, string displayName)
        {
            var normalizedSheetName = (sheetName ?? string.Empty).Trim();
            var existingResult = SheetResults.FirstOrDefault(item => string.Equals(item.SheetName ?? string.Empty, normalizedSheetName, StringComparison.OrdinalIgnoreCase));

            if (existingResult != null)
            {
                if (string.IsNullOrWhiteSpace(existingResult.DisplayName))
                {
                    existingResult.DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedSheetName : displayName.Trim();
                }

                return existingResult;
            }

            var createdResult = new CrmExcelImportSheetResult
            {
                SheetName = normalizedSheetName,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedSheetName : displayName.Trim()
            };

            SheetResults.Add(createdResult);
            return createdResult;
        }

        public string BuildSummaryMessage()
        {
            var parts = new List<string>();
            AppendSummary(parts, "клиенты", AddedClients, UpdatedClients);
            AppendSummary(parts, "водители", AddedDrivers, UpdatedDrivers);
            AppendSummary(parts, "автопарк", AddedFleetVehicles, UpdatedFleetVehicles);
            AppendSummary(parts, "рейсы", AddedTrips, UpdatedTrips);
            AppendSummary(parts, "автомобили", AddedCars, UpdatedCars);

            var message = parts.Count == 0
                ? "Импорт Excel завершен: изменений не найдено."
                : "Импорт Excel завершен: " + string.Join("; ", parts) + ".";

            if (SkippedRows > 0)
            {
                message += " Пропущено строк: " + SkippedRows.ToString(CultureInfo.CurrentCulture) + ".";
            }

            if (Warnings.Count > 0)
            {
                var preview = Warnings.Take(3).ToList();
                message += " " + string.Join(" ", preview);

                if (Warnings.Count > preview.Count)
                {
                    message += " Дополнительных предупреждений: " + (Warnings.Count - preview.Count).ToString(CultureInfo.CurrentCulture) + ".";
                }
            }

            return message;
        }

        private static void AppendSummary(ICollection<string> parts, string label, int added, int updated)
        {
            if (added <= 0 && updated <= 0)
            {
                return;
            }

            if (added > 0 && updated > 0)
            {
                parts.Add(string.Format(CultureInfo.CurrentCulture, "{0}: +{1}, обновлено {2}", label, added, updated));
                return;
            }

            if (added > 0)
            {
                parts.Add(string.Format(CultureInfo.CurrentCulture, "{0}: +{1}", label, added));
                return;
            }

            parts.Add(string.Format(CultureInfo.CurrentCulture, "{0}: обновлено {1}", label, updated));
        }
    }

    public static class CrmExcelImportService
    {
        private static readonly int[] BuiltInDateFormatIds = { 14, 15, 16, 17, 18, 19, 20, 21, 22, 27, 30, 36, 45, 46, 47, 50, 57 };
        private static readonly string[] SupportedDateFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "dd.MM.yyyy",
            "dd.MM.yyyy HH:mm:ss",
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy",
            "M/d/yyyy H:mm:ss"
        };

        private static readonly TemplateSheetDefinition[] TemplateSheets =
        {
            new TemplateSheetDefinition(
                "README",
                new[] { "Раздел", "Инструкция", "Пример" },
                new[]
                {
                    new[] { "Общее", "Не меняйте названия рабочих листов Clients, Drivers, Fleet, Trips и Cars, если планируете импортировать файл обратно в CRM.", "Clients / Drivers / Fleet / Trips / Cars" },
                    new[] { "Общее", "Пустые ячейки не затирают существующие данные, но строки без ключевых полей будут пропущены.", "Оставьте пустым только то, что действительно не нужно обновлять" },
                    new[] { "Даты", "Для дат используйте формат yyyy-MM-dd или dd.MM.yyyy. Время можно указывать как yyyy-MM-dd HH:mm:ss.", "2026-04-29" },
                    new[] { "Clients", "Для надежного обновления клиентов используйте SourceInquiryId или Email. Без них CRM пробует сопоставление по телефону и имени.", "SourceInquiryId = lead-123" },
                    new[] { "Drivers", "Для водителей лучше всего заполнять FullName и Phone. Эти поля используются для сопоставления при повторном импорте.", "FullName = Giorgi Beridze" },
                    new[] { "Fleet", "Для автопарка приоритет сопоставления: VinCode, затем LicensePlate, затем марка + модель + номер.", "VinCode = WDB123456789" },
                    new[] { "Trips", "Для рейсов обязательно заполняйте Number. Связи с клиентом, водителем и автомобилем можно передавать текстом через имена и номера.", "Number = TRIP-2026-001" },
                    new[] { "Cars", "Для автомобилей приоритет сопоставления: SourceInquiryId, затем Vin, затем TripNumber + Brand + Model.", "Vin = JTDBR32E123456789" },
                    new[] { "Отчет", "После импорта CRM покажет детальный отчет по каждому листу: сколько строк добавлено, обновлено, пропущено и какие были предупреждения.", "Пропущено строк: 2" }
                }),
            new TemplateSheetDefinition("Clients", new[] { "SourceInquiryId", "Name", "Direction", "Manager", "Phone", "Email", "CreatedAt" }),
            new TemplateSheetDefinition("Drivers", new[] { "FullName", "BirthDate", "Phone", "Address", "CreatedAt" }),
            new TemplateSheetDefinition("Fleet", new[] { "CarBrand", "CarModel", "LicensePlate", "VinCode", "TrailerBrand", "TrailerModel", "TrailerLicensePlate", "AssignedDrivers", "CreatedAt" }),
            new TemplateSheetDefinition("Trips", new[] { "Number", "ClientName", "ClientEmail", "ClientPhone", "Status", "Country", "VehicleName", "VehicleLicensePlate", "VehicleVin", "VehicleBrand", "VehicleModel", "DriverName", "DriverPhone", "StartDate", "EndDate", "Freight", "Prepayment", "CreatedAt" }),
            new TemplateSheetDefinition("Cars", new[] { "SourceInquiryId", "ClientName", "TripNumber", "Forwarder", "Dealer", "Year", "Brand", "Model", "Vin", "Location", "Title", "Key", "Inspection", "ReExport", "Status", "StartPrice", "Invoice", "PortCost", "LoadingCost", "TowTruckCost", "ParkingCost", "InspectionCost", "ReExportCost", "ExpertiseCost", "DeliveryCost", "Volume", "Power", "Comment", "FirstName", "LastName", "Passport", "Address", "CreatedAt" })
        };

        private sealed class WorkbookSheet
        {
            public WorkbookSheet()
            {
                Rows = new List<WorkbookRow>();
            }

            public string Name { get; set; }

            public List<WorkbookRow> Rows { get; set; }
        }

        private sealed class WorkbookRow
        {
            public WorkbookRow()
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public int RowNumber { get; set; }

            public Dictionary<string, string> Values { get; set; }

            public string GetValue(params string[] aliases)
            {
                if (aliases == null)
                {
                    return string.Empty;
                }

                foreach (var alias in aliases)
                {
                    string value;

                    if (Values.TryGetValue(NormalizeToken(alias), out value) && !string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }

                return string.Empty;
            }
        }

        private sealed class TemplateSheetDefinition
        {
            public TemplateSheetDefinition(string name, IEnumerable<string> headers)
                : this(name, headers, null)
            {
            }

            public TemplateSheetDefinition(string name, IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
            {
                Name = name;
                Headers = headers == null ? new List<string>() : headers.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
                Rows = rows == null
                    ? new List<List<string>>()
                    : rows.Select(row => row == null ? new List<string>() : row.Select(item => item ?? string.Empty).ToList()).ToList();
            }

            public string Name { get; private set; }

            public List<string> Headers { get; private set; }

            public List<List<string>> Rows { get; private set; }
        }

        public static byte[] BuildTemplateWorkbook()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    WriteXmlEntry(archive, "[Content_Types].xml", BuildContentTypesDocument(TemplateSheets.Length));
                    WriteXmlEntry(archive, "_rels/.rels", BuildPackageRelationshipsDocument());
                    WriteXmlEntry(archive, "xl/workbook.xml", BuildWorkbookDocument(TemplateSheets));
                    WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsDocument(TemplateSheets.Length));

                    for (var index = 0; index < TemplateSheets.Length; index++)
                    {
                        WriteXmlEntry(archive, string.Format(CultureInfo.InvariantCulture, "xl/worksheets/sheet{0}.xml", index + 1), BuildWorksheetDocument(TemplateSheets[index].Headers, TemplateSheets[index].Rows));
                    }
                }

                return memoryStream.ToArray();
            }
        }

        public static CrmExcelImportResult Import(Stream workbookStream, CrmDataStore data)
        {
            if (workbookStream == null)
            {
                throw new ArgumentNullException("workbookStream");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            EnsureCollections(data);

            var sheets = ReadWorkbookSheets(workbookStream);
            var result = new CrmExcelImportResult();

            ImportClients(FindSheet(sheets, "clients", "клиенты"), data, result);
            ImportDrivers(FindSheet(sheets, "drivers", "водители"), data, result);
            ImportFleet(FindSheet(sheets, "fleet", "автопарк", "fleetvehicles"), data, result);
            ImportTrips(FindSheet(sheets, "trips", "рейсы"), data, result);
            ImportCars(FindSheet(sheets, "cars", "автомобили"), data, result);

            if (result.RecognizedSheetCount <= 0)
            {
                throw new InvalidOperationException("Excel-файл не содержит поддерживаемых листов. Используйте листы Clients/Клиенты, Drivers/Водители, Fleet/Автопарк, Trips/Рейсы или Cars/Автомобили.");
            }

            RefreshFleetDriverNames(data);
            RepairTripReferences(data);
            RepairCarClientLinks(data);
            return result;
        }

        private static void ImportClients(WorkbookSheet sheet, CrmDataStore data, CrmExcelImportResult result)
        {
            if (sheet == null)
            {
                return;
            }

            var sheetResult = RegisterRecognizedSheet(result, sheet.Name, "Клиенты");

            foreach (var row in sheet.Rows)
            {
                var sourceInquiryId = row.GetValue("SourceInquiryId", "SourceInquiry", "InquiryId", "Id заявки", "Источник заявки");
                var name = row.GetValue("Name", "Client", "ClientName", "Клиент", "Название", "ФИО");
                var direction = row.GetValue("Direction", "Направление");
                var manager = row.GetValue("Manager", "Менеджер");
                var phone = row.GetValue("Phone", "PhoneNumber", "Телефон", "Телефон клиента");
                var email = row.GetValue("Email", "E-mail", "Почта", "Email клиента");
                var createdAt = row.GetValue("CreatedAt", "CreatedAtUtc", "Created", "Дата создания");

                if (IsRowEmpty(sourceInquiryId, name, direction, manager, phone, email, createdAt))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sourceInquiryId)
                    && string.IsNullOrWhiteSpace(name)
                    && string.IsNullOrWhiteSpace(phone)
                    && string.IsNullOrWhiteSpace(email))
                {
                    SkipRow(result, sheetResult, row.RowNumber, "клиент без имени, телефона, email или source inquiry id");
                    continue;
                }

                sheetResult.ProcessedRows++;

                var client = FindClient(data, sourceInquiryId, email, phone, name);
                var isNew = client == null;
                var previousName = isNew ? string.Empty : client.Name;
                var hasChanged = isNew;

                if (isNew)
                {
                    client = new ClientRecord();
                    client.CreatedAtUtc = ResolveCreatedAt(createdAt, client.CreatedAtUtc);
                    data.Clients.Insert(0, client);
                }

                hasChanged |= AssignStringIfProvided(client.SourceInquiryId, sourceInquiryId, value => client.SourceInquiryId = value);
                hasChanged |= AssignStringIfProvided(client.Name, name, value => client.Name = value);
                hasChanged |= AssignStringIfProvided(client.Direction, direction, value => client.Direction = value);
                hasChanged |= AssignStringIfProvided(client.Manager, manager, value => client.Manager = value);
                hasChanged |= AssignStringIfProvided(client.PhoneNumber, phone, value => client.PhoneNumber = value);
                hasChanged |= AssignStringIfProvided(client.Email, email, value => client.Email = value);
                hasChanged |= AssignDateTimeIfProvided(client.CreatedAtUtc, createdAt, value => client.CreatedAtUtc = value);

                if (!isNew && hasChanged && !StringEquals(previousName, client.Name) && !string.IsNullOrWhiteSpace(previousName))
                {
                    SyncClientReferences(data, client, previousName);
                }

                if (isNew)
                {
                    result.AddedClients++;
                    sheetResult.AddedRows++;
                }
                else if (hasChanged)
                {
                    result.UpdatedClients++;
                    sheetResult.UpdatedRows++;
                }
            }
        }

        private static void ImportDrivers(WorkbookSheet sheet, CrmDataStore data, CrmExcelImportResult result)
        {
            if (sheet == null)
            {
                return;
            }

            var sheetResult = RegisterRecognizedSheet(result, sheet.Name, "Водители");

            foreach (var row in sheet.Rows)
            {
                var fullName = row.GetValue("FullName", "Driver", "DriverName", "Водитель", "ФИО");
                var birthDate = row.GetValue("BirthDate", "Дата рождения");
                var phone = row.GetValue("Phone", "PhoneNumber", "Телефон", "Телефон водителя");
                var address = row.GetValue("Address", "Адрес");
                var createdAt = row.GetValue("CreatedAt", "CreatedAtUtc", "Created", "Дата создания");

                if (IsRowEmpty(fullName, birthDate, phone, address, createdAt))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(phone))
                {
                    SkipRow(result, sheetResult, row.RowNumber, "водитель без имени или телефона");
                    continue;
                }

                sheetResult.ProcessedRows++;

                var driver = FindDriver(data, fullName, phone);
                var isNew = driver == null;
                var previousName = isNew ? string.Empty : driver.FullName;
                var hasChanged = isNew;

                if (isNew)
                {
                    driver = new DriverRecord();
                    driver.CreatedAtUtc = ResolveCreatedAt(createdAt, driver.CreatedAtUtc);
                    data.Drivers.Insert(0, driver);
                }

                hasChanged |= AssignStringIfProvided(driver.FullName, fullName, value => driver.FullName = value);
                hasChanged |= AssignDateTextIfProvided(driver.BirthDate, birthDate, value => driver.BirthDate = value);
                hasChanged |= AssignStringIfProvided(driver.PhoneNumber, phone, value => driver.PhoneNumber = value);
                hasChanged |= AssignStringIfProvided(driver.Address, address, value => driver.Address = value);
                hasChanged |= AssignDateTimeIfProvided(driver.CreatedAtUtc, createdAt, value => driver.CreatedAtUtc = value);

                if (!isNew && hasChanged && !StringEquals(previousName, driver.FullName) && !string.IsNullOrWhiteSpace(previousName))
                {
                    SyncDriverReferences(data, driver, previousName);
                }

                if (isNew)
                {
                    result.AddedDrivers++;
                    sheetResult.AddedRows++;
                }
                else if (hasChanged)
                {
                    result.UpdatedDrivers++;
                    sheetResult.UpdatedRows++;
                }
            }
        }

        private static void ImportFleet(WorkbookSheet sheet, CrmDataStore data, CrmExcelImportResult result)
        {
            if (sheet == null)
            {
                return;
            }

            var sheetResult = RegisterRecognizedSheet(result, sheet.Name, "Автопарк");

            foreach (var row in sheet.Rows)
            {
                var brand = row.GetValue("CarBrand", "Brand", "Марка", "Марка авто");
                var model = row.GetValue("CarModel", "Model", "Модель", "Модель авто");
                var licensePlate = row.GetValue("LicensePlate", "Plate", "Госномер", "Номер авто");
                var vinCode = row.GetValue("VinCode", "VIN", "Vin", "Вин", "VIN код");
                var trailerBrand = row.GetValue("TrailerBrand", "Марка прицепа");
                var trailerModel = row.GetValue("TrailerModel", "Модель прицепа");
                var trailerPlate = row.GetValue("TrailerLicensePlate", "TrailerPlate", "Номер прицепа", "Госномер прицепа");
                var assignedDriversRaw = row.GetValue("AssignedDrivers", "AssignedDriverNames", "Drivers", "Водители", "Назначенные водители");
                var createdAt = row.GetValue("CreatedAt", "CreatedAtUtc", "Created", "Дата создания");

                if (IsRowEmpty(brand, model, licensePlate, vinCode, trailerBrand, trailerModel, trailerPlate, assignedDriversRaw, createdAt))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(vinCode) && string.IsNullOrWhiteSpace(licensePlate) && string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(model))
                {
                    SkipRow(result, sheetResult, row.RowNumber, "автопарк без VIN, госномера или марки/модели");
                    continue;
                }

                sheetResult.ProcessedRows++;

                var vehicle = FindVehicle(data, vinCode, licensePlate, brand, model, string.Empty);
                var isNew = vehicle == null;
                var previousVehicleName = isNew ? string.Empty : BuildVehicleName(vehicle);
                var hasChanged = isNew;

                if (isNew)
                {
                    vehicle = new FleetVehicleRecord();
                    vehicle.CreatedAtUtc = ResolveCreatedAt(createdAt, vehicle.CreatedAtUtc);
                    data.FleetVehicles.Insert(0, vehicle);
                }

                hasChanged |= AssignStringIfProvided(vehicle.CarBrand, brand, value => vehicle.CarBrand = value);
                hasChanged |= AssignStringIfProvided(vehicle.CarModel, model, value => vehicle.CarModel = value);
                hasChanged |= AssignStringIfProvided(vehicle.LicensePlate, licensePlate, value => vehicle.LicensePlate = value);
                hasChanged |= AssignStringIfProvided(vehicle.VinCode, vinCode, value => vehicle.VinCode = value);
                hasChanged |= AssignStringIfProvided(vehicle.TrailerBrand, trailerBrand, value => vehicle.TrailerBrand = value);
                hasChanged |= AssignStringIfProvided(vehicle.TrailerModel, trailerModel, value => vehicle.TrailerModel = value);
                hasChanged |= AssignStringIfProvided(vehicle.TrailerLicensePlate, trailerPlate, value => vehicle.TrailerLicensePlate = value);
                hasChanged |= AssignDateTimeIfProvided(vehicle.CreatedAtUtc, createdAt, value => vehicle.CreatedAtUtc = value);

                var importedDriverNames = SplitListValue(assignedDriversRaw);
                var resolvedDriverIds = ResolveDriverIdsByNames(data.Drivers, importedDriverNames);

                if (importedDriverNames.Count > 0)
                {
                    var normalizedNames = importedDriverNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    if (!SequenceEquals(vehicle.AssignedDriverNames, normalizedNames))
                    {
                        vehicle.AssignedDriverNames = normalizedNames;
                        hasChanged = true;
                    }

                    if (!SequenceEquals(vehicle.AssignedDriverIds, resolvedDriverIds))
                    {
                        vehicle.AssignedDriverIds = resolvedDriverIds;
                        hasChanged = true;
                    }
                }

                if (!isNew && hasChanged && !StringEquals(previousVehicleName, BuildVehicleName(vehicle)) && !string.IsNullOrWhiteSpace(previousVehicleName))
                {
                    SyncVehicleReferences(data, vehicle, previousVehicleName);
                }

                if (isNew)
                {
                    result.AddedFleetVehicles++;
                    sheetResult.AddedRows++;
                }
                else if (hasChanged)
                {
                    result.UpdatedFleetVehicles++;
                    sheetResult.UpdatedRows++;
                }
            }
        }

        private static void ImportTrips(WorkbookSheet sheet, CrmDataStore data, CrmExcelImportResult result)
        {
            if (sheet == null)
            {
                return;
            }

            var sheetResult = RegisterRecognizedSheet(result, sheet.Name, "Рейсы");

            foreach (var row in sheet.Rows)
            {
                var number = row.GetValue("Number", "TripNumber", "Номер", "Номер рейса");
                var clientName = row.GetValue("ClientName", "Client", "Клиент");
                var clientEmail = row.GetValue("ClientEmail", "Email клиента", "Почта клиента");
                var clientPhone = row.GetValue("ClientPhone", "Телефон клиента");
                var status = row.GetValue("Status", "Статус");
                var country = row.GetValue("Country", "Страна");
                var vehicleName = row.GetValue("VehicleName", "Vehicle", "Автомобиль");
                var vehiclePlate = row.GetValue("VehicleLicensePlate", "LicensePlate", "Госномер", "Номер авто");
                var vehicleVin = row.GetValue("VehicleVin", "VehicleVIN", "VIN");
                var vehicleBrand = row.GetValue("VehicleBrand", "Марка авто");
                var vehicleModel = row.GetValue("VehicleModel", "Модель авто");
                var driverName = row.GetValue("DriverName", "Driver", "Водитель");
                var driverPhone = row.GetValue("DriverPhone", "Телефон водителя");
                var startDate = row.GetValue("StartDate", "Дата начала", "Начало");
                var endDate = row.GetValue("EndDate", "Дата окончания", "Конец");
                var freight = row.GetValue("Freight", "Фрахт");
                var prepayment = row.GetValue("Prepayment", "Предоплата");
                var createdAt = row.GetValue("CreatedAt", "CreatedAtUtc", "Created", "Дата создания");

                if (IsRowEmpty(number, clientName, clientEmail, clientPhone, status, country, vehicleName, vehiclePlate, vehicleVin, vehicleBrand, vehicleModel, driverName, driverPhone, startDate, endDate, freight, prepayment, createdAt))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(number))
                {
                    SkipRow(result, sheetResult, row.RowNumber, "рейс без номера");
                    continue;
                }

                sheetResult.ProcessedRows++;

                var trip = data.Trips.FirstOrDefault(item => StringEquals(item.Number, number));
                var isNew = trip == null;
                var hasChanged = isNew;

                if (isNew)
                {
                    trip = new TripRecord();
                    trip.CreatedAtUtc = ResolveCreatedAt(createdAt, trip.CreatedAtUtc);
                    data.Trips.Insert(0, trip);
                }

                var client = FindClient(data, string.Empty, clientEmail, clientPhone, clientName);
                var driver = FindDriver(data, driverName, driverPhone);
                var vehicle = FindVehicle(data, vehicleVin, vehiclePlate, vehicleBrand, vehicleModel, vehicleName);

                hasChanged |= AssignStringIfProvided(trip.Number, number, value => trip.Number = value);
                hasChanged |= AssignStringIfDifferent(trip.ClientId, client == null ? string.Empty : client.Id, value => trip.ClientId = value);
                hasChanged |= AssignStringIfProvided(trip.ClientName, string.IsNullOrWhiteSpace(clientName) && client != null ? client.Name : clientName, value => trip.ClientName = value);
                hasChanged |= AssignStringIfProvided(trip.Status, status, value => trip.Status = value);
                hasChanged |= AssignStringIfProvided(trip.Country, country, value => trip.Country = value);
                hasChanged |= AssignStringIfDifferent(trip.VehicleId, vehicle == null ? string.Empty : vehicle.Id, value => trip.VehicleId = value);
                hasChanged |= AssignStringIfProvided(trip.VehicleName, ResolveTripVehicleName(vehicle, vehicleName), value => trip.VehicleName = value);
                hasChanged |= AssignStringIfDifferent(trip.DriverId, driver == null ? string.Empty : driver.Id, value => trip.DriverId = value);
                hasChanged |= AssignStringIfProvided(trip.DriverName, string.IsNullOrWhiteSpace(driverName) && driver != null ? driver.FullName : driverName, value => trip.DriverName = value);
                hasChanged |= AssignDateTextIfProvided(trip.StartDate, startDate, value => trip.StartDate = value);
                hasChanged |= AssignDateTextIfProvided(trip.EndDate, endDate, value => trip.EndDate = value);
                hasChanged |= AssignStringIfProvided(trip.Freight, freight, value => trip.Freight = value);
                hasChanged |= AssignStringIfProvided(trip.Prepayment, prepayment, value => trip.Prepayment = value);
                hasChanged |= AssignDateTimeIfProvided(trip.CreatedAtUtc, createdAt, value => trip.CreatedAtUtc = value);

                if (isNew)
                {
                    result.AddedTrips++;
                    sheetResult.AddedRows++;
                }
                else if (hasChanged)
                {
                    result.UpdatedTrips++;
                    sheetResult.UpdatedRows++;
                }
            }
        }

        private static void ImportCars(WorkbookSheet sheet, CrmDataStore data, CrmExcelImportResult result)
        {
            if (sheet == null)
            {
                return;
            }

            var sheetResult = RegisterRecognizedSheet(result, sheet.Name, "Автомобили");

            foreach (var row in sheet.Rows)
            {
                var sourceInquiryId = row.GetValue("SourceInquiryId", "InquiryId", "Id заявки");
                var clientName = row.GetValue("ClientName", "Client", "Клиент");
                var tripNumber = row.GetValue("TripNumber", "Номер рейса", "Trip");
                var forwarder = row.GetValue("Forwarder", "Экспедитор", "Manager", "Менеджер");
                var dealer = row.GetValue("Dealer", "Дилер", "Source", "Источник");
                var year = row.GetValue("Year", "Год");
                var brand = row.GetValue("Brand", "Марка");
                var model = row.GetValue("Model", "Модель");
                var vin = row.GetValue("Vin", "VIN", "Вин");
                var location = row.GetValue("Location", "Локация", "Направление");
                var title = row.GetValue("Title", "Тайтл", "Титул");
                var key = row.GetValue("Key", "Ключ");
                var inspection = row.GetValue("Inspection", "Осмотр");
                var reExport = row.GetValue("ReExport", "Реэкспорт", "Ре-экспорт");
                var status = row.GetValue("Status", "Статус");
                var startPrice = row.GetValue("StartPrice", "Стартовая цена");
                var invoice = row.GetValue("Invoice", "Инвойс");
                var portCost = row.GetValue("PortCost", "Порт", "Стоимость порта");
                var loadingCost = row.GetValue("LoadingCost", "Погрузка", "Стоимость погрузки");
                var towTruckCost = row.GetValue("TowTruckCost", "Эвакуатор", "Стоимость эвакуатора");
                var parkingCost = row.GetValue("ParkingCost", "Парковка", "Стоимость парковки");
                var inspectionCost = row.GetValue("InspectionCost", "Стоимость осмотра");
                var reExportCost = row.GetValue("ReExportCost", "Стоимость реэкспорта", "Стоимость ре-экспорта");
                var expertiseCost = row.GetValue("ExpertiseCost", "Экспертиза", "Стоимость экспертизы");
                var deliveryCost = row.GetValue("DeliveryCost", "Доставка", "Стоимость доставки");
                var volume = row.GetValue("Volume", "Объем");
                var power = row.GetValue("Power", "Мощность");
                var comment = row.GetValue("Comment", "Комментарий");
                var firstName = row.GetValue("FirstName", "Имя");
                var lastName = row.GetValue("LastName", "Фамилия");
                var passport = row.GetValue("Passport", "Паспорт");
                var address = row.GetValue("Address", "Адрес");
                var createdAt = row.GetValue("CreatedAt", "CreatedAtUtc", "Created", "Дата создания");

                if (IsRowEmpty(sourceInquiryId, clientName, tripNumber, forwarder, dealer, year, brand, model, vin, location, title, key, inspection, reExport, status, startPrice, invoice, portCost, loadingCost, towTruckCost, parkingCost, inspectionCost, reExportCost, expertiseCost, deliveryCost, volume, power, comment, firstName, lastName, passport, address, createdAt))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sourceInquiryId) && string.IsNullOrWhiteSpace(vin) && IsRowEmpty(tripNumber, brand, model))
                {
                    SkipRow(result, sheetResult, row.RowNumber, "автомобиль без source inquiry id, VIN или комбинации номер рейса + марка + модель");
                    continue;
                }

                sheetResult.ProcessedRows++;

                var car = FindCar(data, sourceInquiryId, vin, tripNumber, brand, model);
                var isNew = car == null;
                var hasChanged = isNew;

                if (isNew)
                {
                    car = new CarRecord();
                    car.CreatedAtUtc = ResolveCreatedAt(createdAt, car.CreatedAtUtc);
                    data.Cars.Insert(0, car);
                }

                var client = FindClient(data, string.Empty, string.Empty, string.Empty, clientName);

                hasChanged |= AssignStringIfProvided(car.SourceInquiryId, sourceInquiryId, value => car.SourceInquiryId = value);
                hasChanged |= AssignStringIfDifferent(car.ClientId, client == null ? string.Empty : client.Id, value => car.ClientId = value);
                hasChanged |= AssignStringIfProvided(car.ClientName, string.IsNullOrWhiteSpace(clientName) && client != null ? client.Name : clientName, value => car.ClientName = value);
                hasChanged |= AssignStringIfProvided(car.TripNumber, tripNumber, value => car.TripNumber = value);
                hasChanged |= AssignStringIfProvided(car.Forwarder, forwarder, value => car.Forwarder = value);
                hasChanged |= AssignStringIfProvided(car.Dealer, dealer, value => car.Dealer = value);
                hasChanged |= AssignStringIfProvided(car.Year, year, value => car.Year = value);
                hasChanged |= AssignStringIfProvided(car.Brand, brand, value => car.Brand = value);
                hasChanged |= AssignStringIfProvided(car.Model, model, value => car.Model = value);
                hasChanged |= AssignStringIfProvided(car.Vin, vin, value => car.Vin = value);
                hasChanged |= AssignStringIfProvided(car.Location, location, value => car.Location = value);
                hasChanged |= AssignStringIfProvided(car.Title, title, value => car.Title = value);
                hasChanged |= AssignStringIfProvided(car.Key, key, value => car.Key = value);
                hasChanged |= AssignStringIfProvided(car.Inspection, inspection, value => car.Inspection = value);
                hasChanged |= AssignStringIfProvided(car.ReExport, reExport, value => car.ReExport = value);
                hasChanged |= AssignStringIfProvided(car.Status, status, value => car.Status = value);
                hasChanged |= AssignStringIfProvided(car.StartPrice, startPrice, value => car.StartPrice = value);
                hasChanged |= AssignStringIfProvided(car.Invoice, invoice, value => car.Invoice = value);
                hasChanged |= AssignStringIfProvided(car.PortCost, portCost, value => car.PortCost = value);
                hasChanged |= AssignStringIfProvided(car.LoadingCost, loadingCost, value => car.LoadingCost = value);
                hasChanged |= AssignStringIfProvided(car.TowTruckCost, towTruckCost, value => car.TowTruckCost = value);
                hasChanged |= AssignStringIfProvided(car.ParkingCost, parkingCost, value => car.ParkingCost = value);
                hasChanged |= AssignStringIfProvided(car.InspectionCost, inspectionCost, value => car.InspectionCost = value);
                hasChanged |= AssignStringIfProvided(car.ReExportCost, reExportCost, value => car.ReExportCost = value);
                hasChanged |= AssignStringIfProvided(car.ExpertiseCost, expertiseCost, value => car.ExpertiseCost = value);
                hasChanged |= AssignStringIfProvided(car.DeliveryCost, deliveryCost, value => car.DeliveryCost = value);
                hasChanged |= AssignStringIfProvided(car.Volume, volume, value => car.Volume = value);
                hasChanged |= AssignStringIfProvided(car.Power, power, value => car.Power = value);
                hasChanged |= AssignStringIfProvided(car.Comment, comment, value => car.Comment = value);
                hasChanged |= AssignStringIfProvided(car.FirstName, firstName, value => car.FirstName = value);
                hasChanged |= AssignStringIfProvided(car.LastName, lastName, value => car.LastName = value);
                hasChanged |= AssignStringIfProvided(car.Passport, passport, value => car.Passport = value);
                hasChanged |= AssignStringIfProvided(car.Address, address, value => car.Address = value);
                hasChanged |= AssignDateTimeIfProvided(car.CreatedAtUtc, createdAt, value => car.CreatedAtUtc = value);

                if (isNew)
                {
                    result.AddedCars++;
                    sheetResult.AddedRows++;
                }
                else if (hasChanged)
                {
                    result.UpdatedCars++;
                    sheetResult.UpdatedRows++;
                }
            }
        }

        private static CrmExcelImportSheetResult RegisterRecognizedSheet(CrmExcelImportResult result, string sheetName, string displayName)
        {
            result.RecognizedSheetCount++;
            return result.EnsureSheetResult(sheetName, displayName);
        }

        private static List<WorkbookSheet> ReadWorkbookSheets(Stream workbookStream)
        {
            try
            {
                using (var archive = new ZipArchive(workbookStream, ZipArchiveMode.Read, true))
                {
                    var workbookEntry = archive.GetEntry("xl/workbook.xml");

                    if (workbookEntry == null)
                    {
                        throw new InvalidOperationException("Не найден xl/workbook.xml.");
                    }

                    var workbookRelationships = LoadWorkbookRelationships(archive);
                    var sharedStrings = LoadSharedStrings(archive);
                    var dateStyleIndexes = LoadDateStyleIndexes(archive);
                    var workbookDocument = LoadXmlDocument(workbookEntry);
                    var workbookNamespace = workbookDocument.Root == null ? XNamespace.None : workbookDocument.Root.Name.Namespace;
                    var relationshipNamespace = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                    var sheets = new List<WorkbookSheet>();
                    var sheetContainer = workbookDocument.Root == null ? null : workbookDocument.Root.Element(workbookNamespace + "sheets");

                    if (sheetContainer == null)
                    {
                        return sheets;
                    }

                    foreach (var sheetElement in sheetContainer.Elements(workbookNamespace + "sheet"))
                    {
                        var name = GetAttributeValue(sheetElement, "name");
                        var relationshipId = GetAttributeValue(sheetElement, relationshipNamespace + "id");
                        string targetPath;

                        if (string.IsNullOrWhiteSpace(relationshipId)
                            || !workbookRelationships.TryGetValue(relationshipId, out targetPath)
                            || string.IsNullOrWhiteSpace(targetPath))
                        {
                            continue;
                        }

                        var rows = ReadSheetRows(archive, targetPath, sharedStrings, dateStyleIndexes);
                        sheets.Add(new WorkbookSheet
                        {
                            Name = name,
                            Rows = rows
                        });
                    }

                    return sheets;
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Не удалось прочитать Excel-файл .xlsx. Сохраните файл в стандартном формате Excel и повторите импорт. " + exception.Message, exception);
            }
        }

        private static WorkbookSheet FindSheet(IEnumerable<WorkbookSheet> sheets, params string[] aliases)
        {
            var normalizedAliases = (aliases ?? Enumerable.Empty<string>())
                .Select(NormalizeToken)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (sheets ?? Enumerable.Empty<WorkbookSheet>())
                .FirstOrDefault(sheet => normalizedAliases.Contains(NormalizeToken(sheet == null ? string.Empty : sheet.Name), StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, string> LoadWorkbookRelationships(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/_rels/workbook.xml.rels");

            if (entry == null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var document = LoadXmlDocument(entry);
            var namespaceValue = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;
            var relationships = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var relationship in document.Descendants(namespaceValue + "Relationship"))
            {
                var id = GetAttributeValue(relationship, "Id");
                var target = GetAttributeValue(relationship, "Target");

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                relationships[id] = ResolveZipPath("xl/workbook.xml", target);
            }

            return relationships;
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");

            if (entry == null)
            {
                return new List<string>();
            }

            var document = LoadXmlDocument(entry);
            var namespaceValue = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;

            return document.Descendants(namespaceValue + "si")
                .Select(ReadSharedStringItem)
                .ToList();
        }

        private static string ReadSharedStringItem(XElement item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            return string.Concat(item.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value));
        }

        private static HashSet<int> LoadDateStyleIndexes(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/styles.xml");
            var indexes = new HashSet<int>();

            if (entry == null)
            {
                return indexes;
            }

            var document = LoadXmlDocument(entry);
            var namespaceValue = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;
            var customFormats = document.Descendants(namespaceValue + "numFmt")
                .Where(item => item.Attribute("numFmtId") != null && item.Attribute("formatCode") != null)
                .ToDictionary(
                    item => Convert.ToInt32(item.Attribute("numFmtId").Value, CultureInfo.InvariantCulture),
                    item => item.Attribute("formatCode").Value,
                    EqualityComparer<int>.Default);

            var cellFormats = document.Descendants(namespaceValue + "cellXfs").Elements(namespaceValue + "xf").ToList();

            for (var index = 0; index < cellFormats.Count; index++)
            {
                var numFmtIdValue = GetAttributeValue(cellFormats[index], "numFmtId");
                int numFmtId;

                if (!int.TryParse(numFmtIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out numFmtId))
                {
                    continue;
                }

                if (BuiltInDateFormatIds.Contains(numFmtId) || IsCustomDateFormat(customFormats, numFmtId))
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }

        private static bool IsCustomDateFormat(IDictionary<int, string> formats, int numFmtId)
        {
            string formatCode;

            if (formats == null || !formats.TryGetValue(numFmtId, out formatCode) || string.IsNullOrWhiteSpace(formatCode))
            {
                return false;
            }

            var cleaned = RemoveFormatSections(formatCode).ToLowerInvariant();

            if (cleaned.IndexOf('y') >= 0 || cleaned.IndexOf('d') >= 0)
            {
                return true;
            }

            return cleaned.IndexOf('h') >= 0 || cleaned.IndexOf('s') >= 0;
        }

        private static string RemoveFormatSections(string formatCode)
        {
            var characters = new List<char>();
            var isQuoted = false;
            var bracketDepth = 0;

            foreach (var character in formatCode ?? string.Empty)
            {
                if (character == '"')
                {
                    isQuoted = !isQuoted;
                    continue;
                }

                if (isQuoted)
                {
                    continue;
                }

                if (character == '[')
                {
                    bracketDepth++;
                    continue;
                }

                if (character == ']' && bracketDepth > 0)
                {
                    bracketDepth--;
                    continue;
                }

                if (bracketDepth <= 0)
                {
                    characters.Add(character);
                }
            }

            return new string(characters.ToArray());
        }

        private static List<WorkbookRow> ReadSheetRows(ZipArchive archive, string sheetPath, IList<string> sharedStrings, ISet<int> dateStyleIndexes)
        {
            var entry = archive.GetEntry(sheetPath);

            if (entry == null)
            {
                return new List<WorkbookRow>();
            }

            var document = LoadXmlDocument(entry);
            var namespaceValue = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;
            var sheetData = document.Descendants(namespaceValue + "sheetData").FirstOrDefault();
            var result = new List<WorkbookRow>();

            if (sheetData == null)
            {
                return result;
            }

            Dictionary<int, string> headers = null;

            foreach (var rowElement in sheetData.Elements(namespaceValue + "row"))
            {
                var cells = ReadRowValues(rowElement, namespaceValue, sharedStrings, dateStyleIndexes);

                if (cells.Count == 0)
                {
                    continue;
                }

                if (headers == null)
                {
                    headers = cells
                        .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                        .ToDictionary(item => item.Key, item => NormalizeToken(item.Value));
                    continue;
                }

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var header in headers)
                {
                    string value;

                    if (!string.IsNullOrWhiteSpace(header.Value) && cells.TryGetValue(header.Key, out value) && !string.IsNullOrWhiteSpace(value))
                    {
                        values[header.Value] = value.Trim();
                    }
                }

                if (values.Count == 0)
                {
                    continue;
                }

                result.Add(new WorkbookRow
                {
                    RowNumber = ResolveRowNumber(rowElement),
                    Values = values
                });
            }

            return result;
        }

        private static Dictionary<int, string> ReadRowValues(XElement rowElement, XNamespace namespaceValue, IList<string> sharedStrings, ISet<int> dateStyleIndexes)
        {
            var values = new Dictionary<int, string>();
            var fallbackColumnIndex = 1;

            foreach (var cell in rowElement.Elements(namespaceValue + "c"))
            {
                var cellReference = GetAttributeValue(cell, "r");
                var columnIndex = ResolveColumnIndex(cellReference, fallbackColumnIndex);
                var value = ReadCellValue(cell, namespaceValue, sharedStrings, dateStyleIndexes);
                values[columnIndex] = value;
                fallbackColumnIndex = columnIndex + 1;
            }

            return values;
        }

        private static int ResolveColumnIndex(string cellReference, int fallbackIndex)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                return fallbackIndex;
            }

            var columnIndex = 0;

            foreach (var character in cellReference.ToUpperInvariant())
            {
                if (!char.IsLetter(character))
                {
                    break;
                }

                columnIndex = columnIndex * 26 + (character - 'A' + 1);
            }

            return columnIndex <= 0 ? fallbackIndex : columnIndex;
        }

        private static int ResolveRowNumber(XElement rowElement)
        {
            var rawValue = GetAttributeValue(rowElement, "r");
            int rowNumber;
            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out rowNumber) ? rowNumber : 0;
        }

        private static string ReadCellValue(XElement cell, XNamespace namespaceValue, IList<string> sharedStrings, ISet<int> dateStyleIndexes)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            var cellType = GetAttributeValue(cell, "t");
            var inlineString = cell.Element(namespaceValue + "is");

            if (inlineString != null)
            {
                return string.Concat(inlineString.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value)).Trim();
            }

            var valueElement = cell.Element(namespaceValue + "v");
            var rawValue = valueElement == null ? string.Empty : valueElement.Value;

            if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase))
            {
                int sharedStringIndex;

                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out sharedStringIndex)
                    && sharedStringIndex >= 0
                    && sharedStringIndex < sharedStrings.Count)
                {
                    return (sharedStrings[sharedStringIndex] ?? string.Empty).Trim();
                }

                return rawValue.Trim();
            }

            if (string.Equals(cellType, "b", StringComparison.OrdinalIgnoreCase))
            {
                return rawValue == "1" ? "TRUE" : "FALSE";
            }

            int styleIndex;

            if (int.TryParse(GetAttributeValue(cell, "s"), NumberStyles.Integer, CultureInfo.InvariantCulture, out styleIndex)
                && dateStyleIndexes.Contains(styleIndex))
            {
                return ConvertExcelDateValue(rawValue);
            }

            return rawValue.Trim();
        }

        private static string ConvertExcelDateValue(string rawValue)
        {
            double numericValue;

            if (!double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out numericValue))
            {
                return (rawValue ?? string.Empty).Trim();
            }

            try
            {
                var value = DateTime.FromOADate(numericValue);
                return Math.Abs(numericValue - Math.Truncate(numericValue)) < 0.0000001d
                    ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch (ArgumentException)
            {
                return (rawValue ?? string.Empty).Trim();
            }
        }

        private static XDocument LoadXmlDocument(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                return XDocument.Load(stream);
            }
        }

        private static string ResolveZipPath(string basePartPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return string.Empty;
            }

            var sanitizedTarget = targetPath.Replace('\\', '/');

            if (sanitizedTarget.StartsWith("/", StringComparison.Ordinal))
            {
                return sanitizedTarget.TrimStart('/');
            }

            var segments = new List<string>((basePartPath ?? string.Empty).Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));

            if (segments.Count > 0)
            {
                segments.RemoveAt(segments.Count - 1);
            }

            foreach (var part in sanitizedTarget.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part == ".")
                {
                    continue;
                }

                if (part == "..")
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                }

                segments.Add(part);
            }

            return string.Join("/", segments);
        }

        private static void EnsureCollections(CrmDataStore data)
        {
            data.Trips = data.Trips ?? new List<TripRecord>();
            data.Drivers = data.Drivers ?? new List<DriverRecord>();
            data.FleetVehicles = data.FleetVehicles ?? new List<FleetVehicleRecord>();
            data.Clients = data.Clients ?? new List<ClientRecord>();
            data.Cars = data.Cars ?? new List<CarRecord>();
        }

        private static void SkipRow(CrmExcelImportResult result, CrmExcelImportSheetResult sheetResult, int rowNumber, string reason)
        {
            result.SkippedRows++;
            if (sheetResult != null)
            {
                sheetResult.SkippedRows++;
                sheetResult.RowWarnings.Add(string.Format(CultureInfo.CurrentCulture, "Строка {0}: {1}.", rowNumber <= 0 ? 0 : rowNumber, reason));
            }

            result.AddWarning(string.Format(CultureInfo.CurrentCulture, "Лист {0}, строка {1}: {2}.", sheetResult == null ? string.Empty : sheetResult.SheetName, rowNumber <= 0 ? 0 : rowNumber, reason));
        }

        private static bool IsRowEmpty(params string[] values)
        {
            return values == null || values.All(string.IsNullOrWhiteSpace);
        }

        private static string NormalizeToken(string value)
        {
            return new string((value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static string GetAttributeValue(XElement element, XName attributeName)
        {
            if (element == null || attributeName == null)
            {
                return string.Empty;
            }

            var attribute = element.Attribute(attributeName);
            return attribute == null ? string.Empty : attribute.Value;
        }

        private static void WriteXmlEntry(ZipArchive archive, string entryPath, XDocument document)
        {
            var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);

            using (var stream = entry.Open())
            {
                document.Save(stream);
            }
        }

        private static XDocument BuildContentTypesDocument(int worksheetCount)
        {
            var namespaceValue = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
            var types = new XElement(namespaceValue + "Types",
                new XElement(namespaceValue + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(namespaceValue + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(namespaceValue + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")));

            for (var index = 0; index < worksheetCount; index++)
            {
                types.Add(new XElement(namespaceValue + "Override",
                    new XAttribute("PartName", string.Format(CultureInfo.InvariantCulture, "/xl/worksheets/sheet{0}.xml", index + 1)),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), types);
        }

        private static XDocument BuildPackageRelationshipsDocument()
        {
            var namespaceValue = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(namespaceValue + "Relationships",
                    new XElement(namespaceValue + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml"))));
        }

        private static XDocument BuildWorkbookDocument(IEnumerable<TemplateSheetDefinition> sheets)
        {
            var workbookNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var relationshipNamespace = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            var workbook = new XElement(workbookNamespace + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", relationshipNamespace));
            var sheetContainer = new XElement(workbookNamespace + "sheets");
            workbook.Add(sheetContainer);

            var sheetList = sheets == null ? new List<TemplateSheetDefinition>() : sheets.ToList();

            for (var index = 0; index < sheetList.Count; index++)
            {
                sheetContainer.Add(new XElement(workbookNamespace + "sheet",
                    new XAttribute("name", sheetList[index].Name),
                    new XAttribute("sheetId", index + 1),
                    new XAttribute(relationshipNamespace + "id", "rId" + (index + 1).ToString(CultureInfo.InvariantCulture))));
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), workbook);
        }

        private static XDocument BuildWorkbookRelationshipsDocument(int worksheetCount)
        {
            var namespaceValue = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            var relationships = new XElement(namespaceValue + "Relationships");

            for (var index = 0; index < worksheetCount; index++)
            {
                relationships.Add(new XElement(namespaceValue + "Relationship",
                    new XAttribute("Id", "rId" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", string.Format(CultureInfo.InvariantCulture, "worksheets/sheet{0}.xml", index + 1))));
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), relationships);
        }

        private static XDocument BuildWorksheetDocument(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            var worksheetNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var sheetData = new XElement(worksheetNamespace + "sheetData");
            var headerRow = new XElement(worksheetNamespace + "row", new XAttribute("r", 1));
            var headerList = headers == null ? new List<string>() : headers.ToList();

            for (var index = 0; index < headerList.Count; index++)
            {
                var cellReference = GetExcelColumnName(index + 1) + "1";
                headerRow.Add(new XElement(worksheetNamespace + "c",
                    new XAttribute("r", cellReference),
                    new XAttribute("t", "inlineStr"),
                    new XElement(worksheetNamespace + "is",
                        new XElement(worksheetNamespace + "t", headerList[index]))));
            }

            sheetData.Add(headerRow);

            var dataRows = rows == null ? new List<IEnumerable<string>>() : rows.ToList();

            for (var rowIndex = 0; rowIndex < dataRows.Count; rowIndex++)
            {
                var currentRowNumber = rowIndex + 2;
                var rowElement = new XElement(worksheetNamespace + "row", new XAttribute("r", currentRowNumber));
                var rowValues = dataRows[rowIndex] == null ? new List<string>() : dataRows[rowIndex].ToList();

                for (var columnIndex = 0; columnIndex < rowValues.Count; columnIndex++)
                {
                    if (string.IsNullOrWhiteSpace(rowValues[columnIndex]))
                    {
                        continue;
                    }

                    var cellReference = GetExcelColumnName(columnIndex + 1) + currentRowNumber.ToString(CultureInfo.InvariantCulture);
                    rowElement.Add(new XElement(worksheetNamespace + "c",
                        new XAttribute("r", cellReference),
                        new XAttribute("t", "inlineStr"),
                        new XElement(worksheetNamespace + "is",
                            new XElement(worksheetNamespace + "t", rowValues[columnIndex]))));
                }

                if (rowElement.HasElements)
                {
                    sheetData.Add(rowElement);
                }
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(worksheetNamespace + "worksheet", sheetData));
        }

        private static string GetExcelColumnName(int columnIndex)
        {
            if (columnIndex <= 0)
            {
                return "A";
            }

            var columnName = string.Empty;
            var currentIndex = columnIndex;

            while (currentIndex > 0)
            {
                currentIndex--;
                columnName = (char)('A' + (currentIndex % 26)) + columnName;
                currentIndex /= 26;
            }

            return columnName;
        }

        private static bool AssignStringIfProvided(string currentValue, string importedValue, Action<string> assign)
        {
            var normalizedValue = NormalizeText(importedValue);

            if (string.IsNullOrWhiteSpace(normalizedValue) || StringEquals(currentValue, normalizedValue))
            {
                return false;
            }

            assign(normalizedValue);
            return true;
        }

        private static bool AssignStringIfDifferent(string currentValue, string importedValue, Action<string> assign)
        {
            var normalizedValue = NormalizeText(importedValue);

            if (StringEquals(currentValue, normalizedValue))
            {
                return false;
            }

            assign(normalizedValue);
            return true;
        }

        private static bool AssignDateTextIfProvided(string currentValue, string importedValue, Action<string> assign)
        {
            var normalizedValue = NormalizeDateText(importedValue);

            if (string.IsNullOrWhiteSpace(normalizedValue) || StringEquals(currentValue, normalizedValue))
            {
                return false;
            }

            assign(normalizedValue);
            return true;
        }

        private static bool AssignDateTimeIfProvided(DateTime currentValue, string importedValue, Action<DateTime> assign)
        {
            DateTime parsedValue;

            if (!TryParseFlexibleDateTime(importedValue, out parsedValue))
            {
                return false;
            }

            var utcValue = ToUtc(parsedValue);

            if (currentValue == utcValue)
            {
                return false;
            }

            assign(utcValue);
            return true;
        }

        private static DateTime ResolveCreatedAt(string importedValue, DateTime fallbackValue)
        {
            DateTime parsedValue;
            return TryParseFlexibleDateTime(importedValue, out parsedValue) ? ToUtc(parsedValue) : fallbackValue;
        }

        private static string NormalizeText(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeDateText(string value)
        {
            DateTime parsedValue;
            return TryParseFlexibleDateTime(value, out parsedValue)
                ? parsedValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : NormalizeText(value);
        }

        private static bool TryParseFlexibleDateTime(string value, out DateTime parsedValue)
        {
            var normalizedValue = NormalizeText(value);

            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                parsedValue = DateTime.MinValue;
                return false;
            }

            if (DateTime.TryParseExact(normalizedValue, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out parsedValue))
            {
                return true;
            }

            if (DateTime.TryParse(normalizedValue, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsedValue))
            {
                return true;
            }

            return DateTime.TryParse(normalizedValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out parsedValue);
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Local);
            }

            return value.ToUniversalTime();
        }

        private static ClientRecord FindClient(CrmDataStore data, string sourceInquiryId, string email, string phone, string name)
        {
            if (!string.IsNullOrWhiteSpace(sourceInquiryId))
            {
                var byInquiry = data.Clients.FirstOrDefault(item => StringEquals(item.SourceInquiryId, sourceInquiryId));

                if (byInquiry != null)
                {
                    return byInquiry;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = data.Clients.FirstOrDefault(item => StringEquals(item.Email, email));

                if (byEmail != null)
                {
                    return byEmail;
                }
            }

            if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(name))
            {
                var byPhoneAndName = data.Clients.FirstOrDefault(item => StringEquals(item.PhoneNumber, phone) && StringEquals(item.Name, name));

                if (byPhoneAndName != null)
                {
                    return byPhoneAndName;
                }
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var byPhone = data.Clients.FirstOrDefault(item => StringEquals(item.PhoneNumber, phone));

                if (byPhone != null)
                {
                    return byPhone;
                }
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return data.Clients.FirstOrDefault(item => StringEquals(item.Name, name));
            }

            return null;
        }

        private static DriverRecord FindDriver(CrmDataStore data, string fullName, string phone)
        {
            if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(fullName))
            {
                var byPhoneAndName = data.Drivers.FirstOrDefault(item => StringEquals(item.PhoneNumber, phone) && StringEquals(item.FullName, fullName));

                if (byPhoneAndName != null)
                {
                    return byPhoneAndName;
                }
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var byPhone = data.Drivers.FirstOrDefault(item => StringEquals(item.PhoneNumber, phone));

                if (byPhone != null)
                {
                    return byPhone;
                }
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return data.Drivers.FirstOrDefault(item => StringEquals(item.FullName, fullName));
            }

            return null;
        }

        private static FleetVehicleRecord FindVehicle(CrmDataStore data, string vinCode, string licensePlate, string brand, string model, string vehicleName)
        {
            if (!string.IsNullOrWhiteSpace(vinCode))
            {
                var byVin = data.FleetVehicles.FirstOrDefault(item => StringEquals(item.VinCode, vinCode));

                if (byVin != null)
                {
                    return byVin;
                }
            }

            if (!string.IsNullOrWhiteSpace(licensePlate))
            {
                var byPlate = data.FleetVehicles.FirstOrDefault(item => StringEquals(item.LicensePlate, licensePlate));

                if (byPlate != null)
                {
                    return byPlate;
                }
            }

            if (!string.IsNullOrWhiteSpace(brand) || !string.IsNullOrWhiteSpace(model) || !string.IsNullOrWhiteSpace(licensePlate))
            {
                var byComposite = data.FleetVehicles.FirstOrDefault(item => StringEquals(item.CarBrand, brand) && StringEquals(item.CarModel, model) && StringEquals(item.LicensePlate, licensePlate));

                if (byComposite != null)
                {
                    return byComposite;
                }
            }

            if (!string.IsNullOrWhiteSpace(vehicleName))
            {
                return data.FleetVehicles.FirstOrDefault(item => StringEquals(BuildVehicleName(item), vehicleName));
            }

            return null;
        }

        private static CarRecord FindCar(CrmDataStore data, string sourceInquiryId, string vin, string tripNumber, string brand, string model)
        {
            if (!string.IsNullOrWhiteSpace(sourceInquiryId))
            {
                var byInquiry = data.Cars.FirstOrDefault(item => StringEquals(item.SourceInquiryId, sourceInquiryId));

                if (byInquiry != null)
                {
                    return byInquiry;
                }
            }

            if (!string.IsNullOrWhiteSpace(vin))
            {
                var byVin = data.Cars.FirstOrDefault(item => StringEquals(item.Vin, vin));

                if (byVin != null)
                {
                    return byVin;
                }
            }

            if (!string.IsNullOrWhiteSpace(tripNumber) || !string.IsNullOrWhiteSpace(brand) || !string.IsNullOrWhiteSpace(model))
            {
                return data.Cars.FirstOrDefault(item => StringEquals(item.TripNumber, tripNumber) && StringEquals(item.Brand, brand) && StringEquals(item.Model, model));
            }

            return null;
        }

        private static string ResolveTripVehicleName(FleetVehicleRecord vehicle, string importedVehicleName)
        {
            if (vehicle != null)
            {
                return BuildVehicleName(vehicle);
            }

            return NormalizeText(importedVehicleName);
        }

        private static List<string> SplitListValue(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { '\r', '\n', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> ResolveDriverIdsByNames(IEnumerable<DriverRecord> drivers, IEnumerable<string> driverNames)
        {
            var driverList = drivers == null ? new List<DriverRecord>() : drivers.ToList();
            return (driverNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => driverList.FirstOrDefault(driver => StringEquals(driver.FullName, name)))
                .Where(driver => driver != null && !string.IsNullOrWhiteSpace(driver.Id))
                .Select(driver => driver.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool SequenceEquals(IEnumerable<string> left, IEnumerable<string> right)
        {
            var leftValues = (left ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();
            var rightValues = (right ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();

            if (leftValues.Count != rightValues.Count)
            {
                return false;
            }

            for (var index = 0; index < leftValues.Count; index++)
            {
                if (!StringEquals(leftValues[index], rightValues[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void SyncClientReferences(CrmDataStore data, ClientRecord client, string previousName)
        {
            foreach (var trip in data.Trips.Where(item => StringEquals(item.ClientId, client.Id) || (string.IsNullOrWhiteSpace(item.ClientId) && StringEquals(item.ClientName, previousName))))
            {
                trip.ClientId = client.Id;
                trip.ClientName = client.Name;
            }

            foreach (var car in data.Cars.Where(item => StringEquals(item.ClientId, client.Id) || (string.IsNullOrWhiteSpace(item.ClientId) && StringEquals(item.ClientName, previousName))))
            {
                car.ClientId = client.Id;
                car.ClientName = client.Name;
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

        private static void RepairTripReferences(CrmDataStore data)
        {
            foreach (var trip in data.Trips)
            {
                var client = FindClient(data, string.Empty, string.Empty, string.Empty, trip.ClientName);
                var driver = FindDriver(data, trip.DriverName, string.Empty);
                var vehicle = FindVehicle(data, string.Empty, string.Empty, string.Empty, string.Empty, trip.VehicleName);

                if (client != null)
                {
                    trip.ClientId = client.Id;

                    if (string.IsNullOrWhiteSpace(trip.ClientName))
                    {
                        trip.ClientName = client.Name;
                    }
                }

                if (driver != null)
                {
                    trip.DriverId = driver.Id;

                    if (string.IsNullOrWhiteSpace(trip.DriverName))
                    {
                        trip.DriverName = driver.FullName;
                    }
                }

                if (vehicle != null)
                {
                    trip.VehicleId = vehicle.Id;

                    if (string.IsNullOrWhiteSpace(trip.VehicleName) || !StringEquals(trip.VehicleName, BuildVehicleName(vehicle)))
                    {
                        trip.VehicleName = BuildVehicleName(vehicle);
                    }
                }
            }
        }

        private static void RepairCarClientLinks(CrmDataStore data)
        {
            foreach (var car in data.Cars)
            {
                var client = FindClient(data, string.Empty, string.Empty, string.Empty, car.ClientName);

                if (client != null)
                {
                    car.ClientId = client.Id;

                    if (string.IsNullOrWhiteSpace(car.ClientName))
                    {
                        car.ClientName = client.Name;
                    }
                }
            }
        }

        private static string BuildVehicleName(FleetVehicleRecord vehicle)
        {
            if (vehicle == null)
            {
                return string.Empty;
            }

            return string.Format("{0} {1} ({2})", vehicle.CarBrand, vehicle.CarModel, vehicle.LicensePlate).Trim();
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(NormalizeText(left), NormalizeText(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}