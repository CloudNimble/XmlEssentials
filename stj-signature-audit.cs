#:package Microsoft.CodeAnalysis.CSharp@4.14.0
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Compares, for every type and member name that System.Text.Json and XmlEssentials share, the shape of the
// declaration: the modifiers of the type, the type and settability of a property, and the signatures of a method.
var stj = new SortedDictionary<string, TypeShape>(StringComparer.Ordinal);
Collect(File.ReadAllText(args[0]), stj, publicOnly: false);

var xml = new SortedDictionary<string, TypeShape>(StringComparer.Ordinal);
foreach (var file in Directory.EnumerateFiles(args[1], "*.cs", SearchOption.AllDirectories))
{
    var norm = file.Replace(Path.DirectorySeparatorChar, '/');
    if (norm.Contains("/obj/") || norm.Contains("/bin/")) continue;
    Collect(File.ReadAllText(file), xml, publicOnly: true);
}

using var w = new StreamWriter(args[2]);
w.WriteLine("# type|member|what|System.Text.Json (Json mapped to Xml)|XmlEssentials");
foreach (var (name, s) in stj)
{
    var mapped = name.Replace("Json", "Xml");
    if (!xml.TryGetValue(mapped, out var x)) continue;
    if (s.Modifiers != x.Modifiers) w.WriteLine($"{mapped}||type modifiers|{s.Modifiers}|{x.Modifiers}");
    foreach (var (member, sp) in s.Properties)
    {
        var m = member.Replace("Json", "Xml");
        if (!x.Properties.TryGetValue(m, out var xp)) continue;
        if (sp.Type != xp.Type) w.WriteLine($"{mapped}|{m}|property type|{sp.Type}|{xp.Type}");
        if (sp.Settable != xp.Settable) w.WriteLine($"{mapped}|{m}|settable|{sp.Settable}|{xp.Settable}");
        if (sp.Modifiers != xp.Modifiers) w.WriteLine($"{mapped}|{m}|property modifiers|{sp.Modifiers}|{xp.Modifiers}");
    }
    foreach (var (member, sigs) in s.Methods)
    {
        var m = member.Replace("Json", "Xml");
        if (!x.Methods.TryGetValue(m, out var xsigs)) continue;
        var onlyStj = sigs.Except(xsigs).ToList();
        var onlyXml = xsigs.Except(sigs).ToList();
        if (onlyStj.Count == 0) continue;
        // A signature only XmlEssentials has is an addition, not a difference; it is listed only beside one it may stand for.
        foreach (var sig in onlyStj) w.WriteLine($"{mapped}|{m}|signature|{sig}|{(onlyXml.Count == 0 ? "(no other signature)" : string.Join(" ;; ", onlyXml))}");
    }
}

static void Collect(string text, SortedDictionary<string, TypeShape> into, bool publicOnly)
{
    var root = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: ["NET", "NET10_0_OR_GREATER", "NET11_0_OR_GREATER"])).GetRoot();
    foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
    {
        if (publicOnly && !IsPublicType(type)) continue;
        var name = TypeName(type);
        if (!into.TryGetValue(name, out var shape)) into[name] = shape = new TypeShape();
        var mods = Mods(type.Modifiers, "abstract", "sealed", "static", "readonly", "ref");
        var kind = type.Keyword.Text + (type is RecordDeclarationSyntax r ? " " + r.ClassOrStructKeyword.Text : "");
        shape.Modifiers = (kind + " " + mods).Trim();
        var isInterface = type is InterfaceDeclarationSyntax;
        foreach (var member in type.Members)
        {
            if (publicOnly && !isInterface && !Visible(member.Modifiers)) continue;
            switch (member)
            {
                case PropertyDeclarationSyntax p when p.ExplicitInterfaceSpecifier is null:
                    var settable = p.AccessorList?.Accessors.Any(a => (a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)) && (!publicOnly || !a.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword) || x.IsKind(SyntaxKind.InternalKeyword)) || a.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword)) && !a.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword)))) ?? false;
                    var init = p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false;
                    shape.Properties[p.Identifier.Text] = new PropertyShape(Norm(p.Type.ToString()), settable ? (init ? "init" : "set") : "get-only", Mods(p.Modifiers, "abstract", "virtual", "static", "new", "protected"));
                    break;
                case MethodDeclarationSyntax m when m.ExplicitInterfaceSpecifier is null:
                    if (!shape.Methods.TryGetValue(m.Identifier.Text, out var list)) shape.Methods[m.Identifier.Text] = list = [];
                    var generic = m.TypeParameterList is { } tp ? "<" + string.Join(",", tp.Parameters.Select(t => t.Identifier.Text)) + ">" : "";
                    list.Add(Norm(m.ReturnType.ToString()) + " " + generic + "(" + string.Join(", ", m.ParameterList.Parameters.Select(q => (Mods(q.Modifiers, "ref", "out", "in", "params", "this", "scoped") + " " + Norm(q.Type!.ToString())).Trim())) + ")");
                    break;
            }
        }
    }
}

static bool Visible(SyntaxTokenList modifiers)
{
    if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) return true;
    return modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) && !modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
}

static string Mods(SyntaxTokenList modifiers, params string[] keep) => string.Join(" ", modifiers.Select(m => m.Text).Where(keep.Contains).OrderBy(m => m, StringComparer.Ordinal));

static string Norm(string type)
{
    // Attributes on a type use, qualifying namespaces, and spacing are not part of the shape.
    var t = Regex.Replace(type, @"\[[^\]]*\(.*?\)\]\s*", "");
    t = Regex.Replace(t, @"\[System\.[^\]]*\]\s*", "");
    t = t.Replace("global::", "");
    t = Regex.Replace(t, @"(?:[A-Za-z_]\w*\.)+(?=[A-Za-z_])", "");
    t = Regex.Replace(t, @"\s+", "");
    t = t.Replace("Json", "Xml");
    string[][] aliases = [["String", "string"], ["Int32", "int"], ["Int64", "long"], ["Boolean", "bool"], ["Object", "object"], ["Byte", "byte"], ["Char", "char"], ["Double", "double"], ["Single", "float"], ["Decimal", "decimal"], ["Int16", "short"], ["UInt32", "uint"], ["UInt64", "ulong"], ["UInt16", "ushort"], ["SByte", "sbyte"]];
    foreach (var a in aliases) t = Regex.Replace(t, @"\b" + a[0] + @"\b", a[1]);
    return t;
}

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

record PropertyShape(string Type, string Settable, string Modifiers);

class TypeShape
{
    public string Modifiers = "";
    public SortedDictionary<string, PropertyShape> Properties = new(StringComparer.Ordinal);
    public SortedDictionary<string, List<string>> Methods = new(StringComparer.Ordinal);
}
