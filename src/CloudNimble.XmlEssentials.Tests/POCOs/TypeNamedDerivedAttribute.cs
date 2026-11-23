using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A derived-type declaration that gives no discriminator, so the type name is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TypeNamedDerivedAttribute : XmlDerivedTypeAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type DerivedType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Declares <paramref name="derivedType"/>.
        /// </summary>
        /// <param name="derivedType">The derived type.</param>
        public TypeNamedDerivedAttribute(Type derivedType)
        {
            DerivedType = derivedType;
        }

        #endregion

    }

}
