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

        /// <summary>
        /// The CLR type of a value that names its element, which a member holds when two of its elements have one type.
        /// </summary>
        internal const string NamedValue = "global::CloudNimble.Text.Xml.Serialization.XmlNamedValue";

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
        /// <remarks>
        /// A named complex type becomes a class named for the type. A global element of that type is the root
        /// name of that class; it gets no class of its own. A global element with an anonymous complex type becomes
        /// a class named for the element. A complex type that extends or restricts another named complex type
        /// becomes a class derived from that type's class, and the base class lists it as a derived type that
        /// <c>xsi:type</c> identifies.
        /// </remarks>
        internal static (List<(string Hint, string Source)> Pocos, ContextSpec? Context) EmitSchema(
            MiniElement root,
            string ns,
            string contextName,
            string fileName,
            HashSet<string> classNames,
            List<Diagnostic> diagnostics)
        {
            var targetNs = root.Attr("targetNamespace") ?? "";
            var pocos = new List<(string Hint, string Source)>();
            var typeSpecs = new List<TypeSpec>();
            var bind = new XsdBindContext(ns, fileName, targetNs, classNames, pocos, typeSpecs, diagnostics);
            var enumerations = new List<KeyValuePair<string, List<string>>>();
            var globalElements = new List<MiniElement>();
            foreach (var child in root.Elements)
            {
                if (child.NamespaceUri != Xs)
                {
                    continue;
                }

                if (child.LocalName is "redefine" or "notation")
                {
                    bind.Note("Schema '" + fileName + "' contains xs:" + child.LocalName + ", which is ignored.");
                    continue;
                }

                if (child.LocalName is "key" or "keyref" or "unique")
                {
                    bind.Note("Identity constraint xs:" + child.LocalName + " is ignored.");
                    continue;
                }

                var name = child.Attr("name");
                if (child.LocalName == "element")
                {
                    globalElements.Add(child);
                    if (name is { Length: > 0 })
                    {
                        bind.GlobalElements[name] = child;
                    }

                    if (name is { Length: > 0 } && child.Attr("substitutionGroup") is { Length: > 0 } head)
                    {
                        if (!bind.Substitutes.TryGetValue(LocalName(head), out var substitutes))
                        {
                            substitutes = new List<string>();
                            bind.Substitutes[LocalName(head)] = substitutes;
                        }

                        substitutes.Add(name);
                    }

                    continue;
                }

                if (name is not { Length: > 0 })
                {
                    continue;
                }

                if (child.LocalName == "complexType")
                {
                    if (!bind.ComplexTypes.ContainsKey(name))
                    {
                        bind.ComplexTypeOrder.Add(name);
                    }

                    bind.ComplexTypes[name] = child;
                }
                else if (child.LocalName == "simpleType")
                {
                    bind.SimpleTypes[name] = child;
                    var values = ReadEnumerations(child);
                    if (values.Count > 0)
                    {
                        enumerations.Add(new KeyValuePair<string, List<string>>(name, values));
                    }
                }
                else if (child.LocalName == "attribute")
                {
                    bind.GlobalAttributes[name] = child;
                }
                else if (child.LocalName == "group")
                {
                    bind.Groups[name] = child;
                }
                else if (child.LocalName == "attributeGroup")
                {
                    bind.AttributeGroups[name] = child;
                }
            }

            // A member that names no namespace takes the default namespace in scope. That is the target
            // namespace everywhere when every element is in it. When some element of a complex type is in no
            // namespace, the default namespace changes on the way down, so each member names its own.
            bind.ElementsQualified = root.Attr("elementFormDefault") == "qualified";
            bind.AttributesQualified = root.Attr("attributeFormDefault") == "qualified";
            bind.ExplicitNamespaces = targetNs.Length > 0 && (!bind.ElementsQualified || DeclaresUnqualifiedElement(root));
            foreach (var pair in enumerations)
            {
                bind.TypeMap[pair.Key] = "global::" + ns + "." + Sanitize(pair.Key);
                bind.EnumValues["global::" + ns + "." + Sanitize(pair.Key)] = pair.Value;
            }

            foreach (var typeName in bind.ComplexTypeOrder)
            {
                bind.TypeMap[typeName] = "global::" + ns + "." + Sanitize(typeName);
                if (NamedComplexBase(bind.ComplexTypes[typeName], bind) is { } baseName)
                {
                    bind.BaseOf[typeName] = baseName;
                }
            }

            // Every class a type derives from lists it, because a value declared as any of them may be of this type.
            foreach (var typeName in bind.ComplexTypeOrder)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal) { typeName };
                var current = typeName;
                while (bind.BaseOf.TryGetValue(current, out var baseName) && seen.Add(baseName))
                {
                    if (!bind.DerivedOf.TryGetValue(baseName, out var derived))
                    {
                        derived = new List<string>();
                        bind.DerivedOf[baseName] = derived;
                    }

                    derived.Add(typeName);
                    current = baseName;
                }
            }

            foreach (var typeName in bind.ComplexTypeOrder)
            {
                if (bind.DerivedOf.ContainsKey(typeName) || bind.ComplexTypes[typeName].Attr("abstract") == "true")
                {
                    bind.PolymorphicClr.Add(bind.TypeMap[typeName]);
                }
            }

            foreach (var element in globalElements)
            {
                if (element.Attr("name") is { Length: > 0 } elementName
                    && element.Attr("type") is { } elementType
                    && bind.ComplexTypes.ContainsKey(LocalName(elementType)))
                {
                    if (!bind.ElementsOfType.TryGetValue(LocalName(elementType), out var users))
                    {
                        users = new List<string>();
                        bind.ElementsOfType[LocalName(elementType)] = users;
                    }

                    users.Add(elementName);
                }
            }

            foreach (var pair in enumerations)
            {
                var enumName = Sanitize(pair.Key);
                if (!classNames.Add(enumName))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, enumName));
                    continue;
                }

                pocos.Add((ns + "." + enumName + ".g.cs", EmitEnum(ns, enumName, pair.Value, bind)));
                typeSpecs.Add(new TypeSpec(
                    "global::" + ns + "." + enumName,
                    enumName,
                    3,
                    true,
                    false,
                    false,
                    new EquatableArray<MemberSpec>([])));
            }

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


                if (element.Attr("type") is { } typeAttr)
                {
                    if (bind.ComplexTypes.ContainsKey(LocalName(typeAttr)))
                    {
                        EnsureType(LocalName(typeAttr), bind);
                    }

                    continue;
                }

                if (InlineComplexType(element) is not { } complex)
                {
                    continue;
                }

                var className = Sanitize(elementName!);
                if (!classNames.Add(className))
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, className));
                    continue;
                }

                bind.ElementClasses.Add(className);
                EmitClass(className, elementName!, complex, null, ReadDocumentation(element) ?? ReadDocumentation(complex), bind);
            }

            foreach (var typeName in bind.ComplexTypeOrder)
            {
                EnsureType(typeName, bind);
            }


            if (typeSpecs.Count == 0)
            {
                return (pocos, null);
            }

            // The runtime library asks the context for the type info of each collection a member holds, so the
            // collection types are listed too, with their item types named.
            var listed = new HashSet<string>(StringComparer.Ordinal);
            var objectCount = typeSpecs.Count;
            var hasMixed = false;
            var hasNamed = false;
            for (var i = 0; i < objectCount; i++)
            {
                foreach (var member in typeSpecs[i].Members)
                {
                    hasMixed |= member.IsMixed;
                    hasNamed |= (member.IsCollection ? member.ItemTypeName : member.ClrTypeName) == NamedValue;
                }
            }

            if (hasNamed)
            {
                // A member that holds named values is of this type, which the runtime library asks the context for.
                var named = new[]
                {
                    new MemberSpec("Name", "string", "Name", "", false, false, false, false, false, "", "", true, false, true, -1, -1, false, false),
                    new MemberSpec("Namespace", "string", "Namespace", "", false, false, false, false, false, "", "", true, false, true, -1, -1, false, false),
                    new MemberSpec("Value", "object", "Value", "", false, false, false, false, false, "", "", true, false, true, -1, -1, false, false),
                };
                typeSpecs.Add(new TypeSpec(
                    NamedValue,
                    "XmlNamedValue",
                    0,
                    false,
                    true,
                    false,
                    new EquatableArray<MemberSpec>(named),
                    metadata: MetadataBuilder.BuildFromMembers(NamedValue, named)));
            }

            if (hasMixed)
            {
                // The text of mixed content is read as XmlMixedText, which the runtime library asks the context for.
                const string mixedText = "global::CloudNimble.Text.Xml.Serialization.XmlMixedText";
                var textMember = new MemberSpec("Text", "string", "Text", "", false, true, false, false, false, "", "", true, false, true, -1, -1, false, false);
                typeSpecs.Add(new TypeSpec(
                    mixedText,
                    "XmlMixedText",
                    0,
                    false,
                    true,
                    false,
                    new EquatableArray<MemberSpec>([textMember]),
                    metadata: MetadataBuilder.BuildFromMembers(mixedText, [textMember])));
            }
            for (var i = 0; i < objectCount; i++)
            {
                foreach (var member in typeSpecs[i].Members)
                {
                    var declared = member.ClrTypeName.TrimEnd('?');

                    // The nullable form of an enumeration has type info of its own, which wraps that of the enumeration.
                    if (member.ClrTypeName != declared && IsEnumClr(declared, typeSpecs) && listed.Add(member.ClrTypeName))
                    {
                        typeSpecs.Add(new TypeSpec(member.ClrTypeName, TypeGraphWalker.RuntimeName(member.ClrTypeName), 5, true, true, false, new EquatableArray<MemberSpec>([]), "XmlMetadataServices.CreateNullableInfo<" + declared + ">(Options)"));
                    }

                    if (!member.IsCollection || member.IsExtensionData || !listed.Add(declared))
                    {
                        continue;
                    }

                    var factory = declared.EndsWith("[]", StringComparison.Ordinal)
                        ? "XmlMetadataServices.CreateArrayInfo<" + member.ItemTypeName + ">(Options)"
                        : "XmlMetadataServices.CreateCollectionInfo<" + declared + ", " + member.ItemTypeName + ">(Options)";
                    typeSpecs.Add(new TypeSpec(declared, TypeGraphWalker.RuntimeName(declared), 5, false, true, false, new EquatableArray<MemberSpec>([]), factory));
                }
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
        /// Writes the class of a named complex type, once, after the class of the type it derives from.
        /// </summary>
        /// <param name="typeName">The name of the complex type.</param>
        /// <param name="bind">The bind context.</param>
        internal static void EnsureType(string typeName, XsdBindContext bind)
        {
            // A type that is being written already is its own base, which a schema does not allow. It is written
            // without that base.
            if (bind.Emitted.Contains(typeName) || !bind.InProgress.Add(typeName))
            {
                return;
            }

            var complex = bind.ComplexTypes[typeName];
            var className = Sanitize(typeName);
            if (!bind.ClassNames.Add(className))
            {
                if (bind.ElementClasses.Contains(className))
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, className));
                }
                else
                {
                    // The class was written for a schema file that includes this one, or that this one includes.
                    // Its members are still needed here, for a class that derives from it.
                    if (bind.BaseOf.TryGetValue(typeName, out var baseName))
                    {
                        EnsureType(baseName, bind);
                    }

                    EmitClass(className, typeName, complex, typeName, null, bind.Detached());
                }
            }
            else
            {
                var rootName = typeName;
                if (bind.ElementsOfType.TryGetValue(typeName, out var users))
                {
                    rootName = users[0];
                    if (users.Count > 1)
                    {
                        bind.Warn("The elements '" + string.Join("', '", users) + "' share the type '" + typeName + "'. The generated class has one root name, '" + rootName + "'.");
                    }
                }

                EmitClass(className, rootName, complex, typeName, ReadDocumentation(complex), bind);
            }

            bind.InProgress.Remove(typeName);
            bind.Emitted.Add(typeName);
        }

        /// <summary>
        /// Writes one class and its type spec.
        /// </summary>
        /// <param name="className">The C# class name, which is already reserved.</param>
        /// <param name="rootName">The XML name of the element.</param>
        /// <param name="complex">The complex type.</param>
        /// <param name="typeName">The name of the complex type, or null for an anonymous one.</param>
        /// <param name="documentation">The documentation of the type, or null.</param>
        /// <param name="bind">The bind context.</param>
        internal static void EmitClass(string className, string rootName, MiniElement complex, string? typeName, string? documentation, XsdBindContext bind)
        {
            var inherited = new List<MemberSpec>();
            string? baseClr = null;
            if (NamedComplexBase(complex, bind) is { } baseName)
            {
                EnsureType(baseName, bind);
                if (bind.Members.TryGetValue(baseName, out var baseMembers))
                {
                    inherited = baseMembers;
                    baseClr = bind.TypeMap[baseName];
                }
            }

            var used = new HashSet<string>(StringComparer.Ordinal) { className };
            var inheritsMixed = false;
            foreach (var member in inherited)
            {
                used.Add(member.ClrName);
                inheritsMixed |= member.IsMixed;
            }

            var own = new List<MemberSpec>();
            var bound = BindType(complex, className, bind);
            foreach (var member in bound)
            {
                // A name the schema gives is taken before a name the generator made up is settled.
                if (member.Xsd is not { HasGeneratedName: true })
                {
                    used.Add(member.ClrName);
                }
            }

            var seen = new HashSet<string>(StringComparer.Ordinal) { className };
            foreach (var inheritedMember in inherited)
            {
                seen.Add(inheritedMember.ClrName);
            }

            foreach (var candidate in bound)
            {
                var member = candidate;
                if (member.IsMixed && inheritsMixed)
                {
                    // The base class holds the content of the element already.
                    continue;
                }

                if (member.Xsd is { HasGeneratedName: true })
                {
                    var ident = member.ClrName;
                    for (var suffix = 2; used.Contains(ident); suffix++)
                    {
                        ident = member.ClrName + suffix;
                    }

                    used.Add(ident);
                    seen.Add(ident);
                    own.Add(Renamed(member, ident));
                    continue;
                }

                // A property cannot have the name of its class, of another property, or of a property of a base
                // class. An element and an attribute of one name are the usual cause.
                if (!seen.Add(member.ClrName))
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, className + "." + member.ClrName));
                    continue;
                }

                own.Add(member);
            }

            own = InWriteOrder(own);
            var all = new List<MemberSpec>(inherited);
            all.AddRange(own);
            all = InWriteOrder(all);
            var isAbstract = complex.Attr("abstract") == "true";
            var derived = new List<KeyValuePair<string, string>>();
            if (typeName is not null && bind.DerivedOf.TryGetValue(typeName, out var derivedNames))
            {
                foreach (var derivedName in derivedNames)
                {
                    derived.Add(new KeyValuePair<string, string>(bind.TypeMap[derivedName], derivedName));
                }
            }

            var clrName = "global::" + bind.Namespace + "." + className;
            bind.Pocos.Add((bind.Namespace + "." + className + ".g.cs", EmitPoco(bind.Namespace, className, rootName, bind.TargetNamespace, own, documentation, baseClr, isAbstract, derived)));

            // The write fast path writes one element for a member, in the namespace in scope. Anything else is
            // written from the metadata: a value that may be of a derived class, a choice of elements, mixed
            // content, extension data, and an element or an item that names no namespace of its own.
            var fastPath = !isAbstract && derived.Count == 0;
            foreach (var member in all)
            {
                var valueType = TrimNullable(member.IsCollection ? member.ItemTypeName : member.ClrTypeName);
                fastPath &= !member.IsExtensionData
                    && !member.IsMixed
                    && !bind.PolymorphicClr.Contains(valueType)
                    && member.Xsd is not ({ Choices: not null } or { IsUnqualified: true } or { ItemNamespace: not null });
            }

            bind.TypeSpecs.Add(new TypeSpec(
                clrName,
                className,
                0,
                false,
                true,
                fastPath,
                new EquatableArray<MemberSpec>(all.ToArray()),
                metadata: MetadataBuilder.BuildFromMembers(clrName, all, isAbstract),
                fastPath: fastPath));
            if (typeName is not null)
            {
                bind.Members[typeName] = all;
            }
        }

        /// <summary>
        /// Returns a member the generator named, under another name.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="ident">The C# name, which is its XML name too: no element of the schema has it.</param>
        /// <returns>The member with the new name.</returns>
        internal static MemberSpec Renamed(MemberSpec member, string ident)
        {
            return new MemberSpec(
                new MemberSpec(
                    ident,
                    member.ClrTypeName,
                    ident,
                    member.NamespaceUri,
                    member.IsAttribute,
                    member.IsText,
                    member.IsCData,
                    member.IsCollection,
                    member.IsWrapped,
                    ident,
                    member.ItemTypeName,
                    member.IsNillable,
                    member.IsRequired,
                    member.CanSet,
                    member.IgnoreCondition,
                    member.Order,
                    member.IsExtensionData,
                    member.IsMixed),
                member.Xsd!);
        }

        /// <summary>
        /// Returns the named complex type a complex type extends or restricts.
        /// </summary>
        /// <param name="complex">The complex type.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The name of the base type, or null when the type derives from no complex type of the schema.</returns>
        internal static string? NamedComplexBase(MiniElement complex, XsdBindContext bind)
        {
            foreach (var content in complex.Elements)
            {
                if (content.NamespaceUri != Xs || content.LocalName is not ("complexContent" or "simpleContent"))
                {
                    continue;
                }

                foreach (var derivation in content.Elements)
                {
                    if (derivation.NamespaceUri == Xs
                        && derivation.LocalName is "extension" or "restriction"
                        && LocalName(derivation.Attr("base") ?? "") is { Length: > 0 } baseName
                        && bind.ComplexTypes.ContainsKey(baseName))
                    {
                        return baseName;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Binds what a complex type declares itself to members: its attributes and the elements of its content.
        /// </summary>
        /// <param name="complex">The complex type element.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The members, without those of a base type, in the order the schema declares them.</returns>
        /// <remarks>
        /// A type with mixed content has its attributes and one member that holds the text and the elements in
        /// the order of the document. The elements are not members of their own, because a member cannot say
        /// where among the text its element stood.
        /// </remarks>
        internal static List<MemberSpec> BindType(MiniElement complex, string owner, XsdBindContext bind)
        {
            var members = new List<MemberSpec>();
            var mixed = complex.Attr("mixed") == "true";
            MiniElement? content = complex;
            foreach (var child in complex.Elements)
            {
                if (child.NamespaceUri != Xs || child.LocalName is not ("complexContent" or "simpleContent"))
                {
                    continue;
                }

                mixed |= child.Attr("mixed") == "true";
                foreach (var derivation in child.Elements)
                {
                    if (derivation.NamespaceUri != Xs || derivation.LocalName is not ("extension" or "restriction"))
                    {
                        continue;
                    }

                    var baseName = LocalName(derivation.Attr("base") ?? "");
                    var isNamedComplex = bind.ComplexTypes.ContainsKey(baseName);
                    if (isNamedComplex && derivation.LocalName == "restriction")
                    {
                        // A restriction declares again what it keeps of its base, so it adds no member.
                        bind.Note("'" + owner + "' restricts '" + baseName + "'. It is mapped as a class derived from it, and what it restricts is not enforced.");
                        content = null;
                    }
                    else
                    {
                        content = derivation;
                        if (child.LocalName == "simpleContent" && !isNamedComplex)
                        {
                            members.Add(new MemberSpec("Value", MapXsdType(derivation.Attr("base") ?? "xs:string", bind), "Value", "", false, true, false, false, false, "", "", true, false, true, -1, -1, false, false));
                        }
                        else if (child.LocalName == "complexContent" && !isNamedComplex && baseName is not ("" or "anyType"))
                        {
                            bind.Warn("The base type '" + baseName + "' of '" + owner + "' is not a complex type of the schema, so its members are not generated.");
                        }
                    }
                }
            }

            if (mixed)
            {
                if (content is not null)
                {
                    BindAttributes(content, owner, bind, members);
                }

                const string item = "global::CloudNimble.Text.Xml.Serialization.XmlMixedContentItem";
                members.Add(new MemberSpec("Content", "global::System.Collections.Generic.List<" + item + ">", "Content", "", false, false, false, true, false, "", item, true, false, true, -1, -1, false, true));
            }
            else if (content is not null)
            {
                BindContent(content, owner, bind, members);
            }

            return members;
        }

        /// <summary>
        /// Binds the attributes of a complex type or an extension, and nothing else of it.
        /// </summary>
        /// <param name="content">The element that holds the attributes.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        internal static void BindAttributes(MiniElement content, string owner, XsdBindContext bind, List<MemberSpec> members)
        {
            var bound = new List<MemberSpec>();
            foreach (var child in content.Elements)
            {
                if (child.NamespaceUri != Xs)
                {
                    continue;
                }

                if (child.LocalName == "attribute")
                {
                    BindAttribute(child, bind, bound);
                }
                else if (child.LocalName == "attributeGroup")
                {
                    BindAttributeGroup(child, owner, bind, bound, new HashSet<string>(StringComparer.Ordinal));
                }
            }

            // An attribute group may hold xs:anyAttribute. The member for it is not an attribute, and mixed
            // content is the only member a mixed type has besides its attributes.
            members.AddRange(bound.FindAll(static member => member.IsAttribute));
        }

        /// <summary>
        /// Binds the children of a complex type, an extension, or an attribute group.
        /// </summary>
        /// <param name="content">The element that holds the particles and attributes.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        internal static void BindContent(MiniElement content, string owner, XsdBindContext bind, List<MemberSpec> members)
        {
            foreach (var child in content.Elements)
            {
                if (child.NamespaceUri != Xs || child.LocalName is "annotation" or "complexContent" or "simpleContent")
                {
                    continue;
                }

                if (child.LocalName is "key" or "keyref" or "unique")
                {
                    bind.Note("Identity constraint xs:" + child.LocalName + " is ignored.");
                }
                else if (child.LocalName is "sequence" or "choice" or "all")
                {
                    BindCompositor(child, owner, bind, members, false, false, new HashSet<string>(StringComparer.Ordinal));
                }
                else if (child.LocalName == "group")
                {
                    BindGroup(child, owner, bind, members, false, false, new HashSet<string>(StringComparer.Ordinal));
                }
                else if (child.LocalName == "attribute")
                {
                    BindAttribute(child, bind, members);
                }
                else if (child.LocalName == "attributeGroup")
                {
                    BindAttributeGroup(child, owner, bind, members, new HashSet<string>(StringComparer.Ordinal));
                }
                else if (child.LocalName == "anyAttribute")
                {
                    EnsureExtensionData(members);
                }
                else
                {
                    bind.Note("xs:" + child.LocalName + " in '" + owner + "' is ignored.");
                }
            }
        }

        /// <summary>
        /// Binds the particles of an <c>xs:sequence</c>, <c>xs:choice</c> or <c>xs:all</c>.
        /// </summary>
        /// <param name="compositor">The compositor.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        /// <param name="optional">Whether the particles may be absent because of what holds the compositor.</param>
        /// <param name="repeating">Whether the particles repeat because of what holds the compositor.</param>
        /// <param name="groups">The named groups being bound, which is how a group that holds itself is found.</param>
        /// <remarks>
        /// A choice, and a group that repeats, is one member when its particles are elements of different types:
        /// each element name stands for a type, and a list keeps the order of the document. When two of them have
        /// one type the type does not say which element a value is, so each element is a member of its own.
        /// </remarks>
        internal static void BindCompositor(MiniElement compositor, string owner, XsdBindContext bind, List<MemberSpec> members, bool optional, bool repeating, HashSet<string> groups)
        {
            // A choice of one element is that element.
            var isChoice = compositor.LocalName == "choice" && compositor.Elements.FindAll(static particle => particle.NamespaceUri == Xs && particle.LocalName != "annotation").Count > 1;
            var repeats = Repeats(compositor);
            optional |= compositor.Attr("minOccurs") == "0";
            if (!repeating && (isChoice || repeats) && TryBindAsOneMember(compositor, owner, bind, members, optional, repeats))
            {
                return;
            }

            if (isChoice)
            {
                bind.Note("xs:choice in '" + owner + "' is mapped as optional members, because its alternatives are not single elements.");
                optional = true;
            }

            if (repeats)
            {
                // One element that repeats is a list, and a list keeps its order.
                var particles = compositor.Elements.FindAll(static particle => particle.NamespaceUri == Xs && particle.LocalName != "annotation");
                if (!repeating && !(particles.Count == 1 && particles[0].LocalName == "element"))
                {
                    bind.Warn("A repeating xs:" + compositor.LocalName + " in '" + owner + "' is mapped as members that each repeat, because it holds a wildcard or one element name with two types. The order between them is not kept.");
                }

                repeating = true;
            }

            foreach (var particle in compositor.Elements)
            {
                if (particle.NamespaceUri != Xs || particle.LocalName == "annotation")
                {
                    continue;
                }

                if (particle.LocalName == "element")
                {
                    BindParticle(particle, owner, bind, members, optional, repeating);
                }
                else if (particle.LocalName == "any")
                {
                    EnsureExtensionData(members);
                }
                else if (particle.LocalName is "sequence" or "choice" or "all")
                {
                    BindCompositor(particle, owner, bind, members, optional, repeating, groups);
                }
                else if (particle.LocalName == "group")
                {
                    BindGroup(particle, owner, bind, members, optional, repeating, groups);
                }
                else
                {
                    bind.Note("xs:" + particle.LocalName + " in '" + owner + "' is ignored.");
                }
            }
        }

        /// <summary>
        /// Binds a choice, or a group that repeats, as one member that holds a choice of elements.
        /// </summary>
        /// <param name="compositor">The compositor.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        /// <param name="optional">Whether nothing of the compositor may be present.</param>
        /// <param name="isList">Whether the compositor repeats, so that the member is a list.</param>
        /// <returns>
        /// <see langword="false"/> when the compositor holds a wildcard, fewer than two elements, one element name
        /// with two types, or, for a choice that does not repeat, anything but single elements. Nothing is added then.
        /// </returns>
        /// <remarks>
        /// A value is written under the element of its type. When two of the elements have one CLR type, the type
        /// does not say which element a value is, so the member holds <c>XmlNamedValue</c>, where each value names
        /// its element. A group that repeats is one list whatever groups it holds inside, because the list keeps
        /// the order of the document, which is what separate members would lose.
        /// </remarks>
        internal static bool TryBindAsOneMember(MiniElement compositor, string owner, XsdBindContext bind, List<MemberSpec> members, bool optional, bool isList)
        {
            // First the elements are found. What is reported while finding them is reported again when the
            // particles become members of their own, so it is taken back if this does not work out.
            var reported = bind.Diagnostics.Count;
            var elements = new List<(MiniElement Particle, string Name, MiniElement Declaration, bool IsReference)>();
            var bound = TryCollectElements(compositor, owner, bind, elements, isList, new HashSet<string>(StringComparer.Ordinal));
            var found = bind.Diagnostics.Count;

            // Then their types. A class written for an anonymous type stays written, and so does what it reports.
            var alternatives = new List<XsdAlternative>();
            foreach (var (particle, name, declaration, isReference) in bound ? elements : new List<(MiniElement Particle, string Name, MiniElement Declaration, bool IsReference)>())
            {
                if (isReference && bind.Substitutes.ContainsKey(name))
                {
                    CollectSubstitutes(name, owner, bind, alternatives, new HashSet<string>(StringComparer.Ordinal));
                    continue;
                }

                var typeName = declaration.Attr("type") is { } type && bind.ComplexTypes.ContainsKey(LocalName(type)) ? LocalName(type) : null;
                alternatives.Add(new XsdAlternative(name, ElementNamespace(particle, isReference, bind), TrimNullable(ResolveElementType(declaration, isReference, owner, name, bind)), typeName));
            }

            // One element is an element: a sequence of one that repeats is a list of it, which needs no choice.
            if (!bound || !TryMergeAlternatives(alternatives) || alternatives.Count < 2)
            {
                bind.Diagnostics.RemoveRange(reported, found - reported);
                return false;
            }

            // The name is the generator's, so EmitClass makes it give way to the names the schema gives.
            var ident = isList ? "Items" : "Item";
            AddChoice(members, ident, ident, HaveDifferentTypes(alternatives) ? CommonType(alternatives, bind) : NamedValue, alternatives, isList);
            var added = members[members.Count - 1];
            added.Xsd!.HasGeneratedName = true;
            return true;
        }

        /// <summary>
        /// Collects the element particles of a compositor.
        /// </summary>
        /// <param name="compositor">The compositor.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="elements">The list the elements are added to.</param>
        /// <param name="isList">Whether the compositor repeats, so that the groups inside it are followed.</param>
        /// <param name="groups">The named groups being followed, which is how a group that holds itself is found.</param>
        /// <returns>
        /// <see langword="false"/> for a wildcard, an element that cannot be resolved, and, in a choice that does
        /// not repeat, an element that repeats or a group: one value cannot hold those.
        /// </returns>
        internal static bool TryCollectElements(
            MiniElement compositor,
            string owner,
            XsdBindContext bind,
            List<(MiniElement Particle, string Name, MiniElement Declaration, bool IsReference)> elements,
            bool isList,
            HashSet<string> groups)
        {
            foreach (var particle in compositor.Elements)
            {
                if (particle.NamespaceUri != Xs || particle.LocalName == "annotation")
                {
                    continue;
                }

                if (particle.LocalName == "element")
                {
                    if ((!isList && Repeats(particle)) || !TryResolveElement(particle, owner, bind, out var name, out var declaration, out var isReference))
                    {
                        return false;
                    }

                    elements.Add((particle, name, declaration, isReference));
                    continue;
                }

                if (!isList)
                {
                    return false;
                }

                if (particle.LocalName is "sequence" or "choice" or "all")
                {
                    if (!TryCollectElements(particle, owner, bind, elements, isList, groups))
                    {
                        return false;
                    }

                    continue;
                }

                // A reference to a named group stands for the particles of the group. Anything else (a wildcard)
                // has no element name to stand for a type.
                if (particle.LocalName != "group"
                    || !bind.Groups.TryGetValue(LocalName(particle.Attr("ref") ?? ""), out var group)
                    || !groups.Add(LocalName(particle.Attr("ref") ?? "")))
                {
                    return false;
                }

                if (!TryCollectElements(group, owner, bind, elements, isList, groups))
                {
                    return false;
                }

                groups.Remove(LocalName(particle.Attr("ref") ?? ""));
            }

            return true;
        }

        /// <summary>
        /// Makes each element name appear once among the alternatives.
        /// </summary>
        /// <param name="alternatives">The alternatives, which may name one element more than once.</param>
        /// <returns><see langword="false"/> when one element name has two types, so that a name would not say which.</returns>
        internal static bool TryMergeAlternatives(List<XsdAlternative> alternatives)
        {
            for (var index = alternatives.Count - 1; index > 0; index--)
            {
                var earlier = alternatives.FindIndex(alternative => alternative.Name == alternatives[index].Name && alternative.Namespace == alternatives[index].Namespace);
                if (earlier == index)
                {
                    continue;
                }

                if (alternatives[earlier].ClrTypeName != alternatives[index].ClrTypeName)
                {
                    return false;
                }

                alternatives.RemoveAt(index);
            }

            return true;
        }

        /// <summary>
        /// Adds the member that holds a choice of elements.
        /// </summary>
        /// <param name="members">The members.</param>
        /// <param name="ident">The C# name of the member.</param>
        /// <param name="localName">The XML name of the member, which a value of the member's own type is written under.</param>
        /// <param name="declaredClr">The CLR type every alternative can be assigned to.</param>
        /// <param name="alternatives">The elements the member may hold.</param>
        /// <param name="isList">Whether the member holds many.</param>
        internal static void AddChoice(List<MemberSpec> members, string ident, string localName, string declaredClr, List<XsdAlternative> alternatives, bool isList)
        {
            var member = isList
                ? new MemberSpec(ident, "global::System.Collections.Generic.List<" + declaredClr + ">", localName, "", false, false, false, true, false, localName, declaredClr, true, false, true, -1, -1, false, false)
                : new MemberSpec(ident, declaredClr, localName, "", false, false, false, false, false, localName, "", true, false, true, 3, -1, false, false);
            members.Add(new MemberSpec(member, new XsdMemberDetails { Choices = alternatives }));
        }

        /// <summary>
        /// Returns whether no two alternatives have one CLR type.
        /// </summary>
        /// <param name="alternatives">The alternatives.</param>
        /// <returns><see langword="true"/> when the type of a value says which element it is.</returns>
        internal static bool HaveDifferentTypes(List<XsdAlternative> alternatives)
        {
            var types = new HashSet<string>(StringComparer.Ordinal);
            foreach (var alternative in alternatives)
            {
                if (!types.Add(alternative.ClrTypeName))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the CLR type every alternative can be assigned to.
        /// </summary>
        /// <param name="alternatives">The alternatives.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>
        /// The class of the nearest complex type all of them derive from or are, or <c>object</c> when there is
        /// none or one of them is not of a named complex type.
        /// </returns>
        internal static string CommonType(List<XsdAlternative> alternatives, XsdBindContext bind)
        {
            List<string>? common = null;
            foreach (var alternative in alternatives)
            {
                if (alternative.TypeName is null)
                {
                    return "object";
                }

                var chain = new List<string>();
                for (var current = alternative.TypeName; current is not null && !chain.Contains(current); current = bind.BaseOf.TryGetValue(current, out var next) ? next : null)
                {
                    chain.Add(current);
                }

                common = common is null ? chain : common.FindAll(chain.Contains);
            }

            return common is { Count: > 0 } ? bind.TypeMap[common[0]] : "object";
        }

        /// <summary>
        /// Collects the elements that may stand where the head of a substitution group is referred to: the head,
        /// and every element that names it, or one that names it, as its substitution group.
        /// </summary>
        /// <param name="head">The name of the head element.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="alternatives">The list the elements are added to.</param>
        /// <param name="visited">The heads that have been followed.</param>
        /// <remarks>
        /// An element that is abstract cannot appear in a document, so it is not added; the elements that stand
        /// for it are.
        /// </remarks>
        internal static void CollectSubstitutes(string head, string owner, XsdBindContext bind, List<XsdAlternative> alternatives, HashSet<string> visited)
        {
            if (!visited.Add(head) || !bind.GlobalElements.TryGetValue(head, out var declaration))
            {
                return;
            }

            if (declaration.Attr("abstract") != "true")
            {
                var typeName = declaration.Attr("type") is { } type && bind.ComplexTypes.ContainsKey(LocalName(type)) ? LocalName(type) : null;
                alternatives.Add(new XsdAlternative(head, bind.ExplicitNamespaces ? bind.TargetNamespace : null, TrimNullable(ResolveElementType(declaration, true, owner, head, bind)), typeName));
            }

            if (bind.Substitutes.TryGetValue(head, out var substitutes))
            {
                foreach (var substitute in substitutes)
                {
                    CollectSubstitutes(substitute, owner, bind, alternatives, visited);
                }
            }
        }

        /// <summary>
        /// Binds the particles of the named group an <c>xs:group ref</c> names.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        /// <param name="optional">Whether the particles may be absent because of what holds the reference.</param>
        /// <param name="repeating">Whether the particles repeat because of what holds the reference.</param>
        /// <param name="groups">The named groups being bound.</param>
        internal static void BindGroup(MiniElement reference, string owner, XsdBindContext bind, List<MemberSpec> members, bool optional, bool repeating, HashSet<string> groups)
        {
            var name = LocalName(reference.Attr("ref") ?? "");
            if (!bind.Groups.TryGetValue(name, out var group))
            {
                bind.Warn("The group '" + name + "' that '" + owner + "' refers to is not in the schema, so its elements are not generated.");
                return;
            }

            if (!groups.Add(name))
            {
                return;
            }

            optional |= reference.Attr("minOccurs") == "0";
            if (Repeats(reference))
            {
                if (!repeating)
                {
                    bind.Warn("A repeating xs:group in '" + owner + "' is mapped as members that each repeat. The order between them is not kept.");
                }

                repeating = true;
            }

            foreach (var compositor in group.Elements)
            {
                if (compositor.NamespaceUri == Xs && compositor.LocalName is "sequence" or "choice" or "all")
                {
                    BindCompositor(compositor, owner, bind, members, optional, repeating, groups);
                }
            }

            groups.Remove(name);
        }

        /// <summary>
        /// Binds the attributes of the named group an <c>xs:attributeGroup ref</c> names.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        /// <param name="groups">The named groups being bound.</param>
        internal static void BindAttributeGroup(MiniElement reference, string owner, XsdBindContext bind, List<MemberSpec> members, HashSet<string> groups)
        {
            var name = LocalName(reference.Attr("ref") ?? "");
            if (!bind.AttributeGroups.TryGetValue(name, out var group))
            {
                bind.Warn("The attribute group '" + name + "' that '" + owner + "' refers to is not in the schema, so its attributes are not generated.");
                return;
            }

            if (!groups.Add(name))
            {
                return;
            }

            foreach (var child in group.Elements)
            {
                if (child.NamespaceUri != Xs)
                {
                    continue;
                }

                if (child.LocalName == "attribute")
                {
                    BindAttribute(child, bind, members);
                }
                else if (child.LocalName == "attributeGroup")
                {
                    BindAttributeGroup(child, owner, bind, members, groups);
                }
                else if (child.LocalName == "anyAttribute")
                {
                    EnsureExtensionData(members);
                }
            }

            groups.Remove(name);
        }

        /// <summary>
        /// Binds one element particle.
        /// </summary>
        /// <param name="particle">The particle.</param>
        /// <param name="owner">The owning class name.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        /// <param name="optional">Whether the element may be absent because of what holds it.</param>
        /// <param name="repeating">Whether the element repeats because of what holds it.</param>
        internal static void BindParticle(MiniElement particle, string owner, XsdBindContext bind, List<MemberSpec> members, bool optional, bool repeating)
        {
            if (particle.Attr("substitutionGroup") is { Length: > 0 })
            {
                bind.Note("substitutionGroup is ignored; use [XmlDerivedType] on the generated type.");
            }

            if (!TryResolveElement(particle, owner, bind, out var name, out var declaration, out var isReference))
            {
                return;
            }

            optional |= particle.Attr("minOccurs") == "0";
            var unbounded = repeating || Repeats(particle);
            var nillable = declaration.Attr("nillable") == "true";
            var ident = Sanitize(name);
            var elementNamespace = ElementNamespace(particle, isReference, bind);
            var details = new XsdMemberDetails { IsUnqualified = elementNamespace is { Length: 0 } };
            if (isReference && bind.Substitutes.ContainsKey(name))
            {
                // Where the head of a substitution group is allowed, so is each element of the group.
                var substitutes = new List<XsdAlternative>();
                CollectSubstitutes(name, owner, bind, substitutes, new HashSet<string>(StringComparer.Ordinal));
                if (substitutes.Count > 0)
                {
                    var headType = declaration.Attr("type") is { } type && bind.ComplexTypes.ContainsKey(LocalName(type)) ? LocalName(type) : null;
                    var declared = headType is not null && substitutes.TrueForAll(substitute => substitute.TypeName is not null && DerivesFrom(substitute.TypeName, headType, bind))
                        ? bind.TypeMap[headType]
                        : "object";

                    // One element that may stand there is that element, with its own type. Elements of one type
                    // are told apart by name, which a named value carries.
                    AddChoice(members, ident, name, substitutes.Count == 1 ? substitutes[0].ClrTypeName : HaveDifferentTypes(substitutes) ? declared : NamedValue, substitutes, unbounded);
                }

                // With no element left to stand there, the head being abstract, nothing can appear.
                return;
            }

            // The elements that may stand for the head of a substitution group are a choice, which the member of
            // a class holds. The wrapper element becomes a class then, as any other anonymous type does.
            if (!unbounded
                && !isReference
                && InlineComplexType(declaration) is { } wrapper
                && WrappedItem(wrapper) is { } item
                && !(item.Attr("ref") is { } itemRef && bind.Substitutes.ContainsKey(LocalName(itemRef)))
                && TryResolveElement(item, owner + "_" + name, bind, out var itemName, out var itemDeclaration, out var itemIsReference))
            {
                // An element that holds nothing but one repeating element is the wrapper of a collection.
                var itemClr = TrimNullable(ResolveElementType(itemDeclaration, itemIsReference, Sanitize(owner + "_" + name), itemName, bind));
                details.ItemNamespace = ElementNamespace(item, itemIsReference, bind);
                members.Add(new MemberSpec(
                    new MemberSpec(
                        ident,
                        "global::System.Collections.Generic.List<" + itemClr + ">",
                        name,
                        elementNamespace ?? "",
                        false,
                        false,
                        false,
                        true,
                        true,
                        itemName,
                        itemClr,
                        true,
                        false,
                        true,
                        nillable ? -1 : 3,
                        -1,
                        false,
                        false),
                    details));
                return;
            }

            var clrType = ResolveElementType(declaration, isReference, owner, name, bind);
            if (unbounded)
            {
                var itemType = TrimNullable(clrType);
                members.Add(new MemberSpec(
                    new MemberSpec(ident, "global::System.Collections.Generic.List<" + itemType + ">", name, elementNamespace ?? "", false, false, false, true, false, name, itemType, true, false, true, -1, -1, false, false),
                    details));
                return;
            }

            // A default applies to an element that is present and empty, and an element that may be absent stays
            // absent, so only an element that must be present starts with its default.
            if (!optional)
            {
                details.Initializer = Initializer(particle, declaration, clrType, name, bind);
            }
            else
            {
                NoteValueConstraint(particle, declaration, name, bind);
            }

            // A value type has no way to say that the element is absent or nil, so it becomes nullable when the
            // schema allows either. Only nillable="true" allows xsi:nil: a null that is not nillable is left out.
            var isValue = IsValueClr(clrType) || IsEnumClr(clrType, bind.TypeSpecs);
            var canBeNull = !isValue || nillable || optional;
            if (isValue && canBeNull)
            {
                clrType += "?";
            }

            members.Add(new MemberSpec(
                new MemberSpec(ident, clrType, name, elementNamespace ?? "", false, false, false, false, false, name, "", canBeNull, false, true, canBeNull && !nillable ? 3 : -1, -1, false, false),
                details));
        }

        /// <summary>
        /// Returns the namespace an element member names for itself.
        /// </summary>
        /// <param name="particle">The particle.</param>
        /// <param name="isReference">Whether the particle refers to a global element.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>
        /// <see langword="null"/> when members name no namespace, because every element of the schema is in the
        /// target namespace or there is none. Otherwise the target namespace, for a global element and for an
        /// element whose form is qualified, or an empty string for an element in no namespace.
        /// </returns>
        internal static string? ElementNamespace(MiniElement particle, bool isReference, XsdBindContext bind)
        {
            if (!bind.ExplicitNamespaces)
            {
                return null;
            }

            var qualified = isReference || (particle.Attr("form") is { } form ? form == "qualified" : bind.ElementsQualified);
            return qualified ? bind.TargetNamespace : "";
        }

        /// <summary>
        /// Returns whether a named complex type is another, or derives from it.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="baseName">The name of the type it may derive from.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns><see langword="true"/> when a value of the one can be assigned to the other.</returns>
        internal static bool DerivesFrom(string typeName, string baseName, XsdBindContext bind)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var current = typeName; seen.Add(current); current = bind.BaseOf[current])
            {
                if (current == baseName)
                {
                    return true;
                }

                if (!bind.BaseOf.ContainsKey(current))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether a schema declares an element of a complex type with <c>form="unqualified"</c>.
        /// </summary>
        /// <param name="element">The schema, or an element inside it.</param>
        /// <returns><see langword="true"/> when some element says it is in no namespace.</returns>
        internal static bool DeclaresUnqualifiedElement(MiniElement element)
        {
            foreach (var child in element.Elements)
            {
                if ((child.NamespaceUri == Xs && child.LocalName == "element" && child.Attr("form") == "unqualified") || DeclaresUnqualifiedElement(child))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the name and the declaration of an element particle, which a <c>ref</c> takes from a global element.
        /// </summary>
        /// <param name="particle">The particle.</param>
        /// <param name="owner">The owning class name, for diagnostics.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="name">Receives the XML name.</param>
        /// <param name="declaration">Receives the element that declares the type.</param>
        /// <param name="isReference">Receives whether the particle refers to a global element.</param>
        /// <returns><see langword="true"/> if the particle can be bound.</returns>
        internal static bool TryResolveElement(MiniElement particle, string owner, XsdBindContext bind, out string name, out MiniElement declaration, out bool isReference)
        {
            declaration = particle;
            isReference = false;
            name = particle.Attr("name") ?? "";
            if (string.IsNullOrWhiteSpace(name) && particle.Attr("ref") is { Length: > 0 } elementRef)
            {
                name = LocalName(elementRef);
                isReference = true;
                if (bind.GlobalElements.TryGetValue(name, out var global))
                {
                    declaration = global;
                }
                else
                {
                    bind.Warn("The element '" + name + "' that '" + owner + "' refers to is not in the schema. It is mapped as a string.");
                }


            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (!Naming.IsXmlName(name))
            {
                bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedInvalidXmlName, Location.None, name));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the CLR type of the value of an element.
        /// </summary>
        /// <param name="declaration">The element that declares the type.</param>
        /// <param name="isReference">Whether the declaration is a global element a particle refers to.</param>
        /// <param name="owner">The owning class name, which names the class of an anonymous type.</param>
        /// <param name="name">The XML name of the element.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The fully qualified CLR type name.</returns>
        internal static string ResolveElementType(MiniElement declaration, bool isReference, string owner, string name, XsdBindContext bind)
        {
            if (declaration.Attr("type") is { } typeName)
            {
                return MapXsdType(typeName, bind);
            }

            if (InlineComplexType(declaration) is { } inline)
            {
                if (isReference)
                {
                    // The global element has a class of its own, named for the element.
                    return "global::" + bind.Namespace + "." + Sanitize(name);
                }

                var nestedName = Sanitize(owner + "_" + name);
                if (bind.NestedClasses.Contains(nestedName))
                {
                    // The element has been looked at before, and its class is written.
                }
                else if (bind.ClassNames.Add(nestedName))
                {
                    bind.NestedClasses.Add(nestedName);
                    EmitClass(nestedName, name, inline, null, ReadDocumentation(inline), bind);
                }
                else
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, nestedName));
                }

                return "global::" + bind.Namespace + "." + nestedName;
            }

            foreach (var nested in declaration.Elements)
            {
                if (nested.NamespaceUri == Xs && nested.LocalName == "simpleType")
                {
                    return MapSimpleType(nested, bind, new HashSet<string>(StringComparer.Ordinal));
                }
            }

            return "string";
        }

        /// <summary>
        /// Binds an <c>xs:attribute</c>.
        /// </summary>
        /// <param name="attribute">The attribute particle.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="members">The members.</param>
        internal static void BindAttribute(MiniElement attribute, XsdBindContext bind, List<MemberSpec> members)
        {
            if (attribute.Attr("use") == "prohibited")
            {
                return;
            }

            var declaration = attribute;
            var name = attribute.Attr("name");

            // An attribute declared in a complex type is in no namespace unless its form is qualified. A global
            // attribute is in the target namespace, and so is a reference to one.
            var attributeNamespace = (attribute.Attr("form") is { } form ? form == "qualified" : bind.AttributesQualified) ? bind.TargetNamespace : "";
            if (string.IsNullOrWhiteSpace(name) && attribute.Attr("ref") is { Length: > 0 } attributeRef)
            {
                name = LocalName(attributeRef);
                attributeNamespace = bind.TargetNamespace;
                if (attributeRef.StartsWith("xml:", StringComparison.Ordinal))
                {
                    // xml:lang, xml:space and xml:base are declared by the XML namespace itself, and are text.
                    attributeNamespace = "http://www.w3.org/XML/1998/namespace";
                }
                else if (bind.GlobalAttributes.TryGetValue(name, out var global))
                {
                    declaration = global;
                }
                else
                {
                    bind.Warn("The attribute '" + name + "' that a ref names is not in the schema. It is mapped as a string.");
                }
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

            var clrType = "string";
            if (declaration.Attr("type") is { } typeName)
            {
                clrType = MapXsdType(typeName, bind);
            }
            else
            {
                foreach (var nested in declaration.Elements)
                {
                    if (nested.NamespaceUri == Xs && nested.LocalName == "simpleType")
                    {
                        clrType = MapSimpleType(nested, bind, new HashSet<string>(StringComparer.Ordinal));
                    }
                }
            }

            // An attribute is optional unless use="required", and a value type has no way to say it is absent.
            // An attribute with a default is never absent: a document that leaves it out means the default.
            var initializer = Initializer(attribute, declaration, clrType, name!, bind);
            var isValue = IsValueClr(clrType) || IsEnumClr(clrType, bind.TypeSpecs);
            var canBeNull = !isValue || (attribute.Attr("use") != "required" && initializer.Length == 0);
            if (isValue && canBeNull)
            {
                clrType += "?";
            }

            members.Add(new MemberSpec(
                new MemberSpec(Sanitize(name!), clrType, name!, attributeNamespace, true, false, false, false, false, "", "", canBeNull, false, true, -1, -1, false, false),
                new XsdMemberDetails { Initializer = initializer }));
        }

        /// <summary>
        /// Returns the C# expression for the <c>default</c> or <c>fixed</c> value of an element or attribute.
        /// </summary>
        /// <param name="use">The particle or attribute use.</param>
        /// <param name="declaration">The declaration it refers to, or itself.</param>
        /// <param name="clrType">The CLR type of the member, not nullable.</param>
        /// <param name="name">The XML name.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>
        /// The expression, or an empty string when there is no value or the value cannot be written as a C#
        /// constant of the type, which is reported.
        /// </returns>
        internal static string Initializer(MiniElement use, MiniElement declaration, string clrType, string name, XsdBindContext bind)
        {
            foreach (var constraint in new[] { "default", "fixed" })
            {
                if ((use.Attr(constraint) ?? declaration.Attr(constraint)) is not { } value)
                {
                    continue;
                }

                if (TryLiteral(clrType, value, bind, out var literal))
                {
                    return literal;
                }

                bind.Note("The " + constraint + " value '" + value + "' of '" + name + "' is not applied to the generated member.");
            }

            return "";
        }

        /// <summary>
        /// Writes a value of a schema as a C# constant of a CLR type.
        /// </summary>
        /// <param name="clrType">The CLR type.</param>
        /// <param name="value">The value, in the lexical form of the schema type.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="literal">Receives the C# expression.</param>
        /// <returns>
        /// <see langword="false"/> when the type has no constants (a date, a time, a GUID, a URI, binary data) or
        /// the value is not one of the type.
        /// </returns>
        internal static bool TryLiteral(string clrType, string value, XsdBindContext bind, out string literal)
        {
            var invariant = System.Globalization.CultureInfo.InvariantCulture;
            var text = value.Trim();
            literal = "";
            switch (clrType)
            {
                case "string":
                    literal = "\"" + ContextEmitter.Escape(value) + "\"";
                    return true;
                case "bool":
                    literal = text is "true" or "1" ? "true" : "false";
                    return text is "true" or "1" or "false" or "0";
                case "sbyte" or "short" or "int" or "long":
                    if (!long.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign, invariant, out var signed)
                        || (clrType == "sbyte" && (signed < sbyte.MinValue || signed > sbyte.MaxValue))
                        || (clrType == "short" && (signed < short.MinValue || signed > short.MaxValue))
                        || (clrType == "int" && (signed < int.MinValue || signed > int.MaxValue)))
                    {
                        return false;
                    }

                    literal = signed.ToString(invariant) + (clrType == "long" ? "L" : "");
                    return true;
                case "byte" or "ushort" or "uint" or "ulong":
                    if (!ulong.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign, invariant, out var unsigned)
                        || (clrType == "byte" && unsigned > byte.MaxValue)
                        || (clrType == "ushort" && unsigned > ushort.MaxValue)
                        || (clrType == "uint" && unsigned > uint.MaxValue))
                    {
                        return false;
                    }

                    literal = unsigned.ToString(invariant) + (clrType == "ulong" ? "UL" : clrType == "uint" ? "U" : "");
                    return true;
                case "decimal":
                    if (!decimal.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint, invariant, out var number))
                    {
                        return false;
                    }

                    literal = number.ToString(invariant) + "M";
                    return true;
                case "float" or "double":
                    var type = clrType == "float" ? "float" : "double";
                    if (text is "INF" or "-INF" or "NaN")
                    {
                        literal = type + (text == "INF" ? ".PositiveInfinity" : text == "-INF" ? ".NegativeInfinity" : ".NaN");
                        return true;
                    }

                    if (!double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out var real) || double.IsInfinity(real) || double.IsNaN(real))
                    {
                        return false;
                    }

                    literal = clrType == "float" ? ((float)real).ToString("R", invariant) + "F" : real.ToString("R", invariant) + "D";
                    return true;
                default:
                    if (bind.EnumValues.TryGetValue(clrType, out var values) && values.Contains(value))
                    {
                        literal = clrType + "." + Sanitize(value);
                        return true;
                    }

                    return false;
            }
        }

        /// <summary>
        /// Reports a <c>default</c> or <c>fixed</c> value, which the generated member does not have.
        /// </summary>
        /// <param name="use">The particle or attribute use.</param>
        /// <param name="declaration">The declaration it refers to, or itself.</param>
        /// <param name="name">The XML name.</param>
        /// <param name="bind">The bind context.</param>
        internal static void NoteValueConstraint(MiniElement use, MiniElement declaration, string name, XsdBindContext bind)
        {
            foreach (var constraint in new[] { "default", "fixed" })
            {
                if ((use.Attr(constraint) ?? declaration.Attr(constraint)) is { } value)
                {
                    bind.Note("The " + constraint + " value '" + value + "' of '" + name + "' is not applied to the generated member.");
                }
            }
        }

        /// <summary>
        /// Returns the anonymous complex type of an element declaration.
        /// </summary>
        /// <param name="element">The element declaration.</param>
        /// <returns>The <c>xs:complexType</c> child, or null.</returns>
        internal static MiniElement? InlineComplexType(MiniElement element)
        {
            foreach (var nested in element.Elements)
            {
                if (nested.NamespaceUri == Xs && nested.LocalName == "complexType")
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the one repeating element a complex type holds, when it holds nothing else.
        /// </summary>
        /// <param name="complex">An anonymous complex type.</param>
        /// <returns>
        /// The element particle, or null when the type has an attribute, mixed content, a base type, or a content
        /// other than a sequence or a choice of one element, where the element or the group repeats.
        /// </returns>
        internal static MiniElement? WrappedItem(MiniElement complex)
        {
            if (complex.Attr("mixed") == "true")
            {
                return null;
            }

            MiniElement? group = null;
            foreach (var child in complex.Elements)
            {
                if (child.NamespaceUri == Xs && child.LocalName == "annotation")
                {
                    continue;
                }

                if (group is not null || child.NamespaceUri != Xs || child.LocalName is not ("sequence" or "choice"))
                {
                    return null;
                }

                group = child;
            }

            MiniElement? item = null;
            foreach (var particle in group?.Elements ?? new List<MiniElement>())
            {
                if (particle.NamespaceUri == Xs && particle.LocalName == "annotation")
                {
                    continue;
                }

                if (item is not null || particle.NamespaceUri != Xs || particle.LocalName != "element")
                {
                    return null;
                }

                item = particle;
            }

            // The element repeats, or the group that holds it alone does. Either way the wrapper holds a list.
            return item is not null && (Repeats(item) || Repeats(group!)) ? item : null;
        }

        /// <summary>
        /// Returns whether a particle may occur more than once.
        /// </summary>
        /// <param name="particle">The particle.</param>
        /// <returns><see langword="true"/> when <c>maxOccurs</c> is <c>unbounded</c> or more than one.</returns>
        internal static bool Repeats(MiniElement particle)
        {
            var maxOccurs = particle.Attr("maxOccurs") ?? "1";
            return maxOccurs == "unbounded" || (int.TryParse(maxOccurs, out var max) && max > 1);
        }

        /// <summary>
        /// Returns members with the attributes first, then the text, then the elements, each in the order given.
        /// </summary>
        /// <param name="members">The members.</param>
        /// <returns>The members in the order they are written.</returns>
        internal static List<MemberSpec> InWriteOrder(List<MemberSpec> members)
        {
            // List<T>.Sort is not stable, and the order of the elements of a sequence is part of the schema.
            var ordered = new List<MemberSpec>(members.Count);
            for (var rank = 0; rank < 3; rank++)
            {
                foreach (var member in members)
                {
                    if ((member.IsAttribute ? 0 : member.IsText || member.IsCData ? 1 : 2) == rank)
                    {
                        ordered.Add(member);
                    }
                }
            }

            return ordered;
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

            members.Add(new MemberSpec("Extra", "global::CloudNimble.Text.Xml.Serialization.XmlExtensionData", "Extra", "", false, false, false, false, false, "", "", true, false, true, -1, -1, true, false));
        }

        /// <summary>
        /// Maps an XSD type name to a CLR type name.
        /// </summary>
        /// <param name="xsdType">The XSD type (possibly prefixed).</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The fully qualified CLR type name.</returns>
        internal static string MapXsdType(string xsdType, XsdBindContext bind)
        {
            return MapXsdType(xsdType, bind, new HashSet<string>(StringComparer.Ordinal));
        }

        /// <summary>
        /// Maps an XSD type name to a CLR type name.
        /// </summary>
        /// <param name="xsdType">The XSD type (possibly prefixed).</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="simpleTypes">The named simple types being mapped, which is how one that restricts itself is found.</param>
        /// <returns>The fully qualified CLR type name.</returns>
        internal static string MapXsdType(string xsdType, XsdBindContext bind, HashSet<string> simpleTypes)
        {
            var local = LocalName(xsdType);
            if (bind.TypeMap.TryGetValue(local, out var mapped))
            {
                return mapped;
            }

            // A named simple type that is not an enumeration has the CLR type of what it restricts.
            if (bind.SimpleTypes.TryGetValue(local, out var simpleType))
            {
                return simpleTypes.Add(local) ? MapSimpleType(simpleType, bind, simpleTypes) : "string";
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
        /// Maps a simple type definition to a CLR type name.
        /// </summary>
        /// <param name="simpleType">The <c>xs:simpleType</c> element.</param>
        /// <param name="bind">The bind context.</param>
        /// <param name="simpleTypes">The named simple types being mapped.</param>
        /// <returns>The CLR type of what the type restricts, or <c>string</c> for a list or a union.</returns>
        internal static string MapSimpleType(MiniElement simpleType, XsdBindContext bind, HashSet<string> simpleTypes)
        {
            foreach (var child in simpleType.Elements)
            {
                if (child.NamespaceUri != Xs || child.LocalName != "restriction")
                {
                    continue;
                }

                if (child.Attr("base") is { Length: > 0 } baseType)
                {
                    return MapXsdType(baseType, bind, simpleTypes);
                }

                foreach (var nested in child.Elements)
                {
                    if (nested.NamespaceUri == Xs && nested.LocalName == "simpleType")
                    {
                        return MapSimpleType(nested, bind, simpleTypes);
                    }
                }
            }

            return "string";
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
        /// Emits a POCO class.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="className">The class name.</param>
        /// <param name="rootName">The XML root name.</param>
        /// <param name="rootNs">The XML namespace.</param>
        /// <param name="members">The members the class declares itself.</param>
        /// <param name="documentation">Optional documentation.</param>
        /// <param name="baseClr">The CLR name of the base class, or null.</param>
        /// <param name="isAbstract">Whether the class is abstract.</param>
        /// <param name="derived">The CLR name and the schema type name of each class that derives from this one.</param>
        /// <returns>The C# source.</returns>
        internal static string EmitPoco(
            string ns,
            string className,
            string rootName,
            string rootNs,
            List<MemberSpec> members,
            string? documentation,
            string? baseClr,
            bool isAbstract,
            List<KeyValuePair<string, string>> derived)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using CloudNimble.Text.Xml;");
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
            if (derived.Count > 0)
            {
                // A value declared as this type may be of a type that extends it, which xsi:type names.
                builder.AppendLine("    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]");
                foreach (var pair in derived)
                {
                    builder.Append("    [XmlDerivedType<").Append(pair.Key).Append(">(\"").Append(ContextEmitter.Escape(pair.Value)).AppendLine("\")]");
                }
            }

            builder.Append("    public ").Append(isAbstract ? "abstract " : "").Append("partial class ").Append(className);
            if (baseClr is not null)
            {
                builder.Append(" : ").Append(baseClr);
            }

            builder.AppendLine();
            builder.AppendLine("    {");
            foreach (var member in members)
            {
                foreach (var attribute in MemberAttributes(member))
                {
                    builder.Append("        [").Append(attribute.ToAttribute()).AppendLine("]");
                }

                var propType = member.ClrTypeName;
                if (!propType.EndsWith("?", StringComparison.Ordinal) && (member.IsCollection || member.IsNillable))
                {
                    propType += "?";
                }

                builder.Append("        public ").Append(propType).Append(' ').Append(member.ClrName).Append(" { get; set; }");
                if (member.Xsd is { Initializer.Length: > 0 } details)
                {
                    builder.Append(" = ").Append(details.Initializer).Append(';');
                }

                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// Returns the mapping attributes of a member, which are written on its property and into the metadata.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The attributes, in the order they are written.</returns>
        internal static List<XsdAttributeSyntax> MemberAttributes(MemberSpec member)
        {
            var attributes = new List<XsdAttributeSyntax>();
            var elementNamespace = member.Xsd is { IsUnqualified: true } ? "" : member.NamespaceUri.Length > 0 ? member.NamespaceUri : null;
            if (member.IsExtensionData)
            {
                attributes.Add(new XsdAttributeSyntax("XmlExtensionData"));
            }
            else if (member.IsMixed)
            {
                attributes.Add(new XsdAttributeSyntax("XmlMixed"));
            }
            else if (member.IsAttribute)
            {
                attributes.Add(new XsdAttributeSyntax("XmlAttribute").Argument(member.LocalName).Namespace(elementNamespace));
            }
            else if (member.IsText)
            {
                attributes.Add(new XsdAttributeSyntax("XmlText"));
            }
            else if (member.Xsd is { Choices: { } choices })
            {
                foreach (var choice in choices)
                {
                    attributes.Add(new XsdAttributeSyntax("XmlElement", choice.ClrTypeName).Argument(choice.Name).Namespace(choice.Namespace));
                }
            }
            else if (member.IsCollection && member.IsWrapped)
            {
                attributes.Add(new XsdAttributeSyntax("XmlArray").Argument(member.LocalName).Namespace(elementNamespace));
                attributes.Add(new XsdAttributeSyntax("XmlArrayItem").Argument(member.ItemLocalName).Namespace(member.Xsd?.ItemNamespace));
            }
            else if (member.LocalName != member.ClrName || elementNamespace is not null)
            {
                attributes.Add(new XsdAttributeSyntax("XmlElement").Argument(member.LocalName).Namespace(elementNamespace));
            }

            if (member.IgnoreCondition == 3)
            {
                var ignore = new XsdAttributeSyntax("XmlIgnore");
                ignore.Properties.Add(new KeyValuePair<string, string>("Condition", "XmlIgnoreCondition.WhenWritingNull"));
                attributes.Add(ignore);
            }

            return attributes;
        }

        /// <summary>
        /// Emits an enum from XSD enumerations.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="enumName">The enum name.</param>
        /// <param name="values">The values.</param>
        /// <param name="bind">The bind context.</param>
        /// <returns>The C# source.</returns>
        internal static string EmitEnum(string ns, string enumName, List<string> values, XsdBindContext bind)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("using CloudNimble.Text.Xml.Serialization;");
            builder.AppendLine();
            builder.AppendLine("namespace " + ns);
            builder.AppendLine("{");
            builder.AppendLine();
            builder.Append("    public enum ").Append(enumName);
            builder.AppendLine();
            builder.AppendLine("    {");
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                var ident = Sanitize(value);
                if (!used.Add(ident))
                {
                    bind.Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.GeneratedNameCollision, Location.None, enumName + "." + ident));
                    continue;
                }

                if (ident != value)
                {
                    // The value is not a C# identifier, so the member carries the value the document holds.
                    builder.Append("        [XmlPropertyName(\"").Append(ContextEmitter.Escape(value)).AppendLine("\")]");
                }

                builder.Append("        ").Append(ident).AppendLine(",");
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
