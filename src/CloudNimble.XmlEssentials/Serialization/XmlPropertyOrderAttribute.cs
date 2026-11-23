using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the order in which a property or field is written among the members of a type, including inherited members.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Members with an order greater than or equal to zero are written first, sorted by that value. Members with a negative
    /// order, or with no order, follow in declaration order from the most-base type to the most-derived type. Ordering
    /// applies within each kind of member: attributes are written first, then text content, then child elements.
    /// This attribute is analogous to <c>JsonPropertyOrderAttribute</c> in <c>System.Text.Json</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlPropertyOrderAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the serialization order of the member.
        /// </summary>
        /// <value>
        /// The serialization order of the member. Lower values are written first. A value greater than or equal to zero takes
        /// part in explicit ordering, and a negative value means the order is not set.
        /// </value>
        public int Order { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlPropertyOrderAttribute"/> class with the specified order.
        /// </summary>
        /// <param name="order">The serialization order of the member. Use a value greater than or equal to zero to take part in explicit ordering.</param>
        public XmlPropertyOrderAttribute(int order)
        {
            Order = order;
        }

        #endregion

    }

}
