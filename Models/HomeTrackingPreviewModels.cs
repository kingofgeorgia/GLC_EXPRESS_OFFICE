using System.Collections.Generic;
namespace GLC_EXPRESS.Models {
 public class HomeTrackingPreviewRecord {
  public string Vehicle { get; set; }
  public string Auction { get; set; }
  public string Lot { get; set; }
  public string ImageUrl { get; set; }
  public IList<HomeTrackingPreviewStep> Steps { get; set; }
 }
 public class HomeTrackingPreviewStep {
  public string TitleRu { get; set; }
  public string TitleEn { get; set; }
  public string TitleGe { get; set; }
  public string DetailRu { get; set; }
  public string DetailEn { get; set; }
  public string DetailGe { get; set; }
  public string DateLabel { get; set; }
  public string Icon { get; set; }
  public string State { get; set; }
 }
}
