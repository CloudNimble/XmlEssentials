using System;
using System.Collections.Generic;
using System.Globalization;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A caller-defined reference resolver that names objects <c>k1</c>, <c>k2</c>, and so on.
    /// </summary>
    public sealed class KeyedReferenceResolver : XmlReferenceResolver
    {

        #region Fields

        private readonly Dictionary<object, string> _ids = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void AddReference(string referenceId, object value)
        {
            _values[referenceId] = value;
            _ids[value] = referenceId;
        }

        /// <inheritdoc />
        public override string GetReference(object value, out bool alreadyExists)
        {
            alreadyExists = _ids.TryGetValue(value, out var id);
            if (!alreadyExists)
            {
                id = "k" + (_ids.Count + 1).ToString(CultureInfo.InvariantCulture);
                _ids[value] = id;
            }

            return id!;
        }

        /// <inheritdoc />
        public override object? ResolveReference(string referenceId)
        {
            return _values.TryGetValue(referenceId, out var value) ? value : null;
        }

        #endregion

    }

}
