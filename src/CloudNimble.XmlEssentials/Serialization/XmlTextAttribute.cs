using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps a member to the simple content of the current element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlTextAttribute : Attribute
    {
    }

}
