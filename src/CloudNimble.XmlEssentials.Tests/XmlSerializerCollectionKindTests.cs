using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Every kind of collection and dictionary that can be written can be read back: as the document element
    /// and as a member, with its items in the order they were written.
    /// </summary>
    [TestClass]
    public class XmlSerializerCollectionKindTests
    {

        #region Fields

        private const string Items = "<Int32>1</Int32><Int32>2</Int32><Int32>3</Int32>";
        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private static readonly int[] OneTwoThree = [1, 2, 3];

        #endregion

        #region Public Methods

        [TestMethod]
        public void Collections_AsMembers_RoundTrip()
        {
            var value = new CollectionKinds
            {
                Queue = new Queue<int>(OneTwoThree),
                Stack = new Stack<int>(OneTwoThree),
                Linked = new LinkedList<int>(OneTwoThree),
                Set = new HashSet<int>(OneTwoThree),
                ReadOnly = new ReadOnlyCollection<int>(OneTwoThree),
                Array = [.. OneTwoThree],
                List = [.. OneTwoThree],
                Map = ImmutableDictionary<string, int>.Empty.Add("a", 1),
                View = new Dictionary<string, int> { ["b"] = 2 },
            };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(
                "<CollectionKinds><Queue>1</Queue><Queue>2</Queue><Queue>3</Queue><Stack>3</Stack><Stack>2</Stack><Stack>1</Stack>" +
                "<Linked>1</Linked><Linked>2</Linked><Linked>3</Linked><Set>1</Set><Set>2</Set><Set>3</Set>" +
                "<ReadOnly>1</ReadOnly><ReadOnly>2</ReadOnly><ReadOnly>3</ReadOnly><Array>1</Array><Array>2</Array><Array>3</Array>" +
                "<List>1</List><List>2</List><List>3</List><Map><a>1</a></Map><View><b>2</b></View></CollectionKinds>");
            var read = XmlSerializer.Deserialize<CollectionKinds>(xml)!;
            read.Queue.Should().Equal(1, 2, 3);
            read.Stack.Should().Equal(3, 2, 1);
            read.Stack!.Peek().Should().Be(3);
            read.Linked.Should().Equal(1, 2, 3);
            read.Set.Should().BeOfType<HashSet<int>>().Which.Should().Equal(1, 2, 3);
            read.ReadOnly.Should().Equal(1, 2, 3);
            read.Array.Should().Equal(1, 2, 3);
            read.List.Should().Equal(1, 2, 3);
            read.Map.Should().Equal(new Dictionary<string, int> { ["a"] = 1 });
            read.View.Should().BeOfType<Dictionary<string, int>>().Which.Should().Equal(new Dictionary<string, int> { ["b"] = 2 });
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void Collection_ThatCannotBeCreated_SaysSoWhenItIsRead()
        {
            var xml = XmlSerializer.Serialize(new FixedSizeBag([1, 2]), Bare);
            xml.Should().Be("<FixedSizeBag><Int32>1</Int32><Int32>2</Int32></FixedSizeBag>");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<FixedSizeBag>(xml))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The collection type 'CloudNimble.XmlEssentials.Tests.POCOs.FixedSizeBag' cannot be created. It needs a public parameterless constructor and ICollection<T> or a public Add method, or a public constructor that takes IEnumerable<T> or IList<T>.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<AbstractBag>("<AbstractBag><Int32>1</Int32></AbstractBag>"))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The collection type 'CloudNimble.XmlEssentials.Tests.POCOs.AbstractBag' cannot be created. It needs a public parameterless constructor and ICollection<T> or a public Add method, or a public constructor that takes IEnumerable<T> or IList<T>.");
        }

        [TestMethod]
        public void Dictionary_ThatCannotBeCreated_SaysSoWhenItIsRead()
        {
            var xml = XmlSerializer.Serialize(new FixedMap("a", 1), Bare);
            xml.Should().Be("<FixedMap><a>1</a></FixedMap>");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<FixedMap>(xml))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The dictionary type 'CloudNimble.XmlEssentials.Tests.POCOs.FixedMap' cannot be created. It needs a public parameterless constructor and IDictionary<TKey, TValue>, or a public constructor that takes IDictionary<TKey, TValue>.");
        }

        [TestMethod]
        public void Dictionary_NullValue_IsWrittenAsNil_UnderEveryIgnoreCondition()
        {
            var map = new Dictionary<string, string?> { ["a"] = null, ["b"] = "", ["c"] = "x" };
            var nil = XmlSerializer.Serialize(map, Bare);
            nil.Should().Be("<DictionaryOfStringString><a " + Nil + "/><b></b><c>x</c></DictionaryOfStringString>");
            var read = XmlSerializer.Deserialize<Dictionary<string, string?>>(nil)!;
            read.Should().HaveCount(3);
            read["a"].Should().BeNull();
            read["b"].Should().Be("");
            read["c"].Should().Be("x");

            // The ignore condition is about members. An entry is not one, so its null value is still written.
            var whenNull = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            XmlSerializer.Serialize(map, whenNull).Should().Be(nil);
            var whenDefault = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault };
            XmlSerializer.Serialize(map, whenDefault).Should().Be(nil);
        }

        [TestMethod]
        public void Dictionary_NullValueOfANullableNumber_RoundTrips()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<string, int?> { ["a"] = null, ["b"] = 2 }, Bare);
            xml.Should().Be("<DictionaryOfStringInt32><a " + Nil + "/><b>2</b></DictionaryOfStringInt32>");
            var read = XmlSerializer.Deserialize<Dictionary<string, int?>>(xml)!;
            read["a"].Should().BeNull();
            read["b"].Should().Be(2);
        }

        [TestMethod]
        public void Dictionary_NullValueUnderAKeyThatIsNotAName_IsWrittenAsANilValueElement()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<string, string?> { ["not a name"] = null }, Bare);
            xml.Should().Be("<DictionaryOfStringString><Key>not a name</Key><Value " + Nil + "/></DictionaryOfStringString>");
            var read = XmlSerializer.Deserialize<Dictionary<string, string?>>(xml)!;
            read.Should().HaveCount(1);
            read["not a name"].Should().BeNull();
        }

        [TestMethod]
        public void DictionaryKinds_AsRoots_RoundTrip()
        {
            var source = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

            RoundTrip<IDictionary<string, int>>(source, "IDictionaryOfStringInt32", typeof(Dictionary<string, int>));
            RoundTrip<IReadOnlyDictionary<string, int>>(source, "IReadOnlyDictionaryOfStringInt32", typeof(Dictionary<string, int>));
            RoundTrip(new SortedDictionary<string, int>(source), "SortedDictionaryOfStringInt32", typeof(SortedDictionary<string, int>));
            RoundTrip(new SortedList<string, int>(source), "SortedListOfStringInt32", typeof(SortedList<string, int>));
            RoundTrip(new ReadOnlyDictionary<string, int>(source), "ReadOnlyDictionaryOfStringInt32", typeof(ReadOnlyDictionary<string, int>));
            RoundTrip(ImmutableSortedDictionary.CreateRange(source), "ImmutableSortedDictionaryOfStringInt32", typeof(ImmutableSortedDictionary<string, int>));
            RoundTrip<IImmutableDictionary<string, int>>(ImmutableSortedDictionary.CreateRange(source), "IImmutableDictionaryOfStringInt32", typeof(ImmutableDictionary<string, int>));

            var immutable = XmlSerializer.Deserialize<ImmutableDictionary<string, int>>("<ImmutableDictionaryOfStringInt32><a>1</a><b>2</b></ImmutableDictionaryOfStringInt32>")!;
            immutable.Should().HaveCount(2);
            immutable["a"].Should().Be(1);
            immutable["b"].Should().Be(2);

            var concurrent = XmlSerializer.Deserialize<ConcurrentDictionary<string, int>>("<ConcurrentDictionaryOfStringInt32><a>1</a><b>2</b></ConcurrentDictionaryOfStringInt32>")!;
            concurrent.Should().HaveCount(2);
            concurrent["a"].Should().Be(1);
            concurrent["b"].Should().Be(2);

            var frozen = XmlSerializer.Deserialize<FrozenDictionary<string, int>>("<FrozenDictionaryOfStringInt32><a>1</a><b>2</b></FrozenDictionaryOfStringInt32>")!;
            frozen.Should().HaveCount(2);
            frozen["a"].Should().Be(1);
            frozen["b"].Should().Be(2);

            static void RoundTrip<TMap>(TMap value, string root, Type created)
                where TMap : IEnumerable<KeyValuePair<string, int>>
            {
                var expected = "<" + root + "><a>1</a><b>2</b></" + root + ">";
                XmlSerializer.Serialize(value, Bare).Should().Be(expected);
                var read = XmlSerializer.Deserialize<TMap>(expected)!;
                read.Should().BeOfType(created);
                read.Select(pair => pair.Key + "=" + pair.Value).Order().Should().Equal("a=1", "b=2");
            }
        }

        [TestMethod]
        public void CollectionKinds_AsRoots_RoundTrip()
        {
            RoundTrip(new Queue<int>(OneTwoThree), "QueueOfInt32", typeof(Queue<int>));
            RoundTrip(new LinkedList<int>(OneTwoThree), "LinkedListOfInt32", typeof(LinkedList<int>));
            RoundTrip(new HashSet<int>(OneTwoThree), "HashSetOfInt32", typeof(HashSet<int>));
            RoundTrip(new SortedSet<int>(OneTwoThree), "SortedSetOfInt32", typeof(SortedSet<int>));
            RoundTrip(new Collection<int>([.. OneTwoThree]), "CollectionOfInt32", typeof(Collection<int>));
            RoundTrip(new ObservableCollection<int>(OneTwoThree), "ObservableCollectionOfInt32", typeof(ObservableCollection<int>));
            RoundTrip(new ReadOnlyCollection<int>(OneTwoThree), "ReadOnlyCollectionOfInt32", typeof(ReadOnlyCollection<int>));
            RoundTrip<ISet<int>>(new HashSet<int>(OneTwoThree), "ISetOfInt32", typeof(HashSet<int>));
            RoundTrip<IReadOnlySet<int>>(new HashSet<int>(OneTwoThree), "IReadOnlySetOfInt32", typeof(HashSet<int>));
            RoundTrip<IList<int>>(new List<int>(OneTwoThree), "IListOfInt32", typeof(List<int>));
            RoundTrip<ICollection<int>>(new List<int>(OneTwoThree), "ICollectionOfInt32", typeof(List<int>));
            RoundTrip<IReadOnlyList<int>>(new List<int>(OneTwoThree), "IReadOnlyListOfInt32", typeof(List<int>));
            RoundTrip<IReadOnlyCollection<int>>(new List<int>(OneTwoThree), "IReadOnlyCollectionOfInt32", typeof(List<int>));
            RoundTrip<IEnumerable<int>>(new List<int>(OneTwoThree), "IEnumerableOfInt32", typeof(List<int>));
            RoundTrip(ImmutableArray.Create(1, 2, 3), "ImmutableArrayOfInt32", typeof(ImmutableArray<int>));
            RoundTrip(ImmutableList.Create(1, 2, 3), "ImmutableListOfInt32", typeof(ImmutableList<int>));
            RoundTrip(ImmutableSortedSet.Create(1, 2, 3), "ImmutableSortedSetOfInt32", typeof(ImmutableSortedSet<int>));
            RoundTrip(ImmutableQueue.Create(1, 2, 3), "ImmutableQueueOfInt32", typeof(ImmutableQueue<int>));
            RoundTrip(new ConcurrentQueue<int>(OneTwoThree), "ConcurrentQueueOfInt32", typeof(ConcurrentQueue<int>));
            RoundTrip<IImmutableList<int>>(ImmutableList.Create(1, 2, 3), "IImmutableListOfInt32", typeof(ImmutableList<int>));
            RoundTrip<IImmutableQueue<int>>(ImmutableQueue.Create(1, 2, 3), "IImmutableQueueOfInt32", typeof(ImmutableQueue<int>));

            var hashSet = XmlSerializer.Deserialize<ImmutableHashSet<int>>("<ImmutableHashSetOfInt32>" + Items + "</ImmutableHashSetOfInt32>")!;
            hashSet.Order().Should().Equal(1, 2, 3);
            XmlSerializer.Deserialize<IImmutableSet<int>>("<IImmutableSetOfInt32>" + Items + "</IImmutableSetOfInt32>")!.Order().Should().Equal(1, 2, 3);
            var frozenSet = XmlSerializer.Deserialize<FrozenSet<int>>("<FrozenSetOfInt32>" + Items + "</FrozenSetOfInt32>")!;
            frozenSet.Order().Should().Equal(1, 2, 3);

            var bag = new AddOnlyBag { 1, 2, 3 };
            XmlSerializer.Serialize(bag, Bare).Should().Be("<AddOnlyBag>" + Items + "</AddOnlyBag>");
            XmlSerializer.Deserialize<AddOnlyBag>("<AddOnlyBag>" + Items + "</AddOnlyBag>").Should().Equal(1, 2, 3);

            // A struct has a parameterless constructor that reflection does not list.
            var structBag = new StructBag { 1, 2, 3 };
            XmlSerializer.Serialize(structBag, Bare).Should().Be("<StructBag>" + Items + "</StructBag>");
            XmlSerializer.Deserialize<StructBag>("<StructBag>" + Items + "</StructBag>").Should().Equal(1, 2, 3);

            // The entry point generated code uses makes what the reflection resolver makes.
            var list = new List<int> { 1, 2, 3 };
            XmlMetadataServices.CreateCollection<int[], int>(list).Should().Equal(1, 2, 3);
            XmlMetadataServices.CreateCollection<IEnumerable<int>, int>(list).Should().BeSameAs(list);
            XmlMetadataServices.CreateCollection<Queue<int>, int>(list).Should().BeOfType<Queue<int>>().Which.Should().Equal(1, 2, 3);

            static void RoundTrip<TCollection>(TCollection value, string root, Type created)
                where TCollection : IEnumerable<int>
            {
                var expected = "<" + root + ">" + Items + "</" + root + ">";
                XmlSerializer.Serialize(value, Bare).Should().Be(expected);
                var read = XmlSerializer.Deserialize<TCollection>(expected)!;
                read.Should().BeOfType(created);
                read.Should().Equal(1, 2, 3);
            }
        }

        [TestMethod]
        public void Stacks_ReadBackWithTheSameTop()
        {
            // A stack is written from its top down, so reading must push from the last item up. Otherwise
            // every round trip would turn the stack over.
            var stack = new Stack<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            var xml = XmlSerializer.Serialize(stack, Bare);
            xml.Should().Be("<StackOfInt32><Int32>3</Int32><Int32>2</Int32><Int32>1</Int32></StackOfInt32>");
            var read = XmlSerializer.Deserialize<Stack<int>>(xml)!;
            read.Should().Equal(3, 2, 1);
            read.Pop().Should().Be(3);
            XmlSerializer.Serialize(XmlSerializer.Deserialize<Stack<int>>(xml)!, Bare).Should().Be(xml);

            var immutable = ImmutableStack.Create(1, 2, 3);
            var immutableXml = XmlSerializer.Serialize(immutable, Bare);
            immutableXml.Should().Be("<ImmutableStackOfInt32><Int32>3</Int32><Int32>2</Int32><Int32>1</Int32></ImmutableStackOfInt32>");
            var immutableRead = XmlSerializer.Deserialize<ImmutableStack<int>>(immutableXml)!;
            immutableRead.Should().Equal(3, 2, 1);
            immutableRead.Peek().Should().Be(3);
            XmlSerializer.Deserialize<IImmutableStack<int>>("<IImmutableStackOfInt32><Int32>3</Int32><Int32>2</Int32></IImmutableStackOfInt32>")!.Peek().Should().Be(3);

            var concurrent = new ConcurrentStack<int>(OneTwoThree);
            var concurrentXml = XmlSerializer.Serialize(concurrent, Bare);
            concurrentXml.Should().Be("<ConcurrentStackOfInt32><Int32>3</Int32><Int32>2</Int32><Int32>1</Int32></ConcurrentStackOfInt32>");
            XmlSerializer.Deserialize<ConcurrentStack<int>>(concurrentXml).Should().Equal(3, 2, 1);
        }

        [TestMethod]
        public void ValueTuple_WithoutIncludeFields_WritesNoMembers()
        {
            // A value tuple holds its items in fields, and fields are written only when IncludeFields is
            // set, as in System.Text.Json.
            XmlSerializer.Serialize((1, "a"), Bare).Should().Be("<ValueTupleOfInt32String/>");
            var withFields = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };
            var xml = XmlSerializer.Serialize((1, "a"), withFields);
            xml.Should().Be("<ValueTupleOfInt32String><Item1>1</Item1><Item2>a</Item2></ValueTupleOfInt32String>");
            XmlSerializer.Deserialize<(int, string)>(xml, withFields).Should().Be((1, "a"));
        }

        [TestMethod]
        public void KeyValuePair_AndTuple_RoundTrip()
        {
            var pair = XmlSerializer.Serialize(new KeyValuePair<string, int>("a", 1), Bare);
            pair.Should().Be("<KeyValuePairOfStringInt32><Key>a</Key><Value>1</Value></KeyValuePairOfStringInt32>");
            XmlSerializer.Deserialize<KeyValuePair<string, int>>(pair).Should().Be(new KeyValuePair<string, int>("a", 1));
            var tuple = XmlSerializer.Serialize(Tuple.Create(1, "a"), Bare);
            tuple.Should().Be("<TupleOfInt32String><Item1>1</Item1><Item2>a</Item2></TupleOfInt32String>");
            XmlSerializer.Deserialize<Tuple<int, string>>(tuple).Should().Be(Tuple.Create(1, "a"));
        }

        #endregion

    }

}
