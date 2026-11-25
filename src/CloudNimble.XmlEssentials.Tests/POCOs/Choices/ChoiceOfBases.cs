using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A choice whose alternatives are base types: one whose derived types <c>xsi:type</c> identifies, one whose
    /// derived types an attribute identifies, and one that declares none.
    /// </summary>
    public sealed class ChoiceOfBases
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the values.
        /// </summary>
        [XmlElement<TypedShape>("typed")]
        [XmlElement<KindShape>("kind")]
        [XmlElement<ChoiceShape>("plain")]
        public List<object> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets one value.
        /// </summary>
        [XmlElement<TypedShape>("one-typed")]
        [XmlElement<ChoiceShape>("one-plain")]
        public object? One { get; set; }

        #endregion

    }

}