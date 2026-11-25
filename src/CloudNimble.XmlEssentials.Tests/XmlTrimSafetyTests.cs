using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// The rules that keep the library safe to trim and to compile ahead of time: it hides no warning, the members
    /// that need reflection say so, the members that take generated metadata never do, and generated code uses none
    /// of the former.
    /// </summary>
    [TestClass]
    public class XmlTrimSafetyTests
    {

        #region Fields

        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        #endregion

        #region Public Methods

        [TestMethod]
        public void RuntimeLibrary_HidesNoTrimmingOrAotWarning()
        {
            // A suppression says "the tool is wrong here", and nothing checks that it stays true. The compiler
            // keeps the attribute in the assembly, so its absence can be read from the assembly that ships.
            var assembly = typeof(XmlSerializer).Assembly;
            var suppressed = new List<string>();
            if (assembly.IsDefined(typeof(UnconditionalSuppressMessageAttribute), inherit: false)
                || assembly.ManifestModule.IsDefined(typeof(UnconditionalSuppressMessageAttribute), inherit: false))
            {
                suppressed.Add(assembly.GetName().Name!);
            }

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsDefined(typeof(UnconditionalSuppressMessageAttribute), inherit: false))
                {
                    suppressed.Add(type.FullName!);
                }

                foreach (var member in type.GetMembers(Declared))
                {
                    if (member.IsDefined(typeof(UnconditionalSuppressMessageAttribute), inherit: false))
                    {
                        suppressed.Add(type.FullName + "." + member.Name);
                    }
                }
            }

            suppressed.Should().BeEmpty();
        }

        [TestMethod]
        public void SourceAndBuildFiles_TurnNoTrimmingOrAotWarningOff()
        {
            // The attribute is one way to hide a warning. A pragma in the source and a setting in a build file are
            // the others, and turning the analyzers off, or letting their warnings pass, hides all of them at once.
            var src = FindSrc();
            var hidden = new List<string>();
            foreach (var project in (string[])["CloudNimble.XmlEssentials", "CloudNimble.XmlEssentials.SourceGeneration"])
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(src, project), "*.cs", System.IO.SearchOption.AllDirectories))
                {
                    var relative = System.IO.Path.GetRelativePath(src, file).Replace('\\', '/');
                    if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var number = 0;
                    foreach (var line in System.IO.File.ReadLines(file))
                    {
                        number++;
                        var text = line.TrimStart();
                        if ((text.StartsWith("#pragma warning disable", StringComparison.Ordinal) && text.Contains("IL", StringComparison.Ordinal))
                            || text.StartsWith("[UnconditionalSuppressMessage", StringComparison.Ordinal)
                            || text.StartsWith("[assembly: UnconditionalSuppressMessage", StringComparison.Ordinal)
                            || text.StartsWith("[module: UnconditionalSuppressMessage", StringComparison.Ordinal))
                        {
                            hidden.Add(relative + "(" + number + "): " + text);
                        }
                    }
                }
            }

            var settings = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var file in (string[])[
                "Directory.Build.props",
                "CloudNimble.XmlEssentials/CloudNimble.XmlEssentials.csproj",
                "CloudNimble.XmlEssentials.SourceGeneration/CloudNimble.XmlEssentials.SourceGeneration.csproj",
                "CloudNimble.XmlEssentials.Tests.Aot/CloudNimble.XmlEssentials.Tests.Aot.csproj"])
            {
                using var document = XmlDocument.Parse(System.IO.File.ReadAllBytes(System.IO.Path.Combine(src, file)));
                AddSettings(document.RootElement, file, settings, hidden);
            }

            hidden.Should().BeEmpty();
            settings["CloudNimble.XmlEssentials/CloudNimble.XmlEssentials.csproj:IsAotCompatible"].Should().Equal("true");
            settings["CloudNimble.XmlEssentials/CloudNimble.XmlEssentials.csproj:EnableTrimAnalyzer"].Should().Equal("true");
            settings["Directory.Build.props:TreatWarningsAsErrors"].Should().Equal("true");
            settings["CloudNimble.XmlEssentials.Tests.Aot/CloudNimble.XmlEssentials.Tests.Aot.csproj:PublishAot"].Should().Equal("true");
            settings.Keys.Where(static key => key.EndsWith(":TreatWarningsAsErrors", StringComparison.Ordinal)).Should().Equal("Directory.Build.props:TreatWarningsAsErrors");
        }

        [TestMethod]
        public void XmlSerializerMethods_SayTheyNeedReflection_ExactlyWhenTheyTakeNoGeneratedMetadata()
        {
            // An overload that is handed type info, or a context to get it from, reads and writes from that
            // metadata, so it carries no warning. Every other overload finds the type by reflection and says so.
            var wrong = new List<string>();
            foreach (var method in typeof(XmlSerializer).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                var takesMetadata = method.GetParameters().Any(static parameter =>
                    typeof(XmlTypeInfo).IsAssignableFrom(parameter.ParameterType) || typeof(XmlSerializerContext).IsAssignableFrom(parameter.ParameterType));
                var saysUnreferenced = method.IsDefined(typeof(RequiresUnreferencedCodeAttribute), inherit: false);
                var saysDynamic = method.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false);
                if (saysUnreferenced != saysDynamic || saysUnreferenced == takesMetadata)
                {
                    wrong.Add(method.ToString()!);
                }
            }

            wrong.Should().BeEmpty();
        }

        [TestMethod]
        public void MembersThatBringInTheReflectionResolver_SayTheyNeedReflection()
        {
            static bool Says(MemberInfo member) =>
                member.IsDefined(typeof(RequiresUnreferencedCodeAttribute), inherit: false) && member.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false);

            Says(typeof(DefaultXmlTypeInfoResolver)).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetProperty(nameof(XmlSerializerOptions.Default))!.GetMethod!).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetProperty(nameof(XmlSerializerOptions.Web))!.GetMethod!).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetProperty(nameof(XmlSerializerOptions.Strict))!.GetMethod!).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetProperty(nameof(XmlSerializerOptions.Bcl))!.GetMethod!).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetMethod(nameof(XmlSerializerOptions.GetConverter))!).Should().BeTrue();
            Says(typeof(XmlSerializerOptions).GetMethod(nameof(XmlSerializerOptions.MakeReadOnly), [typeof(bool)])!).Should().BeTrue();

            // What generated code and a caller with a context use carries no warning, so it may not need one.
            Says(typeof(XmlSerializerOptions).GetProperty(nameof(XmlSerializerOptions.TypeInfoResolver))!.GetMethod!).Should().BeFalse();
            Says(typeof(XmlSerializerOptions).GetMethod(nameof(XmlSerializerOptions.MakeReadOnly), Type.EmptyTypes)!).Should().BeFalse();
            Says(typeof(XmlSerializerOptions).GetMethod(nameof(XmlSerializerOptions.GetTypeInfo), [typeof(Type)])!).Should().BeFalse();
            typeof(XmlMetadataServices).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(static method => method.IsDefined(typeof(RequiresUnreferencedCodeAttribute), inherit: false) || method.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false))
                .Select(static method => method.ToString())
                .Should().BeEmpty();
            Says(typeof(XmlSerializerContext)).Should().BeFalse();
        }

        [TestMethod]
        public void GeneratedCodeCheck_FindsEachUseOfAMemberThatNeedsReflection()
        {
            // Every run of the generators in these tests goes through GeneratedCodeChecks. This proves the check
            // sees a method, a property and a type that need reflection, so that its silence means something.
            var tree = CSharpSyntaxTree.ParseText("""
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public static class Planted
                {
                    public static string Method() => XmlSerializer.Serialize(1);
                    public static object Property() => XmlSerializerOptions.Web;
                    public static object Type() => DefaultXmlTypeInfoResolver.Instance;
                    public static string Safe(XmlTypeInfo<int> typeInfo) => XmlSerializer.Serialize(1, typeInfo);
                }
                """, new CSharpParseOptions(LanguageVersion.Preview), path: "Planted.g.cs");
            var compilation = CSharpCompilation.Create("Planted", [tree], GeneratorTestHost.MetadataReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error).Select(static diagnostic => diagnostic.ToString()).Should().BeEmpty();

            GeneratedCodeChecks.UsesOfReflection(compilation, ImmutableArray.Create(tree)).Should().Equal(
                "Planted.g.cs: 'XmlSerializer.Serialize(1)' is CloudNimble.Text.Xml.XmlSerializer.Serialize<int>(int, CloudNimble.Text.Xml.XmlSerializerOptions?), which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'XmlSerializer.Serialize' is CloudNimble.Text.Xml.XmlSerializer.Serialize<int>(int, CloudNimble.Text.Xml.XmlSerializerOptions?), which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'Serialize' is CloudNimble.Text.Xml.XmlSerializer.Serialize<int>(int, CloudNimble.Text.Xml.XmlSerializerOptions?), which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'XmlSerializerOptions.Web' is CloudNimble.Text.Xml.XmlSerializerOptions.Web, which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'Web' is CloudNimble.Text.Xml.XmlSerializerOptions.Web, which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'DefaultXmlTypeInfoResolver.Instance' is CloudNimble.Text.Xml.Serialization.DefaultXmlTypeInfoResolver.Instance, which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'DefaultXmlTypeInfoResolver' is CloudNimble.Text.Xml.Serialization.DefaultXmlTypeInfoResolver, which has RequiresUnreferencedCodeAttribute",
                "Planted.g.cs: 'Instance' is CloudNimble.Text.Xml.Serialization.DefaultXmlTypeInfoResolver.Instance, which has RequiresUnreferencedCodeAttribute");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Records the build settings that decide whether a trimming or Native AOT warning is seen, and each one that hides such a warning.
        /// </summary>
        /// <param name="element">An element of a project file.</param>
        /// <param name="file">The path of the file from the source folder.</param>
        /// <param name="settings">The values of the settings found, by file and name.</param>
        /// <param name="hidden">The list a setting that names an <c>IL</c> warning is added to.</param>
        internal static void AddSettings(XmlElement element, string file, Dictionary<string, List<string>> settings, List<string> hidden)
        {
            foreach (var child in element.EnumerateChildren())
            {
                if (child.ValueKind is not (XmlValueKind.Element or XmlValueKind.EmptyElement))
                {
                    continue;
                }

                var name = child.LocalName;
                if (name is "NoWarn" or "WarningsNotAsErrors" or "MSBuildWarningsAsMessages")
                {
                    var value = child.GetString() ?? "";
                    if (value.Contains("IL", StringComparison.Ordinal))
                    {
                        hidden.Add(file + ": <" + name + ">" + value + "</" + name + ">");
                    }
                }
                else if (name is "IsAotCompatible" or "EnableTrimAnalyzer" or "EnableAotAnalyzer" or "TreatWarningsAsErrors" or "PublishAot" or "SuppressTrimAnalysisWarnings")
                {
                    if (!settings.TryGetValue(file + ":" + name, out var values))
                    {
                        settings.Add(file + ":" + name, values = []);
                    }

                    values.Add(child.GetString() ?? "");
                }
                else
                {
                    AddSettings(child, file, settings, hidden);
                }
            }
        }

        /// <summary>
        /// Finds the source folder, which holds the build files, from the folder the tests run in.
        /// </summary>
        /// <returns>The path of the folder that holds <c>Directory.Build.props</c> and the projects.</returns>
        internal static string FindSrc()
        {
            for (var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "CloudNimble.XmlEssentials", "CloudNimble.XmlEssentials.csproj")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("Could not find the source folder from " + AppContext.BaseDirectory);
        }

        #endregion

    }

}
