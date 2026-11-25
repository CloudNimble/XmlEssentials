namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A discount code, created through its constructor.
    /// </summary>
    /// <param name="Code">The code the customer enters.</param>
    /// <param name="PercentOff">The discount, as a percentage. The default is 10.</param>
    public sealed record Coupon(string Code, int PercentOff = 10);

}
