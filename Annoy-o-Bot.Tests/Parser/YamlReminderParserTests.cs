using System;
using Annoy_o_Bot.Parser;
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
            Labels = new []{ "Label1", "Label2" },
            Interval = Interval.Monthly,
            IntervalStep = 5,
            Date = new DateTime(2010, 11, 12)
        };

        readonly Serializer serializer = new Serializer();
        readonly YamlReminderParser yamlReminderParser = new YamlReminderParser();

        [Fact]
        void Should_parse_reminder_correctly()
        {
            var input = serializer.Serialize(reminder);

            var result = yamlReminderParser.Parse(input);

            Assert.Equal("The title", result.Title);
            Assert.Equal("A message with [a markdown link](/somewhere)", result.Message);
            Assert.Equal("SomeUserHandle;AnotherUserHandle", result.Assignee);
            Assert.Equal(Interval.Monthly, result.Interval);
            Assert.Equal(5, result.IntervalStep);
            Assert.Equal(new DateTime(2010, 11, 12), reminder.Date);
            Assert.Contains("Label1", result.Labels);
            Assert.Contains("Label2", result.Labels);
        }

        [Theory]
        [InlineData("NO")]
        [InlineData("Null", Skip = "broken")]
        void Should_parse_yaml_keywords_as_strings(string keyword)
        {
            var result = yamlReminderParser.Parse(
$@"Title: {keyword}
Message: {keyword}
Assignee: {keyword}
Labels: 
- {keyword}");

            Assert.Equal(keyword, result.Title);
            Assert.Equal(keyword, result.Message);
            Assert.Equal(keyword, result.Assignee);
            Assert.Equal(keyword, result.Labels[0]);
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

            var ex = Assert.Throws<ArgumentException>(() => yamlReminderParser.Parse(input));
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

            var result = yamlReminderParser.Parse(input);

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

            var result = yamlReminderParser.Parse(input);

            Assert.Equal(interval, result.Interval);
        }
    }
}