using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Includes a non-public property or field in serialization. Analogous to <c>JsonIncludeAttribute</c>.
    /// This is not BCL <c>XmlIncludeAttribute</c> (known types); use <c>XmlDerivedTypeAttribute</c> for those.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlIncludeAttribute : Attribute
    {
    }

}
