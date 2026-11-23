using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Shared state while binding one schema document into POCOs and type specs.
    /// </summary>
    internal sealed class XsdBindContext
    {

        #region Public Properties

        /// <summary>
        /// Gets used C# type names for collision detection.
        /// </summary>
        internal HashSet<string> ClassNames { get; }

        /// <summary>
        /// Gets named <c>xs:complexType</c> elements keyed by XSD name.
        /// </summary>
        internal Dictionary<string, MiniElement> ComplexTypes { get; }

        /// <summary>
        /// Gets diagnostics to report.
        /// </summary>
        internal List<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets the schema file name, for diagnostic messages.
        /// </summary>
        internal string FileName { get; }

        /// <summary>
        /// Gets the C# namespace.
        /// </summary>
        internal string Namespace { get; }

        /// <summary>
        /// Gets generated POCO sources.
        /// </summary>
        internal List<(string Hint, string Source)> Pocos { get; }

        /// <summary>
        /// Gets the target namespace URI.
        /// </summary>
        internal string TargetNamespace { get; }

        /// <summary>
        /// Gets XSD type names mapped to fully qualified CLR type names.
        /// </summary>
        internal Dictionary<string, string> TypeMap { get; }

        /// <summary>
        /// Gets type specs for the generated context.
        /// </summary>
        internal List<TypeSpec> TypeSpecs { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the bind context.
        /// </summary>
        /// <param name="namespaceName">The C# namespace.</param>
        /// <param name="fileName">The schema file name.</param>
        /// <param name="targetNamespace">The XML target namespace.</param>
        /// <param name="classNames">Used C# type names.</param>
        /// <param name="complexTypes">Named complex types.</param>
        /// <param name="typeMap">XSD name to CLR type map.</param>
        /// <param name="pocos">POCO sources.</param>
        /// <param name="typeSpecs">Type specs.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        internal XsdBindContext(
            string namespaceName,
            string fileName,
            string targetNamespace,
            HashSet<string> classNames,
            Dictionary<string, MiniElement> complexTypes,
            Dictionary<string, string> typeMap,
            List<(string Hint, string Source)> pocos,
            List<TypeSpec> typeSpecs,
            List<Diagnostic> diagnostics)
        {
            Namespace = namespaceName;
            FileName = fileName;
            TargetNamespace = targetNamespace;
            ClassNames = classNames;
            ComplexTypes = complexTypes;
            TypeMap = typeMap;
            Pocos = pocos;
            TypeSpecs = typeSpecs;
            Diagnostics = diagnostics;
        }

        #endregion

    }

}
