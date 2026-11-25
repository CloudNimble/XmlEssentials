using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Checks inside the serializer that no document or type reaches: each guards a precondition the callers keep.
    /// </summary>
    [TestClass]
    public class XmlSerializerInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void IsResumable_IsTrueForObjectsWhoseMembersCanAllBeReadAPieceAtATime()
        {
            // A read from a stream keeps what it has read of such an object when a block ends inside it. Any
            // other object is read once all of it has arrived, and this is the rule that tells them apart.
            static bool Resumable<T>() => XmlSerializer.GetTypeInfo(typeof(T), new XmlSerializerOptions()).ResumableBinding is { } binding && binding.IsResumable();
            Resumable<ResumeOrder>().Should().BeTrue("its members are scalars, objects and collections of either");
            Resumable<Held<ResumeOrder>>().Should().BeTrue("it holds such an object in a member and in a collection");
            Resumable<ResumeTree>().Should().BeTrue("a type that holds itself is read with a frame for each level");
            Resumable<Paragraph>().Should().BeFalse("it has mixed content");
            Resumable<AnnotatedText>().Should().BeFalse("it has text of its own");
            Resumable<ExtraBag>().Should().BeFalse("it keeps the elements no member takes");
            Resumable<PopulateMarked>().Should().BeFalse("a member is populated");
            Resumable<Shelf>().Should().BeFalse("a member has derived types");
            Resumable<KindProduct>().Should().BeFalse("it has derived types of its own");
            Resumable<Held<Money>>().Should().BeFalse("it holds a value that a converter of the caller's reads");
            Resumable<MemberConverted>().Should().BeFalse("a member is read by a converter of the caller's");
            Resumable<Held<Paragraph>>().Should().BeFalse("it holds an object that is not");

            // The answer is kept with the binding, and is the same when it is asked for again.
            var kept = XmlSerializer.GetTypeInfo(typeof(ResumeOrder), new XmlSerializerOptions()).ResumableBinding!;
            kept.IsResumable().Should().BeTrue();
            kept.IsResumable().Should().BeTrue();

            // Nothing that is not created by the built-in converter from member metadata has a binding to ask.
            XmlSerializer.GetTypeInfo(typeof(KindShape), new XmlSerializerOptions()).ResumableBinding.Should().BeNull("an abstract type is never created");
            XmlSerializer.GetTypeInfo(typeof(int), new XmlSerializerOptions()).ResumableBinding.Should().BeNull("a scalar is not an object");
            var writeOnly = XmlMetadataServices.CreateObjectInfo(new XmlSerializerOptions(), new XmlObjectInfoValues<Person> { SerializeHandler = static (_, _, _) => { } });
            writeOnly.ResumableBinding.Should().BeNull("a type info with no member metadata cannot read");

            // The built-in object converter is the only caller that makes an object type info. One made with any
            // other converter must not be read by the members of its binding, whatever binding it was given.
            var binding = ((XmlObjectTypeInfo<Person>)XmlSerializer.GetTypeInfo(typeof(Person), new XmlSerializerOptions())).Binding;
            var foreign = new XmlObjectTypeInfo<Money>(new MoneyConverter(), new XmlSerializerOptions(), _ => binding);
            foreign.Binding.Should().BeSameAs(binding);
            foreign.ResumableBinding.Should().BeNull("its converter is not the built-in object converter");
        }

        [TestMethod]
        public void TryReadWithReadAhead_ReadsAnElementThatIsAllThere_AndLeavesOneThatIsNot()
        {
            // A streamed value is read at once when a count of its tags finds its end tag in the block. This is
            // what is asked when the count cannot tell: the element is read through to prove it whole, and then
            // read for its content. It must still read an element that is whole.
            var typeInfo = (XmlTypeInfo<ReadAheadItem>)XmlSerializer.GetTypeInfo(typeof(ReadAheadItem), new XmlSerializerOptions());
            var whole = new Utf8XmlReader("<ReadAheadItem A=\"x&amp;y\"><C B=\"1&amp;2\"/></ReadAheadItem>"u8, isFinalBlock: false, default);
            whole.Read().Should().BeTrue();
            typeInfo.TryReadWithReadAhead(ref whole, out var value).Should().BeTrue();
            value!.A.Should().Be("x&y");
            value.C!.B.Should().Be("1&2");
            whole.TokenType.Should().Be(XmlTokenType.EndElement);

            var cut = new Utf8XmlReader("<ReadAheadItem A=\"x&amp;y\"><C B=\"1&amp;2\"/>"u8, isFinalBlock: false, default);
            cut.Read().Should().BeTrue();
            typeInfo.TryReadWithReadAhead(ref cut, out var none).Should().BeFalse();
            none.Should().BeNull();
            cut.TokenType.Should().Be(XmlTokenType.StartElement);
            cut.MoveToNextAttribute().Should().BeTrue();
            cut.GetString().Should().Be("x&y");
        }

        [TestMethod]
        public void TryCreateModel_ForAMemberThatIsIgnored_CreatesNone()
        {
            // CreateModels sets ignored members aside before it asks for a model, because an ignored member still
            // hides the one it overrides. The rule for one member stands on its own all the same.
            var ignored = new XmlMemberFacts(typeof(CamelMarkedCtor).GetProperty(nameof(CamelMarkedCtor.CreatedBy))!);
            XmlObjectBinder.IsIgnored(ignored).Should().BeTrue();
            XmlObjectBinder.TryCreateModel(ignored, new XmlSerializerOptions(), 0, out var model).Should().BeFalse();
            model.Member.Should().BeNull();
        }

        [TestMethod]
        public void CreateProperty_ForFactsThatLackWhatBindingNeeds_SaysWhatIsMissing()
        {
            // The member is bound from its facts alone, with no reflection to fall back on. A generated context and
            // the reflection resolver both fill the facts, so only facts that were taken apart get here.
            var options = new XmlSerializerOptions { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };

            var unread = new XmlMemberFacts("Name", typeof(Person), typeof(string), false, true, false, (Func<object, string?>)(static o => ((Person)o).Name), (Action<object, string?>)(static (o, v) => ((Person)o).Name = v), null);
            XmlObjectBinder.TryCreateModel(unread, options, 0, out var unreadModel).Should().BeTrue();
            unread.Getter = null;
            FluentActions.Invoking(() => XmlObjectBinder.CreateProperty<object, string?>(unreadModel, options, 0, -1))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be($"The metadata of member 'Name' on type '{typeof(Person)}' has no getter.");

            var unset = new XmlMemberFacts("Name", typeof(Person), typeof(string), false, true, false, (Func<object, string?>)(static o => ((Person)o).Name), (Action<object, string?>)(static (o, v) => ((Person)o).Name = v), null);
            XmlObjectBinder.TryCreateModel(unset, options, 0, out var unsetModel).Should().BeTrue();
            unset.Setter = null;
            FluentActions.Invoking(() => XmlObjectBinder.CreateProperty<object, string?>(unsetModel, options, 0, -1))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be($"The metadata of member 'Name' on type '{typeof(Person)}' has no setter.");

            var items = new XmlMemberFacts("Items", typeof(Catalog), typeof(System.Collections.Generic.List<string>), false, true, false, (Func<object, System.Collections.Generic.List<string>?>)(static o => ((Catalog)o).Items), null, null)
            {
                ElementType = typeof(string),
            };
            XmlObjectBinder.TryCreateModel(items, options, 0, out var itemsModel).Should().BeTrue();
            FluentActions.Invoking(() => XmlObjectBinder.CreateProperty<object, System.Collections.Generic.List<string>?>(itemsModel, options, 0, -1))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be($"The metadata of member 'Items' on type '{typeof(Catalog)}' does not say how its collection is made.");

            FluentActions.Invoking(() => XmlObjectBinder.CreateMemberTypeInfo(new XmlConverterAttribute<NegatingIntConverter>(), typeof(int?), options, null))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The metadata of a member of type 'System.Nullable`1[System.Int32]' does not say how the converter of 'System.Int32' is wrapped.");
        }

        [TestMethod]
        public void CollectionShape_OfATypeNothingNamed_IsUnknownWhenNothingReflects()
        {
            // A program that has only generated contexts never looks at the interfaces of a type, which the trimmer
            // may have removed. What a context did not name is then no collection. The reflection resolver looks.
            XmlCollectionShape.Find(typeof(UnnamedBag), null).ElementType.Should().BeNull();
            XmlCollectionShape.Find(typeof(UnnamedBag), null).KeyType.Should().BeNull();

            XmlMetadataServices.RegisterCollection<NamedBag, int>();
            XmlCollectionShape.Find(typeof(NamedBag), null).ElementType.Should().Be(typeof(int));
            XmlMetadataServices.RegisterDictionary<NamedMap, string, int>();
            var map = XmlCollectionShape.Find(typeof(NamedMap), null);
            map.ElementType.Should().BeNull();
            map.KeyType.Should().Be(typeof(string));
            map.ValueType.Should().Be(typeof(int));

            _ = DefaultXmlTypeInfoResolver.Instance;
            XmlCollectionShape.IsCollection(typeof(UnnamedBag), out var element).Should().BeTrue();
            element.Should().Be(typeof(long));
        }

