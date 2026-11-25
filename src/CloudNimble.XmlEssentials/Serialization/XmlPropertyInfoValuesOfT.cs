using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides serialization metadata about a property or field. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the property or field.
    /// </typeparam>
    /// <remarks>
    /// Source-generated contexts create instances of this class. It is analogous to
    /// <c>JsonPropertyInfoValues&lt;T&gt;</c> in <c>System.Text.Json</c>. The names, the ignore conditions and the
    /// mapping of the member are not given here: the serializer works them out from <see cref="Attributes"/> and the
    /// options, exactly as it does for a member it finds through reflection.
    /// </remarks>
    public sealed class XmlPropertyInfoValues<TValue> : XmlPropertyInfoValues
    {

        #region Fields

        /// <summary>
        /// Creates the mapping of a collection member, closed over its item type; or null when the member is not a collection.
        /// </summary>
        internal Func<bool, byte[], XmlTypeInfo, bool, XmlCollectionBinding>? CreateCollectionBinding;

        /// <summary>
        /// The item type of a collection member, or null.
        /// </summary>
        internal Type? ElementType;

        /// <summary>
        /// Wraps a converter for the underlying type of a nullable value type as a converter for <typeparamref name="TValue"/>; or null.
        /// </summary>
        internal Func<XmlConverter, XmlConverter>? WrapNullable;

        /// <summary>
        /// Creates the reusable box that holds a value-type member for a constructor, closed over the member's
        /// type; or null. It returns an <c>XmlHeldBox&lt;TValue&gt;</c>.
        /// </summary>
        internal Func<object>? CreateHeldBox;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or initializes the XmlEssentials attributes on the member.
        /// </summary>
        /// <value>
        /// The attributes, as instances constructed with the arguments the member declares them with; or
        /// <see langword="null"/> when the member has none.
        /// </value>
        /// <remarks>
        /// The serializer reads these as it reads the attributes it finds through reflection.
        /// </remarks>
        public Attribute[]? Attributes { get; init; }

        /// <summary>
        /// Gets or initializes the type that declares the member.
        /// </summary>
        /// <value>
        /// The declaring type.
        /// </value>
        public Type DeclaringType { get; init; } = typeof(object);

        /// <summary>
        /// Gets or initializes a method that gets the value of the member.
        /// </summary>
        /// <value>
        /// A method that takes the object, boxed when it is a value type, and returns the value of the member; or
        /// <see langword="null"/> when the member cannot be read.
        /// </value>
        public Func<object, TValue>? Getter { get; init; }

        /// <summary>
        /// Gets or initializes a value that indicates whether the member may return <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the nullable annotations of the member allow it to return
        /// <see langword="null"/>, or say nothing; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The value is read when <see cref="XmlSerializerOptions.RespectNullableAnnotations"/> is <see langword="true"/>.
        /// </remarks>
        public bool IsGetNullable { get; init; } = true;

        /// <summary>
        /// Gets or initializes a value that indicates whether the member is a property or a field.
        /// </summary>
        /// <value>
        /// <see langword="true"/> for a property; <see langword="false"/> for a field.
        /// </value>
        public bool IsProperty { get; init; } = true;

        /// <summary>
        /// Gets or initializes a value that indicates whether the member is public.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the field, or the get accessor of the property, is public; otherwise,
        /// <see langword="false"/>. A member that is not public is serialized only when it has
        /// <see cref="XmlIncludeAttribute"/>.
        /// </value>
        public bool IsPublic { get; init; } = true;

        /// <summary>
        /// Gets or initializes a value that indicates whether the member is declared with the
        /// <see langword="required"/> modifier.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member has the <see langword="required"/> modifier; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public bool IsRequired { get; init; }

        /// <summary>
        /// Gets or initializes a value that indicates whether the member accepts <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the nullable annotations of the member allow it to be set to
        /// <see langword="null"/>, or say nothing; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The value is read when <see cref="XmlSerializerOptions.RespectNullableAnnotations"/> is <see langword="true"/>.
        /// </remarks>
        public bool IsSetNullable { get; init; } = true;

        /// <summary>
        /// Gets or initializes a value that indicates whether the member is a property with a virtual accessor.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the property is virtual, abstract, an override, or implements an interface
        /// member; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// A property that overrides another is the same member, so only the most derived one is serialized. When
        /// that one is ignored, the property it overrides is not serialized either.
        /// </remarks>
        public bool IsVirtual { get; init; }

        /// <summary>
        /// Gets or initializes the name of the property or field as it is declared.
        /// </summary>
        /// <value>
        /// The C# name of the member.
        /// </value>
        public string PropertyName { get; init; } = "";

        /// <summary>
        /// Gets or initializes a method that sets the value of the member.
        /// </summary>
        /// <value>
        /// A method that takes the object, boxed when it is a value type, and the value to set; or
        /// <see langword="null"/> when the member cannot be set after the object is created.
        /// </value>
        public Action<object, TValue>? Setter { get; init; }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override XmlMemberFacts CreateFacts()
        {
            var facts = new XmlMemberFacts(PropertyName, DeclaringType, typeof(TValue), !IsProperty, IsPublic, IsRequired, Getter, Setter, Attributes)
            {
                CreateCollectionBinding = CreateCollectionBinding,
                ElementType = ElementType,
                IsVirtual = IsVirtual,
                NeverReturnsNull = !IsGetNullable,
                RefusesNull = !IsSetNullable,
                CreateHeldBox = CreateHeldBox,
                WrapNullable = WrapNullable,
            };

            // The bound member is generic over the member's type. It is closed here, where the type is a type
            // argument, so nothing is closed over a type at run time.
            facts.CreateBound = static (model, options, index, constructorIndex) => XmlObjectBinder.CreateProperty<object, TValue>(model, options, index, constructorIndex);
            return facts;
        }

        #endregion

    }

}
