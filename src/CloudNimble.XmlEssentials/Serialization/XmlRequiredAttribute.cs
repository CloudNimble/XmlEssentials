using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates that the annotated member must be present in the XML on deserialization. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// This attribute is analogous to <c>JsonRequiredAttribute</c> in <c>System.Text.Json</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlRequiredAttribute : Attribute
    {
    }

}
