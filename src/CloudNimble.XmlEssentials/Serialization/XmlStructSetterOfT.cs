namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets a property of a struct through a reference to the struct, so the change is made in place.
    /// </summary>
    /// <typeparam name="TStruct">The struct type.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="instance">The struct.</param>
    /// <param name="value">The value to set.</param>
    internal delegate void XmlStructSetter<TStruct, TValue>(ref TStruct instance, TValue value)
        where TStruct : struct;

}
