using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets the global order of a member among the flattened members of a type. Analogous to <c>JsonPropertyOrderAttribute</c>.
    /// </summary>
    /// <remarks>
    /// Members with order greater than or equal to zero are written first, sorted by that integer.
    /// Members with the default of <c>-1</c> follow in declaration order from most-base to most-derived.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlPropertyOrderAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the order. Values greater than or equal to zero participate in explicit ordering.
        /// </summary>
        public int Order { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="order">The order. Use a value greater than or equal to zero to participate in explicit ordering.</param>
        public XmlPropertyOrderAttribute(int order)
        {
            Order = order;
        }

        #endregion

    }

}
