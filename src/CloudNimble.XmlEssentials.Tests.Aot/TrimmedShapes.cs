using System.Collections;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// A point, held by value in a list.
    /// </summary>
    public struct ShapePoint
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the horizontal position.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the vertical position.
        /// </summary>
        public int Y { get; set; }

        #endregion

    }

    /// <summary>
    /// An enumeration, held in a nullable member.
    /// </summary>
    public enum ShapeFit
    {

        /// <summary>
        /// A close fit.
        /// </summary>
        Slim,

        /// <summary>
        /// A wide fit.
        /// </summary>
        Loose,

    }

    /// <summary>
    /// A collection type of the program's own, which the serializer fills through its <c>Add</c> method.
    /// </summary>
    public sealed class ShapeBag : IEnumerable<int>
    {

        #region Fields

        private readonly List<int> _items = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a number to the bag.
        /// </summary>
        /// <param name="item">The number.</param>
        public void Add(int item)
        {
            _items.Add(item);
        }

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

    /// <summary>
    /// A closed generic type. Its derived types are found by the serializer, not declared.
    /// </summary>
    /// <typeparam name="T">The type of what a box holds.</typeparam>
    [XmlPolymorphic(InferClosedTypePolymorphism = true)]
    public closed class ShapeBox<T>
    {
    }

    /// <summary>
    /// A box that holds a value: a generic derived type of a closed generic type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public sealed class ShapeFull<T> : ShapeBox<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T? Value { get; set; }

        #endregion

    }

    /// <summary>
    /// A closed type that is not generic, with a closed type and a sealed type derived from it.
    /// </summary>
    [XmlPolymorphic(InferClosedTypePolymorphism = true)]
    public closed class ShapeKind
    {
    }

    /// <summary>
    /// A closed type under a closed type. It is never created, so its own derived types stand in its place.
    /// </summary>
    public closed class ShapeRound : ShapeKind
    {
    }

    /// <summary>
    /// A derived type two levels down.
    /// </summary>
    public sealed class ShapeDisc : ShapeRound
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        public int R { get; set; }

        #endregion

    }

    /// <summary>
    /// A derived type one level down.
    /// </summary>
    public sealed class ShapeTile : ShapeKind
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the length of a side.
        /// </summary>
        public int Side { get; set; }

        #endregion

    }

    /// <summary>
    /// Holds the member shapes the serializer once found or made by reflection: a nullable enumeration, a list of
    /// value types, a dictionary and a collection that are not the built-in list and dictionary, a collection
    /// declared by its interface, mixed content in an array, and a generic closed type.
    /// </summary>
    public sealed class TrimmedShapes
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a collection type of the program's own.
        /// </summary>
        public ShapeBag Bag { get; set; } = [];

        /// <summary>
        /// Gets or sets a value of a generic closed type.
        /// </summary>
        public ShapeBox<int>? Box { get; set; }

        /// <summary>
        /// Gets or sets a nullable enumeration.
        /// </summary>
        public ShapeFit? Fit { get; set; }

        /// <summary>
        /// Gets or sets values of a closed type that is not generic, whose derived types are one and two levels down.
        /// </summary>
        public List<ShapeKind> Kinds { get; set; } = [];

        /// <summary>
        /// Gets or sets a nullable enumeration that has no value.
        /// </summary>
        public ShapeFit? NoFit { get; set; }

        /// <summary>
        /// Gets or sets a list of value types.
        /// </summary>
        public List<ShapePoint> Points { get; set; } = [];

        /// <summary>
        /// Gets or sets a dictionary that keeps its keys in order.
        /// </summary>
        public SortedDictionary<string, double> Rates { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection declared by its interface.
        /// </summary>
        public ISet<string> Set { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets a stack, which is written from the top down.
        /// </summary>
        public Stack<int> Stack { get; set; } = new();

        #endregion

    }

    /// <summary>
    /// Text with elements in it, held in an array.
    /// </summary>
    public sealed class TrimmedPara
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text and the elements, in document order.
        /// </summary>
        [XmlMixed]
        public XmlMixedContentItem[] Content { get; set; } = [];

        #endregion

    }

    /// <summary>
    /// The generated context of the shapes.
    /// </summary>
    [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
    [XmlSerializable<TrimmedPara>]
    [XmlSerializable<TrimmedShapes>]
    internal partial class TrimmedXmlContext : XmlSerializerContext
    {
    }

}
