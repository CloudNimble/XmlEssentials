using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 9: leaves properties and fields out of the XML that <see cref="XmlSerializer"/> writes and reads.
    /// </summary>
    public static class IgnoreProperties
    {

        #region Public Methods

        /// <summary>
        /// Serializes and deserializes a <see cref="UserAccount"/>, whose password is always ignored.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var account = new UserAccount { UserName = "ada", Password = "hunter2", Email = "ada@contoso.example" };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            output.WriteLine(XmlSerializer.Serialize(account, options));

            UserAccount? copy = XmlSerializer.Deserialize<UserAccount>(
                "<UserAccount><UserName>ada</UserName><Password>injected</Password></UserAccount>");
            output.WriteLine($"Password: {copy!.Password ?? "(null)"}");
        }

        /// <summary>
        /// Serializes a <see cref="UserProfile"/>, whose members are ignored one by one when they are null or default.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Conditions(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            var empty = new UserProfile { DisplayName = "Ada" };
            output.WriteLine(XmlSerializer.Serialize(empty, options));

            var full = new UserProfile { DisplayName = "Ada", Bio = "Mathematician", SignInCount = 3, Website = "https://contoso.example" };
            output.WriteLine(XmlSerializer.Serialize(full, options));
        }

        /// <summary>
        /// Serializes a <see cref="PurchaseOrder"/> with each ignore condition set for every member at once.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DefaultCondition(TextWriter output)
        {
            var order = new PurchaseOrder { Id = 1042 };

            var never = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(order, never));

            var whenNull = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
            };
            output.WriteLine(XmlSerializer.Serialize(order, whenNull));

            var whenDefault = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault,
            };
            output.WriteLine(XmlSerializer.Serialize(order, whenDefault));
        }

        /// <summary>
        /// Serializes a <see cref="Rectangle"/> with and without its read-only property.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadOnlyProperties(TextWriter output)
        {
            var rectangle = new Rectangle { Width = 4, Height = 3 };

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(rectangle, options));

            var skipReadOnly = new XmlSerializerOptions { OmitXmlDeclaration = true, IgnoreReadOnlyProperties = true };
            output.WriteLine(XmlSerializer.Serialize(rectangle, skipReadOnly));

            Rectangle? copy = XmlSerializer.Deserialize<Rectangle>(
                "<Rectangle><Width>4</Width><Height>3</Height><Area>99</Area></Rectangle>");
            output.WriteLine($"Area: {copy!.Area}");
        }

        /// <summary>
        /// Shows that a member with a write condition is still read.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ConditionsOnRead(TextWriter output)
        {
            UserProfile? profile = XmlSerializer.Deserialize<UserProfile>(
                "<UserProfile><DisplayName>Ada</DisplayName><Bio>Mathematician</Bio><SignInCount>3</SignInCount></UserProfile>");

            output.WriteLine($"{profile!.DisplayName}: {profile.Bio}, {profile.SignInCount} sign-ins");
        }

        /// <summary>
        /// Serializes a <see cref="Coordinates"/>, whose data is in public fields.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Fields(TextWriter output)
        {
            var position = new Coordinates { Latitude = 47.5, Longitude = -122.25 };

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(position, options));

            var withFields = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };
            output.WriteLine(XmlSerializer.Serialize(position, withFields));

            var writableFields = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true, IgnoreReadOnlyFields = true };
            output.WriteLine(XmlSerializer.Serialize(position, writableFields));
        }

        #endregion

    }

}
