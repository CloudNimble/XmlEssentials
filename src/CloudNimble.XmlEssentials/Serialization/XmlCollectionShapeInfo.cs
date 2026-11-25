using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What is known about a type as a container: the type of its items when it is a collection, or the types
    /// of its keys and values when it is a dictionary. The default value is a type that is neither.
    /// </summary>
    internal readonly struct XmlCollectionShapeInfo
    {

        #region Fields

        /// <summary>
        /// The type of the items of a collection, or <see langword="null"/> for a type that is not a collection.
        /// </summary>
        internal readonly Type? ElementType;

        /// <summary>
        /// The type of the keys of a dictionary, or <see langword="null"/> for a type that is not a dictionary.
        /// </summary>
        internal readonly Type? KeyType;

        /// <summary>
        /// The type of the values of a dictionary, or <see langword="null"/> for a type that is not a dictionary.
        /// </summary>
        internal readonly Type? ValueType;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlCollectionShapeInfo"/> struct.
        /// </summary>
        /// <param name="elementType">The item type of a collection, or <see langword="null"/>.</param>
        /// <param name="keyType">The key type of a dictionary, or <see langword="null"/>.</param>
        /// <param name="valueType">The value type of a dictionary, or <see langword="null"/>.</param>
        internal XmlCollectionShapeInfo(Type? elementType, Type? keyType, Type? valueType)
        {
            ElementType = elementType;
            KeyType = keyType;
            ValueType = valueType;
        }

        #endregion

    }

}
