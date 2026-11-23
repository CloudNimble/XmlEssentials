using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates that a non-public property or field is included for serialization and deserialization. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// This attribute is analogous to <c>JsonIncludeAttribute</c> in <c>System.Text.Json</c>. It is not the
    /// <c>System.Xml.Serialization.XmlIncludeAttribute</c> of the base class library, which declares known types.
    /// Use <see cref="XmlDerivedTypeAttribute{T}"/> to declare a derived type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlIncludeAttribute : Attribute
    {
    }

}
