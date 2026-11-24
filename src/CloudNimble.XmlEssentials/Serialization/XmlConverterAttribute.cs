using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the converter to use when placed on a property, field, or type.
    /// </summary>
    /// <remarks>
    /// Use <see cref="XmlConverterAttribute{TConverter}"/> to name the converter. This class is the form the serializer
    /// looks for, whatever converter type the attribute names.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public abstract class XmlConverterAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// When overridden in a derived class, gets the type of the converter to use.
        /// </summary>
        /// <value>
        /// The type of the converter, which derives from <see cref="XmlConverter"/>.
        /// </value>
        public abstract Type ConverterType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConverterAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// Only <see cref="XmlConverterAttribute{TConverter}"/> derives from this class: a class outside this library cannot.
        /// </remarks>
        protected XmlConverterAttribute()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the converter the attribute names.
        /// </summary>
        /// <returns>A new converter.</returns>
        internal abstract XmlConverter CreateConverter();

        #endregion

    }

}
