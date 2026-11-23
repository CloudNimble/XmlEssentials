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

        #region Public Methods

        /// <summary>
        /// Builds specs for <paramref name="roots"/> and every nested object they reach.
        /// </summary>
        /// <param name="roots">The [XmlSerializable] types and their property names.</param>
        /// <param name="options">The source-generation options.</param>
        /// <param name="emitSerialization">Whether converters should be emitted.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <returns>The type specs.</returns>
        internal static EquatableArray<TypeSpec> Walk(
            List<(INamedTypeSymbol Type, string PropertyName)> roots,
            OptionsSpec options,
            bool emitSerialization,
            List<Diagnostic> diagnostics)
        {
            var seen = new HashSet<string>();
            var specs = new List<TypeSpec>();
            foreach (var root in roots)
            {
                Visit(root.Type, root.PropertyName, options, emitSerialization, seen, specs, diagnostics);
            }

            return new EquatableArray<TypeSpec>(specs.ToArray());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Visits <paramref name="type"/> if it has not been seen.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The type-info property name.</param>
        /// <param name="options">The options.</param>
        /// <param name="emitSerialization">Whether converters should be emitted.</param>
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

            if (type is INamedTypeSymbol named && named.IsUnboundGenericType)
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, location, type.ToDisplayString()));
                return;
            }

            if (DeclaresDerivedTypes(type))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.DerivedTypesNotGenerated, location, type.ToDisplayString()));
            }

            if (SymbolHelpers.GetAttribute(type, SymbolHelpers.BclXmlInclude) is not null
                || (SymbolHelpers.GetAttribute(type, SymbolHelpers.IncludeAttribute) is not null && type.TypeKind is TypeKind.Class or TypeKind.Interface or TypeKind.Struct))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UseXmlDerivedType, location, type.ToDisplayString()));
            }

            var kind = KindOf(type);
            var rootName = RootName(type, options);
            var rootAttr = SymbolHelpers.GetAttribute(type, SymbolHelpers.RootAttribute);
            var rootNs = rootAttr is null ? options.DefaultNamespace ?? "" : SymbolHelpers.GetString(rootAttr, "Namespace") ?? options.DefaultNamespace ?? "";
            var members = kind == 0 && type is INamedTypeSymbol objectType
                ? BindMembers(objectType, options, diagnostics)
                : new EquatableArray<MemberSpec>([]);

            specs.Add(new TypeSpec(
                clrName,
                propertyName,
                Naming.Convert(rootName, ElementPolicy(options)),
                rootNs,
                kind,
                type.IsValueType,
                !type.IsValueType || type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T,
                emitSerialization && kind == 0,
                members));

            if (kind == 0 && type is INamedTypeSymbol declaring)
            {
                foreach (var member in declaring.GetMembers())
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

                    if (TryCollectionElement(nested, out var element) && KindOf(element) == 0)
                    {
                        Visit(element, element.Name, options, emitSerialization, seen, specs, diagnostics);
                        continue;
                    }

                    if (KindOf(nested) == 0)
                    {
                        Visit(nested, SymbolHelpers.UnwrapNullable(nested).Name, options, emitSerialization, seen, specs, diagnostics);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> cannot be serialized (pointers, <c>IntPtr</c>).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="clrName">The fully qualified name.</param>
        /// <returns><see langword="true"/> when XMLXE002 applies.</returns>
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
        /// Returns 0 for objects, 1 for scalars, 2 for collections, 3 for enums.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The kind.</returns>
        internal static int KindOf(ITypeSymbol type)
        {
            var root = SymbolHelpers.UnwrapNullable(type);
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
        /// Returns the document element local name before the naming policy.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="options">The options.</param>
        /// <returns>The name.</returns>
        internal static string RootName(ITypeSymbol type, OptionsSpec options)
        {
            var root = SymbolHelpers.GetAttribute(type, SymbolHelpers.RootAttribute);
            var named = root is null ? null : SymbolHelpers.GetString(root, "ElementName") ?? SymbolHelpers.GetConstructorString(root, 0);
            if (!string.IsNullOrEmpty(named))
            {
                return named!;
            }

            var symbol = SymbolHelpers.UnwrapNullable(type);
            if (symbol is IArrayTypeSymbol array)
            {
                return Naming.XmlTypeName(symbol.Name, true, array.ElementType.Name, []);
            }

            if (symbol is INamedTypeSymbol generic && generic.IsGenericType)
            {
                var args = new string[generic.TypeArguments.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = generic.TypeArguments[i].Name;
                }

                return Naming.XmlTypeName(generic.Name, false, null, args);
            }

            return symbol.Name;
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
        /// <returns>The members in write order.</returns>
        internal static EquatableArray<MemberSpec> BindMembers(INamedTypeSymbol type, OptionsSpec options, List<Diagnostic> diagnostics)
        {
            var members = new List<MemberSpec>();
            var stack = new Stack<INamedTypeSymbol>();
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                stack.Push(current);
            }

            var index = 0;
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var member in current.GetMembers())
                {
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

            members.Sort(static (left, right) =>
            {
                var rank = Rank(left).CompareTo(Rank(right));
                if (rank != 0)
                {
                    return rank;
                }

                var leftOrder = left.Order >= 0 ? left.Order : int.MaxValue;
                var rightOrder = right.Order >= 0 ? right.Order : int.MaxValue;
                var compared = leftOrder.CompareTo(rightOrder);
                return compared != 0 ? compared : 0;
            });

            foreach (var member in members)
            {
                if (!Naming.IsXmlName(member.LocalName) && !member.IsText && !member.IsExtensionData && !member.IsMixed)
                {
                    diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.InvalidXmlName, type.Locations[0], member.LocalName, type.ToDisplayString() + "." + member.ClrName));
                }
            }

            BindConstructor(type, members, diagnostics);
            return new EquatableArray<MemberSpec>(members.ToArray());
        }

        /// <summary>
        /// Chooses the constructor the generated converter calls, by the rules the reflection resolver uses,
        /// and records on each member the parameter it fills.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="members">The members. A member that fills a parameter is replaced by a copy that says so.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <remarks>
        /// A constructor marked <c>[XmlConstructor]</c> is used. Otherwise the public constructor with the most
        /// parameters is used, among those whose every parameter is named like a member. A parameter no
        /// member fills must have a default value. XMLXE005 is reported when the choice is ambiguous, and
        /// XMLXE002 when the chosen constructor cannot be called from generated code.
        /// </remarks>
        internal static void BindConstructor(INamedTypeSymbol type, List<MemberSpec> members, List<Diagnostic> diagnostics)
        {
            var names = new HashSet<string>();
            foreach (var member in members)
            {
                names.Add(member.ClrName);
                names.Add(member.LocalName);
            }

            IMethodSymbol? marked = null;
            var markedCount = 0;
            IMethodSymbol? best = null;
            var bestCount = -1;
            var bestTies = 0;
            foreach (var constructor in type.InstanceConstructors)
            {
                if (SymbolHelpers.GetAttribute(constructor, SymbolHelpers.ConstructorAttribute) is not null)
                {
                    marked = constructor;
                    markedCount++;
                }

                if (constructor.DeclaredAccessibility is not Accessibility.Public)
                {
                    continue;
                }

                var bindable = true;
                foreach (var parameter in constructor.Parameters)
                {
                    if (!names.Contains(parameter.Name))
                    {
                        bindable = false;
                        break;
                    }
                }

                if (!bindable)
                {
                    continue;
                }

                var count = constructor.Parameters.Length;
                if (count > bestCount)
                {
                    best = constructor;
                    bestCount = count;
                    bestTies = 1;
                }
                else if (count == bestCount && count > 0)
                {
                    bestTies++;
                }
            }

            if (markedCount > 1 || (marked is null && bestTies > 1))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.AmbiguousConstructors, type.Locations[0], type.ToDisplayString()));
                return;
            }

            var chosen = marked ?? best;
            var hasParameterless = false;
            foreach (var constructor in type.InstanceConstructors)
            {
                hasParameterless |= constructor.Parameters.Length == 0
                    && constructor.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal);
            }

            if (chosen is null && !hasParameterless && type is { IsAbstract: false, TypeKind: TypeKind.Class })
            {
                // Every public constructor has a parameter that is not a member, and there is no parameterless
                // one the generated code may call, so there is nothing the converter can use.
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, type.Locations[0], type.ToDisplayString()));
                return;
            }

            if (chosen is null || chosen.Parameters.Length == 0)
            {
                return;
            }

            if (chosen.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal)
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedType, type.Locations[0], type.ToDisplayString()));
                return;
            }

            foreach (var parameter in chosen.Parameters)
            {
                var index = members.FindIndex(member => (member.ClrName == parameter.Name || member.LocalName == parameter.Name) && member.ConstructorParameter is null);
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

                members[index] = new MemberSpec(
                    members[index],
                    parameter.Name,
                    parameter.Ordinal,
                    parameter.HasExplicitDefaultValue ? DefaultExpression(parameter) : null,
                    members[index].IsInitOnly);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="symbol"/> carries <c>[XmlDerivedType]</c> in either of its forms.
        /// </summary>
        /// <param name="symbol">A type or a member.</param>
        /// <returns><see langword="true"/> when derived types are declared on it.</returns>
        internal static bool DeclaresDerivedTypes(ISymbol symbol)
        {
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

            if (DeclaresDerivedTypes(member))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.DerivedTypesNotGenerated, member.Locations[0], member.ContainingType.ToDisplayString() + "." + member.Name));
            }

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

            var ignore = SymbolHelpers.GetAttribute(member, SymbolHelpers.IgnoreAttribute);
            var ignoreCondition = ignore is null ? options.DefaultIgnoreCondition : SymbolHelpers.GetInt(ignore, "Condition", 1);
            if (ignoreCondition == 1)
            {
                return false;
            }

            if (SymbolHelpers.GetAttribute(member, SymbolHelpers.ExtensionDataAttribute) is not null)
            {
                spec = new MemberSpec(member.Name, SymbolHelpers.FullName(memberType!), member.Name, "", false, false, false, false, false, "", "", true, false, canSet, 0, -1, true, false);
                return true;
            }

            var text = SymbolHelpers.GetAttribute(member, SymbolHelpers.TextAttribute);
            var cdata = SymbolHelpers.GetAttribute(member, SymbolHelpers.CDataAttribute);
            var element = SymbolHelpers.GetAttribute(member, SymbolHelpers.ElementAttribute);
            var attribute = SymbolHelpers.GetAttribute(member, SymbolHelpers.AttributeAttribute);
            var mixed = SymbolHelpers.GetAttribute(member, SymbolHelpers.MixedAttribute) is not null;
            if (attribute is not null && !SymbolHelpers.IsScalar(memberType!))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.ComplexTypeAsAttribute, member.Locations[0], member.ContainingType?.ToDisplayString() ?? "", member.Name));
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
                ns = SymbolHelpers.GetString(attribute, "Namespace") ?? "";
            }
            else if (element is not null)
            {
                var elementName = SymbolHelpers.GetString(element, "ElementName") ?? SymbolHelpers.GetConstructorString(element, 0);
                local = xmlName
                    ?? (elementName is { Length: > 0 }
                        ? elementName
                        : Naming.Convert(member.Name, ElementPolicy(options)));
                ns = SymbolHelpers.GetString(element, "Namespace") ?? "";
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
                isWrapped = array is not null;
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
                        ? Naming.Convert(elementType.Name, ElementPolicy(options))
                        : local;
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
            spec = new MemberSpec(spec, null, -1, null, member is IPropertySymbol { SetMethod: { IsInitOnly: true, DeclaredAccessibility: Accessibility.Public } });
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
            if (type.SpecialType is SpecialType.System_String || SymbolHelpers.FullName(type) is "global::System.Byte[]")
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
                    if (implemented.ToDisplayString().StartsWith("System.Collections.Generic.IDictionary")
                        || type.OriginalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IDictionary"))
                    {
                        return false;
                    }

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
