﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandDotNet.Execution;
using CommandDotNet.Rendering;

namespace CommandDotNet.Parsing
{
    internal static class PipedInputMiddleware
    {
        internal static AppRunner EnablePipedInput(this AppRunner appRunner)
        {
            return appRunner.Configure(c => c.UseMiddleware(InjectPipedInput, MiddlewareStages.PostParseInputPreBindValues));
        }

        private static Task<int> InjectPipedInput(CommandContext ctx, Func<CommandContext, Task<int>> next)
        {
            if (ctx.Console.IsInputRedirected)
            {
                // supporting only the list operand for a command gives us a few benefits
                // 1. there can be only one list operand per command.
                //    no need to enforce this only one argument has EnablePipedInput=true
                // 2. no need to handle case where a single value operand has EnablePipedInput=true
                //    otherwise we either drop all but the first value or throw an exception
                //    both have pros & cons
                // 3. List operands are specified and provided last, avoiding awkward cases
                //    where piped input is provided for arguments positioned before others.
                //    We'd need to inject additional middleware to inject tokens in this case.
                // 4. piped values can be merged with args passed to the command.
                //    this can become an option passed into appBuilder.EnablePipedInput(...)
                //    if a need arises to throw instead of merge
                var operand = ctx.ParseResult.TargetCommand.Operands
                    .FirstOrDefault(o => o.Arity.AllowsZeroOrMore());

                if (operand != null)
                {
                    var pipedInput = ctx.ContextData.GetOrAdd(() => GetPipedInput(ctx.Console));
                    ctx.ParseResult.ArgumentValues.GetOrAdd(operand).AddRange(pipedInput);
                }
            }

            return next(ctx);
        }

        public static ICollection<string> GetPipedInput(IConsole console)
        {
            if (console.IsInputRedirected)
            {
                var input = console.In.ReadToEnd().TrimEnd('\r', '\n');
                return input
                    .Split(
                        new[] {"\r\n", "\r", "\n"},
                        StringSplitOptions.None)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            return new string[0];
        }

        private class PipedInput
        {
            public List<string> Input { get; }

            public PipedInput(List<string> input)
            {
                Input = input;
            }
        }
    }
}