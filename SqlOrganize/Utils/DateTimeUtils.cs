namespace SqlOrganize.DateTimeUtils;

public static class Utils
{
    public static short ToSemester(this DateTime date )
    {
        int val = (Int32.Parse(date.ToString("MM")) < 6) ? 1 : 2;
        return (short)val;
    }

    public static string ToYearSemester(this DateTime alta)
    {
        return alta.ToString("yyyy") + alta.ToSemester().ToString();
    }
}
