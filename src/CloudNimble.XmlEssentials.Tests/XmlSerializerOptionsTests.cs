using System;
using System.Collections;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializerOptions"/>: defaults, the read-only rule, copying, converter lookup, and how
    /// <see cref="XmlSerializerOptions.TypeInfoResolver"/>, its chain, and <see cref="XmlSerializerContext"/> relate.
    /// </summary>
    [TestClass]
    public class XmlSerializerOptionsTests
    {

        #region Fields

        private const string ReadOnlyMessage = "This XmlSerializerOptions instance is read-only.";

        #endregion

        #region Public Methods

        [TestMethod]
        public void NewOptions_HaveTheDocumentedDefaults()
        {
            var options = new XmlSerializerOptions();
            options.IsReadOnly.Should().BeFalse();
            options.AllowDuplicateProperties.Should().BeTrue();
            options.RespectRequiredConstructorParameters.Should().BeTrue();
            options.IndentCharacter.Should().Be(' ');
            options.NewLine.Should().Be(Environment.NewLine);
            options.NumberHandling.Should().Be(XmlNumberHandling.AllowNamedFloatingPointLiterals);
            options.ReadCommentHandling.Should().Be(XmlCommentHandling.Skip);
            options.DefaultPropertyMapping.Should().Be(XmlPropertyMapping.Element);
            options.EnumHandling.Should().Be(XmlEnumHandling.Name);
            options.OmitXmlDeclaration.Should().BeFalse();
            options.PropertyNamingPolicy.Should().BeNull();
            options.Converters.Should().BeEmpty();
            options.TypeInfoResolver.Should().BeNull();
            options.TypeInfoResolverChain.Should().BeEmpty();
        }

        [TestMethod]
        public void NamedDefaults_DifferOnlyWhereDocumented()
        {
            var web = new XmlSerializerOptions(XmlSerializerDefaults.Web);
            web.PropertyNamingPolicy.Should().BeSameAs(XmlNamingPolicy.CamelCase);
            web.OmitXmlDeclaration.Should().BeTrue();
            web.AllowDuplicateProperties.Should().BeTrue();
            web.IsReadOnly.Should().BeFalse();

            var strict = new XmlSerializerOptions(XmlSerializerDefaults.Strict);
            strict.AllowDuplicateProperties.Should().BeFalse();
            strict.RespectNullableAnnotations.Should().BeTrue("the Strict preset of System.Text.Json respects nullable annotations");
            strict.ReadCommentHandling.Should().Be(XmlCommentHandling.Disallow);
            strict.UnmappedMemberHandling.Should().Be(XmlUnmappedMemberHandling.Disallow);
            strict.OmitXmlDeclaration.Should().BeFalse();

            var general = new XmlSerializerOptions(XmlSerializerDefaults.General);
            general.PropertyNamingPolicy.Should().BeNull();
            general.RespectNullableAnnotations.Should().BeFalse();
            general.UnmappedMemberHandling.Should().Be(XmlUnmappedMemberHandling.Skip);

            XmlSerializerOptions.Default.IsReadOnly.Should().BeTrue();
            XmlSerializerOptions.Default.PropertyNamingPolicy.Should().BeNull();
            XmlSerializerOptions.Web.IsReadOnly.Should().BeTrue();
            XmlSerializerOptions.Web.PropertyNamingPolicy.Should().BeSameAs(XmlNamingPolicy.CamelCase);
            XmlSerializerOptions.Strict.IsReadOnly.Should().BeTrue();
            XmlSerializerOptions.Strict.ReadCommentHandling.Should().Be(XmlCommentHandling.Disallow);
        }

        [TestMethod]
        public void StrictDefaults_RejectComments_WhereGeneralDefaultsSkipThem()
        {
            XmlSerializer.Deserialize<int>("<int><!--c-->42</int>").Should().Be(42);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<int><!--c-->42</int>", XmlSerializerOptions.Strict))
                .Should().ThrowExactly<XmlException>();
        }

        [TestMethod]
        public void Options_BecomeReadOnly_OnFirstUseOrOnRequest()
        {
            var used = new XmlSerializerOptions { OmitXmlDeclaration = true };
            used.Converters.IsReadOnly.Should().BeFalse();
            XmlSerializer.Serialize(1, used).Should().Be("<int>1</int>");
            used.IsReadOnly.Should().BeTrue();
            used.Converters.IsReadOnly.Should().BeTrue();
            used.TypeInfoResolverChain.IsReadOnly.Should().BeTrue();

            var frozen = new XmlSerializerOptions();
            frozen.MakeReadOnly();
            frozen.IsReadOnly.Should().BeTrue();

            var queried = new XmlSerializerOptions { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };
            queried.TryGetTypeInfo(typeof(int), out _).Should().BeTrue();
            queried.IsReadOnly.Should().BeTrue();

            var converterOnly = new XmlSerializerOptions();
            converterOnly.GetConverter(typeof(int));
            converterOnly.IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void ReadOnlyOptions_RejectEveryChange()
        {
            var options = XmlSerializerOptions.Default;
            var changes = new Action[]
            {
                () => options.AllowDuplicateProperties = false,
                () => options.AttributeNamingPolicy = XmlNamingPolicy.CamelCase,
                () => options.DefaultBufferSize = 1,
                () => options.DefaultIgnoreCondition = XmlIgnoreCondition.Always,
                () => options.DefaultNamespace = "urn:x",
                () => options.DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                () => options.DictionaryKeyPolicy = XmlNamingPolicy.CamelCase,
                () => options.ElementNamingPolicy = XmlNamingPolicy.CamelCase,
                () => options.EmitByteOrderMark = true,
                () => options.EnumHandling = XmlEnumHandling.Integer,
                () => options.IgnoreReadOnlyFields = true,
                () => options.IgnoreReadOnlyProperties = true,
                () => options.IncludeFields = true,
                () => options.IndentCharacter = '\t',
                () => options.IndentSize = 4,
                () => options.InferClosedTypePolymorphism = true,
                () => options.MaxAttributeCount = 1,
                () => options.MaxCommentByteCount = 1,
                () => options.MaxDepth = 1,
                () => options.MaxDocumentByteCount = 1,
                () => options.MaxNameByteCount = 1,
                () => options.MaxTextByteCount = 1,
                () => options.NewLine = "\n",
                () => options.NumberHandling = XmlNumberHandling.Strict,
                () => options.OmitXmlDeclaration = true,
                () => options.PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate,
                () => options.PropertyNameCaseInsensitive = true,
                () => options.PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
                () => options.ReadCommentHandling = XmlCommentHandling.Allow,
                () => options.ReferenceHandler = XmlReferenceHandler.Preserve,
                () => options.RespectNullableAnnotations = true,
                () => options.RespectRequiredConstructorParameters = false,
                () => options.TypeInfoResolver = new PassThroughResolver(),
                () => options.UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode,
                () => options.UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow,
                () => options.WhitespaceHandling = XmlWhitespaceHandling.Significant,
                () => options.WriteIndented = true,
                () => options.XmlVersion = XmlVersion.Version11,
                () => options.Converters.Add(new SentinelIntConverter()),
                () => options.Converters.Clear(),
                () => options.Converters.Insert(0, new SentinelIntConverter()),
                () => options.Converters.RemoveAt(0),
                () => options.TypeInfoResolverChain.Add(new PassThroughResolver()),
                () => options.TypeInfoResolverChain.Clear(),
            };
            foreach (var change in changes)
            {
                change.Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(ReadOnlyMessage);
            }

            options.Converters.Remove(new SentinelIntConverter()).Should().BeFalse();
        }

        [TestMethod]
        public void NewLine_CannotBeNull()
        {
            var options = new XmlSerializerOptions();
            FluentActions.Invoking(() => options.NewLine = null!).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void CopyConstructor_CopiesEverySetting_AndTheCopyIsIndependentAndWritable()
        {
            var converter = new SentinelIntConverter();
            var resolver = new PassThroughResolver();
            var source = new XmlSerializerOptions
            {
                AllowDuplicateProperties = false,
                AttributeNamingPolicy = XmlNamingPolicy.KebabCaseLower,
                DefaultBufferSize = 512,
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                DefaultNamespace = "urn:x",
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                DictionaryKeyPolicy = XmlNamingPolicy.SnakeCaseLower,
                ElementNamingPolicy = XmlNamingPolicy.PascalCase,
                EmitByteOrderMark = true,
                EnumHandling = XmlEnumHandling.Integer,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                IndentCharacter = '\t',
                IndentSize = 3,
                InferClosedTypePolymorphism = true,
                MaxAttributeCount = 11,
                MaxCommentByteCount = 12,
                MaxDepth = 13,
                MaxDocumentByteCount = 14,
                MaxNameByteCount = 15,
                MaxTextByteCount = 16,
                NewLine = "\n",
                NumberHandling = XmlNumberHandling.Strict,
                OmitXmlDeclaration = true,
                PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
                ReadCommentHandling = XmlCommentHandling.Allow,
                ReferenceHandler = XmlReferenceHandler.IgnoreCycles,
                RespectNullableAnnotations = true,
                RespectRequiredConstructorParameters = false,
                TypeInfoResolver = resolver,
                UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode,
                UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
                WriteIndented = true,
                XmlVersion = XmlVersion.Version11,
            };
            source.Converters.Add(converter);
            source.MakeReadOnly();

            var copy = new XmlSerializerOptions(source);
            copy.IsReadOnly.Should().BeFalse();
            copy.AllowDuplicateProperties.Should().BeFalse();
            copy.AttributeNamingPolicy.Should().BeSameAs(XmlNamingPolicy.KebabCaseLower);
            copy.DefaultBufferSize.Should().Be(512);
            copy.DefaultIgnoreCondition.Should().Be(XmlIgnoreCondition.WhenWritingNull);
            copy.DefaultNamespace.Should().Be("urn:x");
            copy.DefaultPropertyMapping.Should().Be(XmlPropertyMapping.Attribute);
            copy.DictionaryKeyPolicy.Should().BeSameAs(XmlNamingPolicy.SnakeCaseLower);
            copy.ElementNamingPolicy.Should().BeSameAs(XmlNamingPolicy.PascalCase);
            copy.EmitByteOrderMark.Should().BeTrue();
            copy.EnumHandling.Should().Be(XmlEnumHandling.Integer);
            copy.IgnoreReadOnlyFields.Should().BeTrue();
            copy.IgnoreReadOnlyProperties.Should().BeTrue();
            copy.IncludeFields.Should().BeTrue();
            copy.IndentCharacter.Should().Be('\t');
            copy.IndentSize.Should().Be(3);
            copy.InferClosedTypePolymorphism.Should().BeTrue();
            copy.MaxAttributeCount.Should().Be(11);
            copy.MaxCommentByteCount.Should().Be(12);
            copy.MaxDepth.Should().Be(13);
            copy.MaxDocumentByteCount.Should().Be(14);
            copy.MaxNameByteCount.Should().Be(15);
            copy.MaxTextByteCount.Should().Be(16);
            copy.NewLine.Should().Be("\n");
            copy.NumberHandling.Should().Be(XmlNumberHandling.Strict);
            copy.OmitXmlDeclaration.Should().BeTrue();
            copy.PreferredObjectCreationHandling.Should().Be(XmlObjectCreationHandling.Populate);
            copy.PropertyNameCaseInsensitive.Should().BeTrue();
            copy.PropertyNamingPolicy.Should().BeSameAs(XmlNamingPolicy.CamelCase);
            copy.ReadCommentHandling.Should().Be(XmlCommentHandling.Allow);
            copy.ReferenceHandler.Should().BeSameAs(XmlReferenceHandler.IgnoreCycles);
            copy.RespectNullableAnnotations.Should().BeTrue();
            copy.RespectRequiredConstructorParameters.Should().BeFalse();
            copy.TypeInfoResolver.Should().BeSameAs(resolver);
            copy.UnknownTypeHandling.Should().Be(XmlUnknownTypeHandling.XmlNode);
            copy.UnmappedMemberHandling.Should().Be(XmlUnmappedMemberHandling.Disallow);
            copy.WhitespaceHandling.Should().Be(XmlWhitespaceHandling.Significant);
            copy.WriteIndented.Should().BeTrue();
            copy.XmlVersion.Should().Be(XmlVersion.Version11);
            copy.Converters.Should().Equal(converter);

            copy.Converters.Clear();
            source.Converters.Should().Equal(converter);
            FluentActions.Invoking(() => new XmlSerializerOptions((XmlSerializerOptions)null!))
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("options");
        }

        [TestMethod]
        public void WriterSettings_ShapeTheDocument()
        {
            var options = new XmlSerializerOptions
            {
                EmitByteOrderMark = true,
                IndentCharacter = '\t',
                IndentSize = 1,
                NewLine = "\n",
                WriteIndented = true,
                XmlVersion = XmlVersion.Version11,
            };
            XmlSerializer.SerializeToUtf8Bytes(7, options).Should().Equal(
                [0xEF, 0xBB, 0xBF, .. "<?xml version=\"1.1\" encoding=\"utf-8\"?>\n<int>7</int>"u8]);
        }

        [TestMethod]
        public void NamingPolicy_NamesTheRoot_ElementPolicyFirst()
        {
            XmlSerializer.Serialize(1, new XmlSerializerOptions { OmitXmlDeclaration = true, PropertyNamingPolicy = XmlNamingPolicy.SnakeCaseUpper })
                .Should().Be("<INT>1</INT>");
            XmlSerializer.Serialize(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), new XmlSerializerOptions
            {
                ElementNamingPolicy = XmlNamingPolicy.KebabCaseLower,
                OmitXmlDeclaration = true,
                PropertyNamingPolicy = XmlNamingPolicy.SnakeCaseUpper,
            }).Should().Be("<date-time-offset>2020-01-02T03:04:05+00:00</date-time-offset>");
        }

        [TestMethod]
        public void ConverterLookup_ListThenAttributeThenBuiltIn()
        {
            var bare = new XmlSerializerOptions { OmitXmlDeclaration = true };
            XmlSerializer.Serialize(new DoublingNumber { Value = 21 }, bare).Should().Be("<DoublingNumber>42</DoublingNumber>");
            XmlSerializer.Deserialize<DoublingNumber>("<DoublingNumber>42</DoublingNumber>", bare).Value.Should().Be(21);
            XmlSerializer.Serialize((DoublingNumber?)new DoublingNumber { Value = 4 }, bare).Should().Be("<DoublingNumber>8</DoublingNumber>");
            XmlSerializer.Deserialize<DoublingNumber?>(
                "<DoublingNumber xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>", bare).Should().BeNull();

            var negating = new XmlSerializerOptions { OmitXmlDeclaration = true };
            negating.Converters.Add(new NegatingIntConverter());
            XmlSerializer.Serialize(5, negating).Should().Be("<int>-5</int>");
            XmlSerializer.Serialize((int?)5, negating).Should().Be("<int>-5</int>");
            XmlSerializer.Deserialize<int?>("<int>-5</int>", negating).Should().Be(5);
            XmlSerializer.Serialize((int?)5, bare).Should().Be("<int>5</int>");

            var listed = new XmlSerializerOptions();
            var sentinel = new SentinelIntConverter();
            listed.Converters.Add(sentinel);
            listed.GetConverter(typeof(int)).Should().BeSameAs(sentinel);
            listed.GetConverter(typeof(long)).Should().BeSameAs(XmlSerializerOptions.Default.GetConverter(typeof(long)));
            listed.GetTypeInfo<int>().Converter.Should().BeSameAs(sentinel);
        }

        [TestMethod]
        public void ConvertersList_ChangesTakeEffect_UntilTheOptionsAreUsed()
        {
            var options = new XmlSerializerOptions();
            var builtIn = options.GetConverter(typeof(int));
            options.GetConverter(typeof(int)).Should().BeSameAs(builtIn);
            var first = new SentinelIntConverter();
            var second = new SentinelIntConverter();

            options.Converters.Add(first);
            options.GetConverter(typeof(int)).Should().BeSameAs(first);
            options.Converters.Insert(0, second);
            options.GetConverter(typeof(int)).Should().BeSameAs(second);
            options.Converters.Should().Equal(second, first);
            options.Converters.Contains(first).Should().BeTrue();
            options.Converters.IndexOf(first).Should().Be(1);
            options.Converters[0] = first;
            options.Converters[1] = second;
            options.Converters[0].Should().BeSameAs(first);
            options.GetConverter(typeof(int)).Should().BeSameAs(first);
            options.Converters.Remove(first).Should().BeTrue();
            options.GetConverter(typeof(int)).Should().BeSameAs(second);
            options.Converters.RemoveAt(0);
            options.GetConverter(typeof(int)).Should().BeSameAs(builtIn);
            options.Converters.Add(first);
            options.Converters.Clear();
            options.Converters.Count.Should().Be(0);
            options.GetConverter(typeof(int)).Should().BeSameAs(builtIn);

            options.Converters.Add(first);
            var copied = new XmlConverter[2];
            options.Converters.CopyTo(copied, 1);
            copied.Should().Equal(null!, first);
            var enumerated = new List<object>();
            foreach (var item in (IEnumerable)options.Converters)
            {
                enumerated.Add(item);
            }

            enumerated.Should().Equal(first);
        }

        [TestMethod]
        public void Lists_RejectNullItems()
        {
            var options = new XmlSerializerOptions();
            options.Converters.Add(new SentinelIntConverter());
            FluentActions.Invoking(() => options.Converters.Add(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("item");
            FluentActions.Invoking(() => options.Converters.Insert(0, null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("item");
            FluentActions.Invoking(() => options.Converters[0] = null!).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => options.TypeInfoResolverChain.Add(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("item");
        }

        [TestMethod]
        public void UnsupportedType_IsNotSupportedException_AndTryGetTypeInfoIsFalse()
        {
            var options = new XmlSerializerOptions();
            FluentActions.Invoking(() => options.GetConverter(typeof(Action)))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type 'System.Action' is not supported.");
            FluentActions.Invoking(() => options.GetTypeInfo(typeof(Action)))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type 'System.Action' is not supported.");
            FluentActions.Invoking(() => XmlSerializer.Serialize<Action>(static () => { }))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type 'System.Action' is not supported.");
            options.TryGetTypeInfo(typeof(Action), out var missing).Should().BeFalse();
            missing.Should().BeNull();
            options.TryGetTypeInfo<Action>(out var missingTyped).Should().BeFalse();
            missingTyped.Should().BeNull();
            options.TryGetTypeInfo(typeof(IntPtr), out _).Should().BeFalse();
            options.TryGetTypeInfo(typeof(IntPtr?), out _).Should().BeFalse();

            options.TryGetTypeInfo(typeof(int), out var found).Should().BeTrue();
            found.Should().BeSameAs(options.GetTypeInfo(typeof(int)));
            options.TryGetTypeInfo<int>(out var foundTyped).Should().BeTrue();
            foundTyped.Should().BeSameAs(found);
            options.GetTypeInfo<int>().Should().BeSameAs(found);
            options.GetTypeInfo<long>().Type.Should().Be(typeof(long));
            options.GetTypeInfo<int>().Should().BeSameAs(found);

            FluentActions.Invoking(() => options.GetConverter(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("typeToConvert");
            FluentActions.Invoking(() => options.GetTypeInfo(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("type");
            FluentActions.Invoking(() => options.TryGetTypeInfo(null!, out _)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("type");
        }

        [TestMethod]
        public void TypeInfo_DescribesTheRootElement()
        {
            var options = new XmlSerializerOptions { DefaultNamespace = "urn:x", PropertyNamingPolicy = XmlNamingPolicy.CamelCase, TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };
            var number = options.GetTypeInfo(typeof(int));
            number.Type.Should().Be(typeof(int));
            number.Options.Should().BeSameAs(options);
            number.RootName.Should().Be(new XmlName("int", "urn:x"));
            number.IsNillable.Should().BeFalse();
            number.Converter.Should().BeSameAs(options.GetConverter(typeof(int)));
            options.GetTypeInfo<int?>().RootName.Should().Be(new XmlName("int", "urn:x"));
            options.GetTypeInfo<int?>().IsNillable.Should().BeTrue();
            options.GetTypeInfo<string>().IsNillable.Should().BeTrue();
            options.GetTypeInfo<byte[]>().RootName.Should().Be(new XmlName("base64Binary", "urn:x"));
        }

        [TestMethod]
        public void MisconfiguredConverters_AreReported_NotTreatedAsUnsupportedTypes()
        {
            var nullFactory = new XmlSerializerOptions();
            nullFactory.Converters.Add(new NullConverterFactory());
            FluentActions.Invoking(() => nullFactory.GetConverter(typeof(uint))).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be($"Converter factory '{typeof(NullConverterFactory).FullName}' returned null for 'System.UInt32'.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(1u, nullFactory)).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be($"Converter factory '{typeof(NullConverterFactory).FullName}' returned null for 'System.UInt32'.");

            var wrongType = new XmlSerializerOptions { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };
            wrongType.Converters.Add(new UIntClaimingConverter());
            FluentActions.Invoking(() => wrongType.GetTypeInfo(typeof(uint))).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be($"The converter '{typeof(UIntClaimingConverter).FullName}' is not compatible with the type 'System.UInt32'.");

            var nested = new XmlSerializerOptions { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };
            nested.Converters.Add(new FactoryReturningFactory());
            FluentActions.Invoking(() => nested.GetTypeInfo(typeof(uint))).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be($"The converter '{typeof(NullConverterFactory).FullName}' is not compatible with the type 'System.UInt32'.");
        }

        [TestMethod]
        public void ReflectionResolver_IsGivenOnlyByTheMembersThatSayTheyNeedReflection()
        {
            // Reading the resolver, and asking for type info, never bring the reflection resolver in: they carry
            // no trimming or Native AOT warning, so they may not need what such a warning is for.
            var bare = new XmlSerializerOptions();
            bare.TypeInfoResolver.Should().BeNull();
            FluentActions.Invoking(() => bare.GetTypeInfo(typeof(int))).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                "No TypeInfoResolver is set. Use an XmlSerializer method that takes XmlSerializerOptions, call MakeReadOnly(populateMissingResolver: true), set TypeInfoResolver, or use an overload that takes an XmlTypeInfo or XmlSerializerContext.");
            bare.TypeInfoResolver.Should().BeNull();

            var requested = new XmlSerializerOptions();
            requested.MakeReadOnly(populateMissingResolver: true);
            requested.IsReadOnly.Should().BeTrue();
            requested.TypeInfoResolver.Should().BeSameAs(DefaultXmlTypeInfoResolver.Instance);
            requested.TypeInfoResolverChain.Should().Equal(DefaultXmlTypeInfoResolver.Instance);
            requested.GetTypeInfo(typeof(int)).Type.Should().Be(typeof(int));

            var viewed = new XmlSerializerOptions();
            var view = viewed.TypeInfoResolverChain;
            view.Should().BeEmpty();
            viewed.MakeReadOnly(populateMissingResolver: true);
            view.Should().Equal(DefaultXmlTypeInfoResolver.Instance);

            var notRequested = new XmlSerializerOptions();
            notRequested.MakeReadOnly(populateMissingResolver: false);
            notRequested.IsReadOnly.Should().BeTrue();
            notRequested.TypeInfoResolver.Should().BeNull();

            var onlyInt = new FixedTypeResolver(typeof(int), typeof(int));
            var kept = new XmlSerializerOptions { TypeInfoResolver = onlyInt };
            kept.MakeReadOnly(populateMissingResolver: true);
            kept.TypeInfoResolver.Should().BeSameAs(onlyInt);

            var serialized = new XmlSerializerOptions { OmitXmlDeclaration = true };
            XmlSerializer.Serialize(1, serialized).Should().Be("<int>1</int>");
            serialized.TypeInfoResolver.Should().BeSameAs(DefaultXmlTypeInfoResolver.Instance);

            // A resolver that options were only given, because they had none, is not copied: a copy of a shared
            // instance has no resolver, so the context it is handed to becomes its resolver. A resolver the caller
            // set is copied, and so is one set after the options were given the default.
            XmlSerializerOptions.Web.TypeInfoResolver.Should().BeSameAs(DefaultXmlTypeInfoResolver.Instance);
            new XmlSerializerOptions(XmlSerializerOptions.Web).TypeInfoResolver.Should().BeNull();
            new XmlSerializerOptions(serialized).TypeInfoResolver.Should().BeNull();
            new XmlSerializerOptions(kept).TypeInfoResolver.Should().BeSameAs(onlyInt);
            var given = new XmlSerializerOptions();
            given.UseReflectionResolverIfMissing();
            given.TypeInfoResolver = onlyInt;
            new XmlSerializerOptions(given).TypeInfoResolver.Should().BeSameAs(onlyInt);
            var extended = new XmlSerializerOptions();
            extended.UseReflectionResolverIfMissing();
            extended.TypeInfoResolverChain.Add(onlyInt);
            new XmlSerializerOptions(extended).TypeInfoResolverChain.Should().Equal(DefaultXmlTypeInfoResolver.Instance, onlyInt);

            var asked = new XmlSerializerOptions();
            asked.GetConverter(typeof(int)).Should().BeSameAs(XmlSerializerOptions.Default.GetConverter(typeof(int)));
            asked.TypeInfoResolver.Should().BeSameAs(DefaultXmlTypeInfoResolver.Instance);
        }

        [TestMethod]
        public void ResolverChain_IsAViewOfTheResolver()
        {
            var options = new XmlSerializerOptions();
            var chain = options.TypeInfoResolverChain;
            chain.Should().BeEmpty();

            var onlyInt = new FixedTypeResolver(typeof(int), typeof(int));
            options.TypeInfoResolver = onlyInt;
            options.TypeInfoResolverChain.Should().BeSameAs(chain);
            chain.Should().Equal(onlyInt);

            var onlyLong = new FixedTypeResolver(typeof(long), typeof(long));
            chain.Add(onlyLong);
            options.TypeInfoResolver.Should().BeSameAs(chain);
            chain.Should().Equal(onlyInt, onlyLong);
            options.TypeInfoResolver = options.TypeInfoResolver;
            chain.Should().Equal(onlyInt, onlyLong);

            var copy = new XmlSerializerOptions(options);
            copy.TypeInfoResolverChain.Should().Equal(onlyInt, onlyLong);
            copy.TypeInfoResolverChain.Should().NotBeSameAs(chain);
            copy.TypeInfoResolver.Should().BeSameAs(copy.TypeInfoResolverChain);
            copy.TypeInfoResolverChain.Clear();
            chain.Should().Equal(onlyInt, onlyLong);

            options.TryGetTypeInfo(typeof(int), out _).Should().BeTrue();
            options.TryGetTypeInfo(typeof(long), out _).Should().BeTrue();
            options.TryGetTypeInfo(typeof(short), out _).Should().BeFalse();
            copy.TryGetTypeInfo(typeof(int), out _).Should().BeFalse();

            var replaced = new XmlSerializerOptions();
            replaced.TypeInfoResolverChain.Add(onlyInt);
            replaced.TypeInfoResolver = options.TypeInfoResolver;
            replaced.TypeInfoResolverChain.Should().Equal(onlyInt, onlyLong);
            replaced.TypeInfoResolver = null;
            replaced.TypeInfoResolverChain.Should().BeEmpty();
            replaced.TypeInfoResolver.Should().BeNull();
        }

        [TestMethod]
        public void Resolver_ThatAnswersWithAnotherType_IsReported()
        {
            var options = new XmlSerializerOptions { TypeInfoResolver = new FixedTypeResolver(typeof(int), typeof(long)) };
            FluentActions.Invoking(() => options.GetTypeInfo(typeof(int))).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be($"The resolver '{typeof(FixedTypeResolver).FullName}' returned type info for 'System.Int64' when asked for 'System.Int32'.");
        }

        [TestMethod]
        public void DefaultResolver_RejectsNullArguments()
        {
            FluentActions.Invoking(() => DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(null!, new XmlSerializerOptions()))
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("type");
            FluentActions.Invoking(() => DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(typeof(int), null!))
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("options");
        }

        [TestMethod]
        public void Context_AskedByOtherOptions_AnswersBuiltInTypesForThoseOptions()
        {
            // A context that is one resolver of several is asked by options that are not its own. A type whose
            // converter exists already is answered for the options that ask. Any other type is whatever the
            // context's own GetTypeInfo returns, which for this hand-written context is bound to its own options.
            var context = new PrimitiveXmlContext();
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.TypeInfoResolverChain.Add(context);
            XmlSerializer.Serialize(5, options).Should().Be("<int>5</int>");
            options.GetTypeInfo<int>().Options.Should().BeSameAs(options);
            options.GetTypeInfo<POCOs.Person>().Options.Should().BeSameAs(context.Options);
            context.Options.GetTypeInfo<int>().Options.Should().BeSameAs(context.Options);
        }

        [TestMethod]
        public void Context_BindsItsOptions_AndIsTheirResolver()
        {
            var created = new PrimitiveXmlContext();
            created.Options.TypeInfoResolver.Should().BeSameAs(created);
            created.Options.TypeInfoResolverChain.Should().Equal(created);

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var context = new PrimitiveXmlContext(options);
            context.Options.Should().BeSameAs(options);
            options.TypeInfoResolver.Should().BeSameAs(context);
            XmlSerializer.Serialize(5, context).Should().Be("<int>5</int>");
            XmlSerializer.Serialize(5, options).Should().Be("<int>5</int>");

            FluentActions.Invoking(() => new PrimitiveXmlContext(XmlSerializerOptions.Default))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(ReadOnlyMessage);
            FluentActions.Invoking(() => XmlSerializer.Serialize<Action>(static () => { }, context)).Should().ThrowExactly<NotSupportedException>().Which.Message
                .Should().Be($"The type 'System.Action' is not supported by '{typeof(PrimitiveXmlContext).FullName}'.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Action/>", typeof(Action), context)).Should().ThrowExactly<NotSupportedException>().Which.Message
                .Should().Be($"The type 'System.Action' is not supported by '{typeof(PrimitiveXmlContext).FullName}'.");
        }

        [TestMethod]
        public void ReferenceHandlers_CreateTheirResolvers()
        {
            var preserve = XmlReferenceHandler.Preserve.CreateResolver();
            var first = new object();
            preserve.GetReference(first, out var seen).Should().Be("1");
            seen.Should().BeFalse();
            preserve.GetReference(new object(), out _).Should().Be("2");
            preserve.GetReference(first, out seen).Should().Be("1");
            seen.Should().BeTrue();
            preserve.AddReference("9", first);
            preserve.ResolveReference("9").Should().BeSameAs(first);
            preserve.ResolveReference("missing").Should().BeNull();
            preserve.GetReference(first, out _).Should().Be("9");

            var ignore = XmlReferenceHandler.IgnoreCycles.CreateResolver();
            ignore.GetReference(first, out seen).Should().Be("");
            seen.Should().BeFalse();
            ignore.GetReference(first, out seen).Should().Be("");
            seen.Should().BeTrue();
            var added = new object();
            ignore.AddReference("a", added);
            ignore.ResolveReference("a").Should().BeSameAs(added);
            ignore.ResolveReference("missing").Should().BeNull();
            ignore.GetReference(added, out seen).Should().Be("");
            seen.Should().BeTrue();
        }

        #endregion

    }

}
