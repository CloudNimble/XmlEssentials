using System;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reports whether an object has already been seen.
    /// </summary>
    internal sealed class XmlIgnoreCyclesReferenceResolver : XmlReferenceResolver
    {

        #region Fields

        private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void AddReference(string referenceId, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
            ArgumentNullException.ThrowIfNull(value);
            _values[referenceId] = value;
            _seen.Add(value);
        }

        /// <inheritdoc />
        public override string GetReference(object value, out bool alreadyExists)
        {
            ArgumentNullException.ThrowIfNull(value);
            alreadyExists = !_seen.Add(value);
            return "";
        }

        /// <inheritdoc />
        public override object? ResolveReference(string referenceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
            return _values.TryGetValue(referenceId, out var value) ? value : null;
        }

        #endregion

    }

}
