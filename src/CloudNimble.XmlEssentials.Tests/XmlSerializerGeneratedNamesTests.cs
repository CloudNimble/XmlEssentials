using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A generated context names and qualifies the document element and the members as the reflection resolver
    /// does: <c>[XmlRoot]</c> names and namespaces, and the namespaces of elements, attributes, wrappers, and
    /// repeated items. Each test compiles a program with the generator and runs it; the program writes each
    /// value through the context and through the reflection resolver, and reads the document back through both.
    /// </summary>
    [TestClass]
    public class XmlSerializerGeneratedNamesTests
    {

        #region Fields

        private const string Usings = """
            using System;
            using System.Collections.Generic;
            using CloudNimble.Text.Xml;
            using CloudNimble.Text.Xml.Serialization;

            """;

        /// <summary>
        /// Writes a value through the context and through the reflection resolver with the same options, and
        /// reads the context's document back through both.
        /// </summary>
        private const string Compare = """

            public static class Both
            {
                public static string Write<T>(T value, Func<XmlSerializerOptions, XmlTypeInfo<T>> info, Func<T, string> show, string? defaultNamespace = null)
                {
                    var generatedOptions = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = defaultNamespace };
                    var typeInfo = info(generatedOptions);
                    var reflectionOptions = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = defaultNamespace };
                    var generated = XmlSerializer.Serialize(value, typeInfo);
                    var reflected = XmlSerializer.Serialize(value, typeof(T), reflectionOptions);
                    var readByContext = show(XmlSerializer.Deserialize(generated, typeInfo)!);
                    var readByReflection = show((T)XmlSerializer.Deserialize(generated, typeof(T), reflectionOptions)!);
                    return generated + "\n" + (generated == reflected ? "same" : "reflection: " + reflected) + "\n" + readByContext + "\n" + readByReflection;
                }
            }
            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void MemberNamespaces_AreWrittenAsTheReflectionResolverWritesThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Order
                {
                    [XmlElement("Address", Namespace = "urn:addr")]
                    public string? ShipTo { get; set; }

                    [XmlAttribute(Namespace = "urn:attr")]
                    public string? Code { get; set; }

                    public int Total { get; set; }

                    [XmlArray(Namespace = "urn:arr")]
                    public List<string> Tags { get; set; } = new();

                    [XmlElement(Namespace = "urn:line")]
                    public List<string> Lines { get; set; } = new();
                }

                [XmlSerializable(typeof(Order))]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var order = new Order { ShipTo = "here", Code = "X", Total = 5, Tags = { "a", "b" }, Lines = { "l1", "l2" } };
                        return Both.Write(order, options => new AppXmlContext(options).Order, o => o.ShipTo + "," + o.Code + "," + o.Total + "," + string.Join("+", o.Tags) + "," + string.Join("+", o.Lines));
                    }
                }
                """ + Compare).Should().Be(
                "<Order xmlns:ns1=\"urn:attr\" ns1:Code=\"X\"><Address xmlns=\"urn:addr\">here</Address><Total>5</Total><Tags xmlns=\"urn:arr\"><String>a</String><String>b</String></Tags><Lines xmlns=\"urn:line\">l1</Lines><Lines xmlns=\"urn:line\">l2</Lines></Order>\n" +
                "same\n" +
                "here,X,5,a+b,l1+l2\n" +
                "here,X,5,a+b,l1+l2");
        }

        [TestMethod]
        public void MemberNamespaces_UnderADefaultNamespace_AreWrittenAsTheReflectionResolverWritesThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Order
                {
                    [XmlElement("Address", Namespace = "urn:addr")]
                    public string? ShipTo { get; set; }

                    public int Total { get; set; }
                }

                [XmlSerializable(typeof(Order))]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(new Order { ShipTo = "here", Total = 5 }, options => new AppXmlContext(options).Order, o => o.ShipTo + "," + o.Total, "urn:d");
                    }
                }
                """ + Compare).Should().Be(
                "<Order xmlns=\"urn:d\"><Address xmlns=\"urn:addr\">here</Address><Total>5</Total></Order>\n" +
                "same\n" +
                "here,5\n" +
                "here,5");
        }

        [TestMethod]
        [DataRow("[XmlRoot(\"purchaseOrder\")]", null, "<purchaseOrder><N>4</N></purchaseOrder>", DisplayName = "a name")]
        [DataRow("[XmlRoot(\"purchaseOrder\")]", "urn:d", "<purchaseOrder xmlns=\"urn:d\"><N>4</N></purchaseOrder>", DisplayName = "a name, under a default namespace")]
        [DataRow("[XmlRoot(\"Person\", Namespace = \"urn:people\")]", null, "<Person xmlns=\"urn:people\"><N>4</N></Person>", DisplayName = "a name and a namespace")]
        [DataRow("[XmlRoot(\"Person\", Namespace = \"urn:people\")]", "urn:d", "<Person xmlns=\"urn:people\"><N>4</N></Person>", DisplayName = "a name and a namespace, under a default namespace")]
        [DataRow("[XmlRoot(Namespace = \"urn:only\")]", null, "<Thing xmlns=\"urn:only\"><N>4</N></Thing>", DisplayName = "a namespace only")]
        [DataRow("[XmlRoot(Namespace = \"\")]", "urn:d", "<Thing><N>4</N></Thing>", DisplayName = "no namespace, under a default namespace")]
        [DataRow("", null, "<Thing><N>4</N></Thing>", DisplayName = "no attribute")]
        [DataRow("", "urn:d", "<Thing xmlns=\"urn:d\"><N>4</N></Thing>", DisplayName = "no attribute, under a default namespace")]
        public void RootAttribute_NamesTheDocumentElement(string attribute, string? defaultNamespace, string expected)
        {
            var defaultNamespaceLiteral = defaultNamespace is null ? "null" : "\"" + defaultNamespace + "\"";
            XmlSerializerGeneratorTests.RunGenerated(Usings + attribute + """

                public sealed class Thing
                {
                    public int N { get; set; }
                }

                [XmlSerializable(typeof(Thing))]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(new Thing { N = 4 }, options => new AppXmlContext(options).Thing, t => t.N.ToString(), DEFAULT_NAMESPACE);
                    }
                }
                """.Replace("DEFAULT_NAMESPACE", defaultNamespaceLiteral) + Compare).Should().Be(expected + "\nsame\n4\n4");
        }

        [TestMethod]
        public void RootAttribute_OnAnInterceptedCall_NamesTheDocumentElement()
        {
            // Adding a context must not change the document a call already wrote.
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlRoot("Person", Namespace = "urn:people")]
                public sealed class Employee
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable(typeof(Employee))]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var intercepted = XmlSerializer.Serialize(new Employee { Name = "Ada" });
                        var reflected = XmlSerializer.Serialize(new Employee { Name = "Ada" }, typeof(Employee));
                        return intercepted + "|" + (intercepted == reflected) + "|" + XmlSerializer.Deserialize<Employee>(intercepted)!.Name;
                    }
                }
                """).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><Person xmlns=\"urn:people\"><Name>Ada</Name></Person>|True|Ada");
        }

        [TestMethod]
        public void RootAttribute_WithAWrongDocumentElement_IsRejectedAsTheReflectionResolverRejectsIt()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlRoot("purchaseOrder", Namespace = "urn:po")]
                public sealed class Order
                {
                    public int N { get; set; }
                }

                [XmlSerializable(typeof(Order))]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Try("<Order><N>4</N></Order>") + "|" + Try("<purchaseOrder><N>4</N></purchaseOrder>") + "|" + Try("<purchaseOrder xmlns=\"urn:po\"><N>4</N></purchaseOrder>");
                    }

                    private static string Try(string xml)
                    {
                        try
                        {
                            return XmlSerializer.Deserialize(xml, AppXmlContext.Default.Order)!.N.ToString();
                        }
                        catch (XmlException ex)
                        {
                            return ex.Message;
                        }
                    }
                }
                """).Should().Be("The root element does not match 'purchaseOrder'.|The root element is not in namespace 'urn:po'.|4");
        }

        #endregion

    }

}
