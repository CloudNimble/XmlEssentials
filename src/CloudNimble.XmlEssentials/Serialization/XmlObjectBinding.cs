using System;
using System.Collections.Generic;
using System.Reflection;

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
        /// The members written as attributes, in write order.
        /// </summary>
        internal readonly XmlBoundProperty[] Attributes;

        /// <summary>
        /// The constructor with parameters used to create an instance, or null when <see cref="CreateObject"/> is used.
        /// </summary>
        internal readonly ConstructorInvoker? Constructor;

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
            ConstructorInfo? constructor,
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
            var parameters = constructor?.GetParameters() ?? [];
            Constructor = constructor is null ? null : ConstructorInvoker.Create(constructor);
            ParameterDefaults = new object?[parameters.Length];
            ParameterHasDefault = new bool[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    ParameterHasDefault[i] = true;
                    ParameterDefaults[i] = parameters[i].DefaultValue;
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

    }

}
