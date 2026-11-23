using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog that binds an unwrapped collection through a constructor parameter.
    /// </summary>
    public sealed class CtorCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the items.
        /// </summary>
        public List<string> Items { get; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string Title { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the catalog.
        /// </summary>
        /// <param name="Items">The items.</param>
        /// <param name="Title">The title.</param>
        [XmlConstructor]
        public CtorCatalog(List<string> Items, string Title)
        {
            this.Items = Items;
            this.Title = Title;
        }

        #endregion

    }

}
