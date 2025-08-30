using System.Xml.Linq;
using UblTr.Core;

namespace UblTr.Rules;

public sealed record RuleContext(int Scale = 2);

public interface IBusinessRule
{
    string Id { get; }
    string Title { get; }
    IEnumerable<RuleViolation> Evaluate(XDocument doc, RuleContext ctx);
}

public sealed class RuleEngine
{
    private readonly List<IBusinessRule> _rules = new();
    public RuleEngine Add(IBusinessRule rule) { _rules.Add(rule); return this; }
    public IEnumerable<RuleViolation> EvaluateAll(XDocument doc, RuleContext ctx)
        => _rules.SelectMany(r => r.Evaluate(doc, ctx));
}
