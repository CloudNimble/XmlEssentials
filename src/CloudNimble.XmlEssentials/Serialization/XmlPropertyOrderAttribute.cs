using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the order in which a property or field is written among the members of a type, including inherited members.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Members are written from the lowest order to the highest. A member with no order is placed as one whose order is
    /// <c>0</c>, so a member with a negative order is written before the members that set none, and a member with a
    /// positive order after them. Members with the same order keep the order they are declared in, from the most-base
    /// type to the most-derived type. Ordering applies within each kind of member: attributes are written first, then
    /// text content, then child elements. This attribute orders members as <c>JsonPropertyOrderAttribute</c> does in
    /// <c>System.Text.Json</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlPropertyOrderAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the serialization order of the member.
        /// </summary>
        /// <value>
        /// The serialization order of the member. Lower values are written first, and a negative value is written before
        /// the members that set no order.
        /// </value>
        public int Order { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlPropertyOrderAttribute"/> class with the specified order.
        /// </summary>
        /// <param name="order">The serialization order of the member. Lower values are written first.</param>
        public XmlPropertyOrderAttribute(int order)
        {
            Order = order;
        }

        #endregion

    }

}
