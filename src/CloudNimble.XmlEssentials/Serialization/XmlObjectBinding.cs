using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The bound members of an object type and how to create an instance.
    /// </summary>
    /// <remarks>
    /// Members are sorted into the groups the reader and writer walk, once, when the type is bound, so neither asks
    /// each member what kind it is for every object.
    /// </remarks>
    internal sealed class XmlObjectBinding
    {

        #region Fields

        /// <summary>
        /// For each slot, whether it holds a box that stays in the values array from one object to the next.
        /// </summary>
        private readonly bool[] _heldSlots;

        /// <summary>
        /// Whether the object can be read a piece at a time: 0 until it is worked out, then 1 for yes and 2 for no.
        /// </summary>
        private int _resumable;

        /// <summary>
        /// A values array no reader is using, or null.
        /// </summary>
        private object?[]? _spareValues;

        /// <summary>
        /// The members written as attributes, in write order.
        /// </summary>
        internal readonly XmlBoundProperty[] Attributes;

        /// <summary>
        /// The constructor with parameters used to create an instance, or null when <see cref="CreateObject"/> is used.
        /// </summary>
        internal readonly Func<object?[], object>? Constructor;

        /// <summary>
        /// For each constructor parameter, the index into <see cref="Properties"/> that supplies it, or <c>-1</c>.
        /// </summary>
        internal readonly int[] ConstructorMap;

        /// <summary>
        /// The members written inside the element, in write order: simple content, child elements, or mixed content.
        /// </summary>
        internal readonly XmlBoundProperty[] Content;

        /// <summary>
        /// Creates an instance without arguments, or null when the type is created through <see cref="Constructor"/>.
        /// </summary>
        internal readonly Func<object>? CreateObject;

        /// <summary>
        /// The members matched to child elements by name, in write order.
        /// </summary>
        internal readonly XmlBoundProperty[] Elements;

        /// <summary>
        /// The extension-data member, or null.
        /// </summary>
        internal readonly XmlExtensionDataBinding? ExtensionData;

        /// <summary>
        /// The member that holds mixed text and child elements, or null.
        /// </summary>
        internal readonly XmlBoundProperty? Mixed;

        /// <summary>
        /// Whether the type was given no metadata: a source generator emitted only a write fast path for it. Such a
        /// type cannot be read, and can be written only through the fast path.
        /// </summary>
        internal bool NoMetadata;

        /// <summary>
        /// For each constructor parameter, its default value, or null when it has none.
        /// </summary>
        internal readonly object?[] ParameterDefaults;

        /// <summary>
        /// For each constructor parameter, whether it has a default value.
        /// </summary>
        internal readonly bool[] ParameterHasDefault;

        /// <summary>
        /// The derived types this type may be written as when it is the document root.
        /// </summary>
        internal readonly XmlPolymorphism Polymorphism;

        /// <summary>
        /// The members in write order: attributes, then simple content, then child elements.
        /// </summary>
        internal readonly XmlBoundProperty[] Properties;

        /// <summary>
        /// The members that must appear when deserializing.
        /// </summary>
        internal readonly XmlBoundProperty[] Required;

        /// <summary>
        /// The number of slots an object uses while it is read: one per constructor parameter, then one per member
        /// that fills no parameter. See <see cref="XmlBoundProperty.Index"/>.
        /// </summary>
        internal readonly int SlotCount;

        /// <summary>
        /// The member that holds the element's simple content, or null.
        /// </summary>
        internal readonly XmlBoundProperty? Text;

        /// <summary>
        /// The collection members that gather repeated or mixed children until the element ends.
        /// </summary>
        internal readonly XmlBoundProperty[] UnwrappedCollections;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a binding.
        /// </summary>
        /// <param name="properties">The members in write order.</param>
        /// <param name="constructor">The constructor with parameters, or null.</param>
        /// <param name="constructorMap">For each constructor parameter, the member index or <c>-1</c>.</param>
        /// <param name="createObject">The factory used when there is no constructor with parameters.</param>
        /// <param name="slotCount">The number of slots an object uses while it is read.</param>
        /// <param name="extensionData">The extension-data member, or null.</param>
        /// <param name="polymorphism">The derived types this type may be written as when it is the document root.</param>
        internal XmlObjectBinding(
            XmlBoundProperty[] properties,
            XmlConstructorFacts? constructor,
            int[] constructorMap,
            Func<object>? createObject,
            int slotCount,
            XmlExtensionDataBinding? extensionData,
            XmlPolymorphism polymorphism)
        {
            Properties = properties;
            ConstructorMap = constructorMap;
            CreateObject = createObject;
            ExtensionData = extensionData;
            SlotCount = slotCount;
            Polymorphism = polymorphism;
            var parameters = constructor?.Parameters ?? [];
            Constructor = constructor?.Invoke;
            ParameterDefaults = new object?[parameters.Length];
            ParameterHasDefault = new bool[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    ParameterHasDefault[i] = true;
                    ParameterDefaults[i] = parameters[i].DefaultValue;
                }
                else if (constructorMap[i] < 0)
                {
                    // The parameter belongs to an ignored member. It is never in the document, so it cannot
                    // be missing from one, and the constructor takes the default of its type: the null held
                    // in its slot is what reflection passes as the default of a value type.
                    ParameterHasDefault[i] = true;
                }
            }

            _heldSlots = new bool[slotCount];
            if (constructor is not null)
            {
                var filled = new List<Type>();
                for (var m = 0; m < properties.Length; m++)
                {
                    filled.Clear();
                    for (var i = 0; i < constructorMap.Length; i++)
                    {
                        if (constructorMap[i] == m)
                        {
                            filled.Add(parameters[i].ParameterType);
                        }
                    }

                    properties[m].UseHeldBox(CollectionsMarshal.AsSpan(filled));
                    _heldSlots[properties[m].Index] = properties[m].CreateHeldBox() is not null;
                }
            }

            var attributes = new List<XmlBoundProperty>();
            var content = new List<XmlBoundProperty>();
            var elements = new List<XmlBoundProperty>();
            var required = new List<XmlBoundProperty>();
            var unwrapped = new List<XmlBoundProperty>();
            foreach (var property in properties)
            {
                if (property.IsRequired)
                {
                    required.Add(property);
                }

                if (property.IsUnwrappedCollection)
                {
                    unwrapped.Add(property);
                }

                if (property.IsAttribute)
                {
                    attributes.Add(property);
                    continue;
                }

                content.Add(property);
                if (property.IsText)
                {
                    Text = property;
                }
                else if (property.Collection is { Mixed: true })
                {
                    Mixed = property;
                }
                else
                {
                    elements.Add(property);
                }
            }

            Attributes = [.. attributes];
            Content = [.. content];
            Elements = [.. elements];
            Required = [.. required];
            UnwrappedCollections = [.. unwrapped];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns whether a value of this type can be read from a stream a piece at a time, going on where the
        /// last block of input ended (<see cref="XmlResumableReader"/>).
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when every member that is read from an element is a built-in scalar, an object
        /// that can itself be read this way, or a collection of either, and none of them is polymorphic or
        /// populated; and the object has no text, mixed content, extension data or derived types.
        /// </returns>
        /// <remarks>
        /// The answer is worked out once. Any other object is read after all of it has arrived.
        /// </remarks>
        internal bool IsResumable()
        {
            if (_resumable == 0)
            {
                _resumable = IsResumable(this, []) ? 1 : 2;
            }

            return _resumable == 1;
        }

        /// <summary>
        /// Works out whether <paramref name="binding"/> and every type its members reach can be read from a stream
        /// a piece at a time.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="visited">The bindings that are being looked at or have been found to be readable this way.</param>
        /// <returns><see langword="true"/> when the object can be read a piece at a time.</returns>
        /// <remarks>
        /// A type that reaches itself is taken to be readable while its own members are looked at, which is
        /// right: the answer is no only when some member, of any type on the way, cannot be read this way.
        /// </remarks>
        internal static bool IsResumable(XmlObjectBinding binding, HashSet<XmlObjectBinding> visited)
        {
            visited.Add(binding);
            if (binding.Text is not null || binding.Mixed is not null || binding.ExtensionData is not null || binding.Polymorphism.IsActive)
            {
                return false;
            }

            foreach (var member in binding.Elements)
            {
                if (member.NeverStored && !member.Populates)
                {
                    // The element is passed over, whatever it holds.
                    continue;
                }

                if (member.Populates || member.Polymorphism.IsActive)
                {
                    return false;
                }

                var isScalar = member.Collection is { } collection ? collection.HasItemValueConverter : member.HasValueConverter;
                if (isScalar)
                {
                    continue;
                }

                var valueInfo = member.Collection is { } items ? items.ItemTypeInfo : member.ValueTypeInfo;
                if (valueInfo.ResumableBinding is not { } inner)
                {
                    return false;
                }

                if (!visited.Contains(inner) && !IsResumable(inner, visited))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Refuses to read or write from a binding that has no metadata.
        /// </summary>
        /// <param name="type">The type that was to be read or written.</param>
        /// <exception cref="InvalidOperationException">The type was generated in <see cref="XmlSourceGenerationMode.Serialization"/> only.</exception>
        internal void ThrowIfNoMetadata(Type type)
        {
            if (NoMetadata)
            {
                throw new InvalidOperationException(
                    $"The type info for '{type}' was generated in XmlSourceGenerationMode.Serialization, which provides no property metadata. It can only write a type that has a write fast path, with the options it was compiled for. Use XmlSourceGenerationMode.Default or XmlSourceGenerationMode.Metadata.");
            }
        }

        /// <summary>
        /// Returns the array that holds an object's values until its constructor is called.
        /// </summary>
        /// <returns>
        /// An array of <see cref="SlotCount"/> slots. A slot whose member holds its value in a reusable box
        /// has that box in it; every other slot is null.
        /// </returns>
        /// <remarks>
        /// One array is kept between objects, so reading a list of records allocates the records and not an
        /// array and a box per value beside each of them. The array is handed to one reader at a time: a
        /// second thread, or the same type nested inside itself, finds none kept and gets a new one.
        /// </remarks>
        internal object?[] RentValues()
        {
            if (Interlocked.Exchange(ref _spareValues, null) is { } spare)
            {
                return spare;
            }

            var values = new object?[SlotCount];
            foreach (var property in Properties)
            {
                values[property.Index] = property.CreateHeldBox();
            }

            return values;
        }

        /// <summary>
        /// Keeps <paramref name="values"/> for the next object, once the constructor has taken what it needs.
        /// </summary>
        /// <param name="values">An array from <see cref="RentValues"/>.</param>
        /// <remarks>
        /// Everything but the reusable boxes is dropped, so the array does not keep the last object's
        /// strings and children alive. An array is not returned when reading fails; it is simply let go.
        /// </remarks>
        internal void ReturnValues(object?[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (!_heldSlots[i])
                {
                    values[i] = null;
                }
            }

            Volatile.Write(ref _spareValues, values);
        }

        #endregion

    }

}
