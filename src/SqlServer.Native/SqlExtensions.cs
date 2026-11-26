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

    //Unique Key Violation = 2627
    public static bool IsKeyViolation(this SqlException sqlException) =>
        sqlException.Number == 2627;
}