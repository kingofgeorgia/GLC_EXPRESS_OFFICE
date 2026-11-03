using System;
using System.Collections.Generic;
using System.Linq;
namespace GLC_EXPRESS.Services
{
    public sealed class HomePartnerCard
    {
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public bool HasLogo { get { return !string.IsNullOrWhiteSpace(LogoUrl); } }
        public string Monogram { get { return string.IsNullOrEmpty(Name) ? string.Empty : Name.Substring(0, 1); } }
    }
    public static class PartnerLogoService
    {
        private static readonly Dictionary<string, string> Logos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Copart", "copart.svg" }, { "IAAI", "iaai.png" }, { "IAA", "iaai.png" },
            { "Manheim", "manheim.svg" }, { "AUTOPAPA", "autopapa.png" },
            { "Port of Poti", "poti.png" }, { "ფოთის პორტი", "poti.png" },
            { "Port of Batumi", "batumi.svg" }, { "ბათუმის პორტი", "batumi.svg" },
            { "MSC", "msc.jpg" }, { "Maersk", "maersk.png" }, { "CMA CGM", "cma-cgm.png" }
        };
        public static IEnumerable<HomePartnerCard> GetCards(IEnumerable<string> names)
        {
            return (names ?? Enumerable.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)).Select(name =>
            {
                string file;
                return new HomePartnerCard { Name = name.Trim(), LogoUrl = Logos.TryGetValue(name.Trim(), out file) ? "~/Content/images/partners/" + file : string.Empty };
            }).ToList();
        }
    }
}
