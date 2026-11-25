using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A caller-defined reference resolver that gives every object an empty identifier and reports an object
    /// it has been asked about before as already existing.
    /// </summary>
    public sealed class NamelessReferenceResolver : XmlReferenceResolver
    {

        #region Fields

        private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void AddReference(string referenceId, object value)
        {
            throw new NotSupportedException("A document written with this resolver holds no identifiers.");
        }

        /// <inheritdoc />
        public override string GetReference(object value, out bool alreadyExists)
        {
            alreadyExists = !_seen.Add(value);
            return "";
        }

        /// <inheritdoc />
        public override object? ResolveReference(string referenceId)
        {
            throw new NotSupportedException("A document written with this resolver holds no references.");
        }

        #endregion

    }

}
