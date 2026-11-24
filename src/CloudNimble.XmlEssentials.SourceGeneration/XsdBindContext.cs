using System;
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
        /// Gets the named attribute groups of the schema, by name.
        /// </summary>
        internal Dictionary<string, MiniElement> AttributeGroups { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the named complex type each named complex type extends or restricts, by the name of the derived type.
        /// </summary>
        internal Dictionary<string, string> BaseOf { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets used C# type names for collision detection.
        /// </summary>
        internal HashSet<string> ClassNames { get; }

        /// <summary>
        /// Gets the names of the named complex types, in the order the schema declares them.
        /// </summary>
        internal List<string> ComplexTypeOrder { get; } = new List<string>();

        /// <summary>
        /// Gets named complex types.
        /// </summary>
        internal Dictionary<string, MiniElement> ComplexTypes { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the named complex types that derive from each named complex type, directly or through another, by
        /// the name of the base type and in the order the schema declares them.
        /// </summary>
        internal Dictionary<string, List<string>> DerivedOf { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the diagnostic sink.
        /// </summary>
        internal List<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets the names of the classes this schema has written for elements with an anonymous complex type.
        /// </summary>
        internal HashSet<string> ElementClasses { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the names of the global elements that share each named complex type, by the name of the type.
        /// </summary>
        internal Dictionary<string, List<string>> ElementsOfType { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the names of the named complex types whose class has been written, or could not be.
        /// </summary>
        internal HashSet<string> Emitted { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the schema file name for diagnostics.
        /// </summary>
        internal string FileName { get; }

        /// <summary>
        /// Gets the global attribute declarations, by name.
        /// </summary>
        internal Dictionary<string, MiniElement> GlobalAttributes { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the global element declarations, by name.
        /// </summary>
        internal Dictionary<string, MiniElement> GlobalElements { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the named model groups of the schema, by name.
        /// </summary>
        internal Dictionary<string, MiniElement> Groups { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets a value indicating whether a local element declaration has been bound.
        /// </summary>
        internal bool HasLocalElements { get; set; }

        /// <summary>
        /// Gets the names of the named complex types whose class is being written, which is how a type that is its
        /// own base is found.
        /// </summary>
        internal HashSet<string> InProgress { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets every member of the class of each named complex type, those of its base types first, by the name
        /// of the type.
        /// </summary>
        internal Dictionary<string, List<MemberSpec>> Members { get; } = new Dictionary<string, List<MemberSpec>>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the C# namespace.
        /// </summary>
        internal string Namespace { get; }

        /// <summary>
        /// Gets emitted POCO sources.
        /// </summary>
        internal List<(string Hint, string Source)> Pocos { get; }

        /// <summary>
        /// Gets the CLR names of the classes that are abstract or have derived classes. A value of one of them
        /// may be an instance of another class, so it is not written by a fast path.
        /// </summary>
        internal HashSet<string> PolymorphicClr { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the named simple types of the schema, by name.
        /// </summary>
        internal Dictionary<string, MiniElement> SimpleTypes { get; } = new Dictionary<string, MiniElement>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the names of the global elements that another element names as its substitution group.
        /// </summary>
        internal HashSet<string> SubstitutionHeads { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the XML target namespace.
        /// </summary>
        internal string TargetNamespace { get; }

        /// <summary>
        /// Gets the names of the named complex types and enumerations, mapped to the CLR names of their classes.
        /// </summary>
        internal Dictionary<string, string> TypeMap { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets emitted type specs.
        /// </summary>
        internal List<TypeSpec> TypeSpecs { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a bind context.
        /// </summary>
        /// <param name="namespaceName">The C# namespace.</param>
        /// <param name="fileName">The schema file name.</param>
        /// <param name="targetNamespace">The XML target namespace.</param>
        /// <param name="classNames">Used C# type names.</param>
        /// <param name="pocos">Emitted POCO sources.</param>
        /// <param name="typeSpecs">Emitted type specs.</param>
        /// <param name="diagnostics">The diagnostic sink.</param>
        internal XsdBindContext(
            string namespaceName,
            string fileName,
            string targetNamespace,
            HashSet<string> classNames,
            List<(string Hint, string Source)> pocos,
            List<TypeSpec> typeSpecs,
            List<Diagnostic> diagnostics)
        {
            Namespace = namespaceName;
            FileName = fileName;
            TargetNamespace = targetNamespace;
            ClassNames = classNames;
            Pocos = pocos;
            TypeSpecs = typeSpecs;
            Diagnostics = diagnostics;
        }

        /// <summary>
        /// Creates a context that binds with what <paramref name="source"/> knows and writes nothing to it.
        /// </summary>
        /// <param name="source">The context of the schema.</param>
        private XsdBindContext(XsdBindContext source)
        {
            AttributeGroups = source.AttributeGroups;
            BaseOf = source.BaseOf;
            ClassNames = new HashSet<string>(StringComparer.Ordinal);
            ComplexTypeOrder = source.ComplexTypeOrder;
            ComplexTypes = source.ComplexTypes;
            DerivedOf = source.DerivedOf;
            Diagnostics = new List<Diagnostic>();
            ElementClasses = source.ElementClasses;
            ElementsOfType = source.ElementsOfType;
            Emitted = source.Emitted;
            FileName = source.FileName;
            GlobalAttributes = source.GlobalAttributes;
            GlobalElements = source.GlobalElements;
            Groups = source.Groups;
            InProgress = source.InProgress;
            Members = source.Members;
            Namespace = source.Namespace;
            Pocos = new List<(string Hint, string Source)>();
            PolymorphicClr = source.PolymorphicClr;
            SimpleTypes = source.SimpleTypes;
            SubstitutionHeads = source.SubstitutionHeads;
            TargetNamespace = source.TargetNamespace;
            TypeMap = source.TypeMap;
            TypeSpecs = new List<TypeSpec>(source.TypeSpecs);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a context that binds a type the way this one would, and keeps the classes, type specs and
        /// diagnostics to itself.
        /// </summary>
        /// <returns>The context.</returns>
        /// <remarks>
        /// A schema file that another includes is also a schema file of its own, so its types are met twice. The
        /// second time the classes exist already, and only the members are wanted: a class in this file may
        /// derive from one of them.
        /// </remarks>
        internal XsdBindContext Detached()
        {
            return new XsdBindContext(this);
        }

        /// <summary>
        /// Reports something the schema says that the generated code does not act on and that changes no document.
        /// </summary>
        /// <param name="message">What is not acted on.</param>
        internal void Note(string message)
        {
            Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnsupportedSchemaConstruct, Location.None, message));
        }

        /// <summary>
        /// Reports something the schema allows that the generated types cannot read or write.
        /// </summary>
        /// <param name="message">What is not mapped.</param>
        internal void Warn(string message)
        {
            Diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.UnmappedSchemaConstruct, Location.None, message));
        }

        #endregion

    }

}
