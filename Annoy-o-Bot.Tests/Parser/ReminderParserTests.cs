using Annoy_o_Bot.Parser;
using Xunit;

namespace Annoy_o_Bot.Tests.Parser;

public class ReminderParserTests
{
    [Fact]
    public void Should_support_json()
    {
        var parser = ReminderParser.GetParser("some_reminder.json");

        Assert.NotNull(parser);
        Assert.IsType<JsonReminderParser>(parser);
    }

    [Theory]
    [InlineData("some_reminder.yaml")]
    [InlineData("some_reminder.yml")]
    public void Should_support_yaml(string fileName)
    {
        var parser = ReminderParser.GetParser(fileName);

        Assert.NotNull(parser);
        Assert.IsType<YamlReminderParser>(parser);
    }

    [Theory]
    [InlineData("file_without_file_format")]
    [InlineData("file_without_file_format.xml")]
    [InlineData("file_without_file_format.png")]
    [InlineData("file_without_file_format.txt")]
    [InlineData("")]
    public void Should_return_null_for_unsupported_formats(string fileName)
    {
        var parser = ReminderParser.GetParser(fileName);

        Assert.Null(parser);
    }
}