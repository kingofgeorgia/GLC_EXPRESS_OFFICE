using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Operations : Page
    {
        protected CrmDataStore Data = new CrmDataStore();
        protected CrmSettingsRecord Settings;
        protected TripRecord SelectedTrip;
        protected string ErrorMessage, Notice, Token, UpdatedAt;
        protected string Section = "dashboard";
        private string savedTripId;
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = Session.SessionID;
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.IsAuthenticated) { Response.Redirect(FormsAuthentication.LoginUrl + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false); Context.ApplicationInstance.CompleteRequest(); return; }
            Settings = CrmSettingsService.GetCurrent();
            if (!Settings.AllowedCrmRoles.Any(Context.User.IsInRole)) { Response.Redirect("~/AccessDenied", false); Context.ApplicationInstance.CompleteRequest(); return; }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Token = Session["OperationsCsrf"] as string;
            if (Token == null) { Token = Guid.NewGuid().ToString("N"); Session["OperationsCsrf"] = Token; }
            var section = Request.QueryString["section"];
            if (new[] { "dashboard", "trips", "cars", "finance", "clients", "carriers" }.Contains(section)) Section = section;
            if (Request.HttpMethod == "POST")
            {
                if (!string.Equals(Token, Request.Form["opsToken"], StringComparison.Ordinal)) throw new HttpException(403, "Недействительный токен формы.");
                try
                {
                    var action = Value("opsAction");
                    CrmRepository.Update(data => ApplyCommand(data, action));
                    Session["OperationsNotice"] = "Изменения сохранены в CRM.";
                    Response.Redirect(Url(action == "createTrip" ? "dashboard" : Section, savedTripId ?? Value("tripId")), false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                catch (ArgumentException ex) { ErrorMessage = ex.Message; }
            }
            Notice = Session["OperationsNotice"] as string;
            Session.Remove("OperationsNotice");
            Data = CrmRepository.Load();
            SelectedTrip = Data.Trips.FirstOrDefault(t => t.Id == Request.QueryString["trip"])
                ?? Data.Trips.OrderByDescending(t => t.CreatedAtUtc).FirstOrDefault();
            UpdatedAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Georgian Standard Time").ToString("dd.MM.yyyy HH:mm");
        }
        private string Value(string name) { return (Request.Form[name] ?? "").Trim(); }
        private string Required(string name, string label) { var value = Value(name); if (value.Length == 0 || value.Length > 200) throw new ArgumentException("Заполните поле «" + label + "» (до 200 символов)."); return value; }
        private void ApplyCommand(CrmDataStore data, string action)
        {
            if (action == "createTrip")
            {
                var number = Required("number", "Номер рейса");
                // Cars reference trips by number, so ambiguous numbers cannot be created here.
                if (data.Trips.Any(t => Same(t.Number, number))) throw new ArgumentException("Номер рейса уже используется.");
                var client = data.Clients.FirstOrDefault(c => c.Id == Value("clientId"));
                var driver = data.Drivers.FirstOrDefault(d => d.Id == Value("driverId"));
                var vehicle = data.FleetVehicles.FirstOrDefault(v => v.Id == Value("vehicleId"));
                if (client == null || driver == null || vehicle == null) throw new ArgumentException("Выберите существующего клиента, водителя и транспорт.");
                DateTime start, end;
                if (!DateTime.TryParseExact(Value("start"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start) || (Value("end").Length > 0 && (!DateTime.TryParseExact(Value("end"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end) || end < start))) throw new ArgumentException("Укажите корректные даты: окончание не раньше начала.");
                var freight = Amount(Value("freight")); var advance = Amount(Value("advance"));
                if (!freight.HasValue || !advance.HasValue || (Settings.ValidatePrepaymentAgainstFreight && advance > freight)) throw new ArgumentException("Проверьте фрахт и предоплату: нужны неотрицательные суммы; предоплата не должна превышать фрахт.");
                var trip = new TripRecord { Number = number, Country = Required("country", "Направление"), ClientId = client.Id, ClientName = client.Name, DriverId = driver.Id, DriverName = driver.FullName, VehicleId = vehicle.Id, VehicleName = vehicle.CarBrand + " " + vehicle.CarModel + " / " + vehicle.LicensePlate, StartDate = Value("start"), EndDate = Value("end"), Freight = freight.Value.ToString(CultureInfo.InvariantCulture), Prepayment = advance.Value.ToString(CultureInfo.InvariantCulture), Status = Settings.DefaultTripStatus };
                data.Trips.Add(trip);
                savedTripId = trip.Id;
                return;
            }
            var current = data.Trips.FirstOrDefault(t => t.Id == Value("tripId"));
            if (current == null) throw new ArgumentException("Рейс не найден. Обновите страницу.");
            if (action == "assignCar")
            {
                if (string.IsNullOrWhiteSpace(current.Number) || data.Trips.Count(t => Same(t.Number, current.Number)) != 1) throw new ArgumentException("Номер рейса отсутствует или повторяется. Исправьте его в CRM перед назначением.");
                var car = data.Cars.FirstOrDefault(c => c.Id == Value("carId"));
                if (car == null || !string.IsNullOrWhiteSpace(car.TripNumber)) throw new ArgumentException("Автомобиль уже назначен или удалён. Обновите страницу.");
                car.ChangeHistory.Add(new CarChangeLogRecord { FieldName = "Рейс", PreviousValue = car.TripNumber, NewValue = current.Number, ChangedBy = Context.User.Identity.Name, ChangedAtUtc = DateTime.UtcNow });
                car.TripNumber = current.Number;
            }
            else if (action == "resources")
            {
                var driver = data.Drivers.FirstOrDefault(d => d.Id == Value("resourceDriver"));
                var vehicle = data.FleetVehicles.FirstOrDefault(v => v.Id == Value("resourceVehicle"));
                if (driver == null || vehicle == null) throw new ArgumentException("Выберите водителя и транспорт из CRM.");
                current.DriverId = driver.Id; current.DriverName = driver.FullName;
                current.VehicleId = vehicle.Id; current.VehicleName = vehicle.CarBrand + " " + vehicle.CarModel + " / " + vehicle.LicensePlate;
            }
            else if (action == "status")
            {
                var status = Value("status");
                if (!Settings.TripStatuses.Contains(status)) throw new ArgumentException("Недопустимый статус рейса.");
                current.Status = status;
            }
            else throw new ArgumentException("Неизвестное действие.");
        }
        protected static bool Same(string a, string b) { return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase); }
        protected bool Active(TripRecord trip) { return !new[] { "Окончен", "Завершен", "Завершён", "Отменено", "Закрыто" }.Any(s => Same(s, trip.Status)); }
        protected List<CarRecord> Cars(TripRecord trip) { return trip == null || string.IsNullOrWhiteSpace(trip.Number) || Data.Trips.Count(t => Same(t.Number, trip.Number)) != 1 ? new List<CarRecord>() : Data.Cars.Where(c => Same(c.TripNumber, trip.Number)).ToList(); }
        protected static decimal? Amount(string value)
        {
            decimal amount;
            return decimal.TryParse((value ?? "").Replace(" ", "").Replace(",", "."), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount) && amount >= 0 ? (decimal?)amount : null;
        }
        protected string Money(string value) { var amount = Amount(value); return amount.HasValue ? amount.Value.ToString("N2") : "Не указано"; }
        protected string Expenses(TripRecord trip)
        {
            var cars = Cars(trip);
            var values = cars.SelectMany(c => new[] { c.PortCost, c.LoadingCost, c.TowTruckCost, c.ParkingCost, c.InspectionCost, c.ReExportCost, c.ExpertiseCost, c.DeliveryCost }).ToList();
            if (values.Count == 0 || values.Any(v => !Amount(v).HasValue)) return "Данные неполные";
            return values.Sum(v => Amount(v).Value).ToString("N2");
        }
        protected string Url(string section, string tripId) { return ResolveUrl("~/Operations?section=" + HttpUtility.UrlEncode(section) + "&trip=" + HttpUtility.UrlEncode(tripId ?? "")); }
        protected string CrmUrl(string tab) { return ResolveUrl("~/orders?tab=" + tab); }
        protected string CarUrl(CarRecord car) { return CrmUrl("cars") + "&carId=" + HttpUtility.UrlEncode(car.Id); }
        protected string AssetUrl(string path) { return ResolveUrl(path) + "?v=" + System.IO.File.GetLastWriteTimeUtc(Server.MapPath(path)).Ticks.ToString(CultureInfo.InvariantCulture); }
        protected string CarName(CarRecord car) { var name = (car.Brand + " " + car.Model).Trim(); return name.Length > 0 ? name : "Автомобиль без названия"; }
        protected string DateLabel(string value) { DateTimeOffset date; return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date.ToString("dd.MM.yyyy") : "Дата не указана"; }
    }
}

