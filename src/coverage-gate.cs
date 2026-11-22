#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property IsPackable=false
#:property GeneratePackageOnBuild=false
#:property GenerateDocumentationFile=false
#:property IsPrimaryProject=false

using System;
using System.Globalization;
using System.IO;
using System.Xml;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run coverage-gate.cs -- <cobertura.xml>");
    return 2;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"Cobertura file not found: {path}");
    return 1;
}

var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null,
    IgnoreComments = true,
    IgnoreWhitespace = true,
};

double lineRateSum = 0;
double branchRateSum = 0;
var matchingClasses = 0;
var lineHits = 0;
var inMatchingClass = false;

using (var reader = XmlReader.Create(path, settings))
{
    while (reader.Read())
    {
        if (reader.NodeType != XmlNodeType.Element)
        {
            continue;
        }

        if (reader.Name == "class")
        {
            var filename = reader.GetAttribute("filename") ?? "";
            var name = reader.GetAttribute("name") ?? "";
            inMatchingClass = IsXmlEssentialsRuntime(filename, name);
            if (inMatchingClass)
            {
                matchingClasses++;
                lineRateSum += ParseRate(reader.GetAttribute("line-rate"));
                branchRateSum += ParseRate(reader.GetAttribute("branch-rate"));
            }
        }
        else if (inMatchingClass && reader.Name == "line")
        {
            lineHits++;
        }
    }
}

Console.WriteLine($"Cobertura: {path}");
Console.WriteLine($"Matching CloudNimble.XmlEssentials classes: {matchingClasses}");
Console.WriteLine($"Lines reported on matching classes: {lineHits}");

if (matchingClasses == 0 || lineHits == 0)
{
    Console.WriteLine("PR 1 empty library: no instrumented lines. Collector ran. Gate passes.");
    return 0;
}

var avgLine = lineRateSum / matchingClasses;
var avgBranch = branchRateSum / matchingClasses;
Console.WriteLine($"Line rate: {avgLine.ToString("P1", CultureInfo.InvariantCulture)}");
Console.WriteLine($"Branch rate: {avgBranch.ToString("P1", CultureInfo.InvariantCulture)}");

if (avgLine + 1e-9 < 1.0)
{
    Console.Error.WriteLine("Line coverage is below 100% on CloudNimble.XmlEssentials.");
    return 1;
}

if (avgBranch + 1e-9 < 1.0)
{
    Console.Error.WriteLine("Branch coverage is below 100% on CloudNimble.XmlEssentials.");
    return 1;
}

return 0;

static bool IsXmlEssentialsRuntime(string filename, string name)
{
    if (name.Contains("Tests", StringComparison.OrdinalIgnoreCase)
        || name.Contains("SourceGeneration", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Performance", StringComparison.OrdinalIgnoreCase)
        || filename.Contains("Tests", StringComparison.OrdinalIgnoreCase)
        || filename.Contains("SourceGeneration", StringComparison.OrdinalIgnoreCase)
        || filename.Contains("Performance", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    return name.Contains("CloudNimble.XmlEssentials", StringComparison.Ordinal)
        || filename.Contains("CloudNimble.XmlEssentials", StringComparison.Ordinal)
        || filename.Replace('\\', '/').Contains("/CloudNimble.XmlEssentials/", StringComparison.Ordinal);
}

static double ParseRate(string? value)
{
    if (string.IsNullOrEmpty(value))
    {
        return 0;
    }

    return double.Parse(value, CultureInfo.InvariantCulture);
}
