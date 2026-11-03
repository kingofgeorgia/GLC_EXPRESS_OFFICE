using System.Collections.Generic;
using GLC_EXPRESS.Models;
namespace GLC_EXPRESS.Services {
 // Public illustrative data only. Replace this provider with a consented, authenticated
 // tracking API when available; never bind private CRM vehicle records to this preview.
 public static class HomeTrackingPreviewService {
  public static HomeTrackingPreviewRecord GetPreview() {
   return new HomeTrackingPreviewRecord {
    Vehicle = "2024 Tesla Model Y", Auction = "Copart", Lot = "44189717",
    ImageUrl = "~/Content/images/tesla-model-y.png",
    Steps = new List<HomeTrackingPreviewStep> {
     new HomeTrackingPreviewStep { TitleRu="Покупка на аукционе", TitleEn="Auction purchase", TitleGe="აუქციონზე შეძენა", DetailRu="Savannah, USA", DetailEn="Savannah, USA", DetailGe="Savannah, USA", DateLabel="20 Oct", Icon="check", State="is-complete" },
     new HomeTrackingPreviewStep { TitleRu="Отправка контейнером", TitleEn="Container shipping", TitleGe="კონტეინერით გაგზავნა", DetailRu="MSC · MEDU1234567", DetailEn="MSC · MEDU1234567", DetailGe="MSC · MEDU1234567", DateLabel="22 Oct", Icon="ship", State="is-current" },
     new HomeTrackingPreviewStep { TitleRu="Прибытие в Поти", TitleEn="Arrival in Poti", TitleGe="ფოთში ჩამოსვლა", DetailRu="ETA · 12 Nov", DetailEn="ETA · 12 Nov", DetailGe="ETA · 12 Nov", DateLabel="", Icon="ship", State="" },
     new HomeTrackingPreviewStep { TitleRu="Техцентр GLC Express", TitleEn="GLC Express workshop", TitleGe="GLC Express სერვისი", DetailRu="Диагностика и ремонт", DetailEn="Inspection and repair", DetailGe="დიაგნოსტიკა და შეკეთება", DateLabel="", Icon="repair", State="" },
     new HomeTrackingPreviewStep { TitleRu="Отправка автовозом", TitleEn="Final delivery", TitleGe="ავტოვოზით გაგზავნა", DetailRu="В страну назначения", DetailEn="To your destination", DetailGe="დანიშნულების ქვეყანაში", DateLabel="", Icon="truck", State="" }
    }
   };
  }
 }
}
