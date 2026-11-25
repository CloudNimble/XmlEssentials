namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A base type whose derived type is declared without a discriminator.
    /// </summary>
    [TypeNamedDerived(typeof(Car))]
    public class Vehicle
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of wheels.
        /// </summary>
        public int Wheels { get; set; }

        #endregion

    }

}
