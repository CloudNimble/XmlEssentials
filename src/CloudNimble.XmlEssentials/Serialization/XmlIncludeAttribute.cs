using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates that a non-public property or field is included for serialization and deserialization, and that a
    /// set accessor that is not public is used when reading. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A public property whose <see langword="set"/> or <see langword="init"/> accessor is not public is written
    /// and is not set when a document is read, as a property with no setter is. Mark it with this attribute to
    /// have the accessor used.
    /// </para>
    /// <para>
    /// This attribute is analogous to <c>JsonIncludeAttribute</c> in <c>System.Text.Json</c>. It is not the
    /// <c>System.Xml.Serialization.XmlIncludeAttribute</c> of the base class library, which declares known types.
    /// Use <see cref="XmlDerivedTypeAttribute{T}"/> to declare a derived type.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlIncludeAttribute : Attribute
    {
    }

}
