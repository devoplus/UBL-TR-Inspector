using System.Xml;
using System.Xml.Linq;
using UblTr.Core;

namespace UblTr.Rules.Rules;

public sealed class Sum001Rule : IBusinessRule
{
    public string Id => "SUM-001";
    public string Title => "Satır toplamları LegalMonetaryTotal ile eşit olmalı";

    public IEnumerable<RuleViolation> Evaluate(XDocument doc, RuleContext ctx)
    {
        XNamespace inv = UblNamespaces.Inv;
        XNamespace cac = UblNamespaces.Cac;
        XNamespace cbc = UblNamespaces.Cbc;

        decimal SumLines() => doc.Root!
            .Descendants(cac + "InvoiceLine")
            .Select(x => (string?)x.Element(cbc + "LineExtensionAmount"))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => decimal.Parse(s!, System.Globalization.CultureInfo.InvariantCulture))
            .Sum();

        var headerStr = doc.Root!
            .Descendants(cac + "LegalMonetaryTotal")
            .Elements(cbc + "LineExtensionAmount")
            .Select(x => (string?)x)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerStr)) yield break;

        var header = decimal.Parse(headerStr!, System.Globalization.CultureInfo.InvariantCulture);
        var calc = Math.Round(SumLines(), ctx.Scale, MidpointRounding.AwayFromZero);

        if (calc != header)
        {
            var li = (IXmlLineInfo?)doc.Root;
            yield return new RuleViolation {
                Id = Id, Severity = Severity.Error,
                Message = $"LineExtensionAmount toplamı {calc} ≠ header {header}",
                Line = li?.LineNumber ?? 0, Column = li?.LinePosition ?? 0
            };
        }
    }
}