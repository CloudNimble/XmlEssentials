using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Excludes a property or field from serialization, or skips it when a condition matches.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlIgnoreAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets when the member is skipped. The default is <see cref="XmlIgnoreCondition.Always"/>.
        /// </summary>
        public XmlIgnoreCondition Condition { get; set; } = XmlIgnoreCondition.Always;

        #endregion

    }

}
