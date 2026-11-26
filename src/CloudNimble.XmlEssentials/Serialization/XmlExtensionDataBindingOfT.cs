using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Accessors for the <see cref="XmlExtensionData"/> member that <typeparamref name="TDeclaring"/> declares.
    /// </summary>
    /// <typeparam name="TDeclaring">The type that declares the member.</typeparam>
    internal sealed class XmlExtensionDataBinding<TDeclaring> : XmlExtensionDataBinding
    {

        #region Fields

        private readonly Func<TDeclaring, XmlExtensionData?> _get;
        private readonly XmlPropertySetter<TDeclaring, XmlExtensionData>? _set;
        private readonly XmlUnbox<TDeclaring>? _unbox;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <param name="get">Reads the bag.</param>
        /// <param name="set">Writes the bag, or null when it is read-only after construction.</param>
        /// <param name="unbox">Reaches the value inside a box, when <typeparamref name="TDeclaring"/> is a value type; otherwise null.</param>
        internal XmlExtensionDataBinding(Func<TDeclaring, XmlExtensionData?> get, XmlPropertySetter<TDeclaring, XmlExtensionData>? set, XmlUnbox<TDeclaring>? unbox)
        {
            _get = get;
            _set = set;
            _unbox = unbox;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        internal override XmlExtensionData? GetValue(object obj)
        {
            return _get((TDeclaring)obj);
        }

        /// <inheritdoc />
        internal override void SetValue(object obj, XmlExtensionData value)
        {
            if (_set is not null)
            {
                XmlMemberAccess.Set(_set, _unbox, obj, value);
            }
        }

        #endregion

    }

}
