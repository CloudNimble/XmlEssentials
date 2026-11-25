using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An amount in a currency, written as one value such as <c>12.50 USD</c> by <see cref="MoneyConverter"/>.
    /// </summary>
    /// <param name="Amount">The amount.</param>
    /// <param name="Currency">The currency code.</param>
    [XmlConverter<MoneyConverter>]
    public readonly record struct Money(decimal Amount, string Currency);

}
