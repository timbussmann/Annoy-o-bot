using System;
using Annoy_o_Bot.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;

namespace Annoy_o_Bot.Tests.Parser
{
    public class JsonReminderParserTests
    {
        ReminderDefinition reminderDefinition = new ReminderDefinition
        {
            Title = "The title",
            Message = "A message",
            Assignee = "SomeUserHandle;AnotherUserHandle",
            Labels = new[] { "Label1", "Label2" },
            Interval = Interval.Monthly,
            IntervalStep = 5,
            Date = new DateTime(2010, 11, 12)
        };

        readonly JsonReminderParser jsonReminderParser = new JsonReminderParser();

        [Fact]
        void Should_parse_serialized_reminder_correctly()
        {
            var input = JsonConvert.SerializeObject(reminderDefinition);

            var result = jsonReminderParser.Parse(input);

            Assert.Equal("The title", result.Title);
            Assert.Equal("A message", result.Message);
            Assert.Equal("SomeUserHandle;AnotherUserHandle", result.Assignee);
            Assert.Equal(Interval.Monthly, result.Interval);
            Assert.Equal(5, result.IntervalStep);
            Assert.Equal(new DateTime(2010, 11, 12), reminderDefinition.Date);
            Assert.Contains("Label1", result.Labels);
            Assert.Contains("Label2", result.Labels);
        }

        [Fact]
        void Should_parse_reminder_using_lowercase_properties_correctly()
        {
            var input = """
                        {
                            "title":"The title",
                            "message":"A message",
                            "assignee":"SomeUserHandle;AnotherUserHandle",
                            "labels":["Label1","Label2"],
                            "date":"2010-11-12T00:00:00",
                            "interval":3,
                            "intervalStep":5
                        }
                        """;

            var result = jsonReminderParser.Parse(input);

            Assert.Equal("The title", result.Title);
            Assert.Equal("A message", result.Message);
            Assert.Equal("SomeUserHandle;AnotherUserHandle", result.Assignee);
            Assert.Equal(Interval.Monthly, result.Interval);
            Assert.Equal(5, result.IntervalStep);
            Assert.Equal(new DateTime(2010, 11, 12), reminderDefinition.Date);
            Assert.Contains("Label1", result.Labels);
            Assert.Contains("Label2", result.Labels);
        }

        [Fact]
        void Should_parse_reminder_using_uppercase_properties_correctly()
        {
            var input = """
                        {
                            "Title":"The title",
                            "Message":"A message",
                            "Assignee":"SomeUserHandle;AnotherUserHandle",
                            "Labels":["Label1","Label2"],
                            "Date":"2010-11-12T00:00:00",
                            "Interval":3,
                            "IntervalStep":5
                        }
                        """;

            var result = jsonReminderParser.Parse(input);

            Assert.Equal("The title", result.Title);
            Assert.Equal("A message", result.Message);
            Assert.Equal("SomeUserHandle;AnotherUserHandle", result.Assignee);
            Assert.Equal(Interval.Monthly, result.Interval);
            Assert.Equal(5, result.IntervalStep);
            Assert.Equal(new DateTime(2010, 11, 12), reminderDefinition.Date);
            Assert.Contains("Label1", result.Labels);
            Assert.Contains("Label2", result.Labels);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        void Must_provide_a_title(string title)
        {
            reminderDefinition.Title = title;
            var input = JsonConvert.SerializeObject(reminderDefinition);

            var ex = Assert.Throws<ArgumentException>(() => jsonReminderParser.Parse(input));
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
            reminderDefinition.Interval = interval;
            var input = JsonConvert.SerializeObject(reminderDefinition);

            var result = jsonReminderParser.Parse(input);

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
            reminderDefinition.Interval = interval;
            var input = JsonConvert.SerializeObject(reminderDefinition, new StringEnumConverter(false));

            var result = jsonReminderParser.Parse(input);

            Assert.Equal(interval, result.Interval);
        }
    }
}
