using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// One member of every built-in type whose text has a known longest length, each holding the value with
    /// the longest text. Every member is an attribute.
    /// </summary>
    public sealed class WholeValueAttributes
    {

        #region Public Properties

        /// <summary>Gets or sets a Boolean.</summary>
        [XmlAttribute]
        public bool Boolean { get; set; } = false;

        /// <summary>Gets or sets a byte.</summary>
        [XmlAttribute]
        public byte Byte { get; set; } = byte.MaxValue;

        /// <summary>Gets or sets a signed byte.</summary>
        [XmlAttribute]
        public sbyte SByte { get; set; } = sbyte.MinValue;

        /// <summary>Gets or sets a 16-bit integer.</summary>
        [XmlAttribute]
        public short Int16 { get; set; } = short.MinValue;

        /// <summary>Gets or sets an unsigned 16-bit integer.</summary>
        [XmlAttribute]
        public ushort UInt16 { get; set; } = ushort.MaxValue;

        /// <summary>Gets or sets a 32-bit integer.</summary>
        [XmlAttribute]
        public int Int32 { get; set; } = int.MinValue;

        /// <summary>Gets or sets an unsigned 32-bit integer.</summary>
        [XmlAttribute]
        public uint UInt32 { get; set; } = uint.MaxValue;

        /// <summary>Gets or sets a 64-bit integer.</summary>
        [XmlAttribute]
        public long Int64 { get; set; } = long.MinValue;

        /// <summary>Gets or sets an unsigned 64-bit integer.</summary>
        [XmlAttribute]
        public ulong UInt64 { get; set; } = ulong.MaxValue;

        /// <summary>Gets or sets a 128-bit integer.</summary>
        [XmlAttribute]
        public Int128 Int128 { get; set; } = Int128.MinValue;

        /// <summary>Gets or sets an unsigned 128-bit integer.</summary>
        [XmlAttribute]
        public UInt128 UInt128 { get; set; } = UInt128.MaxValue;

        /// <summary>Gets or sets a half-precision number.</summary>
        [XmlAttribute]
        public Half Half { get; set; } = Half.MinValue;

        /// <summary>Gets or sets a single-precision number.</summary>
        [XmlAttribute]
        public float Single { get; set; } = float.MinValue;

        /// <summary>Gets or sets a double-precision number.</summary>
        [XmlAttribute]
        public double Double { get; set; } = double.MinValue;

        /// <summary>Gets or sets a number that is not finite.</summary>
        [XmlAttribute]
        public double Infinity { get; set; } = double.NegativeInfinity;

        /// <summary>Gets or sets a decimal.</summary>
        [XmlAttribute]
        public decimal Decimal { get; set; } = -7.9228162514264337593543950335m;

        /// <summary>Gets or sets a date and time with every fraction digit and a time zone.</summary>
        [XmlAttribute]
        public DateTime DateTime { get; set; } = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999);

        /// <summary>Gets or sets a date and time with an offset.</summary>
        [XmlAttribute]
        public DateTimeOffset DateTimeOffset { get; set; } = new DateTimeOffset(9999, 12, 31, 9, 59, 59, TimeSpan.FromHours(-14)).AddTicks(9_999_999);

        /// <summary>Gets or sets a date.</summary>
        [XmlAttribute]
        public DateOnly DateOnly { get; set; } = DateOnly.MaxValue;

        /// <summary>Gets or sets a time of day.</summary>
        [XmlAttribute]
        public TimeOnly TimeOnly { get; set; } = TimeOnly.MaxValue;

        /// <summary>Gets or sets a duration.</summary>
        [XmlAttribute]
        public TimeSpan TimeSpan { get; set; } = TimeSpan.MinValue;

        /// <summary>Gets or sets a GUID.</summary>
        [XmlAttribute]
        public Guid Guid { get; set; } = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

        /// <summary>Gets or sets a nullable number that holds a value.</summary>
        [XmlAttribute]
        public long? Nullable { get; set; } = long.MinValue;

        /// <summary>Gets or sets text that needs no escaping.</summary>
        [XmlAttribute]
        public string Text { get; set; } = "plain text of some length";

        #endregion

    }

}
