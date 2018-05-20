// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests
{
    public class OptionTests
    {
        [Fact]
        public void When_an_option_has_only_one_alias_then_that_alias_is_its_name()
        {
            var option = new OptionDefinition(new[] { "myname" }, "");

            option.Name.Should().Be("myname");
        }

        [Fact]
        public void When_an_option_has_several_aliases_then_the_longest_alias_is_its_name()
        {
            var option = new OptionDefinition(new[] { "myname", "m" }, "");

            option.Name.Should().Be("myname");
        }

        [Fact]
        public void Option_names_do_not_contain_prefix_characters()
        {
            var option = new OptionDefinition(new[] { "--myname", "m" }, "");

            option.Name.Should().Be("myname");
        }

        [Fact]
        public void Option_aliases_are_case_sensitive()
        {
            var option = new OptionDefinition(new[] { "-o" }, "");

            option.HasAlias("O").Should().BeFalse();
        }

        [Fact]
        public void HasAlias_accepts_prefixed_short_value()
        {
            var option = new OptionDefinition(
                new[] { "-o", "--option" }, "",
                    ArgumentDefinition.None);

            option.HasAlias("-o").Should().BeTrue();
        }

        [Fact]
        public void HasAlias_accepts_unprefixed_short_value()
        {
            var option = new OptionDefinition(
                new[] { "-o", "--option" }, "",
                    ArgumentDefinition.None);

            option.HasAlias("o").Should().BeTrue();
        }

        [Fact]
        public void HasAlias_accepts_prefixed_long_value()
        {
            var option = new OptionDefinition(
                new[] { "-o", "--option" }, "",
                    ArgumentDefinition.None);

            option.HasAlias("--option").Should().BeTrue();
        }

        [Fact]
        public void HasAlias_accepts_unprefixed_long_value()
        {
            var option = new OptionDefinition(
                new[] { "-o", "--option" }, "",
                ArgumentDefinition.None);

            option.HasAlias("option").Should().BeTrue();
        }

        [Fact]
        public void It_is_not_necessary_to_specify_a_prefix_when_adding_an_option()
        {
            var option = new OptionDefinition(
                new[] { "o" }, "",
                ArgumentDefinition.None);

            option.HasAlias("o").Should().BeTrue();
            option.HasAlias("-o").Should().BeTrue();
        }

        [Fact]
        public void An_option_must_have_at_least_one_alias()
        {
            Action create = () => new OptionDefinition(Array.Empty<string>(), "",     ArgumentDefinition.None);

            create.Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("An option must have at least one alias.");
        }

        [Fact]
        public void An_option_cannot_have_an_empty_alias()
        {
            Action create = () => new OptionDefinition(new[] { "" }, "",     ArgumentDefinition.None);

            create.Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("An option alias cannot be null, empty, or consist entirely of whitespace.");
        }

        [Fact]
        public void An_option_cannot_have_an_alias_consisting_entirely_of_whitespace()
        {
            Action create = () => new OptionDefinition(new[] { "  \t" }, "",     ArgumentDefinition.None);

            create.Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("An option alias cannot be null, empty, or consist entirely of whitespace.");
        }

        [Fact]
        public void Raw_aliases_are_exposed_by_an_option()
        {
            var option = new OptionDefinition(
                new[] {"-h", "--help", "/?"},
                "",
                argumentDefinition: null);

            option.RawAliases
                  .Should()
                  .BeEquivalentTo("-h", "--help", "/?");
        }

        [Fact]
        public void When_option_argument_is_provided_without_option_name_argument_position_is_assumed()
        {
            var result = new ParserBuilder()
                .AddOption("-a", "", a => a.ExactlyOne())
                .Build()
                .Parse("value-for-a");

            result.ValueForOption("-a").Should().Be("value-for-a");
        }

        [Fact]
        public void When_multiple_option_arguments_are_provided_without_option_name_argument_positions_are_assumed()
        {
            var result = new ParserBuilder()
                .AddOption("-a", "", a => a.ExactlyOne())
                .AddOption("-b", "")
                .AddOption("-c", "", a => a.ExactlyOne())
                .Build()
                .Parse("value-for-a value-for-c");

            result.ValueForOption("-a").Should().Be("value-for-a");
            result.ValueForOption("-c").Should().Be("value-for-c");
            result.HasOption("-b").Should().BeFalse();
        }

        [Fact]
        public void When_multiple_option_arguments_are_provided_with_first_option_name_argument_positions_are_assumed()
        {
            var result = new ParserBuilder()
                .AddOption("-a", "", a => a.ExactlyOne())
                .AddOption("-b", "")
                .AddOption("-c", "", a => a.ExactlyOne())
                .Build()
                .Parse("-a value-for-a value-for-c");

            result.ValueForOption("-a").Should().Be("value-for-a");
            result.ValueForOption("-c").Should().Be("value-for-c");
            result.HasOption("-b").Should().BeFalse();
        }

        [Theory]
        [InlineData("-")]
        [InlineData("--")]
        [InlineData("/")]
        public void When_option_use_differnt_prefixes_they_still_work(string prefix)
        {
            var result = new ParserBuilder()
                .AddOption(prefix + "a", "", a => a.ExactlyOne())
                .AddOption(prefix + "b", "")
                .AddOption(prefix + "c", "", a => a.ExactlyOne())
                .Build()
                .Parse(prefix + "c value-for-c " + prefix + "a value-for-a");

            result.ValueForOption(prefix + "a").Should().Be("value-for-a");
            result.ValueForOption(prefix + "c").Should().Be("value-for-c");
            result.HasOption(prefix + "b").Should().BeFalse();
        }

        [Fact]
        public void When_multiple_option_arguments_are_provided_with_second_option_is_first_positions_are_assumed()
        {
            var result = new ParserBuilder()
                .AddOption("-a", "", a => a.ExactlyOne())
                .AddOption("-b", "")
                .AddOption("-c", "", a => a.ExactlyOne())
                .Build()
                .Parse("-c value-for-c value-for-a");

            result.ValueForOption("-a").Should().Be("value-for-a");
            result.ValueForOption("-c").Should().Be("value-for-c");
            result.HasOption("-b").Should().BeFalse();
        }

        [Fact]
        public void When_multiple_option_arguments_are_provided_with_b_option_positions_are_assumed()
        {
            var result = new ParserBuilder()
                .AddCommand("outer", "")
                .AddOption("-a", "", a => a.ExactlyOne())
                .AddOption("-b", "")
                .AddOption("-c", "", a => a.ExactlyOne())
                .Build()
                .Parse("-c value-for-c -b value-for-a outer");

            result.ValueForOption("-a").Should().Be("value-for-a");
            result.ValueForOption("-c").Should().Be("value-for-c");
            result.HasOption("-b").Should().BeTrue();
            result.Symbols.ElementAt(0).Name.Should().Be("outer");
        }

    }
}
