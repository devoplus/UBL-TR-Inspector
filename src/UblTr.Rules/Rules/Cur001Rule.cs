using System.Xml.Linq;
using UblTr.Core;

namespace UblTr.Rules.Rules;

public sealed class Cur001Rule : IBusinessRule
{
    public string Id => "CUR-001";
    public string Title => "Para birimi ölçeğine göre ondalık basamak sayısı";

    private static readonly Dictionary<string,int> CurrencyScale = new()
    {
        ["TRY"] = 2, ["USD"] = 2, ["EUR"] = 2, ["JPY"] = 0, ["KWD"] = 3
    };

    public IEnumerable<RuleViolation> Evaluate(XDocument doc, RuleContext ctx)
    {
        XNamespace cbc = UblNamespaces.Cbc;
        // Amount niteliği currencyID olan tüm elemanları kontrol et
        foreach (var el in doc.Descendants())
        {
            var cur = (string?)el.Attribute("currencyID");
            if (cur is null) continue;
            var text = (string?)el;
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (!CurrencyScale.TryGetValue(cur, out var scale)) scale = ctx.Scale;

            var sp = text.Split('.', 2);
            var decimals = sp.Length == 2 ? sp[1].Length : 0;
            if (decimals > scale)
            {
                var li = (IXmlLineInfo?)el;
                yield return new RuleViolation {
                    Id = Id, Severity = Severity.Error,
                    Message = $"currencyID={cur} için {scale} ondalık beklenir; değer '{text}' ({decimals} ondalık)",
                    Line = li?.LineNumber ?? 0, Column = li?.LinePosition ?? 0
                };
            }
        }
    }
}
