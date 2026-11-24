using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What the serializer has to know about a property or field before it can map it: its name and types, how it
    /// can be read and set, and the attributes on it.
    /// </summary>
    /// <remarks>
    /// The object binder works from these facts and never from a <see cref="MemberInfo"/>, so it has one set of
    /// rules whoever supplies the facts. The reflection resolver fills them by reflection. A source-generated
    /// context fills them with code, which needs no reflection and survives trimming.
    /// </remarks>
    internal sealed class XmlMemberFacts
    {

        #region Fields

        private static readonly Attribute[] NoAttributes = [];

        /// <summary>
        /// The attributes declared on the member itself.
        /// </summary>
        internal readonly Attribute[] Attributes;

        /// <summary>
        /// Creates the bound member. The delegate is closed over the declaring type and the member type, which the
        /// binder knows only as <see cref="Type"/> values.
        /// </summary>
        internal Func<XmlMemberModel, XmlSerializerOptions, int, int, XmlBoundProperty> CreateBound = null!;

        /// <summary>
        /// Creates the mapping of a collection member, closed over its item type; or null when the reflection
        /// resolver is to close it at run time. The arguments are the declared collection type, whether a wrapper
        /// surrounds the items, the UTF-8 item name, the type info of an item, and whether the content is mixed.
        /// </summary>
        internal Func<Type, bool, byte[], XmlTypeInfo, bool, XmlCollectionBinding>? CreateCollectionBinding;

        /// <summary>
        /// The type that declares the member.
        /// </summary>
        internal readonly Type DeclaringType;

        /// <summary>
        /// The item type of a collection member whose facts were not filled by reflection, or null.
        /// </summary>
        internal Type? ElementType;

        /// <summary>
        /// A <c>Func&lt;object, TValue&gt;</c> that reads the member, or null when the reflection resolver is to make one from <see cref="Member"/>.
        /// </summary>
        internal Delegate? Getter;

        /// <summary>
        /// Whether the member can be read: a field always can, and a property when it has a get accessor.
        /// </summary>
        internal readonly bool HasGetter;

        /// <summary>
        /// Whether the member can be set after the object is created: a property with a set or init accessor, or a
        /// field that is not read-only.
        /// </summary>
        internal readonly bool HasSetter;

        /// <summary>
        /// Whether the member is a field.
        /// </summary>
        internal readonly bool IsField;

        /// <summary>
        /// Whether the member is an indexer, which is never serialized.
        /// </summary>
        internal readonly bool IsIndexer;

        /// <summary>
        /// Whether the member is public: the field itself, or the get accessor of a property.
        /// </summary>
        internal readonly bool IsPublic;

        /// <summary>
        /// Whether the member is a property with a virtual accessor. Fields are never virtual.
        /// </summary>
        /// <remarks>
        /// An override is the same member as the one it overrides, so only the most derived is serialized.
        /// </remarks>
        internal bool IsVirtual;

        /// <summary>
        /// Whether the member is declared with the <see langword="required"/> modifier.
        /// </summary>
        internal readonly bool IsRequiredMember;

        /// <summary>
        /// The reflected member, or null when the facts were not filled by reflection.
        /// </summary>
        internal readonly MemberInfo? Member;

        /// <summary>
        /// The type of the member.
        /// </summary>
        internal readonly Type MemberType;

        /// <summary>
        /// Whether the nullable annotations of the member say it never returns null.
        /// </summary>
        internal bool NeverReturnsNull;

        /// <summary>
        /// The C# name of the member.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// An <c>Action&lt;object, TValue&gt;</c> that sets the member, or null when it cannot be set or the
        /// reflection resolver is to make one from <see cref="Member"/>.
        /// </summary>
        internal Delegate? Setter;

        /// <summary>
        /// Whether the nullable annotations of the member say it does not accept null.
        /// </summary>
        internal bool RefusesNull;

        /// <summary>
        /// Wraps a converter for the underlying type of a nullable value type as a converter for the member's type;
        /// or null when the reflection resolver is to close the wrapper at run time.
        /// </summary>
        internal Func<XmlConverter, XmlConverter>? WrapNullable;

        /// <summary>
        /// Creates the reusable box that holds a value-type member for a constructor, closed over the member's
        /// type; or null. It returns an <c>XmlHeldBox&lt;TValue&gt;</c>.
        /// </summary>
        internal Func<object>? CreateHeldBox;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the facts of a member from reflection.
        /// </summary>
        /// <param name="member">A property or a field.</param>
        internal XmlMemberFacts(MemberInfo member)
        {
            Member = member;
            Name = member.Name;
            DeclaringType = member.DeclaringType!;
            Attributes = Attribute.GetCustomAttributes(member, inherit: false);
            if (member is PropertyInfo property)
            {
                MemberType = property.PropertyType;
                IsIndexer = property.GetIndexParameters().Length > 0;
                HasGetter = property.GetMethod is not null;
                HasSetter = property.SetMethod is not null;
                IsPublic = property.GetMethod is { IsPublic: true };
                IsVirtual = (property.GetMethod ?? property.SetMethod)!.IsVirtual;
            }
            else
            {
                var field = (FieldInfo)member;
                IsField = true;
                MemberType = field.FieldType;
                HasGetter = true;
                HasSetter = !field.IsInitOnly;
                IsPublic = field.IsPublic;
            }

            IsRequiredMember = member.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
        }

        /// <summary>
        /// Creates the facts of a member from values that were worked out when the program was compiled.
        /// </summary>
        /// <param name="name">The C# name of the member.</param>
        /// <param name="declaringType">The type that declares the member.</param>
        /// <param name="memberType">The type of the member.</param>
        /// <param name="isField">Whether the member is a field.</param>
        /// <param name="isPublic">Whether the member, or its get accessor, is public.</param>
        /// <param name="isRequiredMember">Whether the member has the <see langword="required"/> modifier.</param>
        /// <param name="getter">A <c>Func&lt;object, TValue&gt;</c> that reads the member, or null.</param>
        /// <param name="setter">An <c>Action&lt;object, TValue&gt;</c> that sets the member, or null.</param>
        /// <param name="attributes">The attributes on the member, or null.</param>
        internal XmlMemberFacts(string name, Type declaringType, Type memberType, bool isField, bool isPublic, bool isRequiredMember, Delegate? getter, Delegate? setter, Attribute[]? attributes)
        {
            Name = name;
            DeclaringType = declaringType;
            MemberType = memberType;
            IsField = isField;
            IsPublic = isPublic;
            IsRequiredMember = isRequiredMember;
            Getter = getter;
            Setter = setter;
            HasGetter = getter is not null;
            HasSetter = setter is not null;
            Attributes = attributes ?? NoAttributes;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the first attribute of type <typeparamref name="T"/> on the member.
        /// </summary>
        /// <typeparam name="T">The attribute type. An attribute of a derived type is found too.</typeparam>
        /// <returns>The attribute, or null.</returns>
        internal T? GetCustomAttribute<T>()
            where T : Attribute
        {
            foreach (var attribute in Attributes)
            {
                if (attribute is T found)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns every attribute of type <typeparamref name="T"/> declared on the member itself.
        /// </summary>
        /// <typeparam name="T">The attribute type. Attributes of a derived type are returned too.</typeparam>
        /// <returns>The attributes, in the order they were declared.</returns>
        internal IEnumerable<T> GetCustomAttributes<T>()
            where T : Attribute
        {
            foreach (var attribute in Attributes)
            {
                if (attribute is T found)
                {
                    yield return found;
                }
            }
        }

        /// <summary>
        /// Returns whether the member is a collection, and the type of its items.
        /// </summary>
        /// <param name="elementType">The item type.</param>
        /// <returns><see langword="true"/> for a collection.</returns>
        /// <remarks>
        /// Reflection looks at the interfaces of the member's type. Facts that were generated name the item type,
        /// because the interfaces of a type may be trimmed away.
        /// </remarks>
        internal bool IsCollection(out Type elementType)
        {
            if (Member is not null)
            {
                return XmlCollectionShape.IsCollection(MemberType, out elementType);
            }

            elementType = ElementType ?? MemberType;
            return ElementType is not null;
        }

        /// <summary>
        /// Returns whether an attribute of type <paramref name="attributeType"/> is declared on the member itself.
        /// </summary>
        /// <param name="attributeType">The attribute type. An attribute of a derived type counts.</param>
        /// <returns><see langword="true"/> when the member has one.</returns>
        internal bool IsDefined(Type attributeType)
        {
            foreach (var attribute in Attributes)
            {
                if (attributeType.IsInstanceOfType(attribute))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

    }

}
