using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Requires the member to be present when deserializing. Analogous to <c>JsonRequiredAttribute</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlRequiredAttribute : Attribute
    {
    }

}
