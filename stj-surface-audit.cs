#:package Microsoft.CodeAnalysis.CSharp@4.14.0
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var stjRef = args[0];
var xmlRoot = args[1];
var outPath = args[2];

var stj = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
var stjNs = new Dictionary<string, string>();
Collect(File.ReadAllText(stjRef), stj, stjNs, publicOnly: false);

var xml = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
var xmlNs = new Dictionary<string, string>();
foreach (var file in Directory.EnumerateFiles(xmlRoot, "*.cs", SearchOption.AllDirectories))
{
    var norm = file.Replace(Path.DirectorySeparatorChar, '/');
    if (norm.Contains("/obj/") || norm.Contains("/bin/")) continue;
    Collect(File.ReadAllText(file), xml, xmlNs, publicOnly: true);
}

using var w = new StreamWriter(outPath);
int missingTypes = 0, missingMembers = 0, matchedTypes = 0;
var missingTypeLines = new List<string>();
var memberLines = new List<string>();
foreach (var (type, members) in stj)
{
    var mapped = type.Replace("Json", "Xml");
    if (!xml.TryGetValue(mapped, out var have))
    {
        missingTypes++;
        missingTypeLines.Add($"{stjNs[type]}|{type}|{members.Count}");
        continue;
    }
    matchedTypes++;
    foreach (var (member, count) in members)
    {
        var m = member.Replace("Json", "Xml");
        if (!have.TryGetValue(m, out var haveCount))
        {
            missingMembers++;
            memberLines.Add($"{type}|{member}|missing|{count}|0");
        }
        else if (haveCount < count)
        {
            memberLines.Add($"{type}|{member}|fewer overloads|{count}|{haveCount}");
        }
    }
}
w.WriteLine($"STJ types: {stj.Count}; matched by name: {matchedTypes}; no counterpart: {missingTypes}; members missing on matched types: {missingMembers}");
w.WriteLine("## TYPES WITH NO COUNTERPART (namespace|type|member count)");
foreach (var l in missingTypeLines) w.WriteLine(l);
w.WriteLine("## MEMBERS (type|member|status|stj overloads|xml overloads)");
foreach (var l in memberLines) w.WriteLine(l);
w.WriteLine("## XML TYPES WITH NO STJ COUNTERPART");
foreach (var t in xml.Keys) if (!stj.ContainsKey(t.Replace("Xml", "Json"))) w.WriteLine(t);

static void Collect(string text, SortedDictionary<string, SortedDictionary<string, int>> into, Dictionary<string, string> namespaces, bool publicOnly)
{
    var root = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET11_0_OR_GREATER"])).GetRoot();
    foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        if (publicOnly && !IsPublicType(type)) continue;
        var name = TypeName(type);
        var ns = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
        namespaces[name] = ns;
        if (!into.TryGetValue(name, out var members)) into[name] = members = new SortedDictionary<string, int>(StringComparer.Ordinal);
        if (type is EnumDeclarationSyntax e)
        {
            foreach (var m in e.Members) Add(members, m.Identifier.Text);
            continue;
        }
        var isInterface = type is InterfaceDeclarationSyntax;
        foreach (var member in ((TypeDeclarationSyntax)type).Members)
        {
            if (member is BaseTypeDeclarationSyntax) continue;
            if (publicOnly && !isInterface && !member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || (m.IsKind(SyntaxKind.ProtectedKeyword) && !member.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword))))) continue;
            if (publicOnly && member.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)) && !member.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword))) continue;
            switch (member)
            {
                case MethodDeclarationSyntax m when m.ExplicitInterfaceSpecifier is null: Add(members, m.Identifier.Text + "()"); break;
                case ConstructorDeclarationSyntax c when !c.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword)): Add(members, ".ctor"); break;
                case PropertyDeclarationSyntax p when p.ExplicitInterfaceSpecifier is null: Add(members, p.Identifier.Text); break;
                case IndexerDeclarationSyntax: Add(members, "this[]"); break;
                case FieldDeclarationSyntax f: foreach (var v in f.Declaration.Variables) Add(members, v.Identifier.Text); break;
                case EventDeclarationSyntax ev: Add(members, ev.Identifier.Text); break;
                case OperatorDeclarationSyntax o: Add(members, "operator " + o.OperatorToken.Text); break;
                case ConversionOperatorDeclarationSyntax co: Add(members, (co.ImplicitOrExplicitKeyword.Text) + " operator " + co.Type); break;
            }
        }
    }
}

static void Add(SortedDictionary<string, int> members, string name) => members[name] = members.TryGetValue(name, out var c) ? c + 1 : 1;

static bool IsPublicType(BaseTypeDeclarationSyntax type)
{
    for (SyntaxNode? n = type; n is BaseTypeDeclarationSyntax t; n = n.Parent)
    {
        if (!t.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) return false;
    }
    return true;
}

static string TypeName(BaseTypeDeclarationSyntax type)
{
    var parts = new List<string>();
    for (SyntaxNode? n = type; n is BaseTypeDeclarationSyntax t; n = n.Parent)
    {
        var arity = t is TypeDeclarationSyntax { TypeParameterList: { } tp } ? "`" + tp.Parameters.Count : "";
        parts.Insert(0, t.Identifier.Text + arity);
    }
    return string.Join(".", parts);
}
