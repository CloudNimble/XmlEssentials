using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps a member as mixed content (interleaved text and child elements).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlMixedAttribute : Attribute
    {
    }

}
