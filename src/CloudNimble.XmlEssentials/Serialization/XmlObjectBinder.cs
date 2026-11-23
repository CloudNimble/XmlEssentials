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

        private static readonly MethodInfo CreatePropertyMethod = typeof(XmlObjectBinder).GetMethod(nameof(CreateProperty), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo CreateStructGetterMethod = typeof(XmlObjectBinder).GetMethod(nameof(CreateStructGetter), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo CreateStructSetterMethod = typeof(XmlObjectBinder).GetMethod(nameof(CreateStructSetter), BindingFlags.NonPublic | BindingFlags.Static)!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds the binding for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="options">The serializer options.</param>
        /// <returns>The members and constructor.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026")]
        [UnconditionalSuppressMessage("Trimming", "IL2060")]
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        [UnconditionalSuppressMessage("Trimming", "IL2075")]
        [UnconditionalSuppressMessage("Trimming", "IL2087")]
        [UnconditionalSuppressMessage("Trimming", "IL2091")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        internal static XmlObjectBinding Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(XmlSerializerOptions options)
        {
            var members = Discover<T>(options);
            var extensionMember = PeelExtensionData(typeof(T), members);
            ValidateSimpleContent(typeof(T), members);
            // An abstract class or an interface is never created: a document names one of its derived types.
            // It is bound for its members, which describe it and are what is written when an undeclared
            // derived type falls back to the declared type.
            var isAbstract = typeof(T).IsAbstract;
            var ctor = isAbstract ? null : SelectConstructor(typeof(T), members);
            var map = MapConstructor(ctor, members);
            var bound = new XmlBoundProperty[members.Count];
            var nextSlot = map.Length;
            for (var i = 0; i < members.Count; i++)
            {
                var constructorIndex = -1;
                if (ctor is not null)
                {
                    for (var p = 0; p < map.Length; p++)
                    {
                        if (map[p] == i)
                        {
                            constructorIndex = p;
                            break;
                        }
                    }
                }

                try
                {
                    bound[i] = (XmlBoundProperty)CreatePropertyMethod.MakeGenericMethod(typeof(T), members[i].Type).Invoke(null, [members[i], options, constructorIndex >= 0 ? constructorIndex : nextSlot++, constructorIndex])!;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    throw ex.InnerException;
                }
            }

            ValidateElementNames(bound, options.PropertyNameCaseInsensitive);
            Func<object>? create = null;
            if (!isAbstract && (ctor is null || ctor.GetParameters().Length == 0))
            {
                create = typeof(T).IsValueType || ctor is { IsPublic: true }
                    ? CreatePublic<T>
                    : CreateNonPublic<T>;
            }

            XmlExtensionDataBinding? extension = null;
            if (extensionMember is { } ext)
            {
                extension = new XmlExtensionDataBinding(
                    CreateGetter<T, XmlExtensionData>(ext.Member),
                    ext.CanSet ? CreateSetter<T, XmlExtensionData>(ext.Member) : null);
            }

            var polymorphic = typeof(T).GetCustomAttribute<XmlPolymorphicAttribute>();
            var placement = polymorphic?.DiscriminatorPlacement ?? XmlDiscriminatorPlacement.ElementName;
            var unknown = polymorphic?.UnknownDerivedTypeHandling ?? XmlUnknownDerivedTypeHandling.FailSerialization;
            byte[] utf8DiscAttr = [];
            if (placement is XmlDiscriminatorPlacement.Attribute)
            {
                if (string.IsNullOrWhiteSpace(polymorphic!.TypeDiscriminatorPropertyName))
                {
                    throw new NotSupportedException($"[{nameof(XmlPolymorphicAttribute)}] on '{typeof(T)}' uses {nameof(XmlDiscriminatorPlacement.Attribute)} and requires {nameof(XmlPolymorphicAttribute.TypeDiscriminatorPropertyName)}.");
                }

                utf8DiscAttr = Encoding.UTF8.GetBytes(polymorphic.TypeDiscriminatorPropertyName);
            }

            return new XmlObjectBinding(
                bound,
                ctor is { } c && c.GetParameters().Length > 0 ? c : null,
                map,
                create,
                nextSlot,
                extension,
                new XmlPolymorphism(typeof(T), BindDerived(typeof(T), options, placement), placement, utf8DiscAttr, unknown));
        }

        #endregion

        #region Private Methods

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
        /// Creates a typed bound member.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The member model.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="index">The slot the member uses while an object is read.</param>
        /// <param name="constructorIndex">The constructor parameter index, or <c>-1</c>.</param>
        /// <returns>The bound member.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        internal static XmlBoundProperty CreateProperty<TDeclaring, TValue>(XmlMemberModel member, XmlSerializerOptions options, int index, int constructorIndex)
        {
            var mixed = member.Member.IsDefined(typeof(XmlMixedAttribute), inherit: false);
            var typeForInfo = mixed ? typeof(XmlMixedText) : member.ConverterType;
            var typeInfo = options.GetTypeInfo(typeForInfo);
            var name = new XmlName(member.LocalName, member.NamespaceUri.Length == 0 ? null : member.NamespaceUri);
            var info = new XmlPropertyInfo(name, typeInfo, member.IsNillable, member.IsRequired, member.Order, member.IsAttribute, member.IsText);
            var utf8Name = Encoding.UTF8.GetBytes(member.LocalName);
            var utf8Ns = member.NamespaceUri.Length == 0 ? [] : Encoding.UTF8.GetBytes(member.NamespaceUri);
            var getter = CreateGetter<TDeclaring, TValue>(member.Member);
            var setter = member.CanSet ? CreateSetter<TDeclaring, TValue>(member.Member) : null;
            XmlCollectionBinding? collection = null;
            Type? collectionElement = null;
            if (member.IsCollection && XmlCollectionShape.IsCollection(typeof(TValue), out var elementType))
            {
                collectionElement = elementType;
                var itemClr = mixed ? typeof(XmlMixedContentItem) : elementType;
                var itemInfoType = mixed ? typeof(XmlMixedText) : elementType;
                collection = XmlCollectionBinding.Create(
                    typeof(TValue),
                    itemClr,
                    member.IsWrapped,
                    Encoding.UTF8.GetBytes(member.ItemLocalName),
                    options.GetTypeInfo(itemInfoType),
                    mixed);
            }

            var derivedSource = mixed ? null : collectionElement ?? member.ConverterType;
            var polymorphism = XmlPolymorphism.None;
            if (derivedSource is not null)
            {
                var placement = XmlDiscriminatorPlacement.ElementName;
                byte[] utf8DiscAttr = [];
                var unknown = XmlUnknownDerivedTypeHandling.FailSerialization;
                var polymorphic = derivedSource.GetCustomAttribute<XmlPolymorphicAttribute>();
                if (polymorphic is not null)
                {
                    placement = polymorphic.DiscriminatorPlacement;
                    unknown = polymorphic.UnknownDerivedTypeHandling;
                    if (placement is XmlDiscriminatorPlacement.Attribute && polymorphic.TypeDiscriminatorPropertyName is { Length: > 0 } discriminatorName)
                    {
                        utf8DiscAttr = Encoding.UTF8.GetBytes(discriminatorName);
                    }
                }

                polymorphism = new XmlPolymorphism(derivedSource, BindDerived(derivedSource, options, placement), placement, utf8DiscAttr, unknown);
            }

            return new XmlBoundProperty<TDeclaring, TValue>(info, index, utf8Name, utf8Ns, typeInfo, getter, setter, member.Ignore, constructorIndex, member.IsCData, collection, polymorphism);
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
        [UnconditionalSuppressMessage("Trimming", "IL2075")]
        internal static List<XmlMemberModel> Discover<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(XmlSerializerOptions options)
        {
            var types = new Stack<Type>();
            for (var type = typeof(T); type is not null && type != typeof(object); type = type.BaseType)
            {
                types.Push(type);
            }

            var members = new List<XmlMemberModel>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            while (types.Count > 0)
            {
                var type = types.Pop();
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (TryCreateModel(property, property.PropertyType, property.GetMethod, property.SetMethod, options, index, out var model))
                    {
                        AddMember(members, names, model);
                        index++;
                    }
                }

                if (options.IncludeFields)
                {
                    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (TryCreateModel(field, field.FieldType, get: null, set: null, options, index, out var model))
                        {
                            AddMember(members, names, model);
                            index++;
                        }
                    }
                }
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
        /// Returns whether <paramref name="type"/> is mapped as an object with properties.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>
        /// <see langword="true"/> for a concrete class or struct that is not a built-in primitive, and for an
        /// abstract class or an interface that declares derived types with <see cref="XmlDerivedTypeAttribute"/>.
        /// </returns>
        /// <remarks>
        /// An abstract type with no declared derived types stays unsupported: nothing could be created for it
        /// when reading, and writing a value as its declared type would silently drop the members of the
        /// value's own type.
        /// </remarks>
        internal static bool IsObjectType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type.IsArray || type.IsPointer
                || type == typeof(object) || type == typeof(string) || typeof(Delegate).IsAssignableFrom(type)
                || Nullable.GetUnderlyingType(type) is not null)
            {
                return false;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                return type.IsDefined(typeof(XmlDerivedTypeAttribute), inherit: true);
            }

            return type.IsClass || type.IsValueType;
        }

        /// <summary>
        /// Adds <paramref name="model"/> or throws when the XML name is already used.
        /// </summary>
        /// <param name="members">The members.</param>
        /// <param name="names">The XML local names already used.</param>
        /// <param name="model">The member.</param>
        internal static void AddMember(List<XmlMemberModel> members, HashSet<string> names, XmlMemberModel model)
        {
            var key = (model.IsAttribute ? "a:" : model.IsText || model.IsCData ? "t:" : "e:") + model.LocalName;
            if (!names.Add(key))
            {
                throw new InvalidOperationException($"The type maps more than one member to the {(model.IsAttribute ? "attribute" : "element")} name '{model.LocalName}'.");
            }

            members.Add(model);
        }

        /// <summary>
        /// Builds a getter for <paramref name="member"/>.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The property or field.</param>
        /// <returns>The getter. The instance is passed boxed so struct setters can mutate the box.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2060")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
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
        /// Builds a setter for <paramref name="member"/>.
        /// </summary>
        /// <typeparam name="TDeclaring">The declaring type.</typeparam>
        /// <typeparam name="TValue">The member type.</typeparam>
        /// <param name="member">The property or field.</param>
        /// <returns>The setter. The instance is passed boxed so struct setters can mutate the box.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2060")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
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
        /// Maps each constructor parameter to a member index.
        /// </summary>
        /// <param name="constructor">The constructor, or null.</param>
        /// <param name="members">The members.</param>
        /// <returns>The map.</returns>
        internal static int[] MapConstructor(ConstructorInfo? constructor, List<XmlMemberModel> members)
        {
            if (constructor is null)
            {
                return [];
            }

            var parameters = constructor.GetParameters();
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
                if (map[i] < 0 && !parameters[i].HasDefaultValue)
                {
                    throw new NotSupportedException($"The constructor parameter '{name}' is not a serialized member.");
                }
            }

            return map;
        }

        /// <summary>
        /// Chooses the constructor used to create <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="members">The serializable members.</param>
        /// <returns>The constructor, or null to use <see cref="Activator.CreateInstance{T}"/>.</returns>
        /// <remarks>
        /// A constructor marked <see cref="XmlConstructorAttribute"/> is used. Otherwise the public constructor
        /// with the most parameters is used, among those whose every parameter names a member. A parameter
        /// names a member as <see cref="FindConstructorMember"/> has it, without regard to case.
        /// </remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        internal static ConstructorInfo? SelectConstructor(Type type, List<XmlMemberModel> members)
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

            ConstructorInfo? best = null;
            var bestCount = -1;
            foreach (var constructor in constructors)
            {
                if (!constructor.IsPublic)
                {
                    continue;
                }

                var parameters = constructor.GetParameters();
                var subset = true;
                foreach (var parameter in parameters)
                {
                    if (parameter.Name is null || FindConstructorMember(members, parameter.Name) < 0)
                    {
                        subset = false;
                        break;
                    }
                }

                if (!subset)
                {
                    continue;
                }

                if (parameters.Length > bestCount)
                {
                    best = constructor;
                    bestCount = parameters.Length;
                    continue;
                }

                if (parameters.Length == bestCount && bestCount > 0)
                {
                    throw new NotSupportedException($"The type '{type}' has more than one constructor that can bind its members. Mark one with [{nameof(XmlConstructorAttribute)}].");
                }
            }

            return best;
        }

        /// <summary>
        /// Builds a member model, or returns false when the member is not serialized.
        /// </summary>
        /// <param name="member">The property or field.</param>
        /// <param name="memberType">The member CLR type.</param>
        /// <param name="get">The getter, if a property.</param>
        /// <param name="set">The setter, if a property.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="declarationIndex">The discovery index.</param>
        /// <param name="model">When this method returns true, the member.</param>
        /// <returns><see langword="true"/> when the member is serialized.</returns>
        internal static bool TryCreateModel(MemberInfo member, Type memberType, MethodInfo? get, MethodInfo? set, XmlSerializerOptions options, int declarationIndex, out XmlMemberModel model)
        {
            model = default;
            if (member is PropertyInfo property && property.GetIndexParameters().Length > 0)
            {
                return false;
            }

            var include = member.IsDefined(typeof(XmlIncludeAttribute), inherit: false);
            var isPublic = member is PropertyInfo
                ? get is { IsPublic: true }
                : ((FieldInfo)member).IsPublic;
            if (!isPublic && !include)
            {
                return false;
            }

            if (get is null && member is PropertyInfo)
            {
                return false;
            }

            if (member.GetCustomAttribute<XmlIgnoreAttribute>() is { Condition: XmlIgnoreCondition.Always })
            {
                return false;
            }

            if (member.IsDefined(typeof(XmlExtensionDataAttribute), inherit: false))
            {
                if (memberType != typeof(XmlExtensionData))
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is marked [{nameof(XmlExtensionDataAttribute)}] but is not {nameof(XmlExtensionData)}.");
                }

                var extensionCanSet = member is FieldInfo extensionField
                    ? !extensionField.IsInitOnly
                    : set is not null;
                model = new XmlMemberModel(member, memberType, member.Name, "", -1, declarationIndex, false, true, XmlIgnoreCondition.Never, extensionCanSet, false, false, false);
                return true;
            }

            var text = member.GetCustomAttribute<XmlTextAttribute>(inherit: false);
            var cdata = member.GetCustomAttribute<XmlCDataAttribute>(inherit: false);
            var elements = member.GetCustomAttributes<XmlElementAttribute>(inherit: false);
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

            var attribute = member.GetCustomAttribute<XmlAttributeAttribute>(inherit: false);
            if (attribute is not null && element is not null)
            {
                throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as both an XML attribute and an element.");
            }

            if ((text is not null || cdata is not null) && (attribute is not null || element is not null))
            {
                throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as both simple content and an attribute or element.");
            }

            var fieldInfo = member as FieldInfo;
            var canSet = fieldInfo is not null
                ? !fieldInfo.IsInitOnly
                : set is not null;
            if (!canSet && ((member is PropertyInfo && options.IgnoreReadOnlyProperties) || (fieldInfo is not null && options.IgnoreReadOnlyFields)))
            {
                return false;
            }

            var ignore = member.GetCustomAttribute<XmlIgnoreAttribute>()?.Condition ?? options.DefaultIgnoreCondition;
            var xmlName = member.GetCustomAttribute<XmlPropertyNameAttribute>()?.Name;
            var isAttribute = false;
            var isText = false;
            var isCData = false;
            string local;
            var ns = "";
            if (text is not null || cdata is not null)
            {
                if (!XmlBuiltInConverters.IsScalar(memberType))
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as XML text. Complex types cannot be simple content.");
                }

                isText = true;
                isCData = cdata is not null;
                local = xmlName ?? member.Name;
            }
            else if (attribute is not null)
            {
                if (!XmlBuiltInConverters.IsScalar(memberType))
                {
                    throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' is mapped as an XML attribute. Complex types cannot be attributes.");
                }

                isAttribute = true;
                local = xmlName
                    ?? (attribute.AttributeName is { Length: > 0 } attributeName ? attributeName : options.ConvertAttributeName(member.Name));
                ns = attribute.Namespace ?? "";
            }
            else if (typed is { Count: > 0 })
            {
                var first = typed[0];
                local = xmlName
                    ?? (first.ElementName is { Length: > 0 } typedName ? typedName : options.ConvertElementName(member.Name));
                ns = first.Namespace ?? "";
            }
            else if (element is not null)
            {
                local = xmlName
                    ?? (element.ElementName is { Length: > 0 } elementName ? elementName : options.ConvertElementName(member.Name));
                ns = element.Namespace ?? "";
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

            var order = member.GetCustomAttribute<XmlPropertyOrderAttribute>()?.Order ?? element?.Order ?? -1;
            if (order < 0)
            {
                order = -1;
            }

            var isCollection = false;
            var isWrapped = false;
            var itemLocal = "";
            if (!isAttribute && !isText && XmlCollectionShape.IsCollection(memberType, out var elementType))
            {
                isCollection = true;
                var array = member.GetCustomAttribute<XmlArrayAttribute>(inherit: false);
                XmlArrayItemAttribute? arrayItem = null;
                foreach (var candidate in member.GetCustomAttributes<XmlArrayItemAttribute>(inherit: false))
                {
                    if (arrayItem is not null)
                    {
                        throw new NotSupportedException($"Member '{member.DeclaringType}.{member.Name}' has more than one [{nameof(XmlArrayItemAttribute)}] or a typed item choice. Polymorphic collection items are not enabled for this type.");
                    }

                    arrayItem = candidate;
                }

                isWrapped = array is not null;
                if (array is not null)
                {
                    local = xmlName
                        ?? (array.ElementName is { Length: > 0 } arrayName ? arrayName : options.ConvertElementName(member.Name));
                    ns = array.Namespace ?? ns;
                    if (order < 0 && array.Order >= 0)
                    {
                        order = array.Order;
                    }
                }

                var itemClr = arrayItem?.Type ?? elementType;
                itemLocal = arrayItem?.ElementName is { Length: > 0 } itemName
                    ? itemName
                    : isWrapped
                        ? options.ConvertElementName(XmlBuiltInConverters.GetXmlTypeName(itemClr))
                        : local;
            }

            var isRequired = member.IsDefined(typeof(XmlRequiredAttribute), inherit: false)
                || member.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
            var isNillable = !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) is not null || element is { IsNullable: true } || isCollection;
            model = new XmlMemberModel(member, memberType, local, ns, order, declarationIndex, isRequired, isNillable, ignore, canSet, isAttribute, isText, isCData, isCollection, isWrapped, itemLocal, typed is { Count: > 0 } ? typed[0].Type : null);
            return true;
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
        /// Throws when an element name could belong to two members: a member's own name, or the name one of its
        /// derived types is written under, is also used by another member.
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
                if (property.IsAttribute || property.IsText || !property.Polymorphism.HasElementNames)
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

                foreach (var derived in property.Polymorphism.HasElementNames ? property.Polymorphism.Derived : [])
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

                if (member.Member.IsDefined(typeof(XmlMixedAttribute), inherit: false))
                {
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

        /// <summary>
        /// Builds derived-type bindings for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The declared CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <returns>The derived bindings, or null when none are declared.</returns>
        internal static XmlDerivedBinding[]? BindDerived(Type type, XmlSerializerOptions options, XmlDiscriminatorPlacement placement)
        {
            var derivedList = new List<XmlDerivedBinding>();
            foreach (var attr in type.GetCustomAttributes<XmlDerivedTypeAttribute>(inherit: true))
            {
                var disc = attr.TypeDiscriminator as string ?? attr.DerivedType.Name;
                var elementName = placement is XmlDiscriminatorPlacement.ElementName
                    ? options.ConvertElementName(disc)
                    : disc;
                derivedList.Add(new XmlDerivedBinding(
                    Encoding.UTF8.GetBytes(elementName),
                    attr.DerivedType,
                    options.GetTypeInfo(attr.DerivedType),
                    disc));
            }

            return derivedList.Count > 0 ? derivedList.ToArray() : null;
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
                if (!members[i].Member.IsDefined(typeof(XmlExtensionDataAttribute), inherit: false))
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

        #endregion

    }

}
