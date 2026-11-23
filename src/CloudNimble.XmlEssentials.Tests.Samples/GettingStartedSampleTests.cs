using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 1 to 5 and compares what each returns with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class GettingStartedSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample01_SerializeAndDeserialize()
        {
            SampleOutput.Capture(SerializeAndDeserialize.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Person><Name>Ada</Name><Age>36</Age></Person>\n" +
                "Ada is 36");
        }

        [TestMethod]
        public void Sample02_ReadWithUtf8XmlReader()
        {
            SampleOutput.Capture(ReadWithUtf8XmlReader.Run).Should().Be("Order 1042\nItem: Keyboard\nItem: Mouse\nTotal: 129.90");
        }

        [TestMethod]
        public void Sample03_WriteWithUtf8XmlWriter()
        {
            SampleOutput.Capture(WriteWithUtf8XmlWriter.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Order id=\"1042\">\n" +
                "  <Item>Keyboard</Item>\n" +
                "  <Item>Mouse &amp; pad</Item>\n" +
                "  <Total>129.90</Total>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample04_QueryWithXmlDocument()
        {
            SampleOutput.Capture(QueryWithXmlDocument.Run).Should().Be("Order 1042\nItem: Keyboard\nItem: Mouse\nTotal: 129.90");
        }

        [TestMethod]
        public void Sample05_EditWithXmlNode()
        {
            SampleOutput.Capture(EditWithXmlNode.Run).Should().Be("<Order id=\"1042\" status=\"shipped\"><Item>Keyboard</Item><Item>Mouse</Item></Order>");
        }

        #endregion

    }

}
