using System.CommandLine;
using System.Text.Json;
using System.Xml.Linq;
using UblTr.Core;
using UblTr.Xml;
using UblTr.Rules;
using UblTr.Rules.Rules;

var root = new RootCommand("UBL-TR Inspector (Devoplus) – XSD + business rules validator");

var fileArg = new Argument<string>("file", "UBL XML dosyası");
var profileOpt = new Option<string?>(name: "--profile", description: "Profil adı", getDefaultValue: () => "einvoice");
var reportOpt = new Option<string?>(name: "--report", description: "Rapor çıktı (json:path | md:path)");
var failOnOpt = new Option<string?>(name: "--fail-on", description: "none|warn|error", getDefaultValue: () => "error");

var checkCmd = new Command("check", "XSD doğrulaması + iş kuralları");
checkCmd.AddArgument(fileArg);
checkCmd.AddOption(profileOpt);
checkCmd.AddOption(reportOpt);
checkCmd.AddOption(failOnOpt);
checkCmd.SetHandler(async (string file, string profile, string? report, string failOn) =>
{
    var r = await RunAllAsync(file, profile);
    if (!string.IsNullOrWhiteSpace(report)) await WriteReportAsync(report!, r);
    PrintConsole(r);
    Environment.Exit(GetExitCode(r, failOn));
}, fileArg, profileOpt, reportOpt, failOnOpt);

var validateCmd = new Command("validate", "Sadece XSD doğrulaması");
validateCmd.AddArgument(fileArg);
validateCmd.SetHandler(async (string file) =>
{
    var r = await RunXsdAsync(file);
    PrintConsole(r);
}, fileArg);

var rulesCmd = new Command("rules", "Sadece iş kuralları");
rulesCmd.AddArgument(fileArg);
rulesCmd.AddOption(profileOpt);
rulesCmd.AddOption(reportOpt);
rulesCmd.AddOption(failOnOpt);
rulesCmd.SetHandler(async (string file, string profile, string? report, string failOn) =>
{
    var r = await RunRulesAsync(file, profile);
    if (!string.IsNullOrWhiteSpace(report)) await WriteReportAsync(report!, r);
    PrintConsole(r);
    Environment.Exit(GetExitCode(r, failOn));
}, fileArg, profileOpt, reportOpt, failOnOpt);

root.AddCommand(checkCmd);
root.AddCommand(validateCmd);
root.AddCommand(rulesCmd);
await root.InvokeAsync(args);

static async Task<Report> RunAllAsync(string file, string profile)
{
    var xsdFindings = await Task.Run(() => ValidateXsd(file));
    var ruleFindings = await Task.Run(() => EvaluateRules(file, profile));
    return new Report(file, xsdFindings, ruleFindings);
}
static async Task<Report> RunXsdAsync(string file)
{
    var xsdFindings = await Task.Run(() => ValidateXsd(file));
    return new Report(file, xsdFindings, Enumerable.Empty<RuleViolation>());
}
static async Task<Report> RunRulesAsync(string file, string profile)
{
    var ruleFindings = await Task.Run(() => EvaluateRules(file, profile));
    return new Report(file, Enumerable.Empty<Finding>(), ruleFindings);
}

static IEnumerable<Finding> ValidateXsd(string file)
{
    var schemaDir = Path.Combine(AppContext.BaseDirectory, "../../../../../schemas");
    var validator = XsdValidator.FromDirectory(schemaDir);
    using var fs = File.OpenRead(file);
    return validator.Validate(fs);
}

static IEnumerable<RuleViolation> EvaluateRules(string file, string profile)
{
    var doc = XDocument.Load(file, LoadOptions.SetLineInfo);
    var engine = new RuleEngine()
        .Add(new Sum001Rule())
        .Add(new Vat002Rule())
        .Add(new Cur001Rule());
    return engine.EvaluateAll(doc, new RuleContext());
}

static void PrintConsole(Report r)
{
    int err = 0, warn = 0;
    foreach (var f in r.XsdFindings)
        if (f.Severity == Severity.Error) err++; else if (f.Severity == Severity.Warning) warn++;
    foreach (var v in r.RuleFindings)
        if (v.Severity == Severity.Error) err++; else if (v.Severity == Severity.Warning) warn++;

    Console.WriteLine($"{r.File}");
    Console.WriteLine($"XSD: {r.XsdFindings.Count()} findings");
    Console.WriteLine($"Rules: {r.RuleFindings.Count()} findings");
    foreach (var f in r.XsdFindings)
        Console.WriteLine($"  [XSD][{f.Severity}] {f.Message} (line {f.Line})");
    foreach (var v in r.RuleFindings)
        Console.WriteLine($"  [{v.Id}][{v.Severity}] {v.Message} (line {v.Line})");
    Console.WriteLine($"Summary: errors={err}, warnings={warn}");
}

static int GetExitCode(Report r, string failOn = "error")
{
    int err = 0, warn = 0;
    foreach (var f in r.XsdFindings)
        if (f.Severity == Severity.Error) err++; else if (f.Severity == Severity.Warning) warn++;
    foreach (var v in r.RuleFindings)
        if (v.Severity == Severity.Error) err++; else if (v.Severity == Severity.Warning) warn++;
    return failOn switch {
        "none" => 0,
        "warn" => (warn > 0 || err > 0) ? 1 : 0,
        _ => (err > 0) ? 2 : 0
    };
}

static async Task WriteReportAsync(string spec, Report r)
{
    var parts = spec.Split(':', 2);
    if (parts.Length != 2) throw new ArgumentException("report format must be like json:path or md:path");
    var (fmt, path) = (parts[0], parts[1]);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    if (fmt == "json")
    {
        var json = System.Text.Json.JsonSerializer.Serialize(r.ToJson(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
    else if (fmt == "md")
    {
        var md = r.ToMarkdown();
        await File.WriteAllTextAsync(path, md);
    }
    else throw new ArgumentException("unknown report format");
}

public sealed record Report(string File, IEnumerable<Finding> XsdFindings, IEnumerable<RuleViolation> RuleFindings)
{
    public object ToJson()
    {
        var sum = new ReportSummary();
        foreach (var f in XsdFindings)
        {
            if (f.Severity == Severity.Error) sum.Errors++;
            else if (f.Severity == Severity.Warning) sum.Warnings++;
        }
        foreach (var v in RuleFindings)
        {
            if (v.Severity == Severity.Error) sum.Errors++;
            else if (v.Severity == Severity.Warning) sum.Warnings++;
        }
        return new {
            file = File,
            xsd = new { findings = XsdFindings },
            rules = new { findings = RuleFindings },
            summary = new { errors = sum.Errors, warnings = sum.Warnings }
        };
    }

    public string ToMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Report — {File}");
        sb.AppendLine();
        sb.AppendLine("## Findings");
        sb.AppendLine("| Kind | Id | Severity | Message | Line |");
        sb.AppendLine("|------|----|----------|---------|------|");
        foreach (var f in XsdFindings)
            sb.AppendLine($"| XSD |  | {f.Severity} | {Escape(f.Message)} | {f.Line} |");
        foreach (var v in RuleFindings)
            sb.AppendLine($"| Rule | {v.Id} | {v.Severity} | {Escape(v.Message)} | {v.Line} |");
        return sb.ToString();

        static string Escape(string s) => s.Replace("|","\|").Replace("\n"," ");
    }
}
