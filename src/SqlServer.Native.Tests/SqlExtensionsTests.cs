public class SqlExtensionsTests
{
    [Test]
    public async Task Table_name_and_schema_should_be_quoted()
    {
        await Assert.That(SqlExtensions.Sanitize("MyEndpoint")).IsEqualTo("[MyEndpoint]");
        await Assert.That(SqlExtensions.Sanitize("MyEndpoint]; SOME OTHER SQL;--")).IsEqualTo("[MyEndpoint]]; SOME OTHER SQL;--]");
    }
}
