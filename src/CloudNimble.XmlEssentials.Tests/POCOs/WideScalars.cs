using System;
using System.Collections.Generic;
using System.Numerics;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds each scalar type that has no XML Schema type of its own size, as an element, as an attribute,
    /// as a nullable, and as a collection item.
    /// </summary>
    public sealed class WideScalars
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a 128-bit integer written as an attribute.
        /// </summary>
        [XmlAttribute]
        public Int128 Code { get; set; }

        /// <summary>
        /// Gets or sets a character written as an attribute.
        /// </summary>
        [XmlAttribute]
        public char Mark { get; set; }

        /// <summary>
        /// Gets or sets an integer of any size.
        /// </summary>
        public BigInteger Big { get; set; }

        /// <summary>
        /// Gets or sets bytes held as memory.
        /// </summary>
        public Memory<byte> Bytes { get; set; }

        /// <summary>
        /// Gets or sets a character.
        /// </summary>
        public char Letter { get; set; }

        /// <summary>
        /// Gets or sets a half-precision number.
        /// </summary>
        public Half Ratio { get; set; }

        /// <summary>
        /// Gets or sets a nullable 128-bit integer.
        /// </summary>
        public Int128? Maybe { get; set; }

        /// <summary>
        /// Gets or sets bytes held as read-only memory.
        /// </summary>
        public ReadOnlyMemory<byte> Sealed { get; set; }

        /// <summary>
        /// Gets or sets 128-bit unsigned integers, written as repeated elements.
        /// </summary>
        public List<UInt128> Totals { get; set; } = [];

        #endregion

    }

}
