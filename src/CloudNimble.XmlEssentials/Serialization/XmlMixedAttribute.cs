using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a member holds the mixed content of the element for its containing type: text interleaved with child elements. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The member is a collection of <see cref="XmlMixedContentItem"/>. A type can have one member with this attribute, and
    /// that type cannot also map members to child elements or to text content.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlMixedAttribute : Attribute
    {
    }

}
