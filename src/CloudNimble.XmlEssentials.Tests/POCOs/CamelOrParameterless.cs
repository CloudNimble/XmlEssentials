using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with a public parameterless constructor and a constructor with a camelCase parameter.
    /// </summary>
    public sealed class CamelOrParameterless
    {

        #region Public Properties

        /// <summary>
        /// Gets which constructor created the instance.
        /// </summary>
        [XmlIgnore]
        public string CreatedBy { get; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance with no name.
        /// </summary>
        public CamelOrParameterless()
        {
            CreatedBy = "parameterless";
        }

        /// <summary>
        /// Creates an instance with a name.
        /// </summary>
        /// <param name="name">The name.</param>
        public CamelOrParameterless(string? name)
        {
            Name = name;
            CreatedBy = "parameterized";
        }

        #endregion

    }

}
