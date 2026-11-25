namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads a property of a struct through a reference to the struct, so the struct is not copied.
    /// </summary>
    /// <typeparam name="TStruct">The struct type.</typeparam>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="instance">The struct.</param>
    /// <returns>The property value.</returns>
    internal delegate TValue XmlStructGetter<TStruct, TValue>(ref TStruct instance)
        where TStruct : struct;

}
