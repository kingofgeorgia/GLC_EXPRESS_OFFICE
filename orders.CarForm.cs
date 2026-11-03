using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;
namespace GLC_EXPRESS {
 public partial class orders {
  private void CarOptions(DropDownList control, IEnumerable<string> values, string selected) {
   control.Items.Clear(); control.Items.Add(new ListItem("Не выбран", ""));
   foreach(var value in values.Where(v=>!string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase)) control.Items.Add(new ListItem(value,value));
   if(!string.IsNullOrEmpty(selected) && control.Items.FindByValue(selected)==null) control.Items.Add(new ListItem(selected,selected));
   SetSelectedValue(control,selected ?? "");
  }
  private void BindCarEditorLists(CrmDataStore data, CarRecord car) {
   CarOptions(CarsDetailForwarderDropDownList,CarForwarderDirectoryService.GetForwarderNames().Concat(data.Cars.Select(c=>c.Forwarder)),car == null ? GetPostedListControlValue(CarsDetailForwarderDropDownList) : car.Forwarder);
   var newItem=CarsDetailForwarderDropDownList.Items.FindByValue("__new__"); if(newItem!=null) CarsDetailForwarderDropDownList.Items.Remove(newItem);
   CarsDetailForwarderDropDownList.Items.Add(new ListItem("Новый…","__new__"));
   if(car==null && GetPostedListControlValue(CarsDetailForwarderDropDownList)=="__new__") CarsDetailForwarderDropDownList.SelectedValue="__new__";
   var clientId=car==null ? GetPostedListControlValue(CarsDetailClientDropDownList) : car.ClientId;
   CarsDetailClientDropDownList.Items.Clear();CarsDetailClientDropDownList.Items.Add(new ListItem("Не выбран",""));
   foreach(var client in data.Clients.OrderBy(c=>c.Name)) CarsDetailClientDropDownList.Items.Add(new ListItem(client.Name,client.Id));
   if(!string.IsNullOrEmpty(clientId) && CarsDetailClientDropDownList.Items.FindByValue(clientId)==null) CarsDetailClientDropDownList.Items.Add(new ListItem(car == null ? "Клиент из архива" : car.ClientName,clientId));
   SetSelectedValue(CarsDetailClientDropDownList,clientId ?? "");
   CarOptions(CarsDetailStatusDropDownList,new[]{"Ожидает покупки","Куплен","Ожидает отправку","В пути","На таможне","В ремонте","Готов к выдаче","Выдан"}.Concat(data.Cars.Select(c=>c.Status)),car==null ? GetPostedListControlValue(CarsDetailStatusDropDownList):car.Status);
   CarOptions(CarsDetailYearDropDownList,Enumerable.Range(1886,DateTime.Today.Year+2-1886).Reverse().Select(y=>y.ToString()),car==null ? GetPostedListControlValue(CarsDetailYearDropDownList):car.Year);
   var models=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase) {
    {"Toyota",new List<string>{"Camry","Corolla","RAV4","Highlander","Land Cruiser","Tacoma","Tundra"}},
    {"BMW",new List<string>{"3 Series","5 Series","X3","X5","X7"}},
    {"Mercedes-Benz",new List<string>{"C-Class","E-Class","S-Class","GLC","GLE","GLS","G-Class"}},
    {"Ford",new List<string>{"F-150","Mustang","Explorer","Escape","Transit"}},
    {"Tesla",new List<string>{"Model 3","Model Y","Model S","Model X"}},
    {"Honda",new List<string>{"Accord","Civic","CR-V","Pilot"}},
    {"Hyundai",new List<string>{"Elantra","Sonata","Tucson","Santa Fe","Palisade"}},
    {"Kia",new List<string>{"K5","Sportage","Sorento","Telluride"}},
    {"Lexus",new List<string>{"ES","IS","RX","NX","GX","LX"}},
    {"Audi",new List<string>{"A3","A4","A6","Q3","Q5","Q7","Q8"}},
    {"Volkswagen",new List<string>{"Golf","Passat","Jetta","Tiguan","Touareg","Atlas"}},
    {"Nissan",new List<string>{"Altima","Sentra","Rogue","Pathfinder","Patrol"}},
    {"Chevrolet",new List<string>{"Malibu","Camaro","Equinox","Tahoe","Silverado"}},
    {"BYD",new List<string>{"Song Plus","Han","Tang","Seal"}},
    {"Geely",new List<string>{"Monjaro","Coolray","Atlas"}},
    {"Li Auto",new List<string>{"L6","L7","L8","L9"}}
   };
   foreach(var item in data.Cars.Where(c=>!string.IsNullOrWhiteSpace(c.Brand))) {List<string> list;if(!models.TryGetValue(item.Brand,out list)){list=new List<string>();models[item.Brand]=list;} if(!string.IsNullOrWhiteSpace(item.Model)&&!list.Contains(item.Model))list.Add(item.Model);}
   CarBrandsLiteral.Text=BuildDatalistOptionsHtml(models.Keys.OrderBy(v=>v));
   CarModelsLiteral.Text=Newtonsoft.Json.JsonConvert.SerializeObject(models).Replace("<","\\u003c");
  }
  private static bool CarFormFlagChecked(string value) {return !string.IsNullOrWhiteSpace(value) && !new[]{"нет","no","false","0","не получен","отсутствует"}.Contains(value.Trim().ToLowerInvariant());}
  private static string CarFormFlag(string previous,bool value) {return !string.IsNullOrWhiteSpace(previous) && CarFormFlagChecked(previous)==value ? previous : value ? "Да":"Нет";}
  private static string CarNumberText(string value) { return (value ?? "").Trim().Replace(',', '.'); }
  private bool ValidateCarEditor() {
   string error=null;
   if(string.IsNullOrWhiteSpace(CarsDetailVinTextBox.Text))error="VIN обязателен.";
   else if(string.IsNullOrWhiteSpace(CarsDetailLocationTextBox.Text))error="Локация обязательна.";
   else if(CarsDetailForwarderDropDownList.SelectedValue=="__new__" && string.IsNullOrWhiteSpace(CarsNewForwarderTextBox.Text))error="Введите название нового форвардера.";
   var fields=new[]{CarsDetailStartPriceTextBox,CarsDetailVolumeTextBox,CarsDetailPowerTextBox,CarsDetailPortCostTextBox,CarsDetailLoadingCostTextBox,CarsDetailTowTruckCostTextBox,CarsDetailParkingCostTextBox,CarsDetailInspectionCostTextBox,CarsDetailReExportCostTextBox,CarsDetailExpertiseCostTextBox,CarsDetailDeliveryCostTextBox};
   foreach(var field in fields) {decimal number;if(string.IsNullOrWhiteSpace(field.Text))continue;if(!decimal.TryParse(field.Text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out number) || number<0 || (field==CarsDetailPowerTextBox && number!=decimal.Truncate(number))) {error="Цена, объём, мощность и траты должны быть неотрицательными числами; мощность — целым числом.";break;}}
   if(error==null)return true;ShowMessage(error,"warning");return false;
  }
 }
}
