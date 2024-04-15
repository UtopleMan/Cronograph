using Cronos;

namespace Cronograph.Shared;

public static class StringExtensions
{
    public static CronExpression ToCron(this string cron)
    {
        if (cron.Split(' ').Length > 5)
            return CronExpression.Parse(cron, CronFormat.IncludeSeconds);
        else
            return CronExpression.Parse(cron);
    }
}
