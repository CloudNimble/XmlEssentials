using System;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Tracks the objects that are being written, so that one reached again from inside itself is recognized
    /// as a cycle.
    /// </summary>
    /// <remarks>
    /// The serializer calls <see cref="TryEnter"/> before it writes an object and <see cref="Exit"/> after, so
    /// an object that was written and finished can be written again. <see cref="GetReference"/> has no way to
    /// say an object is finished; it is kept for callers that use the resolver through its base type, and
    /// reports every object it has been given before.
    /// </remarks>
    internal sealed class XmlIgnoreCyclesReferenceResolver : XmlReferenceResolver
    {

        #region Fields

        private readonly HashSet<object> _open = new(ReferenceEqualityComparer.Instance);
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

        #region Private Methods

        /// <summary>
        /// Records that <paramref name="value"/> has been written completely.
        /// </summary>
        /// <param name="value">The object passed to <see cref="TryEnter"/>.</param>
        internal void Exit(object value)
        {
            _open.Remove(value);
        }

        /// <summary>
        /// Records that <paramref name="value"/> is about to be written.
        /// </summary>
        /// <param name="value">The object.</param>
        /// <returns>
        /// <see langword="true"/> if the object is not being written already; <see langword="false"/> if it
        /// is, which means the object was reached from inside itself.
        /// </returns>
        internal bool TryEnter(object value)
        {
            return _open.Add(value);
        }

        #endregion

    }

}
