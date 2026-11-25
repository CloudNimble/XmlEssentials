using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates that the constructor is used to create instances of the type on deserialization. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed class XmlConstructorAttribute : Attribute
    {
    }

}
