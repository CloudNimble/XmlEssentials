using System;
using System.IO;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 2: walks a UTF-8 document one token at a time with <see cref="Utf8XmlReader"/>.
    /// </summary>
    public static class ReadWithUtf8XmlReader
    {

        #region Public Methods

        /// <summary>
        /// Reads an order, taking its attribute and the text of each child element.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <Order id="1042">
                  <Item>Keyboard</Item>
                  <Item>Mouse</Item>
                  <Total>129.90</Total>
                </Order>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (reader.LocalNameEquals("Order"u8))
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalNameEquals("id"u8))
                        {
                            output.WriteLine($"Order {reader.GetInt32()}");
                        }
                    }

                    reader.MoveToElement();
                }
                else if (reader.LocalNameEquals("Item"u8))
                {
                    reader.Read();
                    output.WriteLine($"Item: {reader.GetString()}");
                }
                else if (reader.LocalNameEquals("Total"u8))
                {
                    reader.Read();
                    output.WriteLine($"Total: {reader.GetDecimal()}");
                }
            }
        }

        #endregion

    }

}
