using System;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// A type with no mapping attributes, declared as a type for <c>System.Xml.Serialization.XmlSerializer</c>
    /// usually is: a public field, read-only members, collections of several kinds, and members that may be
    /// <see langword="null"/>. <c>XmlSerializerBclDefaultsTests</c> holds what that serializer writes for it.
    /// </summary>
    /// <remarks>
    /// The properties are in the order the document has them, which is the order of declaration, so they are
    /// not in alphabetical order. That serializer writes the field before them.
    /// </remarks>
    public class BclOrder
    {

        #region Fields

        /// <summary>
        /// A public field, which the old serializer writes, and writes first.
        /// </summary>
        public string? Field;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets text that needs escaping.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets text that is <see langword="null"/>.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets a collection of text.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection of objects.
        /// </summary>
        public List<BclLine> Lines { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection with nothing in it.
        /// </summary>
        public List<int> Empty { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection that is <see langword="null"/>.
        /// </summary>
        public List<int>? Missing { get; set; }

        /// <summary>
        /// Gets a collection with no setter.
        /// </summary>
        public List<int> Fixed { get; } = [];

        /// <summary>
        /// Gets or sets a collection of collections.
        /// </summary>
        public int[][] Grid { get; set; } = [[1]];

        /// <summary>
        /// Gets or sets an enumeration value.
        /// </summary>
        public BclStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a character.
        /// </summary>
        public char Initial { get; set; }

        /// <summary>
        /// Gets or sets binary data.
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Gets or sets a nullable number that has no value.
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets a nullable number that has a value.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets a nested object.
        /// </summary>
        public BclLine? Child { get; set; }

        /// <summary>
        /// Gets or sets a point in time.
        /// </summary>
        public DateTime When { get; set; }

        /// <summary>
        /// Gets or sets a point in time with an offset.
        /// </summary>
        public DateTimeOffset Sent { get; set; }

        /// <summary>
        /// Gets or sets a decimal with a trailing zero.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets a floating-point number.
        /// </summary>
        public double Ratio { get; set; }

        /// <summary>
        /// Gets or sets a flag.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets an identifier.
        /// </summary>
        public Guid Key { get; set; }

        /// <summary>
        /// Gets a value with no setter, which the old serializer does not write.
        /// </summary>
        public string ReadOnly => "r";

        #endregion

    }

}
