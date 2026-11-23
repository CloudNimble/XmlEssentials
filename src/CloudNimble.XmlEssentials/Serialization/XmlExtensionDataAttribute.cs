using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Marks a member that receives unmapped attributes and elements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlExtensionDataAttribute : Attribute
    {
    }

}
