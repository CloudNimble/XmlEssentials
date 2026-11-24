using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Holds, for one declared collection type, the function that creates it from the items that were read.
    /// </summary>
    /// <typeparam name="TCollection">The declared collection type.</typeparam>
    /// <typeparam name="TElement">The item type.</typeparam>
    /// <remarks>
    /// The function is found once for each pair of types. It makes what the reflection resolver makes: the
    /// list itself when the declared type accepts one, an array, or a collection built by
    /// <see cref="XmlCollectionShape.CreateCollectionFactory{T}(Type)"/>.
    /// </remarks>
    internal static class XmlCollectionFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TCollection, TElement>
    {

        #region Fields

        /// <summary>
        /// Creates the collection from the items, in document order.
        /// </summary>
        internal static readonly Func<List<TElement>, object> Create = Find();

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds the function for the pair of types.
        /// </summary>
        /// <returns>The function.</returns>
        internal static Func<List<TElement>, object> Find()
        {
            if (typeof(TCollection).IsAssignableFrom(typeof(List<TElement>)))
            {
                return static items => items;
            }

            return typeof(TCollection).IsArray
                ? static items => items.ToArray()
                : XmlCollectionShape.CreateCollectionFactory<TElement>(typeof(TCollection));
        }

        #endregion

    }

}
