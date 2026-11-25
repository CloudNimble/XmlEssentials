using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates, when placed on a member of type <see cref="XmlExtensionData"/>, that XML attributes and child elements that have
    /// no matching member are added to that member during deserialization. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The member must be of type <see cref="XmlExtensionData"/>, and a type can have only one member with this attribute.
    /// The reflection-based serializer throws a <see cref="NotSupportedException"/> when either rule is broken.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlExtensionDataAttribute : Attribute
    {
    }

}
