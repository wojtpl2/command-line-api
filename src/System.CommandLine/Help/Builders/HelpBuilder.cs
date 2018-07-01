using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.CommandLine.DefaultHelpText;

namespace System.CommandLine
{
    public class HelpBuilder : IHelpBuilder
    {
        protected const int DefaultColumnGutter = 4;
        protected const int DefaultIndentationSize = 2;
        protected const int DefaultWindowWidth = 80;

        protected const int WindowMargin = 2;
        private int _indentationLevel;
        protected IConsole _console;

        public int ColumnGutter { get; } = DefaultColumnGutter;
        public int IndentationSize { get; } = DefaultIndentationSize;
        public int MaxWidth { get; } = DefaultWindowWidth;

        /// <summary>
        /// Brokers the generation and output of help text of <see cref="SymbolDefinition"/>
        /// and the <see cref="IConsole"/>
        /// </summary>
        /// <param name="console"><see cref="IConsole"/> instance to write the help text output</param>
        /// <param name="columnGutter">
        /// Number of characters to pad invocation information from their descriptions
        /// </param>
        /// <param name="indentationSize">Number of characters to indent new lines</param>
        /// <param name="maxWidth">
        /// Maximum number of characters available for each line to write to the console
        /// </param>
        public HelpBuilder(
            IConsole console,
            int? columnGutter = null,
            int? indentationSize = null,
            int? maxWidth = null)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            ColumnGutter = columnGutter ?? DefaultColumnGutter;
            IndentationSize = indentationSize ?? DefaultIndentationSize;
            MaxWidth = maxWidth ?? GetWindowWidth();
        }

        /// <inheritdoc />
        public void Write(CommandDefinition commandDefinition)
        {
            if (commandDefinition == null)
            {
                throw new ArgumentNullException(nameof(commandDefinition));
            }

            AddSynopsis(commandDefinition);
            AddUsage(commandDefinition);
            AddArguments(commandDefinition);
            AddOptions(commandDefinition);
            AddSubcommands(commandDefinition);
            AddAdditionalArguments(commandDefinition);
        }

        protected int CurrentIndentation => _indentationLevel * IndentationSize;

        /// <summary>
        /// Increases the current indentation level
        /// </summary>
        protected void Indent(int levels = 1)
        {
            _indentationLevel += levels;
        }

        /// <summary>
        /// Decreases the current indentation level
        /// </summary>
        protected void Outdent(int levels = 1)
        {
            if (_indentationLevel == 0)
            {
                throw new InvalidOperationException("Cannot outdent any further");
            }

            _indentationLevel -= levels;
        }

        /// <summary>
        /// Gets the currently available space based on the <see cref="MaxWidth"/> from the window
        /// and the current indentation level
        /// </summary>
        /// <returns>The number of characters available on the current line</returns>
        protected int GetAvailableWidth()
        {
            return MaxWidth - CurrentIndentation - WindowMargin;
        }

        /// <summary>
        /// Create a string of whitespace for the supplied number of characters
        /// </summary>
        /// <param name="width">The length of whitespace required</param>
        /// <returns>A string of <see cref="width"/> whitespace characters</returns>
        protected static string GetPadding(int width)
        {
            return new string(' ', width);
        }

        /// <summary>
        /// Writes a blank line to the console
        /// </summary>
        private void AppendBlankLine()
        {
            _console.Out.WriteLine();
        }


        /// <summary>
        /// Writes whitespace to the console based on the provided offset,
        /// defaulting to the <see cref="CurrentIndentation"/>
        /// </summary>
        /// <param name="offset">Number of characters to pad</param>
        private void AppendPadding(int? offset = null)
        {
            string padding = GetPadding(offset ?? CurrentIndentation);
            _console.Out.Write(padding);
        }

        /// <summary>
        /// Writes a new line of text to the console, padded with a supplied offset
        /// defaulting to the <see cref="CurrentIndentation"/>
        /// </summary>
        /// <param name="text">The text content to write to the console</param>
        /// <param name="offset">Number of characters to pad the text</param>
        private void AppendLine(string text, int? offset = null)
        {
            AppendPadding(offset);
            _console.Out.WriteLine(text ?? "");
        }

