using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Detects collection and dictionary CLR types for object mapping.
    /// </summary>
    internal static class XmlCollectionShape
    {

        #region Public Methods

        /// <summary>
        /// Returns whether <paramref name="type"/> is a dictionary map.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="keyType">The key type.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns><see langword="true"/> for <see cref="IDictionary{TKey,TValue}"/> and closed dictionary types.</returns>
        internal static bool IsDictionary(Type type, out Type keyType, out Type valueType)
        {
            keyType = typeof(object);
            valueType = typeof(object);
            if (type == typeof(string) || type.IsArray)
            {
                return false;
            }

            foreach (var implemented in EnumerateTypes(type))
            {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var args = implemented.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a collection of items (not a dictionary, string, or byte array).
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="elementType">The item type.</param>
        /// <returns><see langword="true"/> for arrays and <see cref="IEnumerable{T}"/> collections.</returns>
        internal static bool IsCollection(Type type, out Type elementType)
        {
            elementType = typeof(object);
            if (type == typeof(string) || type == typeof(byte[]) || IsDictionary(type, out _, out _))
            {
                return false;
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    return false;
                }

                elementType = type.GetElementType()!;
                return true;
            }

            foreach (var implemented in EnumerateTypes(type))
            {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = implemented.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a <paramref name="collectionType"/> that a <see cref="List{T}"/> cannot stand in for, and adds the items to it.
        /// </summary>
        /// <param name="collectionType">The declared collection type.</param>
        /// <param name="elementType">The item type.</param>
        /// <param name="list">The items.</param>
        /// <returns>The collection.</returns>
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        [UnconditionalSuppressMessage("Trimming", "IL2067")]
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        internal static object Populate(Type collectionType, Type elementType, IList list)
        {
            var instance = Activator.CreateInstance(collectionType);
            if (instance is IList typed)
            {
                foreach (var item in list)
                {
                    typed.Add(item);
                }

                return typed;
            }

            var add = collectionType.GetMethod("Add", [elementType]);
            if (instance is not null && add is not null)
            {
                foreach (var item in list)
                {
                    add.Invoke(instance, [item]);
                }

                return instance;
            }

            throw new NotSupportedException($"Cannot populate collection type '{collectionType}'.");
        }

        /// <summary>
        /// Returns whether <paramref name="key"/> is an XML NCName made of ASCII characters only.
        /// </summary>
        /// <param name="key">The dictionary key after the naming policy.</param>
        /// <returns><see langword="true"/> when it can be an element local name.</returns>
        internal static bool IsNcName(string key)
        {
            if (key.Length == 0 || key[0] is > (char)0x7F or ':' || !XmlChars.IsAsciiNameStart((byte)key[0]))
            {
                return false;
            }

            for (var i = 1; i < key.Length; i++)
            {
                if (key[i] > 0x7F || !XmlChars.IsAsciiNcNameChar((byte)key[i]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Walks <paramref name="type"/> and its interfaces.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The type then each interface.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        internal static IEnumerable<Type> EnumerateTypes(Type type)
        {
            yield return type;
            foreach (var implemented in type.GetInterfaces())
            {
                yield return implemented;
            }
        }

        #endregion

    }

}
