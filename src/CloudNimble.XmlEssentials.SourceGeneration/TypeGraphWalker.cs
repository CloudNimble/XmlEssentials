using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Walks a type's serializable closure and builds <see cref="TypeSpec"/> values.
    /// </summary>
    internal static class TypeGraphWalker
    {

        #region Fields

        /// <summary>
        /// The assembly of the context that is being walked, whose internal members generated code can reach.
        /// </summary>
        /// <remarks>
        /// It is held per thread for the length of one walk, so that it does not have to be passed through every visit.
        /// </remarks>
        [System.ThreadStatic]
        private static IAssemblySymbol? Home;

        /// <summary>
        /// Whether the type whose members are being bound has something the write fast path cannot write: a member
        /// with its own converter, a member with derived types, mixed content, or extension data. Such a type is
        /// read and written from its metadata only.
        /// </summary>
        [System.ThreadStatic]
        private static bool FastPathBlocked;

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds specs for <paramref name="roots"/> and every nested object they reach.
        /// </summary>
        /// <param name="roots">The [XmlSerializable] types and their property names.</param>
        /// <param name="options">The source-generation options.</param>
        /// <param name="emitSerialization">Whether the write fast path is emitted.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <param name="home">The assembly the context is in, or null when no metadata is to be built.</param>
        /// <returns>The type specs.</returns>
        internal static EquatableArray<TypeSpec> Walk(
            List<(ITypeSymbol Type, string PropertyName)> roots,
            OptionsSpec options,
            bool emitSerialization,
            List<Diagnostic> diagnostics,
            IAssemblySymbol? home = null)
        {
            Home = home;
            var seen = new HashSet<string>();
            var specs = new List<TypeSpec>();
            foreach (var root in roots)
            {
                // A root that an earlier root already reached was listed as a type the runtime library handles.
                // It is listed again as a root, so that it gets its property on the context.
                var rootName = SymbolHelpers.FullName(SymbolHelpers.UnwrapNullable(root.Type));
                var reached = specs.FindIndex(spec => spec.Kind == 5 && spec.ClrTypeName == rootName);
                if (reached >= 0)
                {
                    specs.RemoveAt(reached);
                    seen.Remove(rootName);
                }

                Visit(root.Type, root.PropertyName, options, emitSerialization, seen, specs, diagnostics);
            }

            NameTypes(roots, specs);
            return new EquatableArray<TypeSpec>(specs.ToArray());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gives every type its final property name: a root keeps the name it was listed under, and no two
        /// types share a name.
        /// </summary>
        /// <param name="roots">The types listed on the context, with their property names.</param>
        /// <param name="specs">The specs, renamed in place.</param>
        /// <remarks>
        /// The name is the property on the context and part of the names of the generated members. A type reached
        /// before it is visited as a root has the name it was reached under, and two types in different
        /// namespaces, or two closed forms of one generic type, may want the same name. Root names are
        /// settled first; every other type that would repeat a name gets a number.
        /// </remarks>
        internal static void NameTypes(List<(ITypeSymbol Type, string PropertyName)> roots, List<TypeSpec> specs)
        {
            var used = new HashSet<string>(System.StringComparer.Ordinal);
            var isRoot = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var root in roots)
            {
                var clrName = SymbolHelpers.FullName(SymbolHelpers.UnwrapNullable(root.Type));
                var index = specs.FindIndex(spec => spec.ClrTypeName == clrName);
                if (index < 0 || !isRoot.Add(clrName))
                {
                    continue;
                }

                specs[index] = specs[index].WithPropertyName(root.PropertyName);
                used.Add(root.PropertyName);
            }

            for (var i = 0; i < specs.Count; i++)
            {
                if (isRoot.Contains(specs[i].ClrTypeName))
                {
                    continue;
                }

                var name = specs[i].PropertyName;
                for (var suffix = 2; !used.Add(name); suffix++)
                {
                    name = specs[i].PropertyName + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                specs[i] = specs[i].WithPropertyName(name);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is not a closed type: an unbound generic type, a type
        /// parameter, a type the compiler could not resolve, or a type built from one of those.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> when nothing can be generated for the type.</returns>
        internal static bool IsOpen(ITypeSymbol type)
        {
            if (type.TypeKind is TypeKind.TypeParameter or TypeKind.Error)
            {
                return true;
            }

            if (type is IArrayTypeSymbol array)
            {
                return IsOpen(array.ElementType);
            }

            if (type is not INamedTypeSymbol named)
            {
                return false;
            }

            if (named.IsUnboundGenericType)
            {
                return true;
            }

            foreach (var argument in named.TypeArguments)
            {
                if (IsOpen(argument))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the default property name of a type: its name, followed by the names of its type
        /// arguments, or by <c>Array</c> for an array.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name, such as <c>Order</c>, <c>PairInt32String</c>, or <c>OrderArray</c>.</returns>
        internal static string PropertyNameOf(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol array)
            {
                return PropertyNameOf(array.ElementType) + "Array";
            }

            if (type is not INamedTypeSymbol { IsGenericType: true } generic)
            {
                return type.Name;
            }

            var builder = new System.Text.StringBuilder(generic.Name);
            foreach (var argument in generic.TypeArguments)
            {
                builder.Append(PropertyNameOf(argument));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Visits <paramref name="type"/> if it has not been seen.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The type-info property name.</param>
        /// <param name="options">The options.</param>
        /// <param name="emitSerialization">Whether the write fast path is emitted.</param>
        /// <param name="seen">Visited CLR type names.</param>
        /// <param name="specs">The specs.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        internal static void Visit(
            ITypeSymbol type,
            string propertyName,
            OptionsSpec options,
            bool emitSerialization,
            HashSet<string> seen,
            List<TypeSpec> specs,
            List<Diagnostic> diagnostics)
        {
            type = SymbolHelpers.UnwrapNullable(type);
            var clrName = SymbolHelpers.FullName(type);
            if (!seen.Add(clrName))
            {
                return;
            }

            var location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;
            if (IsUnsupported(type, clrName))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, location, type.ToDisplayString()));
                return;
            }

            // The language refuses an unbound generic type as a type argument, and the compiler still hands the
            // generator the attribute. Nothing is generated from a type that is not closed.
            if (IsOpen(type))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, location, type.ToDisplayString()));
                return;
            }

            // A type with derived types is read and written from its metadata, never by the write fast path.
            var declaresDerived = DeclaresDerivedTypes(type);

            if (SymbolHelpers.GetAttribute(type, SymbolHelpers.BclXmlInclude) is not null
                || (SymbolHelpers.GetAttribute(type, SymbolHelpers.IncludeAttribute) is not null && type.TypeKind is TypeKind.Class or TypeKind.Interface or TypeKind.Struct))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UseXmlDerivedType, location, type.ToDisplayString()));
            }

            var kind = KindOf(type);
            IMethodSymbol? constructor = null;
            FastPathBlocked = false;
            var members = kind == 0 && type is INamedTypeSymbol objectType
                ? BindMembers(objectType, options, diagnostics, out constructor)
                : new EquatableArray<MemberSpec>([]);

            var blocked = FastPathBlocked || declaresDerived;

            // The metadata describes the type as it is declared. The runtime library applies the options to it.
            var accessors = new System.Text.StringBuilder();
            var metadata = kind == 0 && type is INamedTypeSymbol described && Home is not null
                ? MetadataBuilder.Build(described, constructor, Home, accessors)
                : "";

            specs.Add(new TypeSpec(
                clrName,
                propertyName,
                kind,
                type.IsValueType,
                !type.IsValueType || type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T,
                emitSerialization && kind == 0 && !blocked,
                members,
                RuntimeFactory(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)) ?? "",
                metadata,
                accessors.ToString(),
                kind == 0 && !blocked));

            if (kind == 2)
            {
                // A collection or a dictionary listed on the context is read and written by the runtime, which
                // asks the context for the types it holds. They are listed too, so that it has an answer.
                VisitReachable(type, options, emitSerialization, seen, specs, diagnostics);
            }

            // A derived type named on the type is read and written through the context too, so it is listed.
            VisitNamedTypes(type, options, emitSerialization, seen, specs, diagnostics);

            if (kind == 0 && type is INamedTypeSymbol declaring)
            {
                // Inherited members are serialized too, so their types are reachable as well.
                for (INamedTypeSymbol? current = declaring; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
                {
                    foreach (var member in current.GetMembers())
                    {
                        ITypeSymbol? nested = member switch
                        {
                            IPropertySymbol property => property.Type,
                            IFieldSymbol field => field.Type,
                            _ => null,
                        };
                        if (nested is null || member.IsStatic || member.IsImplicitlyDeclared || IsUnsupported(nested, SymbolHelpers.FullName(nested)))
                        {
                            continue;
                        }

                        VisitReachable(nested, options, emitSerialization, seen, specs, diagnostics);
                        VisitNamedTypes(member, options, emitSerialization, seen, specs, diagnostics);

                        // The runtime library reads the text of mixed content as XmlMixedText, which lives beside the
                        // attribute, and asks the context for its type info.
                        if (SymbolHelpers.GetAttribute(member, SymbolHelpers.MixedAttribute)?.AttributeClass is { } mixedAttribute)
                        {
                            foreach (var mixedText in mixedAttribute.ContainingNamespace.GetTypeMembers("XmlMixedText"))
                            {
                                VisitReachable(mixedText, options, emitSerialization, seen, specs, diagnostics);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Visits the types that the attributes on a type or a member name: a derived type, a typed element choice,
        /// and a typed array item.
        /// </summary>
        /// <param name="symbol">A type, a property, or a field.</param>
        /// <param name="options">The options.</param>
        /// <param name="emitSerialization">Whether the write fast path is emitted.</param>
        /// <param name="seen">Visited CLR type names.</param>
        /// <param name="specs">The specs.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <remarks>
        /// The runtime library asks the context for the type info of each of these when it binds the type, so a
        /// context that left one out would have no answer.
        /// </remarks>
        internal static void VisitNamedTypes(ISymbol symbol, OptionsSpec options, bool emitSerialization, HashSet<string> seen, List<TypeSpec> specs, List<Diagnostic> diagnostics)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } attributeClass
                    && attributeClass.Name is "XmlDerivedTypeAttribute" or "XmlElementAttribute" or "XmlArrayItemAttribute"
                    && attributeClass.ContainingNamespace.ToDisplayString() is "CloudNimble.Text.Xml.Serialization")
                {
                    VisitReachable(attributeClass.TypeArguments[0], options, emitSerialization, seen, specs, diagnostics);
                }
            }

            // The derived types of a closed type are inferred when the options or the type ask for it, which is
            // only known when the context runs, so the context lists them whenever the type is closed.
            if (symbol is INamedTypeSymbol closedType)
            {
                var inferred = new List<ITypeSymbol>();
                AddClosedDerivedTypes(closedType, inferred);
                foreach (var derived in inferred)
                {
                    VisitReachable(derived, options, emitSerialization, seen, specs, diagnostics);
                }
            }
        }

        /// <summary>
        /// Returns the <c>IsClosedTypeAttribute</c> the compiler puts on a class declared with the
        /// <see langword="closed"/> modifier, or null.
        /// </summary>
        /// <param name="symbol">A type or a member.</param>
        /// <returns>The attribute, which lists the derived types, or null when the symbol is not a closed type.</returns>
        internal static AttributeData? ClosedTypeAttribute(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is { Name: "IsClosedTypeAttribute" } attributeClass
                    && attributeClass.ContainingNamespace.ToDisplayString() is "System.Runtime.CompilerServices")
                {
                    return attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the derived types of a closed type that an instance can have, as the runtime library infers them.
        /// </summary>
        /// <param name="closedType">A type, which may not be closed.</param>
        /// <param name="found">The list the types are added to.</param>
        /// <remarks>
        /// A derived type that is closed itself has its own derived types added in its place. A generic derived
        /// type is listed without type arguments and is given the arguments of the closed type; one that takes
        /// another number of arguments, or does not derive from this closed type, is left out.
        /// </remarks>
        internal static void AddClosedDerivedTypes(INamedTypeSymbol closedType, List<ITypeSymbol> found)
        {
            if (ClosedTypeAttribute(closedType) is not { } attribute)
            {
                return;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key != "DerivedTypes" || argument.Value.Kind is not TypedConstantKind.Array)
                {
                    continue;
                }

                foreach (var listed in argument.Value.Values)
                {
                    if (listed.Value is not INamedTypeSymbol derived)
                    {
                        continue;
                    }

                    if (derived.IsUnboundGenericType)
                    {
                        if (derived.Arity != closedType.TypeArguments.Length)
                        {
                            continue;
                        }

                        derived = derived.OriginalDefinition.Construct([.. closedType.TypeArguments]);
                    }

                    if (!DerivesFrom(derived, closedType))
                    {
                        continue;
                    }

                    if (ClosedTypeAttribute(derived) is not null)
                    {
                        AddClosedDerivedTypes(derived, found);
                    }
                    else
                    {
                        found.Add(derived);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> has <paramref name="baseType"/> among its base types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="baseType">The base type to look for.</param>
        /// <returns><see langword="true"/> when the type derives from the base type.</returns>
        internal static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Visits the object types a member's value can hold: the type itself, or the types inside a
        /// collection or a dictionary, at any depth.
        /// </summary>
        /// <param name="type">The member type.</param>
        /// <param name="options">The options.</param>
        /// <param name="emitSerialization">Whether the write fast path is emitted.</param>
        /// <param name="seen">Visited CLR type names.</param>
        /// <param name="specs">The specs.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        internal static void VisitReachable(
            ITypeSymbol type,
            OptionsSpec options,
            bool emitSerialization,
            HashSet<string> seen,
            List<TypeSpec> specs,
            List<Diagnostic> diagnostics)
        {
            // The nullable form of an enumeration or a struct has a converter of its own, which wraps the
            // one for the value. The built-in scalars have theirs ready-made.
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                var underlying = nullable.TypeArguments[0];
                if (!IsUnsupported(underlying, SymbolHelpers.FullName(underlying)) && (underlying.TypeKind is TypeKind.Enum || !SymbolHelpers.IsScalar(underlying)))
                {
                    AddRuntimeType(type, "XmlMetadataServices.CreateNullableInfo<" + SymbolHelpers.FullName(underlying) + ">(Options)", seen, specs);
                }
            }

            type = SymbolHelpers.UnwrapNullable(type).WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            if (IsUnsupported(type, SymbolHelpers.FullName(type)))
            {
                return;
            }

            if (TryDictionaryTypes(type, out var keyType, out var valueType))
            {
                VisitReachable(keyType, options, emitSerialization, seen, specs, diagnostics);
                VisitReachable(valueType, options, emitSerialization, seen, specs, diagnostics);
                AddRuntimeType(type, RuntimeFactory(type)!, seen, specs);
                return;
            }

            if (TryCollectionElement(type, out var element))
            {
                VisitReachable(element, options, emitSerialization, seen, specs, diagnostics);
                AddRuntimeType(type, RuntimeFactory(type)!, seen, specs);
                return;
            }

            var kind = KindOf(type);
            if (kind == 3)
            {
                AddRuntimeType(type, RuntimeFactory(type)!, seen, specs);
            }
            else if (kind == 0)
            {
                Visit(type, PropertyNameOf(type), options, emitSerialization, seen, specs, diagnostics);
            }
        }

        /// <summary>
        /// Lists a type that the runtime library reads and writes, with the expression that creates its type
        /// info, so that the context answers for it without reflection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="factory">The expression that creates the type info.</param>
        /// <param name="seen">Visited CLR type names.</param>
        /// <param name="specs">The specs.</param>
        /// <remarks>
        /// Such a type gets no property on the context. It is found through <c>GetTypeInfo</c>, which is how the
        /// write fast path and the runtime library ask for the type of a member, an item, or a value.
        /// </remarks>
        internal static void AddRuntimeType(ITypeSymbol type, string factory, HashSet<string> seen, List<TypeSpec> specs)
        {
            var clrName = SymbolHelpers.FullName(type);
            if (!seen.Add(clrName))
            {
                return;
            }

            specs.Add(new TypeSpec(clrName, RuntimeName(clrName), 5, type.IsValueType, true, false, new EquatableArray<MemberSpec>([]), factory));
        }

        /// <summary>
        /// Makes the name of the private member that holds the type info of a type the context does not list as a root.
        /// </summary>
        /// <param name="clrName">The fully qualified type name.</param>
        /// <returns>An identifier that is the same for the same type in every build, and differs between types.</returns>
        internal static string RuntimeName(string clrName)
        {
            var builder = new System.Text.StringBuilder("__Runtime_");
            var hash = 2166136261u;
            foreach (var character in clrName)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
                hash = (hash ^ character) * 16777619u;
            }

            return builder.Append('_').Append(hash.ToString("x8", System.Globalization.CultureInfo.InvariantCulture)).ToString();
        }

        /// <summary>
        /// Returns the expression that creates type info for a dictionary, a collection, or an enumeration
        /// with every type argument named, so that no generic type is closed at run time.
        /// </summary>
        /// <param name="type">The type, with no nullable annotation.</param>
        /// <returns>The expression, or null for any other kind of type.</returns>
        internal static string? RuntimeFactory(ITypeSymbol type)
        {
            var name = SymbolHelpers.FullName(type);
            if (TryDictionaryTypes(type, out var keyType, out var valueType))
            {
                return "XmlMetadataServices.CreateDictionaryInfo<" + name + ", " + SymbolHelpers.FullName(keyType) + ", " + SymbolHelpers.FullName(valueType) + ">(Options)";
            }

            if (TryCollectionElement(type, out var element))
            {
                // An array has its own entry point: the one for other collection types keeps their
                // constructors and methods for the trimmer, and an array's include ones Native AOT refuses.
                return type is IArrayTypeSymbol
                    ? "XmlMetadataServices.CreateArrayInfo<" + SymbolHelpers.FullName(element) + ">(Options)"
                    : "XmlMetadataServices.CreateCollectionInfo<" + name + ", " + SymbolHelpers.FullName(element) + ">(Options)";
            }

            return type.TypeKind is TypeKind.Enum && KindOf(type) == 3
                ? "XmlMetadataServices.CreateEnumInfo<" + name + ">(Options)"
                : null;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is read and written by the runtime library whatever a
        /// context lists: <see cref="object"/>, the types that hold XML, and a type with its own converter.
        /// </summary>
        /// <param name="type">The type, with <c>Nullable&lt;T&gt;</c> removed.</param>
        /// <returns><see langword="true"/> when nothing is generated for the type and none of its members are walked.</returns>
        internal static bool IsRuntimeType(ITypeSymbol type)
        {
            return type.SpecialType is SpecialType.System_Object
                || SymbolHelpers.FullName(type) is "global::CloudNimble.Text.Xml.XmlElement"
                    or "global::CloudNimble.Text.Xml.XmlDocument"
                    or "global::CloudNimble.Text.Xml.Nodes.XmlNode"
                    or "global::CloudNimble.Text.Xml.Nodes.XmlElementNode"
                || SymbolHelpers.GetAttribute(type, SymbolHelpers.ConverterAttribute) is not null;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a dictionary, and its key and value types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="keyType">The key type.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns><see langword="true"/> for a type that is or implements <c>IDictionary&lt;TKey, TValue&gt;</c> or <c>IReadOnlyDictionary&lt;TKey, TValue&gt;</c>.</returns>
        internal static bool TryDictionaryTypes(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
        {
            keyType = type;
            valueType = type;
            if (type is not INamedTypeSymbol named)
            {
                return false;
            }

            if (IsDictionaryInterface(named))
            {
                keyType = named.TypeArguments[0];
                valueType = named.TypeArguments[1];
                return true;
            }

            foreach (var implemented in named.AllInterfaces)
            {
                if (IsDictionaryInterface(implemented))
                {
                    keyType = implemented.TypeArguments[0];
                    valueType = implemented.TypeArguments[1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is one of the two generic dictionary interfaces.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> for <c>IDictionary&lt;TKey, TValue&gt;</c> and <c>IReadOnlyDictionary&lt;TKey, TValue&gt;</c>.</returns>
        internal static bool IsDictionaryInterface(INamedTypeSymbol type)
        {
            return type.TypeArguments.Length == 2
                && type.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.IDictionary<TKey, TValue>" or "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>";
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> cannot be serialized (pointers, <c>IntPtr</c>).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="clrName">The fully qualified name.</param>
        /// <returns><see langword="true"/> when XMLESS002 applies.</returns>
        internal static bool IsUnsupported(ITypeSymbol type, string clrName)
        {
            if (type is IPointerTypeSymbol || type.TypeKind is TypeKind.Pointer)
            {
                return true;
            }

            if (type.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
            {
                return true;
            }

            return clrName is "global::System.IntPtr" or "global::System.UIntPtr" or "nint" or "nuint";
        }

        /// <summary>
        /// Returns 0 for objects, 1 for scalars, 2 for collections and dictionaries, 3 for enums, and 4 for
        /// the types the runtime library reads and writes itself.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The kind.</returns>
        internal static int KindOf(ITypeSymbol type)
        {
            var root = SymbolHelpers.UnwrapNullable(type);

            // A type with its own converter is the runtime library's whatever else it is, an enumeration included.
            if (IsRuntimeType(root))
            {
                return 4;
            }

            if (root.TypeKind is TypeKind.Enum)
            {
                return 3;
            }

            if (SymbolHelpers.IsScalar(root))
            {
                return 1;
            }

            if (root is IArrayTypeSymbol || Implements(root, "System.Collections.Generic.IEnumerable"))
            {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> implements an interface whose metadata name starts with <paramref name="prefix"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="prefix">The interface metadata name prefix.</param>
        /// <returns><see langword="true"/> when it implements the interface.</returns>
        internal static bool Implements(ITypeSymbol type, string prefix)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (implemented.OriginalDefinition.ToDisplayString().StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The element naming policy, falling back to the property policy.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The policy value.</returns>
        internal static int ElementPolicy(OptionsSpec options)
        {
            return options.ElementNamingPolicy != 0 ? options.ElementNamingPolicy : options.PropertyNamingPolicy;
        }

        /// <summary>
        /// The attribute naming policy, falling back to the property policy.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The policy value.</returns>
        internal static int AttributePolicy(OptionsSpec options)
        {
            return options.AttributeNamingPolicy != 0 ? options.AttributeNamingPolicy : options.PropertyNamingPolicy;
        }

        /// <summary>
        /// Binds serializable members of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The options.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <param name="constructor">The constructor the object is created through when it has parameters; otherwise null.</param>
        /// <returns>The members in write order.</returns>
        internal static EquatableArray<MemberSpec> BindMembers(INamedTypeSymbol type, OptionsSpec options, List<Diagnostic> diagnostics, out IMethodSymbol? constructor)
        {
            var members = new List<MemberSpec>();
            var stack = new Stack<INamedTypeSymbol>();
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                stack.Push(current);
            }

            // Which of two members with one name is serialized is decided by the runtime library, from the metadata.
            // The write fast path has its members compiled in, so a type with such members goes without one.
            var declared = new HashSet<string>(System.StringComparer.Ordinal);
            var index = 0;
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol or IFieldSymbol && !member.IsStatic && !member.IsImplicitlyDeclared && !declared.Add(member.Name))
                    {
                        FastPathBlocked = true;
                    }

                    if (TryBindMember(member, options, index, diagnostics, out var spec))
                    {
                        members.Add(spec);
                        index++;
                    }
                }
            }

            var textCount = 0;
            foreach (var member in members)
            {
                if (member.IsText || member.IsCData)
                {
                    textCount++;
                }
            }

            if (textCount > 1)
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.MultipleTextMembers, type.Locations[0], type.ToDisplayString()));
            }

            // The sort is stable, so members that tie keep the order they were declared in, as the reflection
            // resolver has it. List<T>.Sort is not stable, and reorders ties once a type has more than a few members.
            var sorted = System.Linq.Enumerable.ToList(
                System.Linq.Enumerable.ThenBy(
                    System.Linq.Enumerable.OrderBy(members, static member => Rank(member)),
                    static member => member.Order >= 0 ? member.Order : int.MaxValue));
            members.Clear();
            members.AddRange(sorted);

            foreach (var member in members)
            {
                if (!Naming.IsXmlName(member.LocalName) && !member.IsText && !member.IsExtensionData && !member.IsMixed)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.InvalidXmlName, type.Locations[0], member.LocalName, type.ToDisplayString() + "." + member.ClrName));
                }
            }

            BindConstructor(type, members, diagnostics, out constructor);

            foreach (var member in members)
            {
                // A member that is never read takes nothing from the document, so it cannot be required. A
                // constructor parameter of the same name is still filled.
                if (!member.IsExtensionData && member.IsRequired && member.IgnoreCondition == 5 && member.ConstructorParameter is null)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.RequiredMemberNeverRead, type.Locations[0], type.ToDisplayString() + "." + member.ClrName));
                }
            }

            return new EquatableArray<MemberSpec>(members.ToArray());
        }

        /// <summary>
        /// Chooses the constructor the generated metadata calls, by the rules the reflection resolver uses,
        /// and records on each member the parameter it fills.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="members">The members. A member that fills a parameter is replaced by a copy that says so.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <remarks>
        /// A constructor marked <c>[XmlConstructor]</c> is used. Otherwise the public parameterless constructor
        /// is used, and a value type is created as its default. Otherwise the one public constructor with
        /// parameters is used, as in System.Text.Json and in the reflection resolver. A parameter no
        /// member fills must have a default value. XMLESS005 is reported when the choice is ambiguous, and
        /// XMLESS002 when no constructor can be used. A constructor that is not accessible is called through an accessor.
        /// </remarks>
        internal static void BindConstructor(INamedTypeSymbol type, List<MemberSpec> members, List<Diagnostic> diagnostics, out IMethodSymbol? chosenConstructor)
        {
            chosenConstructor = null;
            // A parameter fills the member with the same C# name or XML name, without regard to case, as the
            // reflection resolver has it: a parameter is conventionally "name" where its property is "Name".
            var names = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var member in members)
            {
                names.Add(member.ClrName);
                names.Add(member.LocalName);
            }

            var ignored = IgnoredMembers(type);
            IMethodSymbol? marked = null;
            var markedCount = 0;
            IMethodSymbol? parameterless = null;
            IMethodSymbol? parameterized = null;
            var parameterizedCount = 0;
            var hasParameterless = false;
            foreach (var constructor in type.InstanceConstructors)
            {
                if (SymbolHelpers.GetAttribute(constructor, SymbolHelpers.ConstructorAttribute) is not null)
                {
                    marked = constructor;
                    markedCount++;
                }

                if (constructor.Parameters.Length == 0)
                {
                    hasParameterless |= constructor.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal);
                }

                if (constructor.DeclaredAccessibility is not Accessibility.Public)
                {
                    continue;
                }

                if (constructor.Parameters.Length == 0)
                {
                    parameterless = constructor;
                    continue;
                }

                parameterized = constructor;
                parameterizedCount++;
            }

            // A key and value pair has no setters and no attribute could be put on its constructor, so it is
            // created through the one it has.
            var isPair = type.ConstructedFrom.ToDisplayString() is "System.Collections.Generic.KeyValuePair<TKey, TValue>";
            var usesParameterless = marked is null && !isPair && (parameterless is not null || type.IsValueType);
            if (markedCount > 1 || (marked is null && !usesParameterless && parameterizedCount > 1))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.AmbiguousConstructors, type.Locations[0], type.ToDisplayString()));
                return;
            }

            var chosen = marked ?? (usesParameterless ? null : parameterized);
            if (chosen is null && !hasParameterless && type is { IsAbstract: false, TypeKind: TypeKind.Class })
            {
                // There is no public constructor, and no parameterless one the generated code may call, so there
                // is nothing the metadata can create the object with.
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, type.Locations[0], type.ToDisplayString()));
                return;
            }

            chosenConstructor = chosen is { Parameters.Length: > 0 } ? chosen : null;
            if (chosen is null || chosen.Parameters.Length == 0)
            {
                return;
            }

            foreach (var parameter in chosen.Parameters)
            {
                // A member spelled exactly like the parameter is preferred, so two members that differ only by
                // case each take their own parameter.
                var index = members.FindIndex(member => (member.ClrName == parameter.Name || member.LocalName == parameter.Name) && member.ConstructorParameter is null);
                if (index < 0)
                {
                    index = members.FindIndex(member =>
                        (string.Equals(member.ClrName, parameter.Name, System.StringComparison.OrdinalIgnoreCase) || string.Equals(member.LocalName, parameter.Name, System.StringComparison.OrdinalIgnoreCase))
                        && member.ConstructorParameter is null);
                }

                if (index < 0 && ignored.Contains(parameter.Name) && !names.Contains(parameter.Name))
                {
                    // The parameter belongs to a member that is never serialized, so the document cannot
                    // supply it. It takes the default of its type, as the reflection resolver passes it.
                    continue;
                }

                if (index < 0)
                {
                    // No member fills it, or the member already fills an earlier parameter. It is left out
                    // of the call, which only a parameter with a default value allows.
                    if (!parameter.HasExplicitDefaultValue)
                    {
                        diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, type.Locations[0], type.ToDisplayString()));
                        return;
                    }

                    continue;
                }

                members[index] = new MemberSpec(members[index], parameter.Name, members[index].NullGetOwner);
            }
        }

        /// <summary>
        /// Collects the names of the properties and fields of <paramref name="type"/> and its base types that
        /// are marked <c>[XmlIgnore]</c> with no condition, so they are never serialized.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The names, compared without regard to case as constructor parameters are.</returns>
        internal static HashSet<string> IgnoredMembers(INamedTypeSymbol type)
        {
            var ignored = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol or IFieldSymbol
                        && SymbolHelpers.GetAttribute(member, SymbolHelpers.IgnoreAttribute) is { } attribute
                        && SymbolHelpers.GetInt(attribute, "Condition", 1) == 1)
                    {
                        ignored.Add(member.Name);
                    }
                }
            }

            return ignored;
        }

        /// <summary>
        /// Returns whether <paramref name="symbol"/> carries <c>[XmlDerivedType]</c> in either of its forms, or is
        /// a closed type, whose derived types may be inferred.
        /// </summary>
        /// <param name="symbol">A type or a member.</param>
        /// <returns><see langword="true"/> when derived types are declared on it, or may be inferred for it.</returns>
        internal static bool DeclaresDerivedTypes(ISymbol symbol)
        {
            if (ClosedTypeAttribute(symbol) is not null)
            {
                return true;
            }

            foreach (var attribute in symbol.GetAttributes())
            {
                // The attribute has a generic form, XmlDerivedTypeAttribute<T>, whose definition has the same name.
                if (attribute.AttributeClass?.OriginalDefinition is { Name: "XmlDerivedTypeAttribute" } definition
                    && definition.ContainingNamespace.ToDisplayString() is "CloudNimble.Text.Xml.Serialization")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether a member names more than one type with <c>[XmlElement&lt;T&gt;]</c> or with
        /// <c>[XmlArrayItem&lt;T&gt;]</c>, which makes it a choice of elements.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns><see langword="true"/> for a choice.</returns>
        internal static bool HasElementChoice(ISymbol member)
        {
            var elements = 0;
            var items = 0;
            foreach (var attribute in member.GetAttributes())
            {
                if (attribute.AttributeClass is not { IsGenericType: true } attributeClass
                    || attributeClass.ContainingNamespace.ToDisplayString() is not "CloudNimble.Text.Xml.Serialization")
                {
                    continue;
                }

                elements += attributeClass.Name == "XmlElementAttribute" ? 1 : 0;
                items += attributeClass.Name == "XmlArrayItemAttribute" ? 1 : 0;
            }

            return elements > 1 || items > 1;
        }

        /// <summary>
        /// Returns whether a member's element or wrapper is put in no namespace, with an empty string.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns><see langword="true"/> when <c>[XmlElement]</c> or <c>[XmlArray]</c> gives an empty namespace.</returns>
        internal static bool HasEmptyNamespace(ISymbol member)
        {
            foreach (var attribute in member.GetAttributes())
            {
                if (attribute.AttributeClass is { Name: "XmlElementAttribute" or "XmlArrayAttribute" } attributeClass
                    && attributeClass.ContainingNamespace.ToDisplayString() is "CloudNimble.Text.Xml.Serialization"
                    && (SymbolHelpers.GetString(attribute, "Namespace") ?? SymbolHelpers.GetConstructorString(attribute, 1)) is { Length: 0 })
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether a member of type <paramref name="type"/> holds values that are written as derived types.
        /// </summary>
        /// <param name="type">The type of a member.</param>
        /// <returns>
        /// <see langword="true"/> when the type, the type of its items, or a base type of either declares derived
        /// types.
        /// </returns>
        /// <remarks>
        /// The runtime library chooses the derived type for each value. The write fast path writes the declared
        /// type, so a type with such a member goes without one.
        /// </remarks>
        internal static bool HoldsDerivedTypes(ITypeSymbol type)
        {
            type = SymbolHelpers.UnwrapNullable(type);
            if (TryCollectionElement(type, out var element))
            {
                type = SymbolHelpers.UnwrapNullable(element);
            }

            for (var current = type; current is not null; current = current.BaseType)
            {
                if (DeclaresDerivedTypes(current))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the C# expression for the default value of <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">A parameter with a default value.</param>
        /// <returns>The expression, typed as the parameter.</returns>
        internal static string DefaultExpression(IParameterSymbol parameter)
        {
            var typeName = SymbolHelpers.FullName(parameter.Type);
            return parameter.ExplicitDefaultValue switch
            {
                null => parameter.Type.IsValueType ? "default(" + typeName + ")" : "default(" + typeName + ")!",
                bool flag => flag ? "true" : "false",
                string text => SymbolDisplay.FormatLiteral(text, quote: true),
                char character => SymbolDisplay.FormatLiteral(character, quote: true),
                double.NaN => "double.NaN",
                double.PositiveInfinity => "double.PositiveInfinity",
                double.NegativeInfinity => "double.NegativeInfinity",
                float.NaN => "float.NaN",
                float.PositiveInfinity => "float.PositiveInfinity",
                float.NegativeInfinity => "float.NegativeInfinity",
                var value => "(" + typeName + ")(" + SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false) + ")",
            };
        }

        /// <summary>
        /// Ranks a member for write order: attributes, then text, then elements.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The rank.</returns>
        internal static int Rank(MemberSpec member)
        {
            if (member.IsAttribute)
            {
                return 0;
            }

            return member.IsText || member.IsCData ? 1 : 2;
        }

        /// <summary>
        /// Tries to bind <paramref name="member"/>.
        /// </summary>
        /// <param name="member">The property or field.</param>
        /// <param name="options">The options.</param>
        /// <param name="index">The discovery index.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <param name="spec">The member spec.</param>
        /// <returns><see langword="true"/> when the member is serialized.</returns>
        internal static bool TryBindMember(ISymbol member, OptionsSpec options, int index, List<Diagnostic> diagnostics, out MemberSpec spec)
        {
            spec = default;
            ITypeSymbol? memberType = null;
            var canSet = false;
            var isPublicGet = false;
            if (member is IPropertySymbol property)
            {
                if (property.IsStatic || property.Parameters.Length > 0 || property.GetMethod is null)
                {
                    return false;
                }

                memberType = property.Type;
                isPublicGet = property.GetMethod.DeclaredAccessibility is Accessibility.Public;
                canSet = property.SetMethod is { DeclaredAccessibility: Accessibility.Public } && !property.SetMethod.IsInitOnly;
            }
            else if (member is IFieldSymbol field && options.IncludeFields)
            {
                if (field.IsStatic || field.IsConst)
                {
                    return false;
                }

                memberType = field.Type;
                isPublicGet = field.DeclaredAccessibility is Accessibility.Public;
                canSet = !field.IsReadOnly;
            }
            else
            {
                return false;
            }

            // A member with derived types, or with a converter of its own, is read and written from the metadata.
            // The write fast path does not write either, so the type goes without one.
            FastPathBlocked |= DeclaresDerivedTypes(member) || SymbolHelpers.GetAttribute(member, SymbolHelpers.ConverterAttribute) is not null;
            FastPathBlocked |= memberType is not null && HoldsDerivedTypes(memberType);

            // A choice of elements, and an element put in no namespace, are written from the metadata too.
            FastPathBlocked |= HasElementChoice(member) || HasEmptyNamespace(member);


            if (SymbolHelpers.GetAttribute(member, SymbolHelpers.BclXmlInclude) is not null)
            {
                var owner = member.ContainingType?.ToDisplayString() ?? member.Name;
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UseXmlDerivedType, member.Locations.Length > 0 ? member.Locations[0] : Location.None, owner));
            }

            if (IsUnsupported(memberType!, SymbolHelpers.FullName(memberType!)))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, member.Locations.Length > 0 ? member.Locations[0] : Location.None, memberType!.ToDisplayString()));
                return false;
            }

            var include = SymbolHelpers.GetAttribute(member, SymbolHelpers.IncludeAttribute) is not null;
            if (!isPublicGet && !include)
            {
                return false;
            }

            // The write fast path reads the member directly, which it cannot do for one that is not accessible from
            // the context. Such a member is read and written through the accessor in the metadata.
            var readable = member is IPropertySymbol { GetMethod: { } getMethod } ? getMethod : member;
            FastPathBlocked |= Home is not null && !(MetadataBuilder.IsAccessible(member, Home) && MetadataBuilder.IsAccessible(readable, Home));

            var ignore = SymbolHelpers.GetAttribute(member, SymbolHelpers.IgnoreAttribute);
            // A member with no attribute follows DefaultIgnoreCondition, which is read from the options when the
            // write fast path runs. That is recorded as -1.
            var ignoreCondition = ignore is null ? -1 : SymbolHelpers.GetInt(ignore, "Condition", 1);
            if (ignoreCondition == 1)
            {
                return false;
            }

            if (ignoreCondition == 6)
            {
                // WhenWritingNullReference: a null reference is left out, a nullable value with no value is not.
                ignoreCondition = memberType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } ? 0 : 3;
            }

            if (SymbolHelpers.GetAttribute(member, SymbolHelpers.ExtensionDataAttribute) is not null)
            {
                // Extension data is kept by the runtime library; the write fast path does not write it.
                FastPathBlocked = true;
                spec = new MemberSpec(member.Name, SymbolHelpers.FullName(memberType!), member.Name, "", false, false, false, false, false, "", "", true, false, canSet, 0, -1, true, false);
                return true;
            }

            var text = SymbolHelpers.GetAttribute(member, SymbolHelpers.TextAttribute);
            var cdata = SymbolHelpers.GetAttribute(member, SymbolHelpers.CDataAttribute);
            var element = SymbolHelpers.GetAttribute(member, SymbolHelpers.ElementAttribute);
            var attribute = SymbolHelpers.GetAttribute(member, SymbolHelpers.AttributeAttribute);
            var mixed = SymbolHelpers.GetAttribute(member, SymbolHelpers.MixedAttribute) is not null;
            if (attribute is not null && !SymbolHelpers.IsScalar(memberType!) && SymbolHelpers.GetAttribute(SymbolHelpers.UnwrapNullable(memberType!), SymbolHelpers.ConverterAttribute) is null)
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.ComplexTypeAsAttribute, member.Locations[0], member.ContainingType?.ToDisplayString() ?? "", member.Name));
                return false;
            }

            if (mixed)
            {
                // Mixed content is read and written from the metadata only.
                FastPathBlocked = true;
                return false;
            }

            if ((text is not null || cdata is not null) && !SymbolHelpers.IsScalar(memberType!) && SymbolHelpers.GetAttribute(SymbolHelpers.UnwrapNullable(memberType!), SymbolHelpers.ConverterAttribute) is null)
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.ComplexTypeAsText, member.Locations[0], member.ContainingType?.ToDisplayString() ?? "", member.Name));
                return false;
            }

            var propertyName = SymbolHelpers.GetAttribute(member, SymbolHelpers.PropertyNameAttribute);
            var xmlName = propertyName is null ? null : SymbolHelpers.GetString(propertyName, "Name") ?? SymbolHelpers.GetConstructorString(propertyName, 0);
            var isAttribute = false;
            var isText = false;
            var isCData = false;
            string local;
            var ns = "";
            if (text is not null || cdata is not null)
            {
                isText = true;
                isCData = cdata is not null;
                local = xmlName ?? member.Name;
            }
            else if (attribute is not null)
            {
                isAttribute = true;
                var attributeName = SymbolHelpers.GetString(attribute, "AttributeName") ?? SymbolHelpers.GetConstructorString(attribute, 0);
                local = xmlName
                    ?? (attributeName is { Length: > 0 }
                        ? attributeName
                        : Naming.Convert(member.Name, AttributePolicy(options)));
                ns = SymbolHelpers.GetString(attribute, "Namespace") ?? SymbolHelpers.GetConstructorString(attribute, 1) ?? "";
            }
            else if (element is not null)
            {
                var elementName = SymbolHelpers.GetString(element, "ElementName") ?? SymbolHelpers.GetConstructorString(element, 0);
                local = xmlName
                    ?? (elementName is { Length: > 0 }
                        ? elementName
                        : Naming.Convert(member.Name, ElementPolicy(options)));
                ns = SymbolHelpers.GetString(element, "Namespace") ?? SymbolHelpers.GetConstructorString(element, 1) ?? "";
            }
            else if (options.DefaultPropertyMapping == 1 && SymbolHelpers.IsScalar(memberType!))
            {
                isAttribute = true;
                local = xmlName ?? Naming.Convert(member.Name, AttributePolicy(options));
            }
            else
            {
                local = xmlName ?? Naming.Convert(member.Name, ElementPolicy(options));
            }

            var orderAttr = SymbolHelpers.GetAttribute(member, SymbolHelpers.PropertyOrderAttribute);
            var order = orderAttr is null ? -1 : SymbolHelpers.GetInt(orderAttr, "Order", SymbolHelpers.GetConstructorString(orderAttr, 0) is null && orderAttr.ConstructorArguments.Length > 0 && orderAttr.ConstructorArguments[0].Value is int o ? o : -1);
            if (element is not null && order < 0)
            {
                order = SymbolHelpers.GetInt(element, "Order", -1);
            }

            var isCollection = false;
            var isWrapped = false;
            var itemLocal = "";
            var itemTypeName = "";
            if (!isAttribute && !isText && TryCollectionElement(memberType!, out var elementType))
            {
                isCollection = true;
                var array = SymbolHelpers.GetAttribute(member, SymbolHelpers.ArrayAttribute);
                var arrayItem = SymbolHelpers.GetAttribute(member, SymbolHelpers.ArrayItemAttribute);
                // [XmlArray] wraps a collection, and so does the default of the options unless [XmlElement] says
                // the items are the member's own elements.
                isWrapped = array is not null || (options.DefaultCollectionMapping == 1 && element is null);
                if (array is not null)
                {
                    var arrayName = SymbolHelpers.GetString(array, "ElementName") ?? SymbolHelpers.GetConstructorString(array, 0);
                    local = xmlName
                        ?? (arrayName is { Length: > 0 }
                            ? arrayName
                            : Naming.Convert(member.Name, ElementPolicy(options)));
                    ns = SymbolHelpers.GetString(array, "Namespace") ?? ns;
                    if (order < 0)
                    {
                        order = SymbolHelpers.GetInt(array, "Order", -1);
                    }
                }

                itemTypeName = SymbolHelpers.FullName(elementType);
                itemLocal = arrayItem is not null && (SymbolHelpers.GetString(arrayItem, "ElementName") ?? SymbolHelpers.GetConstructorString(arrayItem, 0)) is { Length: > 0 } itemName
                    ? itemName
                    : isWrapped
                        ? XmlTypeNameSymbols.ElementName(elementType, ElementPolicy(options))
                        : local;

                // An item with a namespace of its own is written by the engine, which declares the namespace.
                FastPathBlocked |= isWrapped && arrayItem is not null && SymbolHelpers.GetString(arrayItem, "Namespace") is not null;
            }

            var isRequired = SymbolHelpers.GetAttribute(member, SymbolHelpers.RequiredAttribute) is not null || member is IPropertySymbol { IsRequired: true };
            var isNillable = !memberType!.IsValueType || memberType.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T || isCollection;
            spec = new MemberSpec(
                member.Name,
                SymbolHelpers.FullName(memberType),
                local,
                ns ?? "",
                isAttribute,
                isText,
                isCData,
                isCollection,
                isWrapped,
                itemLocal,
                itemTypeName,
                isNillable,
                isRequired,
                canSet,
                ignoreCondition,
                order,
                false,
                mixed);
            // What the annotation says is recorded whatever the options are: the write fast path reads
            // RespectNullableAnnotations when it runs.
            var originalType = member.OriginalDefinition is IPropertySymbol originalProperty ? originalProperty.Type : ((IFieldSymbol)member.OriginalDefinition).Type;
            spec = new MemberSpec(
                spec,
                null,
                SymbolHelpers.RefusesNull(member, memberType, originalType, forRead: true) ? SymbolHelpers.FullName(member.ContainingType!) : null);
            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a collection and its element type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="elementType">The element type.</param>
        /// <returns><see langword="true"/> for arrays and <c>IEnumerable&lt;T&gt;</c>.</returns>
        internal static bool TryCollectionElement(ITypeSymbol type, out ITypeSymbol elementType)
        {
            elementType = type;
            if (type.SpecialType is SpecialType.System_String || SymbolHelpers.IsScalar(type))
            {
                return false;
            }

            // A dictionary enumerates pairs and an element node enumerates its children. Neither is written
            // as a list of them: the runtime library writes each as what it is.
            if (TryDictionaryTypes(type, out _, out _) || IsRuntimeType(SymbolHelpers.UnwrapNullable(type)))
            {
                return false;
            }

            if (type is IArrayTypeSymbol array)
            {
                elementType = array.ElementType;
                return true;
            }

            foreach (var implemented in type.AllInterfaces)
            {
                if (implemented.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.IEnumerable<T>"
                    && implemented.TypeArguments.Length == 1)
                {
                    elementType = implemented.TypeArguments[0];
                    return true;
                }
            }

            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.IEnumerable<T>"
                && named.TypeArguments.Length == 1)
            {
                elementType = named.TypeArguments[0];
                return true;
            }

            return false;
        }

        #endregion

    }

}
