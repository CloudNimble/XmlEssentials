// The package is packed and consumed with the SDK that built the tests, so this test exists only in the net11.0 build.
#if NET11_0
using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Packs the library as it is published, and builds and runs an application that has nothing but the
    /// package: no reference to a project of this solution.
    /// </summary>
    /// <remarks>
    /// Every other project here references the source generator as a project, so only this test sees what the
    /// package itself carries. A package that held the generator twice built every sample and broke every
    /// application that declared a context.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public class XmlPackageTests
    {

        #region Public Methods

        [TestMethod]
        [Timeout(600000)]
        public void Package_ConsumedByAnApplicationWithAGeneratedContext_BuildsAndWritesTheExactXml()
        {
            var src = XmlAotPublishTests.FindSrc();
            var configuration = typeof(XmlPackageTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
                ?? "Release";
            var version = "0.0.0-packagetest." + Guid.NewGuid().ToString("N");
            var root = Path.Combine(Path.GetTempPath(), "xmlessentials-package-" + Guid.NewGuid().ToString("N"));
            var feed = Path.Combine(root, "feed");
            var app = Path.Combine(root, "app");
            Directory.CreateDirectory(feed);
            Directory.CreateDirectory(app);
            try
            {
                var library = Path.Combine(src, "CloudNimble.XmlEssentials", "CloudNimble.XmlEssentials.csproj");
                var pack = XmlAotPublishTests.Run(
                    "dotnet",
                    "pack \"" + library + "\" -c " + configuration + " --no-build --disable-build-servers --output \"" + feed + "\" -p:PackageVersion=" + version,
                    src,
                    300000);
                pack.ExitCode.Should().Be(0, pack.StandardOutput + Environment.NewLine + pack.StandardError);

                // The application is outside the repository, so it inherits no props, targets or global.json
                // from it, and it restores into a folder of its own, so the package never reaches the machine's cache.
                File.WriteAllText(Path.Combine(app, "nuget.config"),
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                    + "<configuration>\n"
                    + "  <packageSources>\n"
                    + "    <clear />\n"
                    + "    <add key=\"packed\" value=\"" + feed + "\" />\n"
                    + "  </packageSources>\n"
                    + "</configuration>\n");
                File.WriteAllText(Path.Combine(app, "app.csproj"),
                    "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
                    + "  <PropertyGroup>\n"
                    + "    <OutputType>Exe</OutputType>\n"
                    + "    <TargetFramework>net11.0</TargetFramework>\n"
                    + "    <Nullable>enable</Nullable>\n"
                    + "    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n"
                    + "    <RestorePackagesPath>$(MSBuildThisFileDirectory)packages</RestorePackagesPath>\n"
                    + "  </PropertyGroup>\n"
                    + "  <ItemGroup>\n"
                    + "    <PackageReference Include=\"XmlEssentials\" Version=\"" + version + "\" />\n"
                    + "  </ItemGroup>\n"
                    + "</Project>\n");
                File.WriteAllText(Path.Combine(app, "Program.cs"),
                    "using System;\n"
                    + "using CloudNimble.Text.Xml;\n"
                    + "using CloudNimble.Text.Xml.Serialization;\n"
                    + "\n"
                    + "Console.WriteLine(XmlSerializer.Serialize(new Person { Name = \"Ada\", Age = 36 }, AppXmlContext.Default.Person));\n"
                    + "\n"
                    + "public sealed class Person\n"
                    + "{\n"
                    + "    public int Age { get; set; }\n"
                    + "    public string? Name { get; set; }\n"
                    + "}\n"
                    + "\n"
                    + "[XmlSourceGenerationOptions(OmitXmlDeclaration = true)]\n"
                    + "[XmlSerializable<Person>]\n"
                    + "public partial class AppXmlContext : XmlSerializerContext\n"
                    + "{\n"
                    + "}\n");

                var build = XmlAotPublishTests.Run("dotnet", "build -c Release --disable-build-servers", app, 300000);
                build.ExitCode.Should().Be(0, build.StandardOutput + Environment.NewLine + build.StandardError);

                var run = XmlAotPublishTests.Run("dotnet", "\"" + Path.Combine(app, "bin", "Release", "net11.0", "app.dll") + "\"", app, 60000);
                run.ExitCode.Should().Be(0, run.StandardOutput + Environment.NewLine + run.StandardError);
                run.StandardError.Should().Be("");
                run.StandardOutput.Should().Be("<Person><Age>36</Age><Name>Ada</Name></Person>" + Environment.NewLine);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        #endregion

    }

}
#endif
