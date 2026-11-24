using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// Members that each hold a choice of elements: one value, any value, many values in order, and the items of a wrapper.
    /// </summary>
    public sealed class ChoiceHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that is one of two derived types.
        /// </summary>
        [XmlElement<ChoiceCircle>("circle")]
        [XmlElement<ChoiceSquare>("square")]
        public ChoiceShape? One { get; set; }

        /// <summary>
        /// Gets or sets a value that is text, a number or an object.
        /// </summary>
        [XmlElement<string>("text")]
        [XmlElement<int>("number")]
        [XmlElement<ChoiceCircle>("round")]
        public object? Any { get; set; }

        /// <summary>
        /// Gets or sets values of several types, in the order they were added.
        /// </summary>
        [XmlElement<string>("t")]
        [XmlElement<int>("n")]
        [XmlElement<ChoiceSquare>("sq")]
        public List<object> Many { get; set; } = [];

        /// <summary>
        /// Gets or sets items in a wrapper, each named for its type.
        /// </summary>
        [XmlArray("wrapped")]
        [XmlArrayItem<ChoiceCircle>("c")]
        [XmlArrayItem<ChoiceSquare>("s")]
        public List<ChoiceShape> Wrapped { get; set; } = [];

        /// <summary>
        /// Gets or sets a value whose two alternatives have one element name in two namespaces.
        /// </summary>
        [XmlElement<ChoiceCircle>("shape", "urn:circle")]
        [XmlElement<ChoiceSquare>("shape", "urn:square")]
        public ChoiceShape? Spaced { get; set; }

        /// <summary>
        /// Gets or sets items with no wrapper, where the names of items in a wrapper have no effect.
        /// </summary>
        [XmlArrayItem<ChoiceCircle>("c")]
        [XmlArrayItem<ChoiceSquare>("s")]
        public List<ChoiceShape> Flat { get; set; } = [];

        /// <summary>
        /// Gets or sets a value whose alternatives give no element name, so each is named for its type.
        /// </summary>
        [XmlElement<ChoiceCircle>]
        [XmlElement<ChoiceSquare>(ElementName = "")]
        public ChoiceShape? Unnamed { get; set; }

        #endregion

    }

}
