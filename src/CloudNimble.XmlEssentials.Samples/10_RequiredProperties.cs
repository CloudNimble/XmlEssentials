using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 10: makes <see cref="XmlSerializer"/> reject XML that lacks a member, or that holds a member the type does not have.
    /// </summary>
    public static class RequiredProperties
    {

        #region Public Methods

        /// <summary>
        /// Deserializes a <see cref="Registration"/> from XML that lacks each required member in turn.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            string[] documents =
            [
                "<Registration><Email>ada@contoso.example</Email><Plan>pro</Plan></Registration>",
                "<Registration><Plan>pro</Plan></Registration>",
                "<Registration><Email>ada@contoso.example</Email></Registration>",
            ];

            foreach (string xml in documents)
            {
                try
                {
                    Registration? registration = XmlSerializer.Deserialize<Registration>(xml);
                    output.WriteLine($"{registration!.Email} chose {registration.Plan}");
                }
                catch (XmlException ex)
                {
                    output.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Deserializes a <see cref="Registration"/> whose required member is present but nil, and one whose required member is empty.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NilAndEmpty(TextWriter output)
        {
            const string nil = """
                <Registration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                  <Email xsi:nil="true" />
                  <Plan>pro</Plan>
                </Registration>
                """;

            try
            {
                XmlSerializer.Deserialize<Registration>(nil);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            Registration? empty = XmlSerializer.Deserialize<Registration>("<Registration><Email /><Plan>pro</Plan></Registration>");
            output.WriteLine($"Email is empty: {empty!.Email.Length == 0}");
        }

        /// <summary>
        /// Deserializes a <see cref="Coupon"/>, whose constructor parameters are required unless they have a default value.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ConstructorParameters(TextWriter output)
        {
            Coupon? coupon = XmlSerializer.Deserialize<Coupon>("<Coupon><Code>SPRING</Code></Coupon>");
            output.WriteLine($"{coupon!.Code}: {coupon.PercentOff}% off");

            try
            {
                XmlSerializer.Deserialize<Coupon>("<Coupon><PercentOff>25</PercentOff></Coupon>");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            var options = new XmlSerializerOptions { RespectRequiredConstructorParameters = false };
            Coupon? lenient = XmlSerializer.Deserialize<Coupon>("<Coupon><PercentOff>25</PercentOff></Coupon>", options);
            output.WriteLine($"{lenient!.Code ?? "(null)"}: {lenient.PercentOff}% off");
        }

        /// <summary>
        /// Deserializes XML that holds an element and an attribute that <see cref="Registration"/> does not map, with and without rejecting them.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UnmappedMembers(TextWriter output)
        {
            string[] documents =
            [
                "<Registration><Email>ada@contoso.example</Email><Plan>pro</Plan><Coupon>SPRING</Coupon></Registration>",
                "<Registration source=\"web\"><Email>ada@contoso.example</Email><Plan>pro</Plan></Registration>",
            ];

            var options = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };

            foreach (string xml in documents)
            {
                Registration? registration = XmlSerializer.Deserialize<Registration>(xml);
                output.WriteLine($"Skip: {registration!.Email} chose {registration.Plan}");

                try
                {
                    XmlSerializer.Deserialize<Registration>(xml, options);
                }
                catch (XmlException ex)
                {
                    output.WriteLine($"Disallow: {ex.Message}");
                }
            }
        }

        #endregion

    }

}
