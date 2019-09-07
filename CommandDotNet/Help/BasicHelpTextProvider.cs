﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandDotNet.Help
{
    internal class BasicHelpTextProvider : HelpTextProvider
    {
        public BasicHelpTextProvider(AppSettings appSettings) : base(appSettings)
        {
        }

        protected override string FormatSectionHeader(string header)
            => "usage".Equals(header, StringComparison.OrdinalIgnoreCase)
                ? $"{header}:"
                : $"{header}:{Environment.NewLine}";

        protected override string SectionArguments<T>(ICollection<T> arguments)
        {
            var nameMaxLength = arguments.Max(a => ArgumentName(a)?.Length) ?? 0;

            var sb = new StringBuilder();
            foreach (var argument in arguments)
            {
                sb.AppendLine(Row((nameMaxLength, ArgumentName(argument)), (-1, argument.Description)));
            }
            return sb.ToString();
        }
    }
}