using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CloudNimble.Text.Xml.Nodes;

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
        /// <returns><see langword="true"/> for a type that is or implements <see cref="IDictionary{TKey,TValue}"/> or <see cref="IReadOnlyDictionary{TKey,TValue}"/>.</returns>
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

            foreach (var implemented in EnumerateTypes(type))
            {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
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
            // An element node enumerates its children, and it is written as the element it is, not as a list of them.
            if (type == typeof(string) || type == typeof(byte[]) || type == typeof(XmlElementNode) || IsDictionary(type, out _, out _))
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
        /// Finds how to turn the items read from a document into a <paramref name="collectionType"/>.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collectionType">The declared collection type. It is not an array, and a <see cref="List{T}"/> cannot stand in for it.</param>
        /// <returns>A function that creates the collection from the items, in document order.</returns>
        /// <remarks>
        /// A type that cannot be created still gets a function, which throws <see cref="NotSupportedException"/>:
        /// such a type can be written, so the failure belongs to the read.
        /// </remarks>
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        [UnconditionalSuppressMessage("Trimming", "IL2067")]
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        [UnconditionalSuppressMessage("Trimming", "IL2072")]
        internal static Func<List<T>, object> CreateCollectionFactory<T>(Type collectionType)
        {
            if (collectionType == typeof(ImmutableArray<T>))
            {
                return static list => ImmutableArray.CreateRange(list);
            }

            if (collectionType == typeof(ImmutableList<T>) || collectionType == typeof(IImmutableList<T>))
            {
                return static list => ImmutableList.CreateRange(list);
            }

            if (collectionType == typeof(ImmutableHashSet<T>) || collectionType == typeof(IImmutableSet<T>))
            {
                return static list => ImmutableHashSet.CreateRange(list);
            }

            if (collectionType == typeof(ImmutableSortedSet<T>))
            {
                return static list => ImmutableSortedSet.CreateRange(list);
            }

            if (collectionType == typeof(ImmutableQueue<T>) || collectionType == typeof(IImmutableQueue<T>))
            {
                return static list => ImmutableQueue.CreateRange(list);
            }

            if (collectionType == typeof(ImmutableStack<T>) || collectionType == typeof(IImmutableStack<T>))
            {
                return static list => ImmutableStack.CreateRange(TopLast(list));
            }

            if (collectionType == typeof(FrozenSet<T>))
            {
                return static list => list.ToFrozenSet();
            }

            if (collectionType.IsAssignableFrom(typeof(HashSet<T>)))
            {
                // ISet<T> and IReadOnlySet<T>.
                return static list => new HashSet<T>(list);
            }

            if (collectionType.IsAbstract)
            {
                return list => throw CannotCreate(collectionType);
            }

            // A stack lists its items from the top down, so it is written that way. Pushing the items in
            // document order would turn it over on every round trip.
            var isStack = collectionType == typeof(Stack<T>) || collectionType == typeof(ConcurrentStack<T>);
            // A struct always has a parameterless constructor, which reflection does not list.
            if ((collectionType.IsValueType || collectionType.GetConstructor(Type.EmptyTypes) is not null) && !isStack)
            {
                if (typeof(ICollection<T>).IsAssignableFrom(collectionType))
                {
                    return list =>
                    {
                        var instance = (ICollection<T>)Activator.CreateInstance(collectionType)!;
                        foreach (var item in list)
                        {
                            instance.Add(item);
                        }

                        return instance;
                    };
                }

                if (collectionType.GetMethod("Add", [typeof(T)]) is { } add)
                {
                    return list =>
                    {
                        var instance = Activator.CreateInstance(collectionType)!;
                        foreach (var item in list)
                        {
                            add.Invoke(instance, [item]);
                        }

                        return instance;
                    };
                }
            }

            var constructor = collectionType.GetConstructor([typeof(IEnumerable<T>)]) ?? collectionType.GetConstructor([typeof(IList<T>)]);
            if (constructor is null)
            {
                return list => throw CannotCreate(collectionType);
            }

            return list => constructor.Invoke([isStack ? TopLast(list) : list]);
        }

        /// <summary>
        /// Finds how to create a <typeparamref name="TDictionary"/> and how to finish it once its entries are read.
        /// </summary>
        /// <typeparam name="TDictionary">The declared dictionary type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="finish">A function that turns the filled map into the declared type.</param>
        /// <returns>A function that creates the empty map the entries are read into.</returns>
        /// <remarks>
        /// A type that cannot be created still gets functions, and <paramref name="finish"/> throws
        /// <see cref="NotSupportedException"/>: such a type can be written, so the failure belongs to the read.
        /// </remarks>
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        [UnconditionalSuppressMessage("Trimming", "IL2067")]
        [UnconditionalSuppressMessage("Trimming", "IL2070")]
        [UnconditionalSuppressMessage("Trimming", "IL2072")]
        [UnconditionalSuppressMessage("Trimming", "IL2087")]
        [UnconditionalSuppressMessage("Trimming", "IL2090")]
        [UnconditionalSuppressMessage("Trimming", "IL2091")]
        internal static Func<IDictionary<TKey, TValue>> CreateDictionaryFactory<TDictionary, TKey, TValue>(out Func<IDictionary<TKey, TValue>, TDictionary> finish)
            where TKey : notnull
        {
            var type = typeof(TDictionary);
            Func<IDictionary<TKey, TValue>> createMap = static () => new Dictionary<TKey, TValue>();
            if (type.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                // Dictionary<,> itself, IDictionary<,> and IReadOnlyDictionary<,>.
                finish = static map => (TDictionary)map;
            }
            else if (type == typeof(ImmutableDictionary<TKey, TValue>) || type == typeof(IImmutableDictionary<TKey, TValue>))
            {
                finish = static map => (TDictionary)(object)ImmutableDictionary.CreateRange(map);
            }
            else if (type == typeof(ImmutableSortedDictionary<TKey, TValue>))
            {
                finish = static map => (TDictionary)(object)ImmutableSortedDictionary.CreateRange(map);
            }
            else if (type == typeof(FrozenDictionary<TKey, TValue>))
            {
                finish = static map => (TDictionary)(object)map.ToFrozenDictionary();
            }
            else if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) is not null && typeof(IDictionary<TKey, TValue>).IsAssignableFrom(type))
            {
                // The entries are read straight into the declared type, so a sorted dictionary sorts as it fills.
                finish = static map => (TDictionary)map;
                return static () => (IDictionary<TKey, TValue>)Activator.CreateInstance(typeof(TDictionary))!;
            }
            else if (!type.IsAbstract && type.GetConstructor([typeof(IDictionary<TKey, TValue>)]) is { } constructor)
            {
                finish = map => (TDictionary)constructor.Invoke([map]);
            }
            else
            {
                finish = static map => throw new NotSupportedException(
                    $"The dictionary type '{typeof(TDictionary)}' cannot be created. It needs a public parameterless constructor and IDictionary<TKey, TValue>, or a public constructor that takes IDictionary<TKey, TValue>.");
            }

            return createMap;
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
        /// Adds items to a collection that already exists.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items, in document order.</param>
        /// <param name="target">The collection to add to.</param>
        /// <returns>
        /// <see langword="true"/> when the items were added; <see langword="false"/> when the collection cannot take
        /// items, as with an array or a read-only collection.
        /// </returns>
        /// <remarks>
        /// A stack is written from its top down, so the items are pushed last to first: the first item of the
        /// document ends on top, as it does when a stack is read whole.
        /// </remarks>
        internal static bool AddItems<T>(List<T> items, object target)
        {
            switch (target)
            {
                case ICollection<T> { IsReadOnly: false } collection:
                    foreach (var item in items)
                    {
                        collection.Add(item);
                    }

                    return true;
                case Stack<T> stack:
                    for (var i = items.Count - 1; i >= 0; i--)
                    {
                        stack.Push(items[i]);
                    }

                    return true;
                case Queue<T> queue:
                    foreach (var item in items)
                    {
                        queue.Enqueue(item);
                    }

                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns whether a member declared as <paramref name="collectionType"/> can have items added to the
        /// collection it holds.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collectionType">The declared collection type.</param>
        /// <returns>
        /// <see langword="true"/> for <see cref="ICollection{T}"/> and the interfaces that extend it, for a class that
        /// implements it and has a public parameterless constructor, and for <see cref="Stack{T}"/> and
        /// <see cref="Queue{T}"/>. <see langword="false"/> for an array, a value type, an immutable or frozen
        /// collection, and an interface that only reads, such as <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        /// The declared type decides, as it does in System.Text.Json. A member declared as <see cref="IList{T}"/> that
        /// holds an array passes this test and fails when the items are added.
        /// </remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Only the presence of the parameterless constructor is tested; the collection is never created here.")]
        internal static bool CanPopulate<T>(Type collectionType)
        {
            if (collectionType.IsValueType || collectionType.IsArray)
            {
                return false;
            }

            if (collectionType == typeof(Stack<T>) || collectionType == typeof(Queue<T>))
            {
                return true;
            }

            return typeof(ICollection<T>).IsAssignableFrom(collectionType)
                && (collectionType.IsInterface || collectionType.GetConstructor(Type.EmptyTypes) is not null);
        }

        /// <summary>
        /// Returns whether a member declared as <typeparamref name="TDictionary"/> can have entries added to the
        /// dictionary it holds.
        /// </summary>
        /// <typeparam name="TDictionary">The declared dictionary type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <returns>
        /// <see langword="true"/> for <see cref="IDictionary{TKey, TValue}"/> and for a class that implements it and
        /// has a public parameterless constructor; <see langword="false"/> for an immutable, frozen or read-only one.
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "Only the presence of the parameterless constructor is tested; the dictionary is never created here.")]
        internal static bool CanPopulateDictionary<TDictionary, TKey, TValue>()
        {
            var type = typeof(TDictionary);
            return !type.IsValueType
                && typeof(IDictionary<TKey, TValue>).IsAssignableFrom(type)
                && (type.IsInterface || type.GetConstructor(Type.EmptyTypes) is not null);
        }


        /// <summary>
        /// Creates the error for a collection type that can be written and cannot be created.
        /// </summary>
        /// <param name="collectionType">The declared collection type.</param>
        /// <returns>The exception to throw.</returns>
        internal static NotSupportedException CannotCreate(Type collectionType)
        {
            return new NotSupportedException(
                $"The collection type '{collectionType}' cannot be created. It needs a public parameterless constructor and ICollection<T> or a public Add method, or a public constructor that takes IEnumerable<T> or IList<T>.");
        }

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

        /// <summary>
        /// Copies items that were written from the top of a stack down, so that the top comes last.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="list">The items, top first.</param>
        /// <returns>The items, top last, which is the order a stack is built in.</returns>
        internal static List<T> TopLast<T>(List<T> list)
        {
            var reversed = new List<T>(list);
            reversed.Reverse();
            return reversed;
        }

        #endregion

    }

}
