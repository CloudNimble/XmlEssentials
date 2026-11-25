using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A naming policy that writes every name in lowercase.
    /// </summary>
    public sealed class LowerCaseNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }

        #endregion

    }

}