#if NET11_0_OR_GREATER
        [TestMethod]
        public void ClosedDerivedType_ThatIsGeneric_IsTheOneAContextNamed_OrIsLeftOutWhenNothingReflects()
        {
            // A generic type cannot be given its type arguments at run time under Native AOT. A context names the
            // constructed type, and without that or the reflection resolver the derived type is left out.
            var arguments = new[] { typeof(Guid) };
            XmlObjectBinder.ConstructClosedDerived(typeof(POCOs.Closed.ClosedBox<Guid>), typeof(POCOs.Closed.ClosedFull<>), arguments, null).Should().BeNull();
            XmlObjectBinder.ConstructClosedDerived(typeof(POCOs.Closed.ClosedBox<Guid>), typeof(POCOs.Closed.ClosedFull<>), arguments, XmlObjectBinder.CloseGeneric)
                .Should().Be(typeof(POCOs.Closed.ClosedFull<Guid>));

            XmlMetadataServices.RegisterClosedDerivedType<POCOs.Closed.ClosedBox<Guid>, POCOs.Closed.ClosedFull<Guid>>();
            XmlObjectBinder.ConstructClosedDerived(typeof(POCOs.Closed.ClosedBox<Guid>), typeof(POCOs.Closed.ClosedFull<>), arguments, null)
                .Should().Be(typeof(POCOs.Closed.ClosedFull<Guid>));
        }
