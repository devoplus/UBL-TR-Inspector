using System.Xml;
using System.Xml.Schema;
using UblTr.Core;

namespace UblTr.Xml;

public sealed class XsdValidator
{
    private readonly XmlSchemaSet _schemas;
    private readonly bool _hasSchemas;

    public XsdValidator(IEnumerable<string> xsdPaths)
    {
        _schemas = new XmlSchemaSet();
        foreach (var p in xsdPaths)
        {
            using var fs = File.OpenRead(p);
            _schemas.Add(null, XmlReader.Create(fs));
        }
        _schemas.CompilationSettings = new XmlSchemaCompilationSettings { EnableUpaCheck = false };
        try { _schemas.Compile(); _hasSchemas = _schemas.Count > 0; }
        catch { _hasSchemas = false; }
    }

    public IEnumerable<Finding> Validate(Stream xmlStream)
    {
        var findings = new List<Finding>();

        if (!_hasSchemas)
        {
            findings.Add(Finding.Schema(Severity.Info, "No XSD schemas found. Skipping XSD validation.", 0, 0));
            return findings;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _schemas,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreWhitespace = true
        };

        settings.ValidationEventHandler += (sender, e) =>
        {
            var sev = e.Severity == XmlSeverityType.Warning ? Severity.Warning : Severity.Error;
            var li = sender as IXmlLineInfo;
            findings.Add(Finding.Schema(sev, e.Message, li?.LineNumber ?? 0, li?.LinePosition ?? 0));
        };

        using var reader = XmlReader.Create(xmlStream, settings);
        while (reader.Read()) { /* streaming validation */ }
        return findings;
    }

    public static XsdValidator FromDirectory(string dir)
    {
        var paths = Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.xsd", SearchOption.AllDirectories)
            : Array.Empty<string>();
        return new XsdValidator(paths);
    }
}
