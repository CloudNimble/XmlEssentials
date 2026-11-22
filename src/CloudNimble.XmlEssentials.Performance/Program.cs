using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Entry point for BenchmarkDotNet.
    /// </summary>
    public static class Program
    {

        #region Public Methods

        /// <summary>
        /// Runs the benchmark suite.
        /// </summary>
        /// <param name="args">Command-line arguments forwarded to BenchmarkDotNet.</param>
        /// <returns>The BenchmarkDotNet exit code.</returns>
        public static int Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance))
                .HideColumns("Job");
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
            return 0;
        }

        #endregion

    }

}
