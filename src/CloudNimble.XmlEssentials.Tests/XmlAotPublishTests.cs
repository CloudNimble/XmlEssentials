// CloudNimble.XmlEssentials.Tests.Aot targets net11.0, so this test exists only in the net11.0 build of the tests.
#if NET11_0
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Publishes <c>CloudNimble.XmlEssentials.Tests.Aot</c> as Native AOT, runs the native executable, and
    /// asserts the exact Person XML, Order XML, and success line.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class XmlAotPublishTests
    {

        #region Public Methods

        [TestMethod]
        [Timeout(600000)]
        public void NativeAotSmoke_PublishesRunsAndPrintsTheExactSuccessLine()
        {
            var src = FindSrc();
            var configuration = typeof(XmlAotPublishTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
                ?? "Release";
            var rid = RuntimeInformation.RuntimeIdentifier;
            var csproj = Path.Combine(src, "CloudNimble.XmlEssentials.Tests.Aot", "CloudNimble.XmlEssentials.Tests.Aot.csproj");
            File.Exists(csproj).Should().BeTrue();

            var publish = Run(
                "dotnet",
                "publish \"" + csproj + "\" -c " + configuration + " -r " + rid,
                src,
                600000);
            publish.ExitCode.Should().Be(0, publish.StandardOutput + Environment.NewLine + publish.StandardError);

            // The compiler and the native compiler report what is unsafe to trim or to compile ahead of time as
            // IL warnings. The build treats warnings as errors, and this says the same thing in the open: none.
            (publish.StandardOutput + Environment.NewLine + publish.StandardError)
                .Split('\n')
                .Where(static line => line.Contains(" IL2", StringComparison.Ordinal) || line.Contains(" IL3", StringComparison.Ordinal))
                .Should().BeEmpty();

            var exe = Path.Combine(
                src,
                "CloudNimble.XmlEssentials.Tests.Aot",
                "bin",
                configuration,
                "net11.0",
                rid,
                "publish",
                "CloudNimble.XmlEssentials.Tests.Aot.exe");
            File.Exists(exe).Should().BeTrue();
            File.Exists(Path.ChangeExtension(exe, ".dll")).Should().BeFalse();
            File.Exists(Path.Combine(Path.GetDirectoryName(exe)!, "coreclr.dll")).Should().BeFalse();

            var smoke = Run(exe, "", Path.GetDirectoryName(exe)!, 60000);
            smoke.ExitCode.Should().Be(0, smoke.StandardOutput + Environment.NewLine + smoke.StandardError);
            smoke.StandardError.Should().Be("");
            smoke.StandardOutput.Should().Be(
                "<Person><Age>36</Age><Name>Ada</Name></Person>" + Environment.NewLine
                + "<Order xmlns=\"urn:order\" currency=\"USD\"><Id>A-1</Id><Status>Open</Status><Qty>2</Qty><Line><Sku>sku</Sku><Qty>1</Qty></Line></Order>" + Environment.NewLine
                + "<Inventory><Any>5</Any><Codes>1</Codes><Codes>2</Codes><Counts><bolts>40</bolts></Counts><Notes>n</Notes><Pending>7</Pending><Pending>8</Pending><Raw><part id=\"9\"/></Raw><Sizes>3</Sizes><Tags>new</Tags></Inventory>" + Environment.NewLine
                + "XmlEssentials AOT smoke passed." + Environment.NewLine);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Walks up from the test output directory to the <c>src</c> folder that contains Tests.Aot.
        /// </summary>
        /// <returns>The src path.</returns>
        internal static string FindSrc()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CloudNimble.XmlEssentials.Tests.Aot", "CloudNimble.XmlEssentials.Tests.Aot.csproj")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Could not find src from " + AppContext.BaseDirectory);
        }

        /// <summary>
        /// Runs <paramref name="fileName"/> and captures stdout, stderr, and the exit code.
        /// </summary>
        /// <param name="fileName">The executable.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="timeoutMs">How long to wait.</param>
        /// <returns>The captured result.</returns>
        internal static (int ExitCode, string StandardOutput, string StandardError) Run(
            string fileName,
            string arguments,
            string workingDirectory,
            int timeoutMs)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    stderr.AppendLine(e.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(fileName + " " + arguments + " did not exit within " + timeoutMs + " ms.");
            }

            process.WaitForExit();
            return (process.ExitCode, stdout.ToString(), stderr.ToString());
        }

        #endregion

    }

}
#endif
