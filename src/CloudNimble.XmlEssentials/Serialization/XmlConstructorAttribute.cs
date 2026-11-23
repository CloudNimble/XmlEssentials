using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Marks the constructor used to create an instance when deserializing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed class XmlConstructorAttribute : Attribute
    {
    }

}
