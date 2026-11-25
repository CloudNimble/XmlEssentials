using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// One member of every built-in type whose text has a known longest length, each holding the value with
    /// the longest text. Every member is an element.
    /// </summary>
    public sealed class WholeValues
    {

        #region Public Properties

        /// <summary>Gets or sets a Boolean.</summary>
        public bool Boolean { get; set; } = false;

        /// <summary>Gets or sets a byte.</summary>
        public byte Byte { get; set; } = byte.MaxValue;

        /// <summary>Gets or sets a signed byte.</summary>
        public sbyte SByte { get; set; } = sbyte.MinValue;

        /// <summary>Gets or sets a 16-bit integer.</summary>
        public short Int16 { get; set; } = short.MinValue;

        /// <summary>Gets or sets an unsigned 16-bit integer.</summary>
        public ushort UInt16 { get; set; } = ushort.MaxValue;

        /// <summary>Gets or sets a 32-bit integer.</summary>
        public int Int32 { get; set; } = int.MinValue;

        /// <summary>Gets or sets an unsigned 32-bit integer.</summary>
        public uint UInt32 { get; set; } = uint.MaxValue;

        /// <summary>Gets or sets a 64-bit integer.</summary>
        public long Int64 { get; set; } = long.MinValue;

        /// <summary>Gets or sets an unsigned 64-bit integer.</summary>
        public ulong UInt64 { get; set; } = ulong.MaxValue;

        /// <summary>Gets or sets a 128-bit integer.</summary>
        public Int128 Int128 { get; set; } = Int128.MinValue;

        /// <summary>Gets or sets an unsigned 128-bit integer.</summary>
        public UInt128 UInt128 { get; set; } = UInt128.MaxValue;

        /// <summary>Gets or sets a half-precision number.</summary>
        public Half Half { get; set; } = Half.MinValue;

        /// <summary>Gets or sets a single-precision number.</summary>
        public float Single { get; set; } = float.MinValue;

        /// <summary>Gets or sets a double-precision number.</summary>
        public double Double { get; set; } = double.MinValue;

        /// <summary>Gets or sets a number that is not finite.</summary>
        public double Infinity { get; set; } = double.NegativeInfinity;

        /// <summary>Gets or sets a decimal.</summary>
        public decimal Decimal { get; set; } = -7.9228162514264337593543950335m;

        /// <summary>Gets or sets a date and time with every fraction digit and a time zone.</summary>
        public DateTime DateTime { get; set; } = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999);

        /// <summary>Gets or sets a date and time with an offset.</summary>
        public DateTimeOffset DateTimeOffset { get; set; } = new DateTimeOffset(9999, 12, 31, 9, 59, 59, TimeSpan.FromHours(-14)).AddTicks(9_999_999);

        /// <summary>Gets or sets a date.</summary>
        public DateOnly DateOnly { get; set; } = DateOnly.MaxValue;

        /// <summary>Gets or sets a time of day.</summary>
        public TimeOnly TimeOnly { get; set; } = TimeOnly.MaxValue;

        /// <summary>Gets or sets a duration.</summary>
        public TimeSpan TimeSpan { get; set; } = TimeSpan.MinValue;

        /// <summary>Gets or sets a GUID.</summary>
        public Guid Guid { get; set; } = new("ffffffff-ffff-ffff-ffff-ffffffffffff");

        /// <summary>Gets or sets a nullable number that holds a value.</summary>
        public long? Nullable { get; set; } = long.MinValue;

        /// <summary>Gets or sets text that needs no escaping.</summary>
        public string Text { get; set; } = "plain text of some length";

        #endregion

    }

}
