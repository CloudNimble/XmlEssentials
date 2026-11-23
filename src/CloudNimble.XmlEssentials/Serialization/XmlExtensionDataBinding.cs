using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Accessors for a type's <see cref="XmlExtensionData"/> member.
    /// </summary>
    internal sealed class XmlExtensionDataBinding
    {

        #region Fields

        private readonly Func<object, XmlExtensionData?> _get;
        private readonly Action<object, XmlExtensionData>? _set;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <param name="get">Reads the bag from a boxed instance.</param>
        /// <param name="set">Writes the bag, or null when it is read-only after construction.</param>
        internal XmlExtensionDataBinding(Func<object, XmlExtensionData?> get, Action<object, XmlExtensionData>? set)
        {
            _get = get;
            _set = set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads the bag from <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <returns>The bag, or null.</returns>
        internal XmlExtensionData? GetValue(object obj)
        {
            return _get(obj);
        }

        /// <summary>
        /// Assigns <paramref name="value"/> on <paramref name="obj"/> when the member can be set.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <param name="value">The bag.</param>
        internal void SetValue(object obj, XmlExtensionData value)
        {
            _set?.Invoke(obj, value);
        }

        #endregion

    }

}
