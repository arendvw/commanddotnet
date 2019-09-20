﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandDotNet.ClassModeling.Definitions;
using CommandDotNet.Execution;
using CommandDotNet.Extensions;

namespace CommandDotNet.ClassModeling
{
    internal static class ClassModelingMiddleware
    {
        internal static AppRunner UseClassDefMiddleware(this AppRunner appRunner, Type rootCommandType)
        {
            return appRunner.Configure(c =>
            {
                c.UseMiddleware((context, next) => BuildMiddleware(rootCommandType, context, next), MiddlewareStages.Build);
                c.UseMiddleware(AssembleInvocationPipelineMiddleware, MiddlewareStages.ParseInput);
                c.UseMiddleware(BindValuesMiddleware.BindValues, MiddlewareStages.BindValues);
                c.UseMiddleware(ResolveInstancesMiddleware.ResolveInstances, MiddlewareStages.BindValues);
                c.UseMiddleware(InvokeInvocationPipelineMiddleware, MiddlewareStages.Invoke, int.MaxValue);
            });
        }

        private static Task<int> BuildMiddleware(Type rootCommandType, CommandContext commandContext, ExecutionDelegate next)
        {
            commandContext.RootCommand = ClassCommandDef.CreateRootCommand(rootCommandType, commandContext);
            return next(commandContext);
        }

        private static Task<int> AssembleInvocationPipelineMiddleware(CommandContext commandContext, ExecutionDelegate next)
        {
            var commandDef = commandContext.ParseResult.TargetCommand.Services.Get<ICommandDef>();
            if (commandDef != null)
            {
                var pipeline = commandContext.InvocationPipeline;
                pipeline.TargetCommand = new InvocationStep
                {
                    Command = commandDef.Command,
                    Invocation = commandDef.InvokeMethodDef
                };
                commandDef.Command.GetParentCommands(includeCurrent:false)
                    .Where(cmd => cmd.HasInterceptor)
                    .Reverse()
                    .Select(cmd => (cmd, def: cmd.Services.Get<ICommandDef>()))
                    .Where(c => c.def != null) // in case command is defined by a different middleware
                    .ForEach(c =>
                    {
                        pipeline.AncestorInterceptors.Add(new InvocationStep
                        {
                            Command = c.cmd,
                            Invocation = c.def.InterceptorMethodDef
                        });
                    });
            }

            return next(commandContext);
        }

        private static Task<int> InvokeInvocationPipelineMiddleware(CommandContext commandContext, ExecutionDelegate _)
        {
            Task<int> Invoke(InvocationStep step, CommandContext context, ExecutionDelegate next, bool isCommand)
            {
                var result = step.Invocation.Invoke(context, step.Instance, next);
                return isCommand 
                    ? result.GetResultCodeAsync()
                    : (Task<int>)result;
            }

            var pipeline = commandContext.InvocationPipeline;

            return pipeline.AncestorInterceptors
                .Select(i => new ExecutionMiddleware((ctx, next) => Invoke(i, ctx, next, false)))
                .Concat(new ExecutionMiddleware((ctx, next) => Invoke(pipeline.TargetCommand, ctx, next, true)).ToEnumerable())
                .InvokePipeline(commandContext);
        }
    }
}