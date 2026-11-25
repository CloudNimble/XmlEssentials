namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// A base class with a property declared before a field.
    /// </summary>
    public class MixedBase
    {

        #region Fields

        /// <summary>
        /// A field of the base class.
        /// </summary>
        public int BaseField = 2;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a property of the base class.
        /// </summary>
        public int BaseProp { get; set; } = 1;

        #endregion

    }

}
