using System.Web.UI;
using GLC_EXPRESS.Services;
namespace GLC_EXPRESS {
 public class HomeSection : UserControl {
  protected GLC_EXPRESS.Models.HomeTrackingPreviewRecord Tracking = HomeTrackingPreviewService.GetPreview();
  protected string Icon(string name) { return "<svg aria-hidden=\"true\" class=\"glc-icon\"><use href=\"" + ResolveUrl("~/Content/images/home-icons.svg") + "#" + name + "\"></use></svg>"; }
  protected string T(string key) { return PublicSiteLocalizationService.GetText(key); }
  protected string L(string ru, string en, string ge) {
   var lang = PublicSiteLocalizationService.GetCurrentLanguageCode();
   return lang == "en" ? en : lang == "ge" ? ge : ru;
  }
 }
}


