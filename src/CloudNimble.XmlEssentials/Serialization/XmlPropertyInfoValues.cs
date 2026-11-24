using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides serialization metadata about a property or field, whatever the type of its value.
    /// </summary>
    /// <remarks>
    /// Use <see cref="XmlPropertyInfoValues{TValue}"/> to describe a member. This class is the form a list of members
    /// of different types is held in. It is analogous to <c>JsonPropertyInfo</c> in <c>System.Text.Json</c> as a
    /// source-generated context uses it.
    /// </remarks>
    public abstract class XmlPropertyInfoValues
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlPropertyInfoValues"/> class.
        /// </summary>
        /// <remarks>
        /// Only <see cref="XmlPropertyInfoValues{TValue}"/> derives from this class: a class outside this library cannot.
        /// </remarks>
        private protected XmlPropertyInfoValues()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Turns the values into the facts the object binder works from.
        /// </summary>
        /// <returns>The facts of the member.</returns>
        internal abstract XmlMemberFacts CreateFacts();

        #endregion

    }

}
