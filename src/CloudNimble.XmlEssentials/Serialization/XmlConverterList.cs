using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The <see cref="XmlSerializerOptions.Converters"/> list. Changing it drops the converters the options
    /// have already resolved.
    /// </summary>
    internal sealed class XmlConverterList : XmlConfigurationList<XmlConverter>
    {

        #region Constructors

        /// <summary>
        /// Creates the list.
        /// </summary>
        /// <param name="options">The owner.</param>
        /// <param name="items">The first items.</param>
        internal XmlConverterList(XmlSerializerOptions options, IEnumerable<XmlConverter> items)
            : base(options)
        {
            ReplaceAll(items);
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        private protected override void OnChanged()
        {
            Options.ClearConverterCache();
        }

        #endregion

    }

}