#endif

        [TestMethod]
        public void ConstructorFacts_FromAConstructorWithAnUnnamedParameter_KeepTheNameNull()
        {
            // An array constructor is the one the runtime gives parameters with no names. A parameter with no
            // name fills no member, so nothing is asked about it.
            var constructor = typeof(int[]).GetConstructor([typeof(int)])!;
            constructor.GetParameters()[0].Name.Should().BeNull();
            var facts = XmlConstructorFacts.FromReflection(constructor, static name => throw new InvalidOperationException("Asked about '" + name + "'."));
            facts.Parameters.Should().ContainSingle();
            facts.Parameters[0].Name.Should().BeNull();
            facts.Parameters[0].ParameterType.Should().Be(typeof(int));
            facts.Parameters[0].IsIgnoredMember.Should().BeFalse();
            facts.Parameters[0].HasDefaultValue.Should().BeFalse();
            facts.Invoke([3, "ignored"]).Should().BeOfType<int[]>().Which.Should().HaveCount(3);
            XmlObjectBinder.MapConstructor(facts, []).Should().Equal(-1);
        }

        [TestMethod]
        public void Populate_OnAConverterThatCannotPopulate_IsRejected()
        {
            // The binder asks CanPopulate before a member is populated, so only a caller that skips the question gets here.
            var options = new XmlSerializerOptions();
            var converter = options.GetConverter(typeof(int));
            converter.CanPopulate.Should().BeFalse();
            options.GetConverter(typeof(PopulateChild)).CanPopulate.Should().BeTrue();

            var threw = false;
            var reader = new Utf8XmlReader("<int>1</int>"u8);
            reader.Read();
            try
            {
                converter.Populate(ref reader, 1, options);
            }
            catch (NotSupportedException ex)
            {
                threw = true;
                ex.Message.Should().Be("The converter '" + converter.GetType() + "' cannot populate an existing value.");
            }

            threw.Should().BeTrue();
        }

        #endregion

    }

}
