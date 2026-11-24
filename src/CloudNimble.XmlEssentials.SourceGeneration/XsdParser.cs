using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Reads W3C XML Schema AdditionalFiles into POCO and context specs.
    /// </summary>
    internal static class XsdParser
    {

        #region Fields

        internal const string Xs = "http://www.w3.org/2001/XMLSchema";

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds generated types from <paramref name="files"/>.
        /// </summary>
        /// <param name="files">The AdditionalFiles (path, text, namespace, context name).</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <returns>POCO sources and context specs.</returns>
        internal static (List<(string Hint, string Source)> Pocos, List<ContextSpec> Contexts) Build(
            List<XsdFileInput> files,
            List<Diagnostic> diagnostics)
        {
            var byFileName = new Dictionary<string, XsdFileInput>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                byFileName[Path.GetFileName(file.Path)] = file;
            }

            var pocos = new List<(string Hint, string Source)>();
            var contexts = new List<ContextSpec>();
            var parsed = new Dictionary<string, MiniElement>(StringComparer.OrdinalIgnoreCase);
            // A class is known by its namespace and its name, so the names in use are kept for each namespace.
            // Two schema files that generate into different namespaces may each have a class called Order.
            var classNamesByNamespace = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                if (file.Text.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0
                    || file.Text.IndexOf("<!ENTITY", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.SchemaResolution, Location.None, "Schema '" + Path.GetFileName(file.Path) + "' contains a DTD."));
                    continue;
                }

                MiniElement root;
                try
                {
                    root = MiniElement.Parse(file.Text);
                }
                catch (Exception ex)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.SchemaResolution, Location.None, "Schema '" + Path.GetFileName(file.Path) + "' is not well-formed: " + ex.Message));
                    continue;
                }

                parsed[file.Path] = root;
            }

            foreach (var file in files)
            {
                if (!parsed.TryGetValue(file.Path, out var root))
                {
                    continue;
                }

                var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { file.Path };
                ResolveIncludes(root, file, byFileName, parsed, included, diagnostics);
                var ns = string.IsNullOrWhiteSpace(file.NamespaceName)
                    ? "XmlEssentials.Generated"
                    : file.NamespaceName;
                var contextName = string.IsNullOrWhiteSpace(file.ContextName)
                    ? Naming.ToPascal(Sanitize(Path.GetFileNameWithoutExtension(file.Path))) + "XmlContext"
                    : file.ContextName;
                if (!classNamesByNamespace.TryGetValue(ns, out var classNames))
                {
                    classNames = new HashSet<string>(StringComparer.Ordinal);
                    classNamesByNamespace[ns] = classNames;
                }

                var result = EmitSchema(root, ns, contextName, Path.GetFileName(file.Path), classNames, diagnostics);
                pocos.AddRange(result.Pocos);
                if (result.Context is { } context)
                {
                    contexts.Add(context);
                }
            }

            return (pocos, contexts);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves <c>xs:include</c> / <c>xs:import</c> against AdditionalFiles.
        /// </summary>
        /// <param name="root">The schema root.</param>
        /// <param name="file">The current file.</param>
        /// <param name="byFileName">AdditionalFiles by file name.</param>
        /// <param name="parsed">Parsed roots.</param>
        /// <param name="included">Already merged paths.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        internal static void ResolveIncludes(
            MiniElement root,
            XsdFileInput file,
            Dictionary<string, XsdFileInput> byFileName,
            Dictionary<string, MiniElement> parsed,
            HashSet<string> included,
            List<Diagnostic> diagnostics)
        {
            var pending = new List<MiniElement>();
            foreach (var child in root.Elements)
            {
                if (child.NamespaceUri != Xs || child.LocalName is not "include" and not "import")
                {
                    continue;
                }

                var location = child.Attr("schemaLocation");
                if (string.IsNullOrWhiteSpace(location))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.SchemaResolution, Location.None, "Schema '" + Path.GetFileName(file.Path) + "' has an include/import without schemaLocation."));
                    continue;
                }

                var schemaLocation = location!;
                if (schemaLocation.IndexOf("://", StringComparison.Ordinal) >= 0)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.SchemaResolution, Location.None, "Schema include/import '" + schemaLocation + "' is not an AdditionalFile."));
                    continue;
                }

                var name = Path.GetFileName(schemaLocation.Replace('/', Path.DirectorySeparatorChar));
                if (!byFileName.TryGetValue(name, out var other) || !parsed.TryGetValue(other.Path, out var otherRoot))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.SchemaResolution, Location.None, "Schema include/import '" + schemaLocation + "' is not an AdditionalFile."));
                    continue;
                }

                if (!included.Add(other.Path))
                {
                    continue;
                }

                ResolveIncludes(otherRoot, other, byFileName, parsed, included, diagnostics);
                pending.AddRange(otherRoot.Elements);
            }

            root.Elements.AddRange(pending);
        }

        /// <summary>
        /// Emits POCOs and a context for one schema document (with includes merged).
        /// </summary>
        /// <param name="root">The schema root.</param>
        /// <param name="ns">The C# namespace.</param>
        /// <param name="contextName">The context type name.</param>
        /// <param name="fileName">The schema file name, for diagnostics.</param>
        /// <param name="classNames">The class names already used in <paramref name="ns"/>, by this schema file or an earlier one.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <returns>The POCOs and context.</returns>
        internal static (List<(string Hint, string Source)> Pocos, ContextSpec? Context) EmitSchema(
            MiniElement root,
            string ns,
            string contextName,
            string fileName,
            HashSet<string> classNames,
            List<Diagnostic> diagnostics)
        {
            var targetNs = root.Attr("targetNamespace") ?? "";
            var complexTypes = new Dictionary<string, MiniElement>(StringComparer.Ordinal);
            var simpleEnums = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var globalElements = new List<MiniElement>();
            foreach (var child in root.Elements)
            {
                if (child.NamespaceUri != Xs)
                {
                    continue;
                }

                if (child.LocalName is "redefine" or "notation")
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, "Schema '" + fileName + "' contains xs:" + child.LocalName + ", which is ignored."));
                    continue;
                }

                if (child.LocalName is "key" or "keyref" or "unique")
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, "Identity constraint xs:" + child.LocalName + " is ignored."));
                    continue;
                }

                if (child.LocalName == "complexType" && child.Attr("name") is { Length: > 0 } ctName)
                {
                    complexTypes[ctName] = child;
                }
                else if (child.LocalName == "simpleType" && child.Attr("name") is { Length: > 0 } stName)
                {
                    var enums = ReadEnumerations(child);
                    if (enums.Count > 0)
                    {
                        simpleEnums[stName] = enums;
                    }
                }
                else if (child.LocalName == "element")
                {
                    globalElements.Add(child);
                }
            }

            var pocos = new List<(string Hint, string Source)>();
            var typeSpecs = new List<TypeSpec>();
            var typeMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in simpleEnums)
            {
                typeMap[pair.Key] = "global::" + ns + "." + Sanitize(pair.Key);
            }

            foreach (var named in complexTypes)
            {
                typeMap[named.Key] = "global::" + ns + "." + Sanitize(named.Key);
            }

            foreach (var pair in simpleEnums)
            {
                var enumName = Sanitize(pair.Key);
                if (!classNames.Add(enumName))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, enumName));
                    continue;
                }

                pocos.Add((ns + "." + enumName + ".g.cs", EmitEnum(ns, enumName, pair.Value)));
                typeSpecs.Add(new TypeSpec(
                    "global::" + ns + "." + enumName,
                    enumName,
                    enumName,
                    "",
                    3,
                    true,
                    false,
                    false,
                    new EquatableArray<MemberSpec>([]),
                    ""));
            }

            var bind = new XsdBindContext(ns, fileName, targetNs, classNames, complexTypes, typeMap, pocos, typeSpecs, diagnostics);
            foreach (var element in globalElements)
            {
                var elementName = element.Attr("name");
                if (string.IsNullOrWhiteSpace(elementName))
                {
                    continue;
                }

                if (!Naming.IsXmlName(elementName!))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedInvalidXmlName, Location.None, elementName!));
                    continue;
                }

                MiniElement? complex = null;
                var typeAttr = element.Attr("type");
                if (typeAttr is not null)
                {
                    var local = LocalName(typeAttr);
                    complexTypes.TryGetValue(local, out complex);
                }
                else
                {
                    foreach (var nested in element.Elements)
                    {
                        if (nested.NamespaceUri == Xs && nested.LocalName == "complexType")
                        {
                            complex = nested;
                            break;
                        }
                    }
                }

                if (complex is null)
                {
                    continue;
                }

                var className = Sanitize(elementName!);
                if (!classNames.Add(className))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, className));
                    continue;
                }

                var members = BindParticles(complex, className, bind);
                var documentation = ReadDocumentation(element) ?? ReadDocumentation(complex);
                pocos.Add((ns + "." + className + ".g.cs", EmitPoco(ns, className, elementName!, targetNs, members, documentation)));
                typeSpecs.Add(ToTypeSpec(ns, className, elementName!, targetNs, members));
            }

            foreach (var named in complexTypes)
            {
                var className = Sanitize(named.Key);
                if (!classNames.Add(className))
                {
                    continue;
                }

                var members = BindParticles(named.Value, className, bind);
                pocos.Add((ns + "." + className + ".g.cs", EmitPoco(ns, className, named.Key, targetNs, members, ReadDocumentation(named.Value))));
                typeSpecs.Add(ToTypeSpec(ns, className, named.Key, targetNs, members));
            }

            if (typeSpecs.Count == 0)
            {
                return (pocos, null);
            }

            var clrContext = "global::" + ns + "." + contextName;
            var context = new ContextSpec(
                clrContext,
                contextName,
                ns,
                "public",
                0,
                new OptionsSpec(!string.IsNullOrEmpty(targetNs), false, false, targetNs, 0, 0, 0, 0, 0, false, false, 0, 0),
                new EquatableArray<TypeSpec>(typeSpecs.ToArray()),
                emitInheritance: true);
            return (pocos, context);
        }

        /// <summary>
        /// Binds sequence/choice/attributes of a complex type to members.
        /// </summary>
        /// <param name="complex">The complex type element.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The members.</returns>
        internal static List<MemberSpec> BindParticles(MiniElement complex, string owner, XsdBindContext bind)
        {
            var members = new List<MemberSpec>();
            var content = complex;
            foreach (var child in complex.Elements)
            {
                if (child.NamespaceUri == Xs && child.LocalName == "complexContent")
                {
                    foreach (var ext in child.Elements)
                    {
                        if (ext.LocalName is "extension" or "restriction")
                        {
                            var baseName = LocalName(ext.Attr("base") ?? "");
                            if (ext.LocalName == "extension"
                                && baseName.Length > 0
                                && bind.ComplexTypes.TryGetValue(baseName, out var baseComplex))
                            {
                                members.AddRange(BindParticles(baseComplex, owner, bind));
                            }

                            content = ext;
                        }
                    }
                }
                else if (child.NamespaceUri == Xs && child.LocalName == "simpleContent")
                {
                    foreach (var ext in child.Elements)
                    {
                        if (ext.LocalName is "extension" or "restriction")
                        {
                            content = ext;
                            var baseType = MapXsdType(ext.Attr("base") ?? "xs:string", bind.TypeMap);
                            members.Add(new MemberSpec("Value", baseType, "Value", "", false, true, false, false, false, "", "", true, false, true, 0, -1, false, false));
                        }
                    }
                }
            }

            foreach (var child in content.Elements)
            {
                if (child.NamespaceUri != Xs)
                {
                    continue;
                }

                if (child.LocalName is "key" or "keyref" or "unique")
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, "Identity constraint xs:" + child.LocalName + " is ignored."));
                    continue;
                }

                if (child.LocalName is "sequence" or "choice" or "all")
                {
                    if (child.LocalName == "choice")
                    {
                        bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, "xs:choice is mapped as optional members."));
                    }

                    foreach (var particle in child.Elements)
                    {
                        BindParticle(particle, owner, bind, members);
                    }
                }
                else if (child.LocalName == "attribute")
                {
                    BindAttribute(child, bind, members);
                }
                else if (child.LocalName == "anyAttribute")
                {
                    EnsureExtensionData(members);
                }
            }

            members.Sort(static (left, right) =>
            {
                var leftRank = left.IsAttribute ? 0 : left.IsText || left.IsCData ? 1 : 2;
                var rightRank = right.IsAttribute ? 0 : right.IsText || right.IsCData ? 1 : 2;
                return leftRank.CompareTo(rightRank);
            });
            return members;
        }

        /// <summary>
        /// Binds one particle (element or any).
        /// </summary>
        /// <param name="particle">The particle.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        internal static void BindParticle(MiniElement particle, string owner, XsdBindContext bind, List<MemberSpec> members)
        {
            if (particle.NamespaceUri != Xs)
            {
                return;
            }

            if (particle.LocalName == "any")
            {
                EnsureExtensionData(members);
                return;
            }

            if (particle.LocalName != "element")
            {
                return;
            }

            if (particle.Attr("substitutionGroup") is { Length: > 0 })
            {
                bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, "substitutionGroup is ignored; use [XmlDerivedType] on the generated type."));
            }

            var name = particle.Attr("name");
            if (string.IsNullOrWhiteSpace(name) && particle.Attr("ref") is { Length: > 0 } elementRef)
            {
                name = LocalName(elementRef);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!Naming.IsXmlName(name!))
            {
                bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedInvalidXmlName, Location.None, name!));
                return;
            }

            var maxOccurs = particle.Attr("maxOccurs") ?? "1";
            var unbounded = maxOccurs == "unbounded" || (int.TryParse(maxOccurs, out var max) && max > 1);
            MiniElement? inline = null;
            foreach (var nested in particle.Elements)
            {
                if (nested.NamespaceUri == Xs && nested.LocalName == "complexType")
                {
                    inline = nested;
                    break;
                }
            }

            string clrType;
            if (inline is not null)
            {
                var nestedName = Sanitize(owner + "_" + name);
                if (bind.ClassNames.Add(nestedName))
                {
                    var nestedMembers = BindParticles(inline, nestedName, bind);
                    bind.Pocos.Add((bind.Namespace + "." + nestedName + ".g.cs", EmitPoco(bind.Namespace, nestedName, name!, bind.TargetNamespace, nestedMembers, ReadDocumentation(inline))));
                    bind.TypeSpecs.Add(ToTypeSpec(bind.Namespace, nestedName, name!, bind.TargetNamespace, nestedMembers));
                }
                else
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, nestedName));
                }

                clrType = "global::" + bind.Namespace + "." + nestedName;
            }
            else
            {
                clrType = MapXsdType(particle.Attr("type") ?? "xs:string", bind.TypeMap);
            }

            var nillable = particle.Attr("nillable") == "true" || (!IsValueClr(clrType) && !IsEnumClr(clrType, bind.TypeSpecs));
            var isCollection = unbounded;
            if (nillable && IsValueClr(clrType) && !unbounded)
            {
                clrType += "?";
            }

            var memberClr = unbounded ? "global::System.Collections.Generic.List<" + TrimNullable(clrType) + ">" : clrType;
            var ident = Sanitize(name!);
            members.Add(new MemberSpec(
                ident,
                memberClr,
                name!,
                "",
                false,
                false,
                false,
                isCollection,
                false,
                name!,
                isCollection ? TrimNullable(clrType) : "",
                nillable || unbounded,
                false,
                true,
                0,
                -1,
                false,
                false));
        }

        /// <summary>
        /// Binds an <c>xs:attribute</c>.
        /// </summary>
        /// <param name="attribute">The attribute particle.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        internal static void BindAttribute(MiniElement attribute, XsdBindContext bind, List<MemberSpec> members)
        {
            var name = attribute.Attr("name");
            if (string.IsNullOrWhiteSpace(name) && attribute.Attr("ref") is { Length: > 0 } attributeRef)
            {
                name = LocalName(attributeRef);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!Naming.IsXmlName(name!))
            {
                bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedInvalidXmlName, Location.None, name!));
                return;
            }

            var clrType = MapXsdType(attribute.Attr("type") ?? "xs:string", bind.TypeMap);
            members.Add(new MemberSpec(Sanitize(name!), clrType, name!, "", true, false, false, false, false, "", "", !IsValueClr(clrType), false, true, 0, -1, false, false));
        }

        /// <summary>
        /// Ensures an extension-data member exists.
        /// </summary>
        /// <param name="members">The members.</param>
        internal static void EnsureExtensionData(List<MemberSpec> members)
        {
            foreach (var member in members)
            {
                if (member.IsExtensionData)
                {
                    return;
                }
            }

            members.Add(new MemberSpec("Extra", "global::CloudNimble.Text.Xml.Serialization.XmlExtensionData", "Extra", "", false, false, false, false, false, "", "", true, false, true, 0, -1, true, false));
        }

        /// <summary>
        /// Maps an XSD type name to a CLR type name.
        /// </summary>
        /// <param name="xsdType">The XSD type (possibly prefixed).</param>
        /// <param name="typeMap">Named schema types already mapped to CLR names.</param>
        /// <returns>The fully qualified CLR type name.</returns>
        internal static string MapXsdType(string xsdType, Dictionary<string, string> typeMap)
        {
            var local = LocalName(xsdType);
            if (typeMap.TryGetValue(local, out var mapped))
            {
                return mapped;
            }

            return local switch
            {
                "boolean" => "bool",
                "byte" => "sbyte",
                "unsignedByte" => "byte",
                "short" => "short",
                "unsignedShort" => "ushort",
                "int" or "integer" => "int",
                "unsignedInt" => "uint",
                "long" => "long",
                "unsignedLong" => "ulong",
                "float" => "float",
                "double" => "double",
                "decimal" => "decimal",
                "dateTime" => "global::System.DateTime",
                "date" => "global::System.DateOnly",
                "time" => "global::System.TimeOnly",
                "duration" => "global::System.TimeSpan",
                "base64Binary" => "byte[]",
                "anyURI" => "global::System.Uri",
                "guid" => "global::System.Guid",
                _ => "string",
            };
        }

        /// <summary>
        /// Returns whether <paramref name="clrType"/> is a non-nullable value type name.
        /// </summary>
        /// <param name="clrType">The CLR type name.</param>
        /// <returns><see langword="true"/> for value types.</returns>
        internal static bool IsValueClr(string clrType)
        {
            return clrType is "bool" or "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong"
                or "float" or "double" or "decimal"
                or "global::System.DateTime" or "global::System.DateOnly" or "global::System.TimeOnly"
                or "global::System.TimeSpan" or "global::System.Guid";
        }

        /// <summary>
        /// Returns whether <paramref name="clrType"/> is a generated enum.
        /// </summary>
        /// <param name="clrType">The CLR type name.</param>
        /// <param name="typeSpecs">The type specs collected so far.</param>
        /// <returns><see langword="true"/> for enums.</returns>
        internal static bool IsEnumClr(string clrType, List<TypeSpec> typeSpecs)
        {
            foreach (var spec in typeSpecs)
            {
                if (spec.Kind == 3 && spec.ClrTypeName == clrType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Strips a trailing <c>?</c> from <paramref name="clrType"/>.
        /// </summary>
        /// <param name="clrType">The CLR type name.</param>
        /// <returns>The non-nullable name.</returns>
        internal static string TrimNullable(string clrType)
        {
            return clrType.EndsWith("?", StringComparison.Ordinal) ? clrType.Substring(0, clrType.Length - 1) : clrType;
        }

        /// <summary>
        /// Returns the local part of a QName.
        /// </summary>
        /// <param name="qname">The QName.</param>
        /// <returns>The local name.</returns>
        internal static string LocalName(string qname)
        {
            var colon = qname.IndexOf(':');
            return colon >= 0 ? qname.Substring(colon + 1) : qname;
        }

        /// <summary>
        /// Reads enumeration values from a simpleType restriction.
        /// </summary>
        /// <param name="simpleType">The simpleType element.</param>
        /// <returns>The values.</returns>
        internal static List<string> ReadEnumerations(MiniElement simpleType)
        {
            var values = new List<string>();
            foreach (var restriction in simpleType.Elements)
            {
                if (restriction.LocalName != "restriction")
                {
                    continue;
                }

                foreach (var facet in restriction.Elements)
                {
                    if (facet.LocalName == "enumeration" && facet.Attr("value") is { Length: > 0 } value)
                    {
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Reads xs:documentation text.
        /// </summary>
        /// <param name="element">The annotated element.</param>
        /// <returns>The documentation, or null.</returns>
        internal static string? ReadDocumentation(MiniElement element)
        {
            foreach (var child in element.Elements)
            {
                if (child.LocalName != "annotation")
                {
                    continue;
                }

                foreach (var doc in child.Elements)
                {
                    if (doc.LocalName == "documentation" && !string.IsNullOrWhiteSpace(doc.Text))
                    {
                        return doc.Text.Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Makes a valid C# identifier.
        /// </summary>
        /// <param name="name">The XML or file name.</param>
        /// <returns>The identifier.</returns>
        internal static string Sanitize(string name)
        {
            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            var ident = new string(chars);
            if (ident.Length == 0 || char.IsDigit(ident[0]))
            {
                ident = "_" + ident;
            }

            return ident;
        }

        /// <summary>
        /// Builds a <see cref="TypeSpec"/> for a generated POCO.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="className">The class name.</param>
        /// <param name="rootName">The XML root name.</param>
        /// <param name="rootNs">The XML namespace.</param>
        /// <param name="members">The members.</param>
        /// <returns>The type spec.</returns>
        internal static TypeSpec ToTypeSpec(string ns, string className, string rootName, string rootNs, List<MemberSpec> members)
        {
            return new TypeSpec(
                "global::" + ns + "." + className,
                className,
                rootName,
                rootNs,
                0,
                false,
                true,
                true,
                new EquatableArray<MemberSpec>(members.ToArray()),
                "");
        }

        /// <summary>
        /// Emits a POCO class.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="className">The class name.</param>
        /// <param name="rootName">The XML root name.</param>
        /// <param name="rootNs">The XML namespace.</param>
        /// <param name="members">The members.</param>
        /// <param name="documentation">Optional documentation.</param>
        /// <returns>The C# source.</returns>
        internal static string EmitPoco(string ns, string className, string rootName, string rootNs, List<MemberSpec> members, string? documentation)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using CloudNimble.Text.Xml.Serialization;");
            builder.AppendLine();
            builder.Append("namespace ").Append(ns);
            builder.AppendLine();
            builder.AppendLine("{");
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(documentation))
            {
                builder.AppendLine("    /// <summary>");
                builder.Append("    /// ").Append(EscapeXmlDoc(documentation!)).AppendLine();
                builder.AppendLine("    /// </summary>");
            }

            builder.Append("    [XmlRoot(\"").Append(ContextEmitter.Escape(rootName)).Append('"');
            if (!string.IsNullOrEmpty(rootNs))
            {
                builder.Append(", Namespace = \"").Append(ContextEmitter.Escape(rootNs)).Append('"');
            }

            builder.AppendLine(")]");
            builder.Append("    public partial class ").Append(className);
            builder.AppendLine();
            builder.AppendLine("    {");
            foreach (var member in members)
            {
                if (member.IsExtensionData)
                {
                    builder.AppendLine("        [XmlExtensionData]");
                    builder.Append("        public ").Append(member.ClrTypeName).Append("? ").Append(member.ClrName).AppendLine(" { get; set; }");
                    builder.AppendLine();
                    continue;
                }

                if (member.IsAttribute)
                {
                    builder.Append("        [XmlAttribute(\"").Append(ContextEmitter.Escape(member.LocalName)).AppendLine("\")]");
                }
                else if (member.IsText)
                {
                    builder.AppendLine("        [XmlText]");
                }
                else if (member.IsCollection && member.IsWrapped)
                {
                    builder.Append("        [XmlArray(\"").Append(ContextEmitter.Escape(member.LocalName)).AppendLine("\")]");
                    builder.Append("        [XmlArrayItem(\"").Append(ContextEmitter.Escape(member.ItemLocalName)).AppendLine("\")]");
                }
                else if (member.LocalName != member.ClrName)
                {
                    builder.Append("        [XmlElement(\"").Append(ContextEmitter.Escape(member.LocalName)).AppendLine("\")]");
                }

                var propType = member.ClrTypeName;
                if (!propType.EndsWith("?", StringComparison.Ordinal) && (member.IsCollection || member.IsNillable))
                {
                    propType += "?";
                }

                builder.Append("        public ").Append(propType).Append(' ').Append(member.ClrName).AppendLine(" { get; set; }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// Emits an enum from XSD enumerations.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="enumName">The enum name.</param>
        /// <param name="values">The values.</param>
        /// <returns>The C# source.</returns>
        internal static string EmitEnum(string ns, string enumName, List<string> values)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("namespace " + ns);
            builder.AppendLine("{");
            builder.AppendLine();
            builder.Append("    public enum ").Append(enumName);
            builder.AppendLine();
            builder.AppendLine("    {");
            foreach (var value in values)
            {
                builder.Append("        ").Append(Sanitize(value)).AppendLine(",");
            }

            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// Escapes text for an XML documentation summary.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The escaped text.</returns>
        internal static string EscapeXmlDoc(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");
        }

        #endregion

    }

}
