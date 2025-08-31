using System.Xml;
using System.Xml.Linq;
using UblTr.Core;

namespace UblTr.Rules.Rules;

public sealed class Vat002Rule : IBusinessRule
{
    public string Id => "VAT-002";
    public string Title => "KDV %0 ise istisna kodu (TaxExemptionReasonCode) zorunludur";

    public IEnumerable<RuleViolation> Evaluate(XDocument doc, RuleContext ctx)
    {
        XNamespace cac = UblNamespaces.Cac;
        XNamespace cbc = UblNamespaces.Cbc;

        foreach (var line in doc.Root!.Descendants(cac + "InvoiceLine"))
        {
            var id = (string?)line.Element(cbc + "ID") ?? "?";
            var percentStr = line
                .Descendants(cac + "TaxCategory")
                .Elements(cbc + "Percent")
                .Select(x => (string?)x).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(percentStr)) continue;
            if (!decimal.TryParse(percentStr, System.Globalization.NumberStyles.Number,
                                  System.Globalization.CultureInfo.InvariantCulture, out var pct)) continue;

            if (pct == 0)
            {
                var hasExemption = line
                    .Descendants(cac + "TaxCategory")
                    .Elements(cbc + "TaxExemptionReasonCode")
                    .Any();
                if (!hasExemption)
                {
                    var li = (IXmlLineInfo?)line;
                    yield return new RuleViolation {
                        Id = Id, Severity = Severity.Error,
                        Message = $"Satır {id}: KDV %0 iken TaxExemptionReasonCode eksik",
                        Line = li?.LineNumber ?? 0, Column = li?.LinePosition ?? 0
                    };
                }
            }
        }
    }
}