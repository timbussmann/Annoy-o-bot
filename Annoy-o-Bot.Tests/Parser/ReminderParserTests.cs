using Annoy_o_Bot.Parser;
using Xunit;

namespace Annoy_o_Bot.Tests.Parser;

public class ReminderParserTests
{
    [Theory]
    [InlineData("some_reminder.JSON")]
    [InlineData("some_reminder.json")]
    [InlineData("/path/to/some_reminder.json")]
    public void Should_support_json(string fileName)
    {
        var parser = ReminderParser.GetParser(fileName);

        Assert.NotNull(parser);
        Assert.IsType<JsonReminderParser>(parser);
    }

    [Theory]
    [InlineData("some_reminder.YAML")]
    [InlineData("some_reminder.yaml")]
    [InlineData("/path/to/some_reminder.yaml")]
    [InlineData("path/to/some_reminder.yaml")]
    [InlineData("some_reminder.YML")]
    [InlineData("some_reminder.yml")]
    [InlineData("/path/to/some_reminder.yml")]
    [InlineData("path/to/some_reminder.yml")]
    public void Should_support_yaml(string fileName)
    {
        var parser = ReminderParser.GetParser(fileName);

        Assert.NotNull(parser);
        Assert.IsType<YamlReminderParser>(parser);
    }

    [Theory]
    [InlineData("file_without_file_format")]
    [InlineData("/some/where/file_without_file_format")]
    [InlineData("some/where/file_without_file_format")]
    [InlineData("file_without_file_format.xml")]
    [InlineData("file_without_file_format.png")]
    [InlineData("file_without_file_format.txt")]
    [InlineData(".hiddenfile")]
    [InlineData("folder/.hiddenfile")]
    [InlineData("")]
    public void Should_return_null_for_unsupported_formats(string fileName)
    {
        var parser = ReminderParser.GetParser(fileName);

        Assert.Null(parser);
    }
}