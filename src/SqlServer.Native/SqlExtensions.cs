static class SqlExtensions
{
    public static string Sanitize(string unquotedIdentifier)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        builder.Append(unquotedIdentifier.Replace("]", "]]"));
        builder.Append(']');

        return builder.ToString();
    }

    public static bool IsKeyViolation(this SqlException sqlException)
    {
        dynamic exception = sqlException;
        //Unique Key Violation = 2627
        return exception.Number == 2627;
    }
}