        /// <summary>
        /// Writes text to the console, padded with a supplied offset
        /// </summary>
        /// <param name="text">Text content to write to the console</param>
        /// <param name="offset">Number of characters to pad the text</param>
        private void AppendText(string text, int? offset = null)
        {
            AppendPadding(offset);
            _console.Out.Write(text ?? "");
        }

        /// <summary>
        /// Writes heading text to the console.
        /// </summary>
        /// <param name="heading">Heading text content to write to the console</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual void AppendHeading(string heading)
        {
            if (heading == null)
            {
                throw new ArgumentNullException(nameof(heading));
            }

            AppendLine(heading);
        }

        /// <summary>
        /// Writes a description block to the console
        /// </summary>
        /// <param name="description">Description text to write to the console</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual void AppendDescription(string description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            int availableWidth = GetAvailableWidth();
            IReadOnlyCollection<string> descriptionLines = SplitText(description, availableWidth);

            foreach (string descriptionLine in descriptionLines)
            {
                AppendLine(descriptionLine, CurrentIndentation);
            }
        }

        /// <summary>
        /// Adds columnar content for a <see cref="HelpItem"/> using the current indentation
        /// for the line, and adding the appropriate padding between the columns
        /// </summary>
        /// <param name="helpItem">
        /// Current <see cref="HelpItem" /> to write to the console
        /// </param>
        /// <param name="maxInvocationWidth">
        /// Maximum number of characters accross all <see cref="HelpItem">help items</see>
        /// occupied by the invocation text
        /// </param>
        protected void AppendHelpItem(HelpItem helpItem, int maxInvocationWidth)
        {
            if (helpItem == null)
            {
                throw new ArgumentNullException(nameof(helpItem));
            }

            AppendText(helpItem.Invocation, CurrentIndentation);

            int offset = maxInvocationWidth + ColumnGutter - helpItem.Invocation.Length;
            int availableWidth = GetAvailableWidth();
            int maxDescriptionWidth = availableWidth - maxInvocationWidth - ColumnGutter;

            IReadOnlyCollection<string> descriptionLines = SplitText(helpItem.Description, maxDescriptionWidth);
            int lineCount = descriptionLines.Count;

            AppendLine(descriptionLines.FirstOrDefault(), offset);

            if (lineCount == 1)
            {
                return;
            }

            offset = CurrentIndentation + maxInvocationWidth + ColumnGutter;

            foreach (string descriptionLine in descriptionLines.Skip(1))
            {
                AppendLine(descriptionLine, offset);
            }
        }

        /// <summary>
        /// Takes a string of text and breaks it into lines of <see cref="maxLength"/>
        /// characters. This does not preserve any formatting of the incoming text.
        /// </summary>
        /// <param name="text">Text content to split into writable lines</param>
        /// <param name="maxLength">
        /// Maximum number of characters allowed for writing the supplied <see cref="text"/>
        /// </param>
        /// <returns>
        /// Collection of lines of at most <see cref="maxLength"/> characters
        /// generated from the supplied <see cref="text"/>
        /// </returns>
        protected virtual IReadOnlyCollection<string> SplitText(string text, int maxLength)
        {
            string cleanText = Regex.Replace(text, "\\s+", " ");
            int textLength = cleanText.Length;

            if (string.IsNullOrWhiteSpace(cleanText) || textLength < maxLength)
            {
                return new[] {cleanText};
            }

            var lines = new List<string>();
            var builder = new StringBuilder();

            foreach (string item in cleanText.Split(new char[0], StringSplitOptions.RemoveEmptyEntries))
            {
                int nextLength = item.Length + builder.Length;

                if (nextLength >= maxLength)
                {
                    lines.Add(builder.ToString());
                    builder.Clear();
                }

                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(item);
            }

            if (builder.Length > 0)
            {
                lines.Add(builder.ToString());
            }

            return lines;
        }

        /// <summary>
        /// Formats the help rows for a given argument
        /// </summary>
        /// <param name="commandDef"></param>
        /// <returns>A new <see cref="HelpItem"/></returns>
        protected virtual HelpItem ArgumentFormatter(SymbolDefinition commandDef)
        {
            var argHelp = commandDef.ArgumentDefinition?.Help;

            return new HelpItem {
                Invocation = $"<{argHelp?.Name}>",
                Description = argHelp?.Description ?? "",
            };
        }

        /// <summary>
        /// Formats the help rows for a given option
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>A new <see cref="HelpItem"/></returns>
        protected virtual HelpItem OptionFormatter(SymbolDefinition symbol)
        {
            string option = string.Join(", ",  symbol.RawAliases);

            if (symbol.HasArguments && !string.IsNullOrWhiteSpace(symbol.ArgumentDefinition?.Help?.Name))
            {
                option = $"{option} <{symbol.ArgumentDefinition?.Help?.Name}>";
            }

            return new HelpItem {
                Invocation = option,
                Description = symbol.Help?.Description ?? "",
            };
        }

        /// <summary>
        /// Writes a summary, if configured, for the supplied <see cref="commandDefinition"/>
        /// </summary>
        /// <param name="commandDefinition"></param>
        protected virtual void AddSynopsis(CommandDefinition commandDefinition)
        {
            if (!commandDefinition.HasHelp)
            {
                return;
            }

            var title = $"{commandDefinition.Help.Name}:";
            HelpSection.Write(this, title, commandDefinition.Help.Description);
        }

        /// <summary>
        /// Writes the usage summary for the supplied <see cref="commandDefinition"/>
        /// </summary>
        /// <param name="commandDefinition"></param>
        protected virtual void AddUsage(CommandDefinition commandDefinition)
        {
            var usage = new List<string>();

            var subcommands = commandDefinition
                .RecurseWhileNotNull(commandDef => commandDef.Parent)
                .Reverse();

            foreach (CommandDefinition subcommand in subcommands)
            {
                usage.Add(subcommand.Name);

                string subcommandArgHelp = GetArgumentHelp(subcommand);
                if (subcommand != commandDefinition && subcommandArgHelp != null)
                {
                    usage.Add($"<{subcommandArgHelp}>");
                }
            }

            var hasOptionHelp = commandDefinition.SymbolDefinitions
                .OfType<OptionDefinition>()
                .Any(symbolDef => symbolDef.HasHelp);

            if (hasOptionHelp)
            {
                usage.Add(Usage.Options);
            }

            var commandArgHelp = GetArgumentHelp(commandDefinition);
            if (commandArgHelp != null)
            {
                usage.Add($"<{commandArgHelp}>");
            }

            var hasCommandHelp = commandDefinition.SymbolDefinitions
                .OfType<CommandDefinition>()
                .Any(f => f.HasHelp);

            if (hasCommandHelp)
            {
                usage.Add(Usage.Command);
            }

            if (!commandDefinition.TreatUnmatchedTokensAsErrors)
            {
                usage.Add(Usage.AdditionalArguments);
            }

            HelpSection.Write(this, Usage.Title, string.Join(" ", usage));
        }

        /// <summary>
        /// Writes the arguments, if any, for the supplied <see cref="commandDefinition"/>
        /// </summary>
        /// <param name="commandDefinition"></param>
        protected virtual void AddArguments(CommandDefinition commandDefinition)
        {
            var arguments = new List<CommandDefinition>();

            if (commandDefinition.Parent?.HasArguments == true && commandDefinition.Parent.HasHelp)
            {
                arguments.Add(commandDefinition.Parent);
            }

            if (commandDefinition.HasArguments && commandDefinition.HasHelp)
            {
                arguments.Add(commandDefinition);
            }

            HelpSection.Write(this, Arguments.Title, arguments, ArgumentFormatter);
        }

        /// <summary>
        /// Writes the <see cref="OptionDefinition"/> help content, if any,
        /// for the supplied <see cref="commandDefinition"/>
        /// </summary>
        /// <param name="commandDefinition"></param>
        protected virtual void AddOptions(SymbolDefinition commandDefinition)
        {
            var options = commandDefinition
                .SymbolDefinitions
                .OfType<OptionDefinition>()
                .Where(opt => opt.HasHelp)
                .ToArray();

            HelpSection.Write(this, Options.Title, options, OptionFormatter);
        }

        /// <summary>
        /// Writes the help content of the <see cref="CommandDefinition"/> subcommands, if any,
        /// for the supplied <see cref="commandDefinition"/>
        /// </summary>
        /// <param name="commandDefinition"></param>
        protected virtual void AddSubcommands(SymbolDefinition commandDefinition)
        {
            var subcommands = commandDefinition
                .SymbolDefinitions
                .OfType<CommandDefinition>()
                .Where(subCommand => subCommand.HasHelp)
                .ToArray();

            HelpSection.Write(this, Commands.Title, subcommands, OptionFormatter);
        }

        protected virtual void AddAdditionalArguments(CommandDefinition commandDefinition)
        {
            if (commandDefinition.TreatUnmatchedTokensAsErrors)
            {
                return;
            }

            HelpSection.Write(this, AdditionalArguments.Title, AdditionalArguments.Description);
        }

        private static string GetArgumentHelp(SymbolDefinition symbolDef)
        {
            var argDef = symbolDef?.ArgumentDefinition;
            var argHelp = argDef?.Help?.Name;

            if (argDef?.HasHelp != true || string.IsNullOrEmpty(argHelp))
            {
                return null;
            }

            return argHelp;
        }

        /// <summary>
        /// Gets the number of characters of the current <see cref="IConsole"/> window if necessary
        /// </summary>
        /// <returns>
        /// The current width (number of characters) of the configured <see cref="IConsole"/>,
        /// or the <see cref="DefaultWindowWidth"/> if unavailable
        /// </returns>
        private int GetWindowWidth()
        {
            try
            {
                return _console.WindowWidth;
            }
            catch (Exception exception) when (exception is ArgumentOutOfRangeException || exception is IOException)
            {
                return DefaultWindowWidth;
            }
        }

        protected class HelpItem
        {
            public string Invocation { get; set; }

            public string Description { get; set; }
        }

        private static class HelpSection
        {
            public static void Write(
                HelpBuilder builder,
                string title,
                string description)
            {
                if (!ShouldWrite(description, null))
                {
                    return;
                }

                AppendHeading(builder, title);
                builder.Indent();
                AddDescription(builder, description);
                builder.Outdent();
                builder.AppendBlankLine();
            }

            public static void Write(
                HelpBuilder builder,
                string title,
                IReadOnlyCollection<SymbolDefinition> usageItems = null,
                Func<SymbolDefinition, HelpItem> formatter = null,
                string description = null)
            {
                if (!ShouldWrite(description, usageItems))
                {
                    return;
                }

                AppendHeading(builder, title);
                builder.Indent();
                AddDescription(builder, description);
                AddInvocation(builder, usageItems, formatter);
                builder.Outdent();
                builder.AppendBlankLine();
            }

            private static bool ShouldWrite(string description, IReadOnlyCollection<SymbolDefinition> usageItems)
            {
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return true;
                }

                return usageItems?.Any() == true;
            }

            private static void AppendHeading(HelpBuilder builder, string title)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return;
                }

                builder.AppendHeading(title);
            }

            private static void AddDescription(HelpBuilder builder, string description)
            {
                if (string.IsNullOrWhiteSpace(description))
                {
                    return;
                }

                builder.AppendDescription(description);
            }

            private static void AddInvocation(
                HelpBuilder builder,
                IReadOnlyCollection<SymbolDefinition> symbolDefinitions,
                Func<SymbolDefinition, HelpItem> formatter)
            {
                if (symbolDefinitions?.Any() != true)
                {
                    return;
                }

                List<HelpItem> helpItems = symbolDefinitions
                    .Select(formatter).ToList();

                int maxWidth = helpItems
                    .Select(line => line.Invocation.Length)
                    .OrderByDescending(textLength => textLength)
                    .First();

                foreach (HelpItem helpItem in helpItems)
                {
                    builder.AppendHelpItem(helpItem, maxWidth);
                }
            }
        }
    }
}
