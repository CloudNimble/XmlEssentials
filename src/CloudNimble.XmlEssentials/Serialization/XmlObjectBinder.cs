using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Discovers members and constructors for object mapping.
    /// </summary>
    internal static class XmlObjectBinder
    {

        #region Fields

        private static readonly MethodInfo CreateStructGetterMethod = typeof(XmlObjectBinder).GetMethod(nameof(CreateStructGetter), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo CreateStructSetterMethod = typeof(XmlObjectBinder).GetMethod(nameof(CreateStructSetter), BindingFlags.NonPublic | BindingFlags.Static)!;

#if NET11_0_OR_GREATER
        /// <summary>
        /// The derived types of generic closed types that generated contexts named, by the closed type and the
        /// definition of the derived type the compiler lists for it.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type Closed, Type Definition), Type> s_closedDerived = new();

        /// <summary>
        /// Closes the definition of a generic derived type over the arguments of its closed base type and returns
        /// it, or <see langword="null"/> when it cannot take them. The field is <see langword="null"/> when nothing
        /// in the program reflects: the reflection resolver sets it when it is created.
        /// </summary>
        private static Func<Type, Type[], Type?>? s_closeGeneric;
#endif

        /// <summary>
        /// <see cref="CreateReflectedProperty"/>, found the first time the reflection resolver binds a member. It is
        /// not found when the class is loaded, because a program that has only generated contexts never needs it.
        /// </summary>
        private static MethodInfo? s_createReflectedProperty;

        #endregion

        #region Private Methods

        /// <summary>
        /// Records, on a bound member, the nulls its nullable annotations refuse.
        /// </summary>
        /// <param name="property">The bound member.</param>
        /// <param name="member">The facts of the member, which say what its annotations refuse.</param>
        /// <param name="type">The type that is bound, which the message for a constructor parameter names.</param>
        /// <param name="parameter">The constructor parameter the member fills, or null when it is set after construction.</param>
        /// <remarks>
        /// As System.Text.Json has it with <c>RespectNullableAnnotations</c>: what is read is checked against what
        /// the member, or the parameter, accepts, and what is written against what the member returns. The two
        /// differ under <c>[AllowNull]</c>, <c>[DisallowNull]</c>, <c>[MaybeNull]</c> and <c>[NotNull]</c>. A member
        /// compiled without annotations, and one whose type is a type parameter, says nothing and accepts null.
        /// </remarks>
        internal static void ApplyNullableAnnotations(XmlBoundProperty property, XmlMemberFacts member, Type type, XmlParameterFacts? parameter)
        {
            if (member.NeverReturnsNull)
            {
                property.NullWriteError = $"The property or field '{member.Name}' on type '{member.DeclaringType}' doesn't allow getting null values. Consider updating its nullability annotation.";
            }

            if (parameter is not null)
            {
                if (parameter.RefusesNull)
                {
                    property.NullReadError = $"The constructor parameter '{parameter.Name}' on type '{type}' doesn't allow null values. Consider updating its nullability annotation.";
                }

                return;
            }

            if (member.RefusesNull)
            {
                property.NullReadError = $"The property or field '{member.Name}' on type '{member.DeclaringType}' doesn't allow setting null values. Consider updating its nullability annotation.";
            }
        }

        /// <summary>
        /// Decides, for each member of a type, whether the value it already holds is populated when it is read.
        /// </summary>
        /// <param name="type">The type that is bound.</param>
        /// <param name="bound">The bound members.</param>
        /// <param name="members">The member models, in the same order.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="hasConstructorParameters">Whether the type is created through a constructor with parameters.</param>
        /// <exception cref="NotSupportedException">
        /// The type is created through a constructor with parameters, and it or one of its members is marked
        /// <see cref="XmlObjectCreationHandling.Populate"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A member marked <see cref="XmlObjectCreationHandling.Populate"/> cannot be populated, or a member is
        /// populated and the options handle references.
        /// </exception>
        /// <remarks>
        /// The rules are those of System.Text.Json. The attribute on a member comes first, then the attribute on the
        /// type, then <see cref="XmlSerializerOptions.PreferredObjectCreationHandling"/>. Only a member marked itself
        /// is refused when it cannot be populated; the attribute on the type and the option apply where they can.
        /// A type created through a constructor with parameters populates nothing.
        /// </remarks>
        internal static void ApplyObjectCreationHandling(Type type, XmlBoundProperty[] bound, List<XmlMemberModel> members, XmlSerializerOptions options, bool hasConstructorParameters)
        {
            var onType = type.GetCustomAttribute<XmlObjectCreationHandlingAttribute>(inherit: false)?.Handling;
            var populates = false;
            for (var i = 0; i < bound.Length; i++)
            {
                var onMember = members[i].Member.GetCustomAttribute<XmlObjectCreationHandlingAttribute>()?.Handling;
                if (hasConstructorParameters)
                {
                    if ((onMember ?? onType) is XmlObjectCreationHandling.Populate)
                    {
                        throw new NotSupportedException("XmlObjectCreationHandling.Populate is currently not supported in types with parameterized constructors.");
                    }

                    continue;
                }

                if ((onMember ?? onType ?? options.PreferredObjectCreationHandling) is not XmlObjectCreationHandling.Populate)
                {
                    continue;
                }

                if (bound[i].PopulateRefusal() is { } refusal)
                {
                    if (onMember is XmlObjectCreationHandling.Populate)
                    {
                        throw new InvalidOperationException($"Property '{members[i].Member.Name}' on type '{members[i].Member.DeclaringType}' is marked with XmlObjectCreationHandling.Populate {refusal}");
                    }

                    continue;
                }

                bound[i].Populates = true;
                populates = true;
            }

            if (populates && options.ReferenceHandler is not null)
            {
                throw new InvalidOperationException("XmlObjectCreationHandling.Populate is incompatible with reference handling.");
            }
        }

        /// <summary>
        /// Tells each element member that has no namespace which namespaces the other members of its local name
        /// have, so that it leaves their elements to them.
        /// </summary>
        /// <param name="properties">The bound members.</param>
        /// <param name="caseInsensitive">Whether names are compared without regard to ASCII case.</param>
        /// <remarks>
        /// A member with no namespace matches an element of its local name in any namespace. When another member
        /// has the same local name and a namespace, an element in that namespace is that member's: without this,
        /// <c>&lt;atom:link&gt;</c> would be read into the member for <c>&lt;link&gt;</c> whenever that member was
        /// tried first.
        /// </remarks>
        internal static void AssignSiblingNamespaces(XmlBoundProperty[] properties, bool caseInsensitive)
        {
            foreach (var property in properties)
            {
                if (property.IsAttribute || property.IsText || property.Utf8NamespaceUri.Length > 0)
                {
                    continue;
                }

                List<byte[]>? taken = null;
                foreach (var other in properties)
                {
                    if (other.IsAttribute || other.IsText || other.Utf8NamespaceUri.Length == 0)
                    {
                        continue;
                    }

                    if (other.Utf8LocalName.AsSpan().SequenceEqual(property.Utf8LocalName)
                        || (caseInsensitive && Ascii.EqualsIgnoreCase(other.Utf8LocalName, property.Utf8LocalName)))
                    {
                        (taken ??= []).Add(other.Utf8NamespaceUri);
                    }
                }

                property.SiblingNamespaces = taken?.ToArray();
            }
        }

        /// <summary>
        /// Builds the binding for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="options">The serializer options.</param>
        /// <returns>The members and constructor.</returns>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static XmlObjectBinding Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(XmlSerializerOptions options)
        {
            var members = Discover<T>(options);

            // An abstract class or an interface is never created: a document names one of its derived types.
            // It is bound for its members, which describe it and are what is written when an undeclared
            // derived type falls back to the declared type.
            var isAbstract = typeof(T).IsAbstract;
            var constructor = isAbstract ? null : SelectConstructor(typeof(T));
            Func<object>? create = null;
            XmlConstructorFacts? constructorFacts = null;
            if (constructor is { } chosen && chosen.GetParameters().Length > 0)
            {
                constructorFacts = XmlConstructorFacts.FromReflection(chosen, static name => IsIgnoredMember(typeof(T), name));
            }
            else if (!isAbstract)
            {
                create = typeof(T).IsValueType || constructor is { IsPublic: true }
                    ? CreatePublic<T>
                    : CreateNonPublic<T>;
            }

            if (options.RespectNullableAnnotations)
            {
                // The context caches what it reads and is not safe to share, so one is made for each type that is bound.
                ReadNullableAnnotations(new NullabilityInfoContext(), members, constructorFacts);
            }

            return Bind(typeof(T), members, constructorFacts, create, options);
        }

        /// <summary>
        /// Builds the binding of a type from metadata a source generator emitted.
        /// </summary>
        /// <typeparam name="T">The type that is bound.</typeparam>
        /// <param name="objectInfo">The metadata.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The members and constructor.</returns>
        /// <remarks>
        /// Nothing here reflects over the members of <typeparamref name="T"/>. The attributes on the type itself are
        /// read from it, which trimming allows: a type the program uses keeps its attributes.
        /// </remarks>
        internal static XmlObjectBinding Bind<T>(XmlObjectInfoValues<T> objectInfo, XmlSerializerOptions options)
        {
            var values = objectInfo.PropertyMetadataInitializer?.Invoke(options) ?? [];
            var facts = new List<XmlMemberFacts>(values.Length);
            foreach (var value in values)
            {
                facts.Add(value.CreateFacts());
            }

            XmlConstructorFacts? constructor = null;
            if (objectInfo.ObjectWithParameterizedConstructorCreator is { } createWithArguments)
            {
                var declared = objectInfo.ConstructorParameterMetadataInitializer!();
                var parameters = new XmlParameterFacts[declared.Length];
                foreach (var parameter in declared)
                {
                    parameters[parameter.Position] = new XmlParameterFacts(parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue, isIgnoredMember: false)
                    {
                        RefusesNull = !parameter.IsNullable,
                    };
                }

                constructor = new XmlConstructorFacts(parameters, arguments => createWithArguments(arguments)!);
            }

            Func<object>? create = null;
            if (constructor is null && objectInfo.ObjectCreator is { } creator)
            {
                create = () => creator()!;
            }

            var binding = Bind(typeof(T), CreateModels(facts, options), constructor, create, options);
            binding.NoMetadata = objectInfo.PropertyMetadataInitializer is null;
            return binding;
        }

        /// <summary>
        /// Builds the binding of a type from the models of its members and the facts of its constructor.
        /// </summary>
        /// <param name="type">The type that is bound.</param>
        /// <param name="members">The member models, in the order they are written. An extension-data member is taken out of the list.</param>
        /// <param name="constructor">The constructor the object is created through, or null when it is created with no arguments or never created.</param>
        /// <param name="create">Creates the object with no arguments, or null.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The members and constructor.</returns>
        /// <remarks>
        /// This is the one place a type is bound. The reflection resolver and a source-generated context both end
        /// here, with facts that came from reflection or from generated code.
        /// </remarks>
        internal static XmlObjectBinding Bind(Type type, List<XmlMemberModel> members, XmlConstructorFacts? constructor, Func<object>? create, XmlSerializerOptions options)
        {
            var extensionMember = PeelExtensionData(type, members);
            ValidateSimpleContent(type, members);
            var map = MapConstructor(constructor, members);
            var bound = new XmlBoundProperty[members.Count];
            var nextSlot = map.Length;
            for (var i = 0; i < members.Count; i++)
            {
                var constructorIndex = Array.IndexOf(map, i);
                bound[i] = members[i].Member.CreateBound(members[i], options, constructorIndex >= 0 ? constructorIndex : nextSlot++, constructorIndex);
                if (options.RespectNullableAnnotations)
                {
                    ApplyNullableAnnotations(bound[i], members[i].Member, type, constructorIndex >= 0 ? constructor!.Parameters[constructorIndex] : null);
                }
            }

            ApplyObjectCreationHandling(type, bound, members, options, constructor is not null);
            ValidateElementNames(bound, options.PropertyNameCaseInsensitive);
            AssignSiblingNamespaces(bound, options.PropertyNameCaseInsensitive);
            XmlExtensionDataBinding? extension = null;
            if (extensionMember is { } ext)
            {
                extension = new XmlExtensionDataBinding(
                    (Func<object, XmlExtensionData>)ext.Member.Getter!,
                    ext.CanSet ? (Action<object, XmlExtensionData>?)ext.Member.Setter : null);
            }

            var polymorphic = type.GetCustomAttribute<XmlPolymorphicAttribute>();
            var placement = polymorphic?.DiscriminatorPlacement ?? XmlDiscriminatorPlacement.ElementName;
            var unknown = polymorphic?.UnknownDerivedTypeHandling ?? XmlUnknownDerivedTypeHandling.FailSerialization;
            byte[] utf8DiscAttr = [];
            if (placement is XmlDiscriminatorPlacement.Attribute)
            {
                if (string.IsNullOrWhiteSpace(polymorphic!.TypeDiscriminatorPropertyName))
                {
                    throw new NotSupportedException($"[{nameof(XmlPolymorphicAttribute)}] on '{type}' uses {nameof(XmlDiscriminatorPlacement.Attribute)} and requires {nameof(XmlPolymorphicAttribute.TypeDiscriminatorPropertyName)}.");
                }

                utf8DiscAttr = Encoding.UTF8.GetBytes(polymorphic.TypeDiscriminatorPropertyName);
            }

            return new XmlObjectBinding(
                bound,
                constructor,
                map,
                create,
                nextSlot,
                extension,
                CreatePolymorphism(type, BindDerived(type, options, placement, polymorphic), placement, utf8DiscAttr, unknown, options.PropertyNameCaseInsensitive));
        }

        /// <summary>
        /// Binds the alternatives of a member that holds a choice of elements.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="declared">The type of the member, or of its items.</param>
        /// <param name="choices">The alternatives.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The choice, as the derived types of the member with the element name as the discriminator.</returns>
        /// <exception cref="NotSupportedException">
        /// An alternative names a type that is not the member's type, or two alternatives have one type or one
        /// element name.
        /// </exception>
        /// <remarks>
        /// <para>
        /// A value is written under the element name of its type, so a type can stand for one element only. An
        /// element is read as the type of its name.
        /// </para>
        /// <para>
        /// The type of an alternative may declare derived types of its own that an attribute identifies
        /// (<c>xsi:type</c>, or a discriminator attribute). They still apply inside the element of the
        /// alternative: a value of one of them is written under that element with its discriminator.
        /// </para>
        /// </remarks>
        internal static XmlPolymorphism BindChoices(XmlMemberModel member, Type declared, List<XmlElementChoice> choices, XmlSerializerOptions options)
        {
            // A named value says which element it is, so its elements may hold any types, and two of them one type.
            var isNamed = declared == typeof(XmlNamedValue);
            var bindings = new XmlDerivedBinding[choices.Count];
            for (var index = 0; index < choices.Count; index++)
            {
                var choice = choices[index];
                if (!isNamed && !declared.IsAssignableFrom(choice.Type))
                {
                    throw new NotSupportedException($"Member '{member.Member.DeclaringType}.{member.Member.Name}' has an element for the type '{choice.Type}', which is not a '{declared}'.");
                }

                var name = choice.ElementName ?? XmlTypeNames.ElementName(choice.Type, options);
                for (var earlier = 0; earlier < index; earlier++)
                {
                    if (!isNamed && bindings[earlier].Type == choice.Type)
                    {
                        throw new NotSupportedException($"Member '{member.Member.DeclaringType}.{member.Member.Name}' has more than one element for the type '{choice.Type}'. A value is written under the element of its type, so a type can have one. Declare the member as {nameof(XmlNamedValue)} to let each value name its element.");
                    }

                    if (bindings[earlier].Discriminator == name && choices[earlier].Namespace == choice.Namespace)
                    {
                        throw new NotSupportedException($"Member '{member.Member.DeclaringType}.{member.Member.Name}' has more than one element named '{name}'. An element is read as the type of its name, so a name can have one.");
                    }
                }

                var nested = BindPolymorphism(choice.Type, options);
                bindings[index] = new XmlDerivedBinding(
                    Encoding.UTF8.GetBytes(name),
                    choice.Type,
                    options.GetTypeInfo(choice.Type),
                    name,
                    choice.Namespace is null ? null : Encoding.UTF8.GetBytes(choice.Namespace))
                {
                    IsOfDeclaredType = declared.IsAssignableFrom(choice.Type),
                    Namespace = choice.Namespace,
                    Nested = nested.Derived.Length > 0 && nested.Placement is not XmlDiscriminatorPlacement.ElementName ? nested : null,
                };
            }

            return new XmlPolymorphism(declared, bindings, XmlDiscriminatorPlacement.ElementName, null, XmlUnknownDerivedTypeHandling.FailSerialization)
            {
                CaseInsensitive = options.PropertyNameCaseInsensitive,
                IsNamed = isNamed,
            };
        }

        /// <summary>
        /// Builds derived-type bindings for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The declared CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <param name="polymorphic">The <see cref="XmlPolymorphicAttribute"/> of the type, or null when it has none.</param>
        /// <returns>The derived bindings, or null when none are declared and none are inferred.</returns>
        /// <remarks>
        /// The derived types a type declares with <see cref="XmlDerivedTypeAttribute"/> are the whole list. A type
        /// that declares none has them inferred from its closed hierarchy when the attribute or the options ask.
        /// </remarks>
        internal static XmlDerivedBinding[]? BindDerived(Type type, XmlSerializerOptions options, XmlDiscriminatorPlacement placement, XmlPolymorphicAttribute? polymorphic)
        {
            var derivedList = new List<XmlDerivedBinding>();
            foreach (var attr in type.GetCustomAttributes<XmlDerivedTypeAttribute>(inherit: true))
            {
                if (attr.Namespace is not null && placement is not XmlDiscriminatorPlacement.ElementName)
                {
                    throw new NotSupportedException($"[{nameof(XmlDerivedTypeAttribute)}] for '{attr.DerivedType}' on '{type}' sets {nameof(XmlDerivedTypeAttribute.Namespace)}, which is the namespace of the element named for the derived type. The discriminator of '{type}' is not an element name.");
                }

                var disc = attr.TypeDiscriminator as string ?? attr.DerivedType.Name;
                var elementName = placement is XmlDiscriminatorPlacement.ElementName
                    ? options.ConvertElementName(disc)
                    : disc;
                derivedList.Add(new XmlDerivedBinding(
                    Encoding.UTF8.GetBytes(elementName),
                    attr.DerivedType,
                    options.GetTypeInfo(attr.DerivedType),
                    disc,
                    attr.Namespace is { } derivedNamespace ? Encoding.UTF8.GetBytes(derivedNamespace) : null)
                {
                    IsOfDeclaredType = type.IsAssignableFrom(attr.DerivedType),
                });
            }

            if (derivedList.Count == 0 && InfersDerivedTypes(type, options))
            {
                if (!IsClosedType(type))
                {
                    // Only the attribute gets here: the options infer for closed types and for nothing else.
                    throw new InvalidOperationException($"The type '{type}' enables {nameof(XmlPolymorphicAttribute)}.{nameof(XmlPolymorphicAttribute.InferClosedTypePolymorphism)} but is not a closed type, so no derived types can be inferred. Declare the type 'closed' or register its derived types using {nameof(XmlDerivedTypeAttribute)}.");
                }

#if NET11_0_OR_GREATER
                var inferred = new List<Type>();
                AddClosedDerivedTypes(type, inferred);
                foreach (var derived in inferred)
                {
                    // The name of a generic type carries its arity after a grave accent, which is no part of an XML name.
                    var name = derived.Name;
                    var arity = name.IndexOf('`');
                    var disc = arity < 0 ? name : name[..arity];
                    var elementName = placement is XmlDiscriminatorPlacement.ElementName
                        ? options.ConvertElementName(disc)
                        : disc;
                    derivedList.Add(new XmlDerivedBinding(Encoding.UTF8.GetBytes(elementName), derived, options.GetTypeInfo(derived), disc) { IsOfDeclaredType = true });
                }
#endif
            }

            return derivedList.Count > 0 ? derivedList.ToArray() : null;
        }

        /// <summary>
        /// Binds the derived types a type declares, as a value of that type is written and read.
        /// </summary>
        /// <param name="type">The declared type of a member, of an item, or of an alternative of a choice.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The derived types and where their discriminator is written.</returns>
        internal static XmlPolymorphism BindPolymorphism(Type type, XmlSerializerOptions options)
        {
            var placement = XmlDiscriminatorPlacement.ElementName;
            byte[] utf8DiscAttr = [];
            var unknown = XmlUnknownDerivedTypeHandling.FailSerialization;
            var polymorphic = type.GetCustomAttribute<XmlPolymorphicAttribute>();
            if (polymorphic is not null)
            {
                placement = polymorphic.DiscriminatorPlacement;
                unknown = polymorphic.UnknownDerivedTypeHandling;
                if (placement is XmlDiscriminatorPlacement.Attribute && polymorphic.TypeDiscriminatorPropertyName is { Length: > 0 } discriminatorName)
                {
                    utf8DiscAttr = Encoding.UTF8.GetBytes(discriminatorName);
                }
            }

            return CreatePolymorphism(type, BindDerived(type, options, placement, polymorphic), placement, utf8DiscAttr, unknown, options.PropertyNameCaseInsensitive);
        }

#if NET11_0_OR_GREATER
        /// <summary>
        /// Closes the definition of a generic derived type over the arguments of its closed base type.
        /// </summary>
        /// <param name="definition">The generic type definition the compiler lists as a derived type.</param>
        /// <param name="arguments">The type arguments of the closed base type.</param>
        /// <returns>The constructed type, or <see langword="null"/> when the arguments do not meet a constraint of the definition.</returns>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static Type? CloseGeneric(Type definition, Type[] arguments)
        {
            try
            {
                return definition.MakeGenericType(arguments);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a generic derived type of a closed type, constructed over the type arguments of the closed type.
        /// </summary>
        /// <param name="closedType">The closed type, with its type arguments.</param>
        /// <param name="definition">The definition of the derived type, as the compiler lists it.</param>
        /// <param name="arguments">The type arguments of <paramref name="closedType"/>.</param>
        /// <param name="close">Closes a generic type at run time, or <see langword="null"/> in a program that does not reflect.</param>
        /// <returns>
        /// The type a generated context named for the pair; otherwise the type <paramref name="close"/> makes; and
        /// <see langword="null"/> when nothing named it and nothing can close it, or it cannot take the arguments.
        /// </returns>
        internal static Type? ConstructClosedDerived(Type closedType, Type definition, Type[] arguments, Func<Type, Type[], Type?>? close)
        {
            return s_closedDerived.TryGetValue((closedType, definition), out var named)
                ? named
                : close?.Invoke(definition, arguments);
        }

        /// <summary>
        /// Records a derived type of a generic closed type, which a generated context names because it cannot be
        /// closed over its arguments at run time under Native AOT.
        /// </summary>
        /// <param name="closedType">The closed type, with its type arguments.</param>
        /// <param name="derivedType">The derived type, with the same type arguments.</param>
        internal static void RegisterClosedDerived(Type closedType, Type derivedType)
        {
            s_closedDerived[(closedType, derivedType.GetGenericTypeDefinition())] = derivedType;
        }

        /// <summary>
        /// Lets a generic derived type that no generated context named be closed at run time.
        /// </summary>
        /// <remarks>
        /// Only the reflection resolver calls this, so the code that closes a generic type at run time is in the
        /// program only when that resolver is.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static void UseReflection()
        {
            s_closeGeneric ??= CloseGeneric;
        }

        /// <summary>
        /// Adds the derived types of a closed type that an instance can have.
        /// </summary>
        /// <param name="closedType">The closed type.</param>
        /// <param name="found">The list the types are added to.</param>
        /// <remarks>
        /// <para>
        /// A derived type that is closed itself cannot be created, so its own derived types are added in its place.
        /// </para>
        /// <para>
        /// The compiler lists a generic derived type without its type arguments. It is a derived type of
        /// <paramref name="closedType"/> when it takes the same arguments, so it is given them. A type that
        /// takes another number of arguments, cannot take these, or derives from the closed type with other
        /// arguments is not a derived type of this one, and is left out.
        /// </para>
        /// </remarks>
        internal static void AddClosedDerivedTypes(Type closedType, List<Type> found)
        {
            foreach (var listed in closedType.GetCustomAttribute<System.Runtime.CompilerServices.IsClosedTypeAttribute>(inherit: false)!.DerivedTypes)
            {
                var derived = listed;
                if (derived.IsGenericTypeDefinition)
                {
                    var arguments = closedType.GetGenericArguments();
                    if (derived.GetGenericArguments().Length != arguments.Length)
                    {
                        continue;
                    }

                    var constructed = ConstructClosedDerived(closedType, derived, arguments, s_closeGeneric);
                    if (constructed is null)
                    {
                        continue;
                    }

                    derived = constructed;
                }

                if (!closedType.IsAssignableFrom(derived))
                {
                    continue;
                }

                if (IsClosedType(derived))
                {
                    AddClosedDerivedTypes(derived, found);
                }
                else
                {
                    found.Add(derived);
                }
            }
        }
#endif

        /// <summary>
        /// Fills the facts of a reflected member, with the reflection call that creates its bound member.
        /// </summary>
        /// <typeparam name="T">The type that is bound, which may be derived from the type that declares the member.</typeparam>
        /// <param name="member">A property or a field.</param>
        /// <returns>The facts.</returns>
        /// <remarks>
        /// The bound member is generic over the member's type, which reflection knows only as a <see cref="Type"/>, so
        /// it is closed at run time. A source-generated context supplies a delegate that was closed when the program
        /// was compiled, which is what makes it usable under Native AOT.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static XmlMemberFacts CreateFacts<T>(MemberInfo member)
        {
            var facts = new XmlMemberFacts(member);
            if (facts.MemberType == typeof(XmlExtensionData))
            {
                // The extension-data member is not bound as a member, so its accessors are made here, where the type is known.
                facts.Getter = CreateGetter<T, XmlExtensionData>(member);
                facts.Setter = facts.HasSetter ? CreateSetter<T, XmlExtensionData>(member) : null;
            }

            facts.CreateBound = static (model, options, index, constructorIndex) =>
            {
                try
                {
                    var create = s_createReflectedProperty ??= typeof(XmlObjectBinder).GetMethod(nameof(CreateReflectedProperty), BindingFlags.NonPublic | BindingFlags.Static)!;
                    return (XmlBoundProperty)create.MakeGenericMethod(typeof(T), model.Type).Invoke(null, [model, options, index, constructorIndex])!;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    throw ex.InnerException;
                }
            };
            return facts;
        }

        /// <summary>
        /// Builds a getter for <paramref name="member"/>.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The property or field.</param>
        /// <returns>The getter. The instance is passed boxed so struct setters can mutate the box.</returns>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static Func<object, TValue> CreateGetter<TDeclaring, TValue>(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                if (typeof(TDeclaring).IsValueType)
                {
                    return (Func<object, TValue>)CreateStructGetterMethod.MakeGenericMethod(typeof(TDeclaring), typeof(TValue)).Invoke(null, [property])!;
                }

                var typed = property.GetMethod!.CreateDelegate<Func<TDeclaring, TValue>>();
                return obj => typed((TDeclaring)obj);
            }

            var field = (FieldInfo)member;
            return obj => (TValue)field.GetValue(obj)!;
        }

        /// <summary>
        /// Creates type info for a member that names its own converter with <see cref="XmlConverterAttribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute on the member.</param>
        /// <param name="type">The member type.</param>
        /// <param name="wrapNullable">Wraps the converter for a member whose type is a nullable value type, or null to close the wrapper by reflection.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>Type info that reads and writes the member with that converter. It belongs to the member, not to the options.</returns>
        /// <exception cref="InvalidOperationException">The converter does not convert the member's type.</exception>
        /// <remarks>
        /// A converter for <c>T</c> on a member of type <c>T?</c> converts the value, and null is handled around it.
        /// </remarks>
        internal static XmlTypeInfo CreateMemberTypeInfo(XmlConverterAttribute attribute, Type type, XmlSerializerOptions options, Func<XmlConverter, XmlConverter>? wrapNullable)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            var converter = attribute.CreateConverter().Resolve(target, options);
            if (converter.CreateTypeInfo(target, options) is not { } typeInfo)
            {
                throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
            }

            if (target == type)
            {
                return typeInfo;
            }

            if (wrapNullable is null)
            {
                throw new InvalidOperationException($"The metadata of a member of type '{type}' does not say how the converter of '{target}' is wrapped.");
            }

            return wrapNullable(converter).CreateTypeInfo(type, options)!;
        }

        /// <summary>
        /// Turns the facts of the members of a type into the models of the ones that are serialized, in the order
        /// they are written.
        /// </summary>
        /// <param name="facts">The facts of every property and field of the type and its base types: base types first, and within a type its properties and then its fields.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The models.</returns>
        /// <remarks>
        /// <para>
        /// A field is serialized only when <see cref="XmlSerializerOptions.IncludeFields"/> is set. The rules for one
        /// member are in <see cref="TryCreateModel"/>.
        /// </para>
        /// <para>
        /// A type and its base types may declare a member of the same name. The rules are the ones
        /// <c>System.Text.Json</c> has. The members are taken from the most derived type down. A member that
        /// overrides or hides one of a base type is serialized in its place. An override that is ignored hides the
        /// property it overrides; a member declared with <see langword="new"/> that is ignored leaves the member it
        /// hides to be serialized. Two members that are not related in one of these ways and have the same XML name
        /// are an error.
        /// </para>
        /// </remarks>
        internal static List<XmlMemberModel> CreateModels(List<XmlMemberFacts> facts, XmlSerializerOptions options)
        {
            if (options.MemberOrder is XmlMemberOrder.FieldsFirst)
            {
                facts = OrderFieldsFirst(facts);
            }

            // The members are taken from the most derived type down. First, which of them are candidates: an ignored
            // override stands for the property it overrides, so that one is not serialized either.
            var candidates = new bool[facts.Count];
            var ignored = new Dictionary<string, XmlMemberFacts>(StringComparer.Ordinal);
            var hiddenBy = new XmlMemberFacts?[facts.Count];
            for (var index = facts.Count - 1; index >= 0; index--)
            {
                var member = facts[index];
                if (member.IsField && !options.IncludeFields)
                {
                    continue;
                }

                ignored.TryGetValue(member.Name, out hiddenBy[index]);
                if (hiddenBy[index] is { IsVirtual: true } hidden && member.IsVirtual && hidden.MemberType == member.MemberType)
                {
                    continue;
                }

                if (IsIgnored(member))
                {
                    ignored.TryAdd(member.Name, member);
                    continue;
                }

                candidates[index] = true;
            }

            // Then the models, in declaration order, so the first member that is mapped wrongly is the one reported.
            var models = new XmlMemberModel?[facts.Count];
            for (var index = 0; index < facts.Count; index++)
            {
                if (candidates[index] && TryCreateModel(facts[index], options, index, out var model))
                {
                    models[index] = model;
                }
            }

            // Then the names, again from the most derived type down. A name that is taken belongs to a member of a
            // more derived type: that is what overriding and hiding a member mean, and it is what an ignored member
            // of the same C# name left behind. Two element members, or two attribute members, may share a local
            // name when their namespaces differ, as <link> and <atom:link> do in a feed and href and xlink:href
            // do on a link. Anything else is two members mapped to one name.
            var members = new List<XmlMemberModel>();
            var owners = new Dictionary<string, List<XmlMemberModel>>(StringComparer.Ordinal);
            for (var index = facts.Count - 1; index >= 0; index--)
            {
                if (models[index] is not { } model)
                {
                    continue;
                }

                var member = facts[index];
                var key = (model.IsAttribute ? "a:" : model.IsText || model.IsCData ? "t:" : "e:") + model.LocalName;
                if (!owners.TryGetValue(key, out var sameName))
                {
                    owners.Add(key, [model]);
                    members.Add(model);
                    continue;
                }

                if (hiddenBy[index] is not null || sameName.Exists(owner => member.Name == owner.Member.Name && member.DeclaringType.IsAssignableFrom(owner.Member.DeclaringType)))
                {
                    continue;
                }

                if (key[0] is 't' || sameName.Exists(owner => owner.NamespaceUri == model.NamespaceUri))
                {
                    throw new InvalidOperationException($"The type maps more than one member to the {(model.IsAttribute ? "attribute" : "element")} name '{model.LocalName}'.");
                }

                sameName.Add(model);
                members.Add(model);
            }

            members.Sort(static (left, right) =>
            {
                var kind = Rank(left).CompareTo(Rank(right));
                if (kind != 0)
                {
                    return kind;
                }

                var leftOrder = left.Order >= 0 ? left.Order : int.MaxValue;
                var rightOrder = right.Order >= 0 ? right.Order : int.MaxValue;
                var compared = leftOrder.CompareTo(rightOrder);
                return compared != 0 ? compared : left.DeclarationIndex.CompareTo(right.DeclarationIndex);
            });
            return members;
        }

        /// <summary>
        /// Creates a <typeparamref name="T"/> through a parameterless constructor that is not public.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <returns>The instance.</returns>
        /// <remarks>
        /// An exception the constructor throws is rethrown as it is, not wrapped in a <see cref="TargetInvocationException"/>.
        /// </remarks>
        internal static object CreateNonPublic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>()
        {
            try
            {
                return Activator.CreateInstance(typeof(T), nonPublic: true)!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        }

        /// <summary>
        /// Creates the polymorphism of <paramref name="type"/> from the list of derived types it declares or inherits.
        /// </summary>
        /// <param name="type">The declared CLR type.</param>
        /// <param name="known">The whole list, as <see cref="BindDerived"/> returns it, or null.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <param name="utf8DiscriminatorAttributeName">The discriminator attribute local name.</param>
        /// <param name="unknown">How a runtime type that is not declared is handled.</param>
        /// <param name="caseInsensitive">Whether the element name of a derived type is matched without regard to ASCII case.</param>
        /// <returns>The polymorphism.</returns>
        /// <remarks>
        /// A derived type inherits the list its base declares, which names the derived type itself and the types
        /// beside it. Only a type that derives from <paramref name="type"/> is one a value may be written as, so a
        /// value of the declared type is written under the name of its member with no discriminator, as
        /// <c>System.Xml.Serialization</c> writes it. The whole list is kept for reading: a document that identifies
        /// the declared type is read as it, and one that identifies a type beside it is refused.
        /// </remarks>
        internal static XmlPolymorphism CreatePolymorphism(Type type, XmlDerivedBinding[]? known, XmlDiscriminatorPlacement placement, byte[] utf8DiscriminatorAttributeName, XmlUnknownDerivedTypeHandling unknown, bool caseInsensitive)
        {
            if (known is null)
            {
                return new XmlPolymorphism(type, null, placement, utf8DiscriminatorAttributeName, unknown) { CaseInsensitive = caseInsensitive };
            }

            var derived = new List<XmlDerivedBinding>(known.Length);
            foreach (var binding in known)
            {
                if (binding.Type != type && type.IsAssignableFrom(binding.Type))
                {
                    derived.Add(binding);
                }
            }

            return new XmlPolymorphism(type, derived.Count == known.Length ? known : derived.ToArray(), placement, utf8DiscriminatorAttributeName, unknown, known) { CaseInsensitive = caseInsensitive };
        }

        /// <summary>
        /// Creates a typed bound member.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The member model.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="index">The slot the member uses while an object is read.</param>
        /// <param name="constructorIndex">The constructor parameter index, or <c>-1</c>.</param>
        /// <returns>The bound member.</returns>
        internal static XmlBoundProperty CreateProperty<TDeclaring, TValue>(XmlMemberModel member, XmlSerializerOptions options, int index, int constructorIndex)
        {
            var mixed = member.Member.IsDefined(typeof(XmlMixedAttribute));
            var typeForInfo = mixed ? typeof(XmlMixedText) : member.ConverterType;
            var typeInfo = member.Member.GetCustomAttribute<XmlConverterAttribute>() is { } memberConverter
                ? CreateMemberTypeInfo(memberConverter, typeForInfo, options, member.Member.WrapNullable)
                : options.GetTypeInfo(typeForInfo);
            var name = new XmlName(member.LocalName, member.NamespaceUri.Length == 0 ? null : member.NamespaceUri);
            var info = new XmlPropertyInfo(name, typeInfo, member.IsNillable, member.IsRequired, member.Order, member.IsAttribute, member.IsText);
            var utf8Name = Encoding.UTF8.GetBytes(member.LocalName);
            var utf8Ns = member.NamespaceUri.Length == 0 ? [] : Encoding.UTF8.GetBytes(member.NamespaceUri);
            var getter = (Func<object, TValue>?)member.Member.Getter
                ?? throw new InvalidOperationException($"The metadata of member '{member.Member.Name}' on type '{member.Member.DeclaringType}' has no getter.");
            // A member that is never read has no setter as far as reading goes. A constructor parameter of the same
            // name is still filled, as System.Text.Json fills it.
            var neverRead = member.Ignore is XmlIgnoreCondition.WhenReading && constructorIndex < 0;
            if (neverRead && member.IsRequired)
            {
                throw new InvalidOperationException($"The member '{member.Member.Name}' on type '{member.Member.DeclaringType}' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read.");
            }

            var setter = member.CanSet && !neverRead
                ? (Action<object, TValue>?)member.Member.Setter
                    ?? throw new InvalidOperationException($"The metadata of member '{member.Member.Name}' on type '{member.Member.DeclaringType}' has no setter.")
                : null;
            XmlCollectionBinding? collection = null;
            Type? collectionElement = null;
            if (member.IsCollection && member.Member.IsCollection(out var elementType))
            {
                collectionElement = elementType;
                var itemInfoType = mixed ? typeof(XmlMixedText) : elementType;
                var utf8ItemName = Encoding.UTF8.GetBytes(member.ItemLocalName);
                var itemInfo = options.GetTypeInfo(itemInfoType);
                var createBinding = member.Member.CreateCollectionBinding
                    ?? throw new InvalidOperationException($"The metadata of member '{member.Member.Name}' on type '{member.Member.DeclaringType}' does not say how its collection is made.");
                collection = createBinding(member.IsWrapped, utf8ItemName, itemInfo, mixed);
                if (member.ItemNamespaceUri is { } itemNamespace)
                {
                    collection.SetItemNamespace(Encoding.UTF8.GetBytes(itemNamespace));
                }
            }

            var derivedSource = mixed ? null : collectionElement ?? member.ConverterType;
            var polymorphism = derivedSource is null ? XmlPolymorphism.None : BindPolymorphism(derivedSource, options);

            if (member.Choices is { } choices)
            {
                // The alternatives the member names stand in place of the derived types its type declares.
                polymorphism = BindChoices(member, collectionElement ?? member.ConverterType, choices, options);
            }

            return new XmlBoundProperty<TDeclaring, TValue>(info, index, utf8Name, utf8Ns, typeInfo, getter, setter, member.Ignore, constructorIndex, member.IsCData, collection, polymorphism)
            {
                HeldBoxFactory = member.Member.CreateHeldBox,
                NoNamespace = member.IsUnqualified,
            };
        }

        /// <summary>
        /// Creates a <typeparamref name="T"/> through its public parameterless constructor, or as a default value type.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <returns>The instance.</returns>
        /// <remarks>
        /// An exception the constructor throws is rethrown as it is, not wrapped in a <see cref="TargetInvocationException"/>.
        /// </remarks>
        internal static object CreatePublic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
        {
            try
            {
                return Activator.CreateInstance<T>()!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        }

        /// <summary>
        /// Creates a typed bound member for the reflection resolver.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The member model, whose facts were read from a <see cref="MemberInfo"/>.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="index">The slot the member uses while an object is read.</param>
        /// <param name="constructorIndex">The constructor parameter index, or <c>-1</c>.</param>
        /// <returns>The bound member.</returns>
        /// <remarks>
        /// Everything that has to be found by reflection or closed over a type at run time is made here and put
        /// in the member's facts: the accessors, the wrapper of a nullable value, the box of a value type, and
        /// the mapping of a collection. <see cref="CreateProperty"/> then binds the member from its facts alone,
        /// as it binds one whose facts a generated context wrote, so it holds no reflection of its own.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static XmlBoundProperty CreateReflectedProperty<TDeclaring, TValue>(XmlMemberModel member, XmlSerializerOptions options, int index, int constructorIndex)
        {
            var facts = member.Member;
            var info = facts.Member!;
            var type = typeof(TValue);
            facts.Getter ??= CreateGetter<TDeclaring, TValue>(info);
            if (member.CanSet)
            {
                facts.Setter ??= CreateSetter<TDeclaring, TValue>(info);
            }

            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                facts.WrapNullable ??= converter => (XmlConverter)Activator.CreateInstance(typeof(XmlNullableConverter<>).MakeGenericType(underlying), converter)!;
            }
            else if (type.IsValueType)
            {
                facts.CreateHeldBox ??= () => Activator.CreateInstance(typeof(XmlStructHeldBox<>).MakeGenericType(type))!;
            }

            if (facts.IsCollection(out var elementType))
            {
                facts.CreateCollectionBinding ??= (wrapped, utf8ItemName, itemTypeInfo, mixed) =>
                    XmlCollectionBinding.Create(type, mixed ? typeof(XmlMixedContentItem) : elementType, wrapped, utf8ItemName, itemTypeInfo, mixed);
            }

            return CreateProperty<TDeclaring, TValue>(member, options, index, constructorIndex);
        }

        /// <summary>
        /// Builds a setter for <paramref name="member"/>.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The property or field.</param>
        /// <returns>The setter. The instance is passed boxed so struct setters can mutate the box.</returns>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static Action<object, TValue>? CreateSetter<TDeclaring, TValue>(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                if (typeof(TDeclaring).IsValueType)
                {
                    return (Action<object, TValue>)CreateStructSetterMethod.MakeGenericMethod(typeof(TDeclaring), typeof(TValue)).Invoke(null, [property])!;
                }

                var typed = property.SetMethod!.CreateDelegate<Action<TDeclaring, TValue>>();
                return (obj, value) => typed((TDeclaring)obj, value);
            }

            var field = (FieldInfo)member;
            return (obj, value) => field.SetValue(obj, value);
        }

        /// <summary>
        /// Builds a getter for a property of the struct <typeparamref name="TStruct"/>.
        /// </summary>
        /// <typeparam name="TStruct">The struct type.</typeparam>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The getter. It reads from the boxed struct in place.</returns>
        internal static Func<object, TValue> CreateStructGetter<TStruct, TValue>(PropertyInfo property)
            where TStruct : struct
        {
            var typed = property.GetMethod!.CreateDelegate<XmlStructGetter<TStruct, TValue>>();
            return instance => typed(ref Unsafe.Unbox<TStruct>(instance));
        }

        /// <summary>
        /// Builds a setter for a property of the struct <typeparamref name="TStruct"/>.
        /// </summary>
        /// <typeparam name="TStruct">The struct type.</typeparam>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The setter. It changes the boxed struct in place.</returns>
        internal static Action<object, TValue> CreateStructSetter<TStruct, TValue>(PropertyInfo property)
            where TStruct : struct
        {
            var typed = property.SetMethod!.CreateDelegate<XmlStructSetter<TStruct, TValue>>();
            return (instance, value) => typed(ref Unsafe.Unbox<TStruct>(instance), value);
        }

        /// <summary>
        /// Discovers serializable members of <typeparamref name="T"/> from most-base to most-derived.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="options">The serializer options.</param>
        /// <returns>The members in declaration order, then explicit order.</returns>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static List<XmlMemberModel> Discover<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(XmlSerializerOptions options)
        {
            var types = new Stack<Type>();
            for (var type = typeof(T); type is not null && type != typeof(object); type = type.BaseType)
            {
                types.Push(type);
            }

            var facts = new List<XmlMemberFacts>();
            while (types.Count > 0)
            {
                var type = types.Pop();
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    facts.Add(CreateFacts<T>(property));
                }

                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    facts.Add(CreateFacts<T>(field));
                }
            }

            return CreateModels(facts, options);
        }

        /// <summary>
        /// Finds the member a constructor parameter fills.
        /// </summary>
        /// <param name="members">The members.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The index of the member, or -1 when no member has that name.</returns>
        /// <remarks>
        /// A parameter fills the member with the same C# name or the same XML name. Case does not matter,
        /// because a parameter is conventionally <c>name</c> where its property is <c>Name</c>. A member
        /// spelled exactly like the parameter is preferred, so two members that differ only by case each
        /// take their own parameter.
        /// </remarks>
        internal static int FindConstructorMember(List<XmlMemberModel> members, string parameterName)
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Member.Name.Equals(parameterName, StringComparison.Ordinal) || members[i].LocalName.Equals(parameterName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Member.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase) || members[i].LocalName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns whether the derived types of <paramref name="type"/> are to be inferred from its closed hierarchy.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The serializer options, or null when the question is asked without them.</param>
        /// <returns>
        /// What <see cref="XmlPolymorphicAttribute.InferClosedTypePolymorphism"/> says when it is set on the type.
        /// Otherwise <see langword="true"/> for a closed type when the options ask for it, or when there are no
        /// options to ask.
        /// </returns>
        internal static bool InfersDerivedTypes(Type type, XmlSerializerOptions? options)
        {
            if (type.GetCustomAttribute<XmlPolymorphicAttribute>(inherit: false)?.InferClosedTypePolymorphismOrNull is { } asked)
            {
                return asked;
            }

            return (options is null || options.InferClosedTypePolymorphism) && IsClosedType(type);
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a closed type: a class declared with the
        /// <see langword="closed"/> modifier, whose derived types the compiler lists.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns><see langword="true"/> for a closed type. Always <see langword="false"/> before .NET 11, which has no closed types.</returns>
        internal static bool IsClosedType(Type type)
        {
#if NET11_0_OR_GREATER
            return type.IsDefined(typeof(System.Runtime.CompilerServices.IsClosedTypeAttribute), inherit: false);
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns whether <paramref name="member"/> would be serialized and is marked never to be.
        /// </summary>
        /// <param name="member">The facts of a property or a field.</param>
        /// <returns>
        /// <see langword="true"/> for a member that is public or included, can be read, and has
        /// <see cref="XmlIgnoreAttribute"/> with <see cref="XmlIgnoreCondition.Always"/>.
        /// </returns>
        /// <remarks>
        /// Such a member still hides the member of a base type that it overrides, which a member that was never a
        /// candidate does not.
        /// </remarks>
        internal static bool IsIgnored(XmlMemberFacts member)
        {
            return !member.IsIndexer
                && (member.IsPublic || member.IsDefined(typeof(XmlIncludeAttribute)))
                && member.HasGetter
                && member.GetCustomAttribute<XmlIgnoreAttribute>() is { Condition: XmlIgnoreCondition.Always };
        }

        /// <summary>
        /// Returns whether a constructor parameter belongs to a member that is always ignored.
        /// </summary>
        /// <param name="type">The type that declares the constructor.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns><see langword="true"/> if a property or field with that name, in any letter case, is marked <c>[XmlIgnore]</c>.</returns>
        /// <remarks>
        /// Such a parameter is never in the document, so the constructor is called with the default of its type.
        /// A positional record parameter marked <c>[property: XmlIgnore]</c> is the usual case.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static bool IsIgnoredMember(Type type, string parameterName)
        {
            foreach (var member in type.GetMember(parameterName, MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase))
            {
                if (member.GetCustomAttribute<XmlIgnoreAttribute>() is { Condition: XmlIgnoreCondition.Always })
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is mapped as an object with properties.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The serializer options, or null when the question is asked without them.</param>
        /// <returns>
        /// <see langword="true"/> for a concrete class or struct that is not a built-in primitive, and for an
        /// abstract class or an interface that declares derived types with <see cref="XmlDerivedTypeAttribute"/>
        /// or has them inferred from its closed hierarchy.
        /// </returns>
        /// <remarks>
        /// An abstract type with no declared derived types stays unsupported: nothing could be created for it
        /// when reading, and writing a value as its declared type would silently drop the members of the
        /// value's own type.
        /// </remarks>
        internal static bool IsObjectType(Type type, XmlSerializerOptions? options = null)
        {
            if (type.IsPrimitive || type.IsEnum || type.IsArray || type.IsPointer
                || type == typeof(object) || type == typeof(string) || typeof(Delegate).IsAssignableFrom(type)
                || Nullable.GetUnderlyingType(type) is not null)
            {
                return false;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                return type.IsDefined(typeof(XmlDerivedTypeAttribute), inherit: true) || InfersDerivedTypes(type, options);
            }

            return type.IsClass || type.IsValueType;
        }

        /// <summary>
        /// Returns whether a member of <paramref name="type"/> can be an attribute or the text of its element.
        /// </summary>
        /// <param name="type">The member type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>
        /// <see langword="true"/> for a built-in scalar, and for a type the caller supplied a converter for. That
        /// converter decides whether it can write one value: its <see cref="XmlConverter{T}.WriteAsAttribute"/> and
        /// <see cref="XmlConverter{T}.ReadLexical"/> say so when they are called.
        /// </returns>
        internal static bool IsSingleValue(Type type, XmlSerializerOptions options)
        {
            return XmlBuiltInConverters.IsScalar(type) || options.HasCustomConverter(Nullable.GetUnderlyingType(type) ?? type);
        }

        /// <summary>
        /// Maps each constructor parameter to a member index.
        /// </summary>
        /// <param name="constructor">The constructor, or null.</param>
        /// <param name="members">The members.</param>
        /// <returns>The map.</returns>
        internal static int[] MapConstructor(XmlConstructorFacts? constructor, List<XmlMemberModel> members)
        {
            if (constructor is null)
            {
                return [];
            }

            var parameters = constructor.Parameters;
            var map = new int[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                map[i] = -1;
                var name = parameters[i].Name;
                if (name is null)
                {
                    continue;
                }

                map[i] = FindConstructorMember(members, name);
                if (map[i] < 0 && !parameters[i].HasDefaultValue && !parameters[i].IsIgnoredMember)
                {
                    throw new NotSupportedException($"The constructor parameter '{name}' is not a serialized member.");
                }
            }

            return map;
        }

        /// <summary>
        /// Returns the members with the fields of each class before its properties.
        /// </summary>
        /// <param name="facts">The members, those of one class after those of another.</param>
        /// <returns>
        /// The same members. The classes keep their order, and so do the fields of a class among themselves and
        /// its properties among themselves.
        /// </returns>
        internal static List<XmlMemberFacts> OrderFieldsFirst(List<XmlMemberFacts> facts)
        {
            var classes = new List<Type>();
            foreach (var member in facts)
            {
                if (!classes.Contains(member.DeclaringType))
                {
                    classes.Add(member.DeclaringType);
                }
            }

            var ordered = new List<XmlMemberFacts>(facts.Count);
            foreach (var declaring in classes)
            {
                ordered.AddRange(facts.FindAll(member => member.DeclaringType == declaring && member.IsField));
                ordered.AddRange(facts.FindAll(member => member.DeclaringType == declaring && !member.IsField));
            }

            return ordered;
        }

        /// <summary>
        /// Removes the extension-data member from <paramref name="members"/> and returns it.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="members">The discovered members.</param>
        /// <returns>The extension-data member, or null.</returns>
        internal static XmlMemberModel? PeelExtensionData(Type type, List<XmlMemberModel> members)
        {
            XmlMemberModel? extension = null;
            for (var i = members.Count - 1; i >= 0; i--)
            {
                if (!members[i].Member.IsDefined(typeof(XmlExtensionDataAttribute)))
                {
                    continue;
                }

                if (extension is not null)
                {
                    throw new NotSupportedException($"The type '{type}' has more than one [{nameof(XmlExtensionDataAttribute)}].");
                }

                extension = members[i];
                members.RemoveAt(i);
            }

            return extension;
        }

        /// <summary>
        /// Ranks a member for write order: attributes, then simple content, then child elements.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The rank.</returns>
        internal static int Rank(XmlMemberModel member)
        {
            if (member.IsAttribute)
            {
                return 0;
            }

            return member.IsText || member.IsCData ? 1 : 2;
        }

        /// <summary>
        /// Reads the nullable annotations of reflected members and constructor parameters into their facts.
        /// </summary>
        /// <param name="nullability">Reads the annotations.</param>
        /// <param name="members">The member models.</param>
        /// <param name="constructor">The constructor, or null.</param>
        /// <remarks>
        /// Reading the annotations takes reflection, so the reflection resolver does it only when the options ask
        /// for them. A source-generated context has them already: the generator read them from the symbols.
        /// </remarks>
        internal static void ReadNullableAnnotations(NullabilityInfoContext nullability, List<XmlMemberModel> members, XmlConstructorFacts? constructor)
        {
            foreach (var model in members)
            {
                var facts = model.Member;
                var info = facts.Member is PropertyInfo propertyInfo ? nullability.Create(propertyInfo) : nullability.Create((FieldInfo)facts.Member!);
                facts.NeverReturnsNull = info.ReadState is NullabilityState.NotNull;
                facts.RefusesNull = info.WriteState is NullabilityState.NotNull;
            }

            if (constructor is null)
            {
                return;
            }

            foreach (var parameter in constructor.Parameters)
            {
                parameter.RefusesNull = nullability.Create(parameter.Parameter!).WriteState is NullabilityState.NotNull;
            }
        }

        /// <summary>
        /// Chooses the constructor used to create <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The constructor, or null to use <see cref="Activator.CreateInstance{T}"/>.</returns>
        /// <exception cref="NotSupportedException">
        /// More than one constructor is marked; or the type has no public parameterless constructor and more than
        /// one public constructor with parameters, none of them marked.
        /// </exception>
        /// <remarks>
        /// The rule is the one System.Text.Json has. A constructor marked <see cref="XmlConstructorAttribute"/>
        /// is used. Otherwise the public parameterless constructor is used, and a value type is created as its
        /// default. Otherwise the one public constructor with parameters is used.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static ConstructorInfo? SelectConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ConstructorInfo? marked = null;
            foreach (var constructor in constructors)
            {
                if (!constructor.IsDefined(typeof(XmlConstructorAttribute), inherit: false))
                {
                    continue;
                }

                if (marked is not null)
                {
                    throw new NotSupportedException($"The type '{type}' has more than one constructor marked [{nameof(XmlConstructorAttribute)}].");
                }

                marked = constructor;
            }

            if (marked is not null)
            {
                return marked;
            }

            ConstructorInfo? parameterless = null;
            ConstructorInfo? parameterized = null;
            var parameterizedCount = 0;
            foreach (var constructor in constructors)
            {
                if (!constructor.IsPublic)
                {
                    continue;
                }

                if (constructor.GetParameters().Length == 0)
                {
                    parameterless = constructor;
                    continue;
                }

                parameterized = constructor;
                parameterizedCount++;
            }

            // A key and value pair has no setters and no attribute could be put on its constructor, so it is
            // created through the one it has.
            var isPair = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
            if (!isPair && (parameterless is not null || type.IsValueType))
            {
                return parameterless;
            }

            if (parameterizedCount > 1)
            {
                throw new NotSupportedException($"The type '{type}' has more than one constructor that can bind its members. Mark one with [{nameof(XmlConstructorAttribute)}].");
            }

            return parameterized;
        }

        /// <summary>
        /// Builds a member model, or returns false when the member is not serialized.
        /// </summary>
        /// <param name="member">The facts of the property or field.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="declarationIndex">The discovery index.</param>
        /// <param name="model">When this method returns true, the member.</param>
        /// <returns><see langword="true"/> when the member is serialized.</returns>
        internal static bool TryCreateModel(XmlMemberFacts member, XmlSerializerOptions options, int declarationIndex, out XmlMemberModel model)
        {
            // The checks that turn a member away come first, the cheapest first: the two that read a flag, and then
            // the two that look for an attribute.
            model = default;
            if (member.IsIndexer || !member.HasGetter)
            {
                return false;
            }

            if (!member.IsPublic && !member.IsDefined(typeof(XmlIncludeAttribute)))
            {
                return false;
            }

            if (member.GetCustomAttribute<XmlIgnoreAttribute>() is { Condition: XmlIgnoreCondition.Always })
            {
                return false;
            }

            var memberType = member.MemberType;
            if (member.IsDefined(typeof(XmlExtensionDataAttribute)))
            {
                if (memberType != typeof(XmlExtensionData))
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is marked [{nameof(XmlExtensionDataAttribute)}] but is not {nameof(XmlExtensionData)}.");
                }

                var extensionCanSet = member.HasSetter;
                model = new XmlMemberModel(member, memberType, member.Name, "", -1, declarationIndex, false, true, XmlIgnoreCondition.Never, extensionCanSet, false, false, false);
                return true;
            }

            // A converter named on the member reads and writes the whole value, whatever its type: it may be an
            // attribute or text, and a collection is not taken apart into items.
            var hasConverter = member.IsDefined(typeof(XmlConverterAttribute));
            var text = member.GetCustomAttribute<XmlTextAttribute>();
            var cdata = member.GetCustomAttribute<XmlCDataAttribute>();
            var elements = member.GetCustomAttributes<XmlElementAttribute>();
            XmlElementAttribute? element = null;
            List<XmlElementAttribute>? typed = null;
            foreach (var candidate in elements)
            {
                if (candidate.Type is not null)
                {
                    typed ??= [];
                    typed.Add(candidate);
                    continue;
                }

                if (element is not null || typed is not null)
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' has more than one [{nameof(XmlElementAttribute)}] or a typed element choice. Polymorphic element choice is not enabled for this type.");
                }

                element = candidate;
            }

            if (typed is { Count: > 0 } && element is not null)
            {
                throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' has more than one [{nameof(XmlElementAttribute)}] or a typed element choice. Polymorphic element choice is not enabled for this type.");
            }

            var attribute = member.GetCustomAttribute<XmlAttributeAttribute>();
            if (attribute is not null && element is not null)
            {
                throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as both an XML attribute and an element.");
            }

            if ((text is not null || cdata is not null) && (attribute is not null || element is not null))
            {
                throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as both simple content and an attribute or element.");
            }

            var canSet = member.HasSetter;
            if (!canSet
                && ((!member.IsField && options.IgnoreReadOnlyProperties) || (member.IsField && options.IgnoreReadOnlyFields))
                && !member.IsCollection(out _)
                && !XmlCollectionShape.IsDictionary(memberType, out _, out _))
            {
                // A collection needs no setter to be written, or to be filled, so it is not a read-only member
                // in this sense. System.Text.Json makes the same exception.
                return false;
            }

            // As the default for every member, WhenWriting and WhenReading have no effect: System.Text.Json accepts
            // them on the options and applies them only where a member is marked. A member with no mark is then
            // written and read as under Never.
            var ignore = member.GetCustomAttribute<XmlIgnoreAttribute>()?.Condition
                ?? (options.DefaultIgnoreCondition is XmlIgnoreCondition.WhenWriting or XmlIgnoreCondition.WhenReading ? XmlIgnoreCondition.Never : options.DefaultIgnoreCondition);
            if (ignore is XmlIgnoreCondition.WhenWritingNullReference)
            {
                ignore = Nullable.GetUnderlyingType(memberType) is null ? XmlIgnoreCondition.WhenWritingNull : XmlIgnoreCondition.Never;
            }

            var xmlName = member.GetCustomAttribute<XmlPropertyNameAttribute>()?.Name;
            List<XmlElementChoice>? choices = null;
            var isUnqualified = false;
            var isAttribute = false;
            var isText = false;
            var isCData = false;
            string local;
            var ns = "";
            if (text is not null || cdata is not null)
            {
                if (!IsSingleValue(memberType, options) && !hasConverter)
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as XML text. Complex types cannot be simple content.");
                }

                isText = true;
                isCData = cdata is not null;
                local = xmlName ?? member.Name;
            }
            else if (attribute is not null)
            {
                if (!IsSingleValue(memberType, options) && !hasConverter)
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as an XML attribute. Complex types cannot be attributes.");
                }

                isAttribute = true;
                local = xmlName
                    ?? (attribute.AttributeName is { Length: > 0 } attributeName ? attributeName : options.ConvertAttributeName(member.Name));
                ns = attribute.Namespace ?? "";
            }
            else if (typed is [var first])
            {
                local = xmlName
                    ?? (first.ElementName is { Length: > 0 } typedName ? typedName : options.ConvertElementName(member.Name));
                ns = first.Namespace ?? "";
                isUnqualified = first.Namespace is { Length: 0 };
            }
            else if (typed is { Count: > 1 })
            {
                // A choice: each element name stands for a type. A value of the member's own type is written
                // under the member's name.
                local = xmlName ?? options.ConvertElementName(member.Name);
                choices = typed.ConvertAll(static choice => new XmlElementChoice(choice.ElementName, choice.Namespace, choice.Type!));
            }
            else if (element is not null)
            {
                local = xmlName
                    ?? (element.ElementName is { Length: > 0 } elementName ? elementName : options.ConvertElementName(member.Name));
                ns = element.Namespace ?? "";
                isUnqualified = element.Namespace is { Length: 0 };
            }
            else if (options.DefaultPropertyMapping is XmlPropertyMapping.Attribute && XmlBuiltInConverters.IsScalar(memberType))
            {
                isAttribute = true;
                local = xmlName ?? options.ConvertAttributeName(member.Name);
            }
            else
            {
                local = xmlName ?? options.ConvertElementName(member.Name);
            }

            var order = member.GetCustomAttribute<XmlPropertyOrderAttribute>()?.Order ?? element?.Order ?? typed?[0].Order ?? -1;
            if (order < 0)
            {
                order = -1;
            }

            var isCollection = false;
            var isWrapped = false;
            var itemLocal = "";
            string? itemNamespace = null;
            if (!isAttribute && !isText && !hasConverter && member.IsCollection(out var elementType))
            {
                isCollection = true;
                var array = member.GetCustomAttribute<XmlArrayAttribute>();
                XmlArrayItemAttribute? arrayItem = null;
                List<XmlElementChoice>? itemChoices = null;
                foreach (var candidate in member.GetCustomAttributes<XmlArrayItemAttribute>())
                {
                    if (arrayItem is { Type: not null } && candidate.Type is not null)
                    {
                        // More than one is a choice of items, and then each names its type.
                        itemChoices ??= [new XmlElementChoice(arrayItem.ElementName, arrayItem.Namespace, arrayItem.Type)];
                        itemChoices.Add(new XmlElementChoice(candidate.ElementName, candidate.Namespace, candidate.Type));
                        continue;
                    }

                    if (arrayItem is not null)
                    {
                        throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' has more than one [{nameof(XmlArrayItemAttribute)}] or a typed item choice. Polymorphic collection items are not enabled for this type.");
                    }

                    arrayItem = candidate;
                }

                // [XmlArray] wraps a collection, and so does the default of the options unless [XmlElement] says
                // the items are the member's own elements.
                isWrapped = array is not null
                    || (options.DefaultCollectionMapping is XmlCollectionMapping.Wrapped && element is null && typed is not { Count: > 0 });
                if (array is not null)
                {
                    local = xmlName
                        ?? (array.ElementName is { Length: > 0 } arrayName ? arrayName : options.ConvertElementName(member.Name));
                    ns = array.Namespace ?? ns;
                    isUnqualified = array.Namespace is { Length: 0 };
                    if (order < 0 && array.Order >= 0)
                    {
                        order = array.Order;
                    }
                }

                if (isWrapped && itemChoices is not null)
                {
                    // The items of the wrapper are the choice. No one of them names the item.
                    choices = itemChoices;
                    arrayItem = null;
                }

                var itemClr = arrayItem?.Type ?? elementType;
                itemLocal = arrayItem?.ElementName is { Length: > 0 } itemName
                    ? itemName
                    : isWrapped
                        ? XmlTypeNames.ElementName(itemClr, options)
                        : local;

                // Inside a wrapper an item has a name of its own, and so a namespace of its own. With no wrapper the
                // item is the member's element, which has the member's namespace.
                itemNamespace = isWrapped ? arrayItem?.Namespace : null;
            }

            var isRequired = member.IsDefined(typeof(XmlRequiredAttribute))
                || member.IsRequiredMember;
            var isNillable = !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) is not null || element is { IsNullable: true } || typed is [{ IsNullable: true }, ..] || isCollection;
            model = new XmlMemberModel(member, memberType, local, ns, order, declarationIndex, isRequired, isNillable, ignore, canSet, isAttribute, isText, isCData, isCollection, isWrapped, itemLocal, typed is [var only] ? only.Type : null, itemNamespace)
            {
                Choices = choices,
                IsUnqualified = isUnqualified,
            };
            return true;
        }

        /// <summary>
        /// Throws when an element name could belong to two members: a member's own name, or the name one of its
        /// derived types is written under, is also used by another member. The derived types of a collection in a
        /// wrapper are not counted, because they are written inside the wrapper.
        /// </summary>
        /// <param name="properties">The bound members.</param>
        /// <param name="caseInsensitive">Whether names are compared without regard to case.</param>
        /// <exception cref="InvalidOperationException">Two members claim the same element name.</exception>
        /// <remarks>
        /// Two members with the same name are rejected when they are discovered. This catches the case that check
        /// cannot see, because derived types are only known once a member is bound.
        /// </remarks>
        internal static void ValidateElementNames(XmlBoundProperty[] properties, bool caseInsensitive)
        {
            var owners = new Dictionary<string, XmlBoundProperty>(caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var property in properties)
            {
                if (property.IsAttribute || property.IsText || !property.ClaimsDerivedElementNames)
                {
                    continue;
                }

                foreach (var derived in property.Polymorphism.Derived)
                {
                    owners[Encoding.UTF8.GetString(derived.Utf8Name)] = property;
                }
            }

            foreach (var property in properties)
            {
                if (property.IsAttribute || property.IsText)
                {
                    continue;
                }

                if (owners.TryGetValue(property.Info.Name.LocalName, out var owner) && owner != property)
                {
                    throw new InvalidOperationException($"The type maps more than one member to the element name '{property.Info.Name.LocalName}'.");
                }

                foreach (var derived in property.ClaimsDerivedElementNames ? property.Polymorphism.Derived : [])
                {
                    if (owners[Encoding.UTF8.GetString(derived.Utf8Name)] != property)
                    {
                        throw new InvalidOperationException($"The type maps more than one member to the element name '{Encoding.UTF8.GetString(derived.Utf8Name)}'.");
                    }
                }
            }
        }

        /// <summary>
        /// Throws when a type has two simple-content members, or simple content mixed with child elements.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="members">The members.</param>
        internal static void ValidateSimpleContent(Type type, List<XmlMemberModel> members)
        {
            var textCount = 0;
            var mixedCount = 0;
            var elementCount = 0;
            foreach (var member in members)
            {
                if (member.IsText || member.IsCData)
                {
                    textCount++;
                    continue;
                }

                if (member.Member.IsDefined(typeof(XmlMixedAttribute)))
                {
                    // Mixed content is read into and written from XmlMixedContentItem only. A member of another
                    // type would be written as nothing and could not be read, so it is refused here.
                    if (!member.Member.IsCollection(out var mixedItem) || mixedItem != typeof(XmlMixedContentItem))
                    {
                        throw new NotSupportedException($"Member '{member.Member.DeclaringType}.{member.Member.Name}' is marked [{nameof(XmlMixedAttribute)}] but is not a collection of {nameof(XmlMixedContentItem)}.");
                    }

                    mixedCount++;
                    continue;
                }

                if (!member.IsAttribute)
                {
                    elementCount++;
                }
            }

            if (mixedCount > 1)
            {
                throw new NotSupportedException($"The type '{type}' has more than one [{nameof(XmlMixedAttribute)}].");
            }

            if (mixedCount > 0 && (elementCount > 0 || textCount > 0))
            {
                throw new NotSupportedException($"The type '{type}' maps [{nameof(XmlMixedAttribute)}] with other element or text members.");
            }

            if (textCount > 1)
            {
                throw new NotSupportedException($"The type '{type}' has more than one [{nameof(XmlTextAttribute)}] or [{nameof(XmlCDataAttribute)}].");
            }

            if (textCount > 0 && elementCount > 0)
            {
                throw new NotSupportedException($"The type '{type}' maps simple content and child elements. Use [{nameof(XmlMixedAttribute)}] for mixed content.");
            }
        }

        #endregion

    }

}
