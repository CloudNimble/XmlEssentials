using System;
using System.Collections.Generic;
using System.Globalization;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Assigns incrementing ids and resolves them.
    /// </summary>
    internal sealed class XmlPreserveReferenceResolver : XmlReferenceResolver
    {

        #region Fields

        private readonly Dictionary<object, string> _ids = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
        private int _next = 1;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void AddReference(string referenceId, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
            ArgumentNullException.ThrowIfNull(value);
            _values[referenceId] = value;
            _ids[value] = referenceId;
        }

        /// <inheritdoc />
        public override string GetReference(object value, out bool alreadyExists)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_ids.TryGetValue(value, out var id))
            {
                alreadyExists = true;
                return id;
            }

            id = _next.ToString(CultureInfo.InvariantCulture);
            _next++;
            _ids[value] = id;
            alreadyExists = false;
            return id;
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
