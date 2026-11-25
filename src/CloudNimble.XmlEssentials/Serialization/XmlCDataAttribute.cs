using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a member is serialized as the text content of the element for its containing type, written as a CDATA section. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The member must have a simple type: a type with a built-in value converter, an enumeration, or the nullable form of
    /// either. A type can map one member with this attribute or <see cref="XmlTextAttribute"/>, and that type cannot also
    /// map members to child elements.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlCDataAttribute : Attribute
    {
    }

}
