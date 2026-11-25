using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 1: writes an object as XML and reads it back with <see cref="XmlSerializer"/>.
    /// </summary>
    public static class SerializeAndDeserialize
    {

        #region Public Methods

        /// <summary>
        /// Serializes a <see cref="Person"/> to a string and deserializes that string.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var person = new Person { Name = "Ada", Age = 36 };

            string xml = XmlSerializer.Serialize(person);
            output.WriteLine(xml);

            Person? copy = XmlSerializer.Deserialize<Person>(xml);
            output.WriteLine($"{copy!.Name} is {copy.Age}");
        }

        #endregion

    }

}
