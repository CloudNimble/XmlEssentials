namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Represents a method that sets the value of a property or field on an object.
    /// </summary>
    /// <typeparam name="TDeclaring">
    /// The type that declares the property or field.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the property or field.
    /// </typeparam>
    /// <param name="instance">The object whose member is set.</param>
    /// <param name="value">The value to set.</param>
    /// <example>
    /// <code>
    /// XmlPropertySetter&lt;Point, int&gt; setX = static (ref Point point, int value) =&gt; point.X = value;
    /// </code>
    /// </example>
    /// <remarks>
    /// The object is passed by reference so that the method can set a member of a value type in place. For a
    /// reference type, the method sets the member of the object that <paramref name="instance"/> refers to and
    /// leaves the reference as it is.
    /// </remarks>
    public delegate void XmlPropertySetter<TDeclaring, TValue>(ref TDeclaring instance, TValue value);

}
