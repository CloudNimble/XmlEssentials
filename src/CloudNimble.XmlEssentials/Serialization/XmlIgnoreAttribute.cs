using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Prevents a property or field from being serialized or deserialized, or from being written when a condition is met. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlIgnoreAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the condition that must be met before the member is ignored.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies when the member is ignored. The default is <see cref="XmlIgnoreCondition.Always"/>.
        /// </value>
        /// <remarks>
        /// The value takes precedence over <see cref="XmlSerializerOptions.DefaultIgnoreCondition"/> for the member.
        /// </remarks>
        public XmlIgnoreCondition Condition { get; set; } = XmlIgnoreCondition.Always;

        #endregion

    }

}
