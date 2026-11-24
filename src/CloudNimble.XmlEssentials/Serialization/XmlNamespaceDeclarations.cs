using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The namespace declarations the serializer writes on a document element: checking them, and gathering
    /// those of the options and of a type.
    /// </summary>
    internal static class XmlNamespaceDeclarations
    {

        #region Public Methods

        /// <summary>
        /// Returns why a declaration cannot be written, or null when it can.
        /// </summary>
        /// <param name="prefix">The prefix, or an empty string for the default namespace.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The reason, as a sentence, or null.</returns>
        /// <remarks>
        /// The rules are the writer's, so the writer is asked: a declaration that passes here is one
        /// <see cref="Utf8XmlWriter.WriteNamespaceDeclaration(string, string)"/> accepts on a document element.
        /// An empty namespace URI is refused as well. On a document element it would declare nothing.
        /// </remarks>
        internal static string? Check(string? prefix, string? namespaceUri)
        {
            if (prefix is null)
            {
                return "A prefix is null. Use an empty string for the default namespace.";
            }

            if (string.IsNullOrEmpty(namespaceUri))
            {
                return $"The namespace URI of the prefix '{prefix}' is null or empty.";
            }

            // The writer refuses a declaration before it writes any of it, so the element can still be ended.
            using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            string? reason = null;
            writer.WriteStartElement("e"u8);
            try
            {
                writer.WriteNamespaceDeclaration(prefix, namespaceUri);
            }
            catch (XmlException ex)
            {
                reason = $"The prefix '{prefix}' cannot be bound to '{namespaceUri}': {ex.Message}";
            }

            writer.WriteEndElement();
            return reason;
        }

        /// <summary>
        /// Copies the declarations a caller gave, in their order, after checking each one.
        /// </summary>
        /// <param name="namespaces">The declarations, keyed by prefix.</param>
        /// <returns>A read-only copy that later changes to <paramref name="namespaces"/> do not reach.</returns>
        /// <exception cref="ArgumentException">A declaration cannot be written.</exception>
        internal static IReadOnlyDictionary<string, string> Copy(IReadOnlyDictionary<string, string> namespaces)
        {
            var copy = new OrderedDictionary<string, string>(namespaces.Count, StringComparer.Ordinal);
            foreach (var (prefix, namespaceUri) in namespaces)
            {
                if (Check(prefix, namespaceUri) is { } reason)
                {
                    throw new ArgumentException(reason, nameof(namespaces));
                }

                copy.Add(prefix, namespaceUri);
            }

            return new ReadOnlyDictionary<string, string>(copy);
        }

        /// <summary>
        /// Gathers the declarations that are written on a document element of <paramref name="type"/>: those of
        /// the options, in their order, then those of the type that name neither a prefix nor a namespace already
        /// declared.
        /// </summary>
        /// <param name="type">The type that is serialized as a document.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The plan, or null when nothing is declared and the element is started as it always was.</returns>
        /// <exception cref="InvalidOperationException">An <see cref="XmlNamespaceDeclarationAttribute"/> on the type cannot be written.</exception>
        internal static XmlDocumentElementPlan? Plan(Type type, XmlSerializerOptions options)
        {
            var declared = (XmlNamespaceDeclarationAttribute[])Attribute.GetCustomAttributes(type, typeof(XmlNamespaceDeclarationAttribute), inherit: true);
            if (options.Namespaces is null && declared.Length == 0)
            {
                return null;
            }

            var entries = new List<KeyValuePair<string, string>>();
            if (options.Namespaces is { } fromOptions)
            {
                entries.AddRange(fromOptions);
            }

            foreach (var attribute in declared)
            {
                if (Check(attribute.Prefix, attribute.NamespaceUri) is { } reason)
                {
                    throw new InvalidOperationException($"[{nameof(XmlNamespaceDeclarationAttribute)}] on '{type}' cannot be written. {reason}");
                }

                if (!entries.Exists(entry => entry.Key == attribute.Prefix || entry.Value == attribute.NamespaceUri))
                {
                    entries.Add(new KeyValuePair<string, string>(attribute.Prefix, attribute.NamespaceUri));
                }
            }

            var declarations = new byte[entries.Count * 2][];
            for (var i = 0; i < entries.Count; i++)
            {
                declarations[i * 2] = Encoding.UTF8.GetBytes(entries[i].Key);
                declarations[(i * 2) + 1] = Encoding.UTF8.GetBytes(entries[i].Value);
            }

            return new XmlDocumentElementPlan(type, declarations);
        }

        #endregion

    }

}
