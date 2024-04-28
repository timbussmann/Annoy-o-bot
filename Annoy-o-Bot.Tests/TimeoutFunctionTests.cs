using System;
using Annoy_o_Bot.CosmosDB;
using Xunit;

namespace Annoy_o_Bot.Tests
{
    public class TimeoutFunctionTests
    {
        private static readonly DateTime Now = new DateTime(2020, 1, 1, 0, 0, 0);

        [Theory]
        [InlineData("2020.01.01", 1, "2020.01.02")]
        [InlineData("2020.01.01", 0, "2020.01.02")]
        [InlineData("2020.01.01", null, "2020.01.02")]
        [InlineData("2020.01.01", -1, "2020.01.02")]
        [InlineData("2020.01.01", int.MinValue, "2020.01.02")]
        [InlineData("2020.01.01", 2, "2020.01.03")]
        [InlineData("2020.01.01", 5, "2020.01.06")]
        [InlineData("2019.12.24", 1, "2020.01.02")]
        [InlineData("2019.12.24", null, "2020.01.02")]
        [InlineData("2019.12.24", 5, "2020.01.03")]
        [InlineData("2019.12.30", 10, "2020.01.09")]
        public void Should_calculate_next_reminder_date_for_daily_interval(string nextReminder, int? intervalStep, string expectedResult)
        {
            var result = CalculateReminder(Interval.Daily, nextReminder, intervalStep);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("2020.01.01", 1, "2020.01.08")]
        [InlineData("2020.01.01", 2, "2020.01.15")]
        [InlineData("2020.01.01", 5, "2020.02.05")]
        [InlineData("2019.12.30", 1, "2020.01.06")] // retain originally defined interval
        [InlineData("2019.12.30", null, "2020.01.06")]
        [InlineData("2019.12.30", 5, "2020.02.03")]
        [InlineData("2019.12.06", 2, "2020.01.03")]
        public void Should_calculate_next_reminder_date_for_weekly_interval(string nextReminder, int? intervalStep, string expectedResult)
        {
            var result = CalculateReminder(Interval.Weekly, nextReminder, intervalStep);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("2020.01.01", 1, "2020.02.01")]
        [InlineData("2020.01.01", 2, "2020.03.01")]
        [InlineData("2020.01.01", 5, "2020.06.01")]
        [InlineData("2019.12.15", 1, "2020.01.15")] // retain originally defined interval
        [InlineData("2019.12.15", 5, "2020.05.15")]
        [InlineData("2019.10.01", 2, "2020.02.01")]
        [InlineData("2019.12.31", 1, "2020.01.31")]
        [InlineData("2019.12.31", 2, "2020.02.29")]
        [InlineData("2019.12.31", 3, "2020.03.31")]
        [InlineData("2019.12.31", 4, "2020.04.30")]
        [InlineData("2019.12.31", 12, "2020.12.31")]
        [InlineData("2019.11.30", 1, "2020.1.30")]
        public void Should_calculate_next_reminder_date_for_monthly_interval(string nextReminder, int? intervalStep,
            string expectedResult)
        {
            var result = CalculateReminder(Interval.Monthly, nextReminder, intervalStep);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("2020.01.01", 1, "2021.01.01")]
        [InlineData("2020.01.01", 2, "2022.01.01")]
        [InlineData("2020.01.01", 5, "2025.01.01")]
        [InlineData("2019.12.15", 1, "2020.12.15")] // retain originally defined interval
        [InlineData("2019.12.15", 5, "2024.12.15")]
        [InlineData("2019.01.01", 2, "2021.01.01")]
        [InlineData("2016.02.29", 1, "2020.02.28")]
        [InlineData("2016.02.29", 4, "2020.02.29")]
        [InlineData("2016.02.29", 8, "2024.02.29")]
        [InlineData("2012.02.29", 4, "2020.02.29")]
        public void Should_calculate_next_reminder_date_for_yearly_interval(string nextReminder, int? intervalStep,
            string expectedResult)
        {
            var result = CalculateReminder(Interval.Yearly, nextReminder, intervalStep);

            Assert.Equal(DateTime.Parse(expectedResult), result);
        }

        [Theory]
        [InlineData("2020.01.01", 1, null)]
        [InlineData("2020.01.01", 2, null)]
        [InlineData("2020.01.01", 5, null)]
        [InlineData("2019.12.24", 1, null)]
        public void Should_not_calculate_next_reminder_date_once(string nextReminder, int? intervalStep,
            string expectedResult)
        {
            var result = CalculateReminder(Interval.Once, nextReminder, intervalStep);

            Assert.Equal(expectedResult, result?.ToString("yyyy.MM.dd"));
        }

        static DateTime? CalculateReminder(Interval interval, string nextReminder, int? intervalStep)
        {
            var reminder = new ReminderDocument
            {
                NextReminder = DateTime.Parse(nextReminder),
                Reminder = new ReminderDefinition
                {
                    Interval = interval,
                    IntervalStep = intervalStep
                }
            };

            reminder.CalculateNextReminder(Now);

            return reminder.NextReminder;
        }
    }
}