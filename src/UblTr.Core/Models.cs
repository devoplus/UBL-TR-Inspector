using System.Text.Json.Serialization;

namespace UblTr.Core;

public enum Severity { Info, Warning, Error }

public sealed class Finding
{
    public string Kind { get; init; } = "Schema"; // Schema or Rule
    public Severity Severity { get; init; } = Severity.Error;
    public string Message { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
    public string? RuleId { get; init; }

    public static Finding Schema(Severity sev, string message, int line, int col)
        => new() { Kind = "Schema", Severity = sev, Message = message, Line = line, Column = col };

    public static Finding Rule(string id, Severity sev, string message, int line = 0, int col = 0)
        => new() { Kind = "Rule", RuleId = id, Severity = sev, Message = message, Line = line, Column = col };
}

public sealed class RuleViolation
{
    public string Id { get; init; } = "";
    public Severity Severity { get; init; } = Severity.Error;
    public string Message { get; init; } = "";
    public int Line { get; init; }
    public int Column { get; init; }
}

public sealed class ReportSummary
{
    public int Errors { get; set; }
    public int Warnings { get; set; }
}

public static class UblNamespaces
{
    public const string Inv = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public const string Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    public const string Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
}
