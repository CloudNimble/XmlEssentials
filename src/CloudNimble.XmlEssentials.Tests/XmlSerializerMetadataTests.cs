using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Types described by metadata, as a source-generated context describes them, are read and written by the same
    /// engine and with the same result as types the serializer maps through reflection.
    /// </summary>
    [TestClass]
    public class XmlSerializerMetadataTests
    {

        #region Fields

        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

        #endregion

        #region Public Methods

        [TestMethod]
        public void ConstructorParameters_AreFilledAndChecked()
        {
            Both(o => XmlSerializer.Serialize(new MetadataLine(2, "s") { Price = 1.5m }, o))
                .Should().Be("<MetadataLine><Qty>2</Qty><Sku>s</Sku><Price>1.5</Price></MetadataLine>");
            Both(o => XmlSerializer.Deserialize<MetadataLine>("<MetadataLine><Price>1.5</Price><Sku>s</Sku><Qty>2</Qty></MetadataLine>", o)!.ToString())
                .Should().Be("MetadataLine { Qty = 2, Sku = s, Price = 1.5 }");
            Both(o => XmlSerializer.Deserialize<MetadataLine>("<MetadataLine><Sku>s</Sku></MetadataLine>", o)!.ToString())
                .Should().Be("XmlException: The required constructor parameter 'Qty' is missing.");
            Both(o => XmlSerializer.Deserialize<MetadataLine>("<MetadataLine><Qty>2</Qty><Sku" + Nil + "</MetadataLine>", o)!.ToString(), o => o.RespectNullableAnnotations = true)
                .Should().Be("XmlException: The constructor parameter 'Sku' on type 'CloudNimble.XmlEssentials.Tests.POCOs.MetadataLine' doesn't allow null values. Consider updating its nullability annotation.");
        }

        [TestMethod]
        public void DerivedTypes_AreReadAndWritten()
        {
            Both(o => XmlSerializer.Serialize<FallbackProduct>(new FallbackBook { Title = "t", Author = "a" }, o))
                .Should().Be("<FallbackProduct kind=\"book\"><Title>t</Title><Author>a</Author></FallbackProduct>");
            Both(o => XmlSerializer.Deserialize<FallbackProduct>("<FallbackProduct kind=\"book\"><Title>t</Title><Author>a</Author></FallbackProduct>", o)!.GetType().Name)
                .Should().Be("FallbackBook");
        }

        [TestMethod]
        public void ExtensionData_IsKept()
        {
            const string xml = "<ExtraBag other=\"1\"><Name>n</Name><More>m</More></ExtraBag>";
            Both(o => XmlSerializer.Serialize(XmlSerializer.Deserialize<ExtraBag>(xml, o)!, o)).Should().Be(xml);
        }

        [TestMethod]
        public void MemberConverters_ReadAndWriteTheirMembers()
        {
            var value = new MemberConverted { Flags = 255, Plain = 255, Hex = 255, Maybe = 16, Joined = [1, 2, 3] };
            const string xml = "<MemberConverted Flags=\"ff\"><Plain>255</Plain><Hex>ff</Hex><Maybe>10</Maybe><Joined>1,2,3</Joined></MemberConverted>";
            Both(o => XmlSerializer.Serialize(value, o)).Should().Be(xml);
            Both(o =>
            {
                var read = XmlSerializer.Deserialize<MemberConverted>(xml, o)!;
                return read.Flags + "," + read.Plain + "," + read.Hex + "," + read.Maybe + "," + string.Join(";", read.Joined!);
            }).Should().Be("255,255,255,16,1;2;3");
            Both(o => XmlSerializer.Deserialize<MemberConverted>("<MemberConverted><Maybe" + Nil + "</MemberConverted>", o)!.Maybe?.ToString() ?? "null").Should().Be("null");
        }

        [TestMethod]
        public void Members_OfEveryKind_AreMappedAsReflectionMapsThem()
        {
            var order = new MetadataOrder { Id = 7, Name = "n", Counts = { ["a"] = 1 }, Numbers = [1, 2], Origin = new PopulatePoint { X = 3, Y = 4 }, Revision = 9 };
            order.Lines.Add(new MetadataLine(2, "s"));
            const string xml =
                "<MetadataOrder Id=\"7\"><Counts><a>1</a></Counts><Lines><MetadataLine><Qty>2</Qty><Sku>s</Sku><Price>0</Price></MetadataLine></Lines>" +
                "<Name>n</Name><N>1</N><N>2</N><Origin><X>3</X><Y>4</Y></Origin></MetadataOrder>";
            Both(o => XmlSerializer.Serialize(order, o)).Should().Be(xml);
            Both(o => Show(XmlSerializer.Deserialize<MetadataOrder>(xml, o)!)).Should().Be("7|n|a=1|0 lines|1,2|3,4|0|null");
            Both(o => Show(XmlSerializer.Deserialize<MetadataOrder>(xml, o)!), o => o.PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate)
                .Should().Be("7|n|a=1|1 lines|1,2|3,4|0|null");
        }

        [TestMethod]
        public void Options_AreAppliedWhenTheMetadataIsBound()
        {
            var order = new MetadataOrder { Id = 7, Revision = 9 };
            Both(o => XmlSerializer.Serialize(order, o), o => o.PropertyNamingPolicy = XmlNamingPolicy.CamelCase)
                .Should().Be("<metadataOrder id=\"7\"><counts/><lines/><name" + Nil + "<origin><x>0</x><y>0</y></origin></metadataOrder>");
            Both(o => XmlSerializer.Serialize(order, o), o => { o.IncludeFields = true; o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull; })
                .Should().Be("<MetadataOrder Id=\"7\"><Counts/><Lines/><Origin><X>0</X><Y>0</Y></Origin><Revision>9</Revision></MetadataOrder>");
            Both(o => XmlSerializer.Serialize(order, o), o => { o.IgnoreReadOnlyProperties = true; o.DefaultPropertyMapping = XmlPropertyMapping.Attribute; o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault; })
                .Should().Be("<MetadataOrder Id=\"7\"><Counts/></MetadataOrder>", "a value type that holds its default value is left out");
            Both(o => XmlSerializer.Deserialize<Person>("<Person><name>n</name><AGE>3</AGE></Person>", o)!.Name + XmlSerializer.Deserialize<Person>("<Person><name>n</name><AGE>3</AGE></Person>", o)!.Age,
                o => o.PropertyNameCaseInsensitive = true).Should().Be("n3");
            Both(o => XmlSerializer.Deserialize<Person>("<Person><Other>1</Other></Person>", o)!.Age.ToString(), o => o.UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow)
                .Should().Be("XmlException: Unknown element 'Other'.");
        }

        [TestMethod]
        public void Populate_SetsMembersOnTheObjectsThatExist()
        {
            const string xml = "<PopulateNested><Inner><Child><B>2</B></Child><List><Int32>9</Int32></List><Size>4</Size></Inner></PopulateNested>";
            Func<XmlSerializerOptions, string> read = o =>
            {
                var nested = XmlSerializer.Deserialize<PopulateNested>(xml, o)!;
                return nested.Inner.Size + "," + nested.Inner.Child.A + "," + nested.Inner.Child.B + "," + string.Join(";", nested.Inner.List);
            };
            Both(read).Should().Be("3,7,0,1");
            Both(read, o => o.PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate).Should().Be("4,7,2,1;9");
        }

        [TestMethod]
        public void References_ArePreserved()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            const string xml = "<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Child ns1:ref=\"1\"/><Name>root</Name></Node>";
            Both(o => XmlSerializer.Serialize(node, o), o => o.ReferenceHandler = XmlReferenceHandler.Preserve).Should().Be(xml);
            Both(o =>
            {
                var read = XmlSerializer.Deserialize<Node>(xml, o)!;
                return read.Name + "," + ReferenceEquals(read, read.Child);
            }, o => o.ReferenceHandler = XmlReferenceHandler.Preserve).Should().Be("root,True");
        }

        [TestMethod]
        public void SerializeHandler_WritesTheMembers_AndATypeWithNoMetadataCannotBeRead()
        {
            const string none = "The type info for 'CloudNimble.XmlEssentials.Tests.POCOs.Person' was generated in XmlSourceGenerationMode.Serialization, which provides no property metadata. " +
                "It can only write a type that has a write fast path, with the options it was compiled for. Use XmlSourceGenerationMode.Default or XmlSourceGenerationMode.Metadata.";
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            Action<Utf8XmlWriter, Person, XmlSerializerOptions> handler = static (writer, person, _) =>
            {
                writer.WriteStartElement("Fast");
                writer.WriteStringValue(person.Name);
                writer.WriteEndElement();
            };

            // With metadata and a handler, the handler writes and the metadata reads.
            var both = XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<Person>
            {
                ObjectCreator = static () => new Person(),
                PropertyMetadataInitializer = static _ => [new XmlPropertyInfoValues<string?> { PropertyName = "Name", DeclaringType = typeof(Person), Getter = static o => ((Person)o).Name, Setter = static (o, v) => ((Person)o).Name = v }],
                SerializeHandler = handler,
            });
            XmlSerializer.Serialize(new Person { Name = "n" }, both).Should().Be("<Person><Fast>n</Fast></Person>");
            XmlSerializer.Deserialize("<Person><Name>n</Name></Person>", both)!.Name.Should().Be("n");

            // With a handler and no metadata, the type is written and cannot be read.
            var writeOnly = XmlMetadataServices.CreateObjectInfo(new XmlSerializerOptions { OmitXmlDeclaration = true }, new XmlObjectInfoValues<Person> { SerializeHandler = handler });
            XmlSerializer.Serialize(new Person { Name = "n" }, writeOnly).Should().Be("<Person><Fast>n</Fast></Person>");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Person><Name>n</Name></Person>", writeOnly))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(none);

            // With neither, which is what a serialization-only context gives for options it was not compiled for, nothing works.
            var neither = XmlMetadataServices.CreateObjectInfo(new XmlSerializerOptions { OmitXmlDeclaration = true }, new XmlObjectInfoValues<Person>());
            FluentActions.Invoking(() => XmlSerializer.Serialize(new Person { Name = "n" }, neither))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(none);
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Person><Name>n</Name></Person>", neither))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(none);
        }

        [TestMethod]
        public void CanUseSerializeHandler_IsTrueOnlyForTheOptionsTheFastPathWasCompiledFor()
        {
            static bool Can(Action<XmlSerializerOptions>? configure = null)
            {
                var options = new XmlSerializerOptions();
                configure?.Invoke(options);
                return XmlMetadataServices.CanUseSerializeHandler(options, null, null, XmlPropertyMapping.Element, false, typeof(Person), typeof(int));
            }

            Can().Should().BeTrue();
            Can(o => o.PropertyNamingPolicy = XmlNamingPolicy.CamelCase).Should().BeFalse();
            Can(o => o.ElementNamingPolicy = XmlNamingPolicy.CamelCase).Should().BeFalse();
            Can(o => o.AttributeNamingPolicy = XmlNamingPolicy.CamelCase).Should().BeFalse();
            Can(o => o.DefaultPropertyMapping = XmlPropertyMapping.Attribute).Should().BeFalse();
            Can(o => o.IncludeFields = true).Should().BeFalse();
            Can(o => o.IgnoreReadOnlyProperties = true).Should().BeFalse();
            Can(o => o.IgnoreReadOnlyFields = true).Should().BeFalse();
            Can(o => o.ReferenceHandler = XmlReferenceHandler.Preserve).Should().BeFalse();
            Can(o => o.Converters.Add(new HexInt32Converter())).Should().BeFalse("the fast path writes an int directly");
            Can(o => o.WriteIndented = true).Should().BeTrue("the writer indents whatever writes to it");

            var camel = new XmlSerializerOptions { PropertyNamingPolicy = XmlNamingPolicy.CamelCase };
            XmlMetadataServices.CanUseSerializeHandler(camel, XmlNamingPolicy.CamelCase, XmlNamingPolicy.CamelCase, XmlPropertyMapping.Element, false).Should().BeTrue();
            FluentActions.Invoking(() => XmlMetadataServices.CanUseSerializeHandler(null!, null, null, XmlPropertyMapping.Element, false))
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("options");
        }

        [TestMethod]
        public void CustomConverter_ForTheType_IsUsedInPlaceOfTheMetadata()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, TypeInfoResolver = new MetadataResolver() };
            options.Converters.Add(new PersonNameConverter());
            XmlSerializer.Serialize(new Person { Name = "n", Age = 3 }, options).Should().Be("<Person>n</Person>");
        }

        [TestMethod]
        public void TypeInfo_DescribesTheMembers()
        {
            var options = new XmlSerializerOptions { TypeInfoResolver = new MetadataResolver() };
            var info = options.GetTypeInfo(typeof(MetadataOrder));
            info.Should().BeAssignableTo<XmlTypeInfo<MetadataOrder>>();
            info.Properties.Should().HaveCount(7);
            info.Properties[0].Name.LocalName.Should().Be("Id");
            info.Properties[0].IsAttribute.Should().BeTrue();
            info.CreateObject!().Should().BeOfType<MetadataOrder>();
            options.GetTypeInfo(typeof(MetadataLine)).CreateObject.Should().BeNull("it is created through a constructor with parameters");

            FluentActions.Invoking(() => XmlMetadataServices.CreateObjectInfo<Person>(null!, new XmlObjectInfoValues<Person>())).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("options");
            FluentActions.Invoking(() => XmlMetadataServices.CreateObjectInfo<Person>(options, null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("objectInfo");
            FluentActions.Invoking(() => XmlMetadataServices.CreateCollectionPropertyInfo<List<int>, int>(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("propertyInfo");
            FluentActions.Invoking(() => XmlMetadataServices.CreateNullablePropertyInfo<int>(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("propertyInfo");
            FluentActions.Invoking(() => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<MetadataLine> { ObjectWithParameterizedConstructorCreator = static args => new MetadataLine((int)args[0]!, (string)args[1]!) }))
                .Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be(
                    "ObjectWithParameterizedConstructorCreator needs ConstructorParameterMetadataInitializer: the serializer has to know the parameters it fills. (Parameter 'objectInfo')");

            // A type with no members to describe is an empty element, and an abstract one is never created.
            var empty = XmlMetadataServices.CreateObjectInfo(new XmlSerializerOptions { OmitXmlDeclaration = true }, new XmlObjectInfoValues<Person> { ObjectCreator = static () => new Person(), PropertyMetadataInitializer = static _ => [] });
            empty.Properties.Should().BeEmpty();
            XmlSerializer.Serialize(new Person { Name = "n" }, empty).Should().Be("<Person/>");
            XmlMetadataServices.CreateObjectInfo(new XmlSerializerOptions(), new XmlObjectInfoValues<Person>()).CreateObject.Should().BeNull();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs the same work with types described by metadata and with types mapped through reflection, requires
        /// one result from both, and returns it. A failure is reported as its type and message.
        /// </summary>
        private static string Both(Func<XmlSerializerOptions, string> work, Action<XmlSerializerOptions>? configure = null)
        {
            string Run(IXmlTypeInfoResolver? resolver)
            {
                var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                if (resolver is not null)
                {
                    options.TypeInfoResolver = resolver;
                }

                configure?.Invoke(options);
                try
                {
                    return work(options);
                }
                catch (Exception ex)
                {
                    return ex.GetType().Name + ": " + ex.Message;
                }
            }

            var fromMetadata = Run(new MetadataResolver());
            Run(null).Should().Be(fromMetadata, "the reflection resolver and the metadata go through one engine");
            return fromMetadata;
        }

        private static string Show(MetadataOrder order)
        {
            return string.Join("|",
                order.Id,
                order.Name ?? "null",
                string.Join(";", System.Linq.Enumerable.Select(order.Counts, pair => pair.Key + "=" + pair.Value)),
                order.Lines.Count + " lines",
                string.Join(",", order.Numbers),
                order.Origin.X + "," + order.Origin.Y,
                order.Revision,
                order.Note ?? "null");
        }

        #endregion

    }

}
