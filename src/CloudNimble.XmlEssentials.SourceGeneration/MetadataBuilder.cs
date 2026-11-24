using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Writes the metadata of an object type as C# source: the object creators, the constructor parameters, and one
    /// entry for each property and field with its getter, its setter and its attributes.
    /// </summary>
    /// <remarks>
    /// The source describes the type and decides nothing. Names, order, ignore conditions, the constructor mapping
    /// and every other rule are applied by the runtime library when the metadata is bound, by the code that applies
    /// them for the reflection resolver. That is what keeps a generated context and the reflection resolver alike.
    /// </remarks>
    internal static class MetadataBuilder
    {

        #region Fields

        private const string AttributeNamespace = "CloudNimble.Text.Xml.Serialization";

        /// <summary>
        /// The line of the metadata that stands for the write fast path. The context replaces it or leaves it out.
        /// </summary>
        internal const string HandlerLine = "    /*SerializeHandler*/";

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the body of the method that creates the type info of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="constructor">The constructor the object is created through when it has parameters, or null.</param>
        /// <param name="home">The assembly the context is in, whose internal members generated code can reach.</param>
        /// <param name="accessors">Receives the declarations of the accessors the body calls, each a complete member.</param>
        /// <returns>
        /// The statements, each line ending in a line feed and starting at column 0, that end in
        /// <c>return XmlMetadataServices.CreateObjectInfo(options, objectInfo);</c>.
        /// </returns>
        internal static string Build(INamedTypeSymbol type, IMethodSymbol? constructor, IAssemblySymbol home, StringBuilder accessors)
        {
            var typeName = SymbolHelpers.FullName(type);
            var key = Identifier(typeName);
            var body = new StringBuilder();
            body.Append("var objectInfo = new XmlObjectInfoValues<").Append(typeName).AppendLine(">");
            body.AppendLine("{");

            // The context puts the write fast path on this line when the type has one.
            body.AppendLine(HandlerLine);
            AppendCreators(body, type, typeName, key, constructor, home, accessors);
            body.AppendLine("    PropertyMetadataInitializer = static _ => new XmlPropertyInfoValues[]");
            body.AppendLine("    {");
            var chain = new Stack<INamedTypeSymbol>();
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                chain.Push(current);
            }

            var index = 0;
            while (chain.Count > 0)
            {
                // Base types first, and within a type its properties and then its fields: the order reflection gives.
                var current = chain.Pop();
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol property && IsSerializable(property))
                    {
                        AppendMember(body, property, property.Type, typeName, key, index++, home, accessors);
                    }
                }

                foreach (var member in current.GetMembers())
                {
                    if (member is IFieldSymbol field && IsSerializable(field))
                    {
                        AppendMember(body, field, field.Type, typeName, key, index++, home, accessors);
                    }
                }
            }

            body.AppendLine("    },");
            body.AppendLine("};");
            body.AppendLine("return XmlMetadataServices.CreateObjectInfo(options, objectInfo);");
            return body.ToString();
        }

        /// <summary>
        /// Builds the body of the method that creates the type info of a class the schema generator wrote.
        /// </summary>
        /// <param name="typeName">The fully qualified name of the class.</param>
        /// <param name="members">The members, as the class and its base classes declare them.</param>
        /// <param name="isAbstract">Whether the class is abstract, and so has no creator.</param>
        /// <returns>The statements, in the form <see cref="Build"/> returns them.</returns>
        /// <remarks>
        /// The class does not exist as a symbol while the generator runs, so its metadata is written from the same
        /// member list its source is written from: public properties with a getter and a setter, and the attributes
        /// <see cref="XsdParser.EmitPoco"/> puts on them. The property type is inferred from the getter, which spares
        /// naming a nullable type that depends on whether the type is a value type.
        /// </remarks>
        internal static string BuildFromMembers(string typeName, IEnumerable<MemberSpec> members, bool isAbstract = false)
        {
            var body = new StringBuilder();
            body.Append("var objectInfo = new XmlObjectInfoValues<").Append(typeName).AppendLine(">");
            body.AppendLine("{");
            body.AppendLine(HandlerLine);
            if (!isAbstract)
            {
                // An abstract class is never created: a document names one of its derived classes.
                body.Append("    ObjectCreator = static () => new ").Append(typeName).AppendLine("(),");
            }

            body.AppendLine("    PropertyMetadataInitializer = static _ => new XmlPropertyInfoValues[]");
            body.AppendLine("    {");
            foreach (var member in members)
            {
                // The attributes are the ones XsdParser.EmitPoco writes on the property, from one description.
                var expressions = new List<string>();
                foreach (var attribute in XsdParser.MemberAttributes(member))
                {
                    expressions.Add(attribute.ToExpression());
                }

                var attributes = string.Join(", ", expressions);
                var source = "__Property(\"" + member.ClrName + "\", static (" + typeName + " obj) => obj." + member.ClrName + ", static (obj, value) => obj." + member.ClrName + " = value"
                    + (attributes.Length > 0 ? ", " + attributes : "") + ")";
                if (member.IsCollection && !member.IsExtensionData)
                {
                    var declared = member.ClrTypeName.TrimEnd('?');
                    source = declared.EndsWith("[]", System.StringComparison.Ordinal)
                        ? "XmlMetadataServices.CreateArrayPropertyInfo<" + member.ItemTypeName + ">(" + source + "!)"
                        : "XmlMetadataServices.CreateCollectionPropertyInfo<" + declared + ", " + member.ItemTypeName + ">(" + source + "!)";
                }

                body.Append("        ").Append(source).AppendLine(",");
            }

            body.AppendLine("    },");
            body.AppendLine("};");
            body.AppendLine("return XmlMetadataServices.CreateObjectInfo(options, objectInfo);");
            return body.ToString();
        }

        /// <summary>
        /// Appends the object creators, and the constructor parameters when the object is created through a
        /// constructor with parameters.
        /// </summary>
        /// <param name="body">The source.</param>
        /// <param name="type">The object type.</param>
        /// <param name="typeName">Its fully qualified name.</param>
        /// <param name="key">A name for the type that is a valid identifier.</param>
        /// <param name="constructor">The constructor with parameters, or null.</param>
        /// <param name="home">The assembly the context is in.</param>
        /// <param name="accessors">Receives accessor declarations.</param>
        internal static void AppendCreators(StringBuilder body, INamedTypeSymbol type, string typeName, string key, IMethodSymbol? constructor, IAssemblySymbol home, StringBuilder accessors)
        {
            if (type.IsAbstract || type.TypeKind is TypeKind.Interface)
            {
                // An abstract type is never created: a document names one of its derived types.
                return;
            }

            // An object with required members cannot be created with "new" here without setting them, and a
            // constructor that is not accessible cannot be called at all. An accessor creates the object either way.
            var requiresAccessor = HasRequiredMembers(type);
            if (constructor is { Parameters.Length: > 0 })
            {
                var ignored = TypeGraphWalker.IgnoredMembers(type);
                var arguments = new StringBuilder();
                var declared = new StringBuilder();
                body.AppendLine("    ConstructorParameterMetadataInitializer = static () => new XmlParameterInfoValues[]");
                body.AppendLine("    {");
                foreach (var parameter in constructor.Parameters)
                {
                    var parameterType = SymbolHelpers.FullName(parameter.Type);
                    var isIgnored = ignored.Contains(parameter.Name);
                    // A parameter the document left out holds null, which stands for the default of a value type,
                    // as it does when a constructor is invoked through reflection.
                    var argument = isIgnored
                        ? "default(" + parameterType + ")!"
                        : parameter.Type.IsValueType && parameter.Type.OriginalDefinition.SpecialType is not SpecialType.System_Nullable_T
                            ? "(args[" + parameter.Ordinal + "] is " + parameterType + " a" + parameter.Ordinal + " ? a" + parameter.Ordinal + " : default(" + parameterType + "))"
                            : "(" + parameterType + ")args[" + parameter.Ordinal + "]!";
                    arguments.Append(arguments.Length == 0 ? "" : ", ").Append(argument);
                    declared.Append(declared.Length == 0 ? "" : ", ").Append(parameterType).Append(" p").Append(parameter.Ordinal);
                    body.Append("        new XmlParameterInfoValues { Name = \"").Append(parameter.Name).Append("\", ParameterType = typeof(").Append(TypeOf(parameter.Type))
                        .Append("), Position = ").Append(parameter.Ordinal)
                        .Append(", IsNullable = ").Append(SymbolHelpers.RefusesNull(parameter, parameter.Type, parameter.OriginalDefinition.Type, forRead: false) ? "false" : "true");
                    if (isIgnored)
                    {
                        // The member the parameter belongs to is never serialized, so no document supplies it.
                        body.Append(", HasDefaultValue = true");
                    }
                    else if (parameter.HasExplicitDefaultValue)
                    {
                        body.Append(", HasDefaultValue = true, DefaultValue = ").Append(TypeGraphWalker.DefaultExpression(parameter));
                    }

                    body.AppendLine(" },");
                }

                body.AppendLine("    },");
                if (requiresAccessor || !IsAccessible(constructor, home))
                {
                    var name = "__Create_" + key;
                    accessors.Append("[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]").AppendLine();
                    accessors.Append("private static extern ").Append(typeName).Append(' ').Append(name).Append('(').Append(declared).AppendLine(");");
                    body.Append("    ObjectWithParameterizedConstructorCreator = static args => ").Append(name).Append('(').Append(arguments).AppendLine("),");
                }
                else
                {
                    body.Append("    ObjectWithParameterizedConstructorCreator = static args => new ").Append(typeName).Append('(').Append(arguments).AppendLine("),");
                }

                return;
            }

            IMethodSymbol? parameterless = null;
            foreach (var candidate in type.InstanceConstructors)
            {
                if (candidate.Parameters.Length == 0)
                {
                    parameterless = candidate;
                }
            }

            if (parameterless is null)
            {
                // The type has no way to be created with no arguments. It can still be written.
                return;
            }

            if (type.IsValueType && parameterless.IsImplicitlyDeclared)
            {
                body.Append("    ObjectCreator = static () => default(").Append(typeName).AppendLine("),");
            }
            else if (requiresAccessor || !IsAccessible(parameterless, home))
            {
                var name = "__Create_" + key;
                accessors.Append("[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Constructor)]").AppendLine();
                accessors.Append("private static extern ").Append(typeName).Append(' ').Append(name).AppendLine("();");
                body.Append("    ObjectCreator = static () => ").Append(name).AppendLine("(),");
            }
            else
            {
                body.Append("    ObjectCreator = static () => new ").Append(typeName).AppendLine("(),");
            }
        }

        /// <summary>
        /// Appends the metadata of one property or field.
        /// </summary>
        /// <param name="body">The source.</param>
        /// <param name="member">The property or field.</param>
        /// <param name="memberType">Its type.</param>
        /// <param name="typeName">The fully qualified name of the type that is described.</param>
        /// <param name="key">A name for that type that is a valid identifier.</param>
        /// <param name="index">The position of the member, which makes accessor names unique.</param>
        /// <param name="home">The assembly the context is in.</param>
        /// <param name="accessors">Receives accessor declarations.</param>
        internal static void AppendMember(StringBuilder body, ISymbol member, ITypeSymbol memberType, string typeName, string key, int index, IAssemblySymbol home, StringBuilder accessors)
        {
            var declaring = member.ContainingType;
            var declaringName = SymbolHelpers.FullName(declaring);
            var valueName = SymbolHelpers.FullName(memberType);
            var property = member as IPropertySymbol;
            var field = member as IFieldSymbol;
            var instance = declaring.IsValueType
                ? "global::System.Runtime.CompilerServices.Unsafe.Unbox<" + declaringName + ">(obj)"
                : "((" + declaringName + ")obj)";
            var accessorTarget = declaring.IsValueType ? "ref " + instance : instance;
            var accessorParameter = (declaring.IsValueType ? "ref " : "") + declaringName + " target";
            var accessorName = "__" + key + "_" + index + "_" + member.Name;
            const string accessorAttribute = "[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.";

            string getter;
            string? setter = null;
            if (field is not null)
            {
                if (IsAccessible(field, home))
                {
                    getter = instance + "." + field.Name;
                    setter = field.IsReadOnly ? null : instance + "." + field.Name + " = value!";
                }
                else
                {
                    accessors.Append(accessorAttribute).Append("Field, Name = \"").Append(field.Name).AppendLine("\")]");
                    accessors.Append("private static extern ref ").Append(valueName).Append(' ').Append(accessorName).Append('(').Append(accessorParameter).AppendLine(");");
                    getter = accessorName + "(" + accessorTarget + ")";
                    setter = field.IsReadOnly ? null : accessorName + "(" + accessorTarget + ") = value!";
                }
            }
            else
            {
                var get = property!.GetMethod!;
                if (IsAccessible(get, home) && IsAccessible(property, home))
                {
                    getter = instance + "." + property.Name;
                }
                else
                {
                    accessors.Append(accessorAttribute).Append("Method, Name = \"get_").Append(property.Name).AppendLine("\")]");
                    accessors.Append("private static extern ").Append(valueName).Append(' ').Append(accessorName).Append("_get(").Append(accessorParameter).AppendLine(");");
                    getter = accessorName + "_get(" + accessorTarget + ")";
                }

                // A property has a setter when its set or init accessor is public, or the property is marked
                // [XmlInclude]: that is what the reflection resolver uses. An init accessor and one that is not
                // accessible are called through an accessor.
                if (property.SetMethod is { } set
                    && (set.DeclaredAccessibility is Accessibility.Public || SymbolHelpers.GetAttribute(property, SymbolHelpers.IncludeAttribute) is not null))
                {
                    if (!set.IsInitOnly && IsAccessible(set, home) && IsAccessible(property, home))
                    {
                        setter = instance + "." + property.Name + " = value!";
                    }
                    else
                    {
                        accessors.Append(accessorAttribute).Append("Method, Name = \"set_").Append(property.Name).AppendLine("\")]");
                        accessors.Append("private static extern void ").Append(accessorName).Append("_set(").Append(accessorParameter).Append(", ").Append(valueName).AppendLine(" value);");
                        setter = accessorName + "_set(" + accessorTarget + ", value!)";
                    }
                }
            }

            var isPublic = field is not null
                ? field.DeclaredAccessibility is Accessibility.Public
                : property!.GetMethod!.DeclaredAccessibility is Accessibility.Public && property.DeclaredAccessibility is Accessibility.Public;
            var isRequired = field is not null ? field.IsRequired : property!.IsRequired;
            var originalType = member.OriginalDefinition is IPropertySymbol originalProperty ? originalProperty.Type : ((IFieldSymbol)member.OriginalDefinition).Type;
            var getNullable = !SymbolHelpers.RefusesNull(member, memberType, originalType, forRead: true);
            var setNullable = property is { SetMethod: null } || !SymbolHelpers.RefusesNull(member, memberType, originalType, forRead: false);

            var values = new StringBuilder();
            values.Append("new XmlPropertyInfoValues<").Append(valueName).Append("> { PropertyName = \"").Append(member.Name).Append("\", DeclaringType = typeof(").Append(TypeOf(declaring)).Append(')');
            if (field is not null)
            {
                values.Append(", IsProperty = false");
            }

            if (!isPublic)
            {
                values.Append(", IsPublic = false");
            }

            if (isRequired)
            {
                values.Append(", IsRequired = true");
            }

            if (property is not null && IsVirtual(property))
            {
                values.Append(", IsVirtual = true");
            }

            if (!getNullable)
            {
                values.Append(", IsGetNullable = false");
            }

            if (!setNullable)
            {
                values.Append(", IsSetNullable = false");
            }

            values.Append(", Getter = static obj => ").Append(getter);
            if (setter is not null)
            {
                values.Append(", Setter = static (obj, value) => ").Append(setter);
            }

            var attributes = Attributes(member);
            if (attributes.Length > 0)
            {
                values.Append(", Attributes = new global::System.Attribute[] { ").Append(attributes).Append(" }");
            }

            values.Append(" }");
            var source = values.ToString();
            if (memberType.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T && SymbolHelpers.GetAttribute(member, SymbolHelpers.ConverterAttribute) is not null)
            {
                // A converter for T on a member of type T? converts the value. The wrapper that handles null round it
                // is generic over T, so it is named here.
                source = "XmlMetadataServices.CreateNullablePropertyInfo<" + SymbolHelpers.FullName(SymbolHelpers.UnwrapNullable(memberType)) + ">(" + source + ")";
            }
            else if (TypeGraphWalker.TryCollectionElement(memberType, out var elementType))
            {
                // An array has a method of its own: the constructors and methods of System.Array cannot be kept for Native AOT.
                source = memberType is IArrayTypeSymbol
                    ? "XmlMetadataServices.CreateArrayPropertyInfo<" + SymbolHelpers.FullName(elementType) + ">(" + source + ")"
                    : "XmlMetadataServices.CreateCollectionPropertyInfo<" + valueName.TrimEnd('?') + ", " + SymbolHelpers.FullName(elementType) + ">(" + source + ")";
            }

            else if (memberType.IsValueType && memberType.OriginalDefinition.SpecialType is not SpecialType.System_Nullable_T)
            {
                // A value type that fills a constructor parameter is held in a box that is reused, which is generic over the type.
                source = "XmlMetadataServices.CreateValuePropertyInfo<" + valueName + ">(" + source + ")";
            }

            body.Append("        ").Append(source).AppendLine(",");
        }

        /// <summary>
        /// Returns the XmlEssentials attributes on <paramref name="member"/> as the source of expressions that construct them.
        /// </summary>
        /// <param name="member">The property or field.</param>
        /// <returns>The expressions, separated by commas, or an empty string.</returns>
        /// <remarks>
        /// The runtime library reads the instances as it reads attributes it finds through reflection, so every
        /// constructor argument and named argument is carried across as it was written.
        /// </remarks>
        internal static string Attributes(ISymbol member)
        {
            var source = new StringBuilder();
            foreach (var attribute in member.GetAttributes())
            {
                if (attribute.AttributeClass is not { } attributeClass || attributeClass.ContainingNamespace?.ToDisplayString() != AttributeNamespace)
                {
                    continue;
                }

                source.Append(source.Length == 0 ? "" : ", ").Append("new ").Append(SymbolHelpers.FullName(attributeClass)).Append('(');
                for (var i = 0; i < attribute.ConstructorArguments.Length; i++)
                {
                    source.Append(i == 0 ? "" : ", ").Append(attribute.ConstructorArguments[i].ToCSharpString());
                }

                source.Append(')');
                if (attribute.NamedArguments.Length > 0)
                {
                    source.Append(" { ");
                    for (var i = 0; i < attribute.NamedArguments.Length; i++)
                    {
                        source.Append(i == 0 ? "" : ", ").Append(attribute.NamedArguments[i].Key).Append(" = ").Append(attribute.NamedArguments[i].Value.ToCSharpString());
                    }

                    source.Append(" }");
                }
            }

            return source.ToString();
        }

        /// <summary>
        /// Returns whether the accessors of <paramref name="property"/> are virtual methods.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// <see langword="true"/> for a property that is virtual, abstract or an override, and for one that
        /// implements an interface member, whose accessors the compiler marks virtual.
        /// </returns>
        /// <remarks>
        /// This is what reflection reports as <c>MethodBase.IsVirtual</c>, which the runtime library reads when it
        /// finds the members itself.
        /// </remarks>
        internal static bool IsVirtual(IPropertySymbol property)
        {
            if (property.IsVirtual || property.IsOverride || property.IsAbstract)
            {
                return true;
            }

            foreach (var contract in property.ContainingType.AllInterfaces)
            {
                foreach (var declared in contract.GetMembers(property.Name))
                {
                    if (SymbolEqualityComparer.Default.Equals(property.ContainingType.FindImplementationForInterfaceMember(declared), property))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> or a base type has a member with the <see langword="required"/> modifier.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> when it has one.</returns>
        internal static bool HasRequiredMembers(INamedTypeSymbol type)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Turns a type name into an identifier.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The name with every character that an identifier cannot hold replaced.</returns>
        internal static string Identifier(string typeName)
        {
            var name = new StringBuilder(typeName.Length);
            foreach (var character in typeName.StartsWith("global::", System.StringComparison.Ordinal) ? typeName.Substring(8) : typeName)
            {
                name.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return name.ToString();
        }

        /// <summary>
        /// Returns whether generated code in <paramref name="home"/> can name <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">A member or an accessor.</param>
        /// <param name="home">The assembly the context is in.</param>
        /// <returns><see langword="true"/> for a public symbol, and for an internal one in the same assembly.</returns>
        internal static bool IsAccessible(ISymbol symbol, IAssemblySymbol home)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                Accessibility.Internal or Accessibility.ProtectedOrInternal => SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, home),
                _ => false,
            };
        }

        /// <summary>
        /// Returns whether a property is one the runtime library may serialize: an instance property with a getter
        /// that is public or marked <c>[XmlInclude]</c>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns><see langword="true"/> when its metadata is emitted.</returns>
        internal static bool IsSerializable(IPropertySymbol property)
        {
            if (property.IsStatic || property.IsIndexer || property.GetMethod is null || property.ExplicitInterfaceImplementations.Length > 0)
            {
                return false;
            }

            return (property.DeclaredAccessibility is Accessibility.Public && property.GetMethod.DeclaredAccessibility is Accessibility.Public)
                || SymbolHelpers.GetAttribute(property, SymbolHelpers.IncludeAttribute) is not null;
        }

        /// <summary>
        /// Returns whether a field is one the runtime library may serialize: an instance field the program declares
        /// that is public or marked <c>[XmlInclude]</c>.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns><see langword="true"/> when its metadata is emitted.</returns>
        internal static bool IsSerializable(IFieldSymbol field)
        {
            if (field.IsStatic || field.IsConst || field.IsImplicitlyDeclared || field.AssociatedSymbol is not null)
            {
                return false;
            }

            return field.DeclaredAccessibility is Accessibility.Public || SymbolHelpers.GetAttribute(field, SymbolHelpers.IncludeAttribute) is not null;
        }

        /// <summary>
        /// Returns the name of a type as it is written inside <c>typeof</c>, where a nullable reference type is not allowed.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The fully qualified name with no nullable annotation on a reference type.</returns>
        internal static string TypeOf(ITypeSymbol type)
        {
            return SymbolHelpers.FullName(type.IsReferenceType ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type);
        }

        #endregion

    }

}
