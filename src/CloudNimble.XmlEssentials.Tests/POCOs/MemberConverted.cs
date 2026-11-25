using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds members that name their own converter, next to a member of the same type that does not.
    /// </summary>
    public sealed class MemberConverted
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a number written in hexadecimal as an attribute.
        /// </summary>
        [XmlAttribute]
        [XmlConverter<HexInt32Converter>]
        public int Flags { get; set; }

        /// <summary>
        /// Gets or sets a number written as decimal digits.
        /// </summary>
        public int Plain { get; set; }

        /// <summary>
        /// Gets or sets a number written in hexadecimal.
        /// </summary>
        [XmlConverter<HexInt32Converter>]
        public int Hex { get; set; }

        /// <summary>
        /// Gets or sets a number that can be null, written in hexadecimal.
        /// </summary>
        [XmlConverter<HexInt32Converter>]
        public int? Maybe { get; set; }

        /// <summary>
        /// Gets or sets a list whose whole value is written by one converter.
        /// </summary>
        [XmlConverter<JoinedInt32ListConverter>]
        public List<int>? Joined { get; set; }

        #endregion

    }

}
