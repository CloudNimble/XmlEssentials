using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Collections whose item elements are given a namespace, or none, by <see cref="XmlArrayItemAttribute.Namespace"/>.
    /// </summary>
    public sealed class NamespacedItems
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a collection with no wrapper, where the item is the element of the member.
        /// </summary>
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<int> Bare { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items are given no namespace of their own.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i")]
        public List<int> Inherit { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items are put in no namespace.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i", Namespace = "")]
        public List<int> None { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items have a namespace and whose wrapper has another.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<int> Own { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items have a namespace and whose wrapper has none, with an item that is null.
        /// </summary>
        [XmlArray]
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<string?> Plain { get; set; } = [];

        #endregion

    }

    /// <summary>
    /// A base type whose derived types are named by elements in namespaces of their own.
    /// </summary>
    [XmlDerivedType<NamespacedCircle>("shape", Namespace = "urn:round")]
    [XmlDerivedType<NamespacedSquare>("shape", Namespace = "urn:square")]
    [XmlDerivedType<NamespacedBlob>("blob")]
    [XmlDerivedType<NamespacedDot>("dot", Namespace = "")]
    public abstract class NamespacedShape
    {
    }

    /// <summary>
    /// A derived type named <c>shape</c> in one namespace.
    /// </summary>
    public sealed class NamespacedCircle : NamespacedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        public int Radius { get; set; }

        #endregion

    }

    /// <summary>
    /// A derived type named <c>shape</c> in another namespace.
    /// </summary>
    public sealed class NamespacedSquare : NamespacedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the side.
        /// </summary>
        public int Side { get; set; }

        #endregion

    }

    /// <summary>
    /// A derived type that is given no namespace.
    /// </summary>
    public sealed class NamespacedBlob : NamespacedShape
    {
    }

    /// <summary>
    /// A derived type that is put in no namespace.
    /// </summary>
    public sealed class NamespacedDot : NamespacedShape
    {
    }

    /// <summary>
    /// Holds a shape as a member, under a default namespace.
    /// </summary>
    [XmlRoot(Namespace = "urn:holder")]
    public sealed class NamespacedShapeHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets one shape.
        /// </summary>
        public NamespacedShape? Shape { get; set; }

        #endregion

    }

    /// <summary>
    /// Holds shapes as the items of a collection in a wrapper, under a default namespace.
    /// </summary>
    [XmlRoot(Namespace = "urn:holder")]
    public sealed class NamespacedShapes
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shapes, in a wrapper.
        /// </summary>
        [XmlArray]
        public List<NamespacedShape> Shapes { get; set; } = [];

        #endregion

    }

    /// <summary>
    /// A base type that gives a derived type a namespace when its discriminator is not an element name.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<NamespacedBadLeaf>("leaf", Namespace = "urn:leaf")]
    public class NamespacedBadBase
    {
    }

    /// <summary>
    /// The derived type of <see cref="NamespacedBadBase"/>.
    /// </summary>
    public sealed class NamespacedBadLeaf : NamespacedBadBase
    {
    }

}
