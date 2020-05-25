using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;
using YamlDotNet.Serialization;

namespace Annoy_o_Bot.Tests
{
    public class YamlReminderParserTests
    {
        Reminder reminder = new Reminder
        {
            Title = "The title",
            Message = "A message with [a markdown link](/somewhere)",
            Assignee = "SomeUserHandle;AnotherUserHandle",
            Interval = Interval.Monthly,
            IntervalStep = 5
        };

        readonly Serializer serializer = new Serializer();

        [Fact]
        void Should_parse_reminder_correctly()
        {
            var input = serializer.Serialize(reminder);

            var result = YamlReminderParser.Parse(input);

            Assert.Equal("The title", result.Title);
            Assert.Equal("A message with [a markdown link](/somewhere)", result.Message);
            Assert.Equal("SomeUserHandle;AnotherUserHandle", result.Assignee);
            Assert.Equal(Interval.Monthly, result.Interval);
            Assert.Equal(5, result.IntervalStep);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        void Must_provide_a_title(string title)
        {
            reminder.Title = title;
            var input = serializer.Serialize(reminder);

            var ex = Assert.Throws<ArgumentException>(() => YamlReminderParser.Parse(input));
            Assert.Contains("A reminder must provide a non-empty Title property", ex.Message);
        }

        [Theory]
        [InlineData(Interval.Daily)]
        [InlineData(Interval.Weekly)]
        [InlineData(Interval.Monthly)]
        [InlineData(Interval.Yearly)]
        [InlineData(Interval.Once)]
        void Should_parse_interval_int_value(Interval interval)
        {
            reminder.Interval = interval;
            var input = serializer.Serialize(reminder);

            var result = YamlReminderParser.Parse(input);

            Assert.Equal(interval, result.Interval);
        }

        [Theory]
        [InlineData(Interval.Daily)]
        [InlineData(Interval.Weekly)]
        [InlineData(Interval.Monthly)]
        [InlineData(Interval.Yearly)]
        [InlineData(Interval.Once)]
        void Should_parse_interval_string_value(Interval interval)
        {
            reminder.Interval = interval;
            var input = serializer.Serialize(reminder);

            var result = YamlReminderParser.Parse(input);

            Assert.Equal(interval, result.Interval);
        }
    }
}