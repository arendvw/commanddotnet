﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CommandDotNet.Directives;
using CommandDotNet.Execution;
using CommandDotNet.Logging;
using CommandDotNet.Rendering;

namespace CommandDotNet.Example
{
    internal static class Log2ConsoleDirective
    {
        /// <summary>
        /// Output internal CommandDotNet logs to the console when user specifies 'log' or 'log:{level}'.<br/>
        /// FYI: there aren't many logs at this time.
        /// </summary>
        internal static AppRunner UseLog2ConsoleDirective(this AppRunner appRunner)
        {
            return appRunner.Configure(c =>
                c.UseMiddleware(LogToConsole, MiddlewareStages.PreTokenize, int.MinValue));
        }

        private static Task<int> LogToConsole(CommandContext context, ExecutionDelegate next)
        {
            if(context.Tokens.TryGetDirective("log", out var logDirective))
            {
                var parts = logDirective.Split(new []{':', '='});
                var level = parts.Length == 2 
                    ? (LogLevel) Enum.Parse(typeof(LogLevel), parts.Last(), ignoreCase: true) 
                    : LogLevel.Trace;
                LogProvider.SetCurrentLogProvider(new ConsoleLogProvider(context.Console, level));
            }

            return next(context);
        }

        private class ConsoleLogProvider : ILogProvider
        {
            private readonly IConsole _console;
            private readonly LogLevel _level;

            public ConsoleLogProvider(IConsole console, LogLevel level)
            {
                _console = console ?? throw new ArgumentNullException(nameof(console));
                _level = level;
            }

            public Logger GetLogger(string name)
            {
                return (level, messageFunc, exception, parameters) =>
                {
                    if (level < _level)
                    {
                        return false;
                    }

                    var msg = messageFunc?.Invoke();

                    if (msg != null && (parameters?.Any() ?? false))
                    {
                        msg = string.Format(msg, parameters);
                    }

                    if (msg != null || exception != null)
                    {
                        var stream = level == LogLevel.Error || level == LogLevel.Fatal ? _console.Error : _console.Out;
                        stream.WriteLine($"{level.ToString().First()} {name} > {msg} {exception}");
                    }

                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                throw new NotImplementedException();
            }
        }
    }
}