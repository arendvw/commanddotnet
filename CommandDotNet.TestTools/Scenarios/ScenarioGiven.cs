using System;
using CommandDotNet.Rendering;

namespace CommandDotNet.TestTools.Scenarios
{
    public class ScenarioGiven
    {
        /// <summary>Set this to override the default <see cref="AppSettings"/></summary>
        public AppSettings AppSettings { get; set; }

        /// <summary>
        /// Use this delegate to mimic input in response to a <see cref="IConsole"/>In.ReadLine().
        /// Use <see cref="TestConsole"/>.Out.ToString() to get the output up to that point.
        /// </summary>
        public Func<TestConsole, string> OnReadLine { get; set; }

        /// <summary>Use to mimic piped input from the shell.</summary>
        public string[] PipedInput { get; set; }
    }
}