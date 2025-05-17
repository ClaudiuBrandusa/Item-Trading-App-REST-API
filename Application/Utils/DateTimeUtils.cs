namespace Application.Utils;

public static class DateTimeUtils
{
    public static DateTime DateTimeWithTimeSpanFromUtcNow(TimeSpan timeSpan) => DateTime.UtcNow.Add(timeSpan);
}
