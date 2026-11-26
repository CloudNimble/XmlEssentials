namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Returns a reference to the value inside a boxed value type, so a member of it can be set in place.
    /// </summary>
    /// <typeparam name="TDeclaring">The value type.</typeparam>
    /// <param name="instance">The box.</param>
    /// <returns>A reference to the value the box holds.</returns>
    /// <remarks>
    /// The serializer holds an object it is reading as an <see cref="object"/>, so a value type is in a box.
    /// Reaching into the box needs a method that is constrained to value types, which code that is generic over
    /// any declaring type cannot call. This delegate is made where the type is known to be a value type.
    /// </remarks>
    internal delegate ref TDeclaring XmlUnbox<TDeclaring>(object instance);

}
