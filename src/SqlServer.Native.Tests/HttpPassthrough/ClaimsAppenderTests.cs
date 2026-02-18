public class ClaimsAppenderTests :
    TestBase
{
    [Test]
    public Task Append()
    {
        var headers = new Dictionary<string, string>();
        var claims = BuildClaims();
        ClaimsAppender.Append(claims, headers, "prefix.");
        return Verify(headers);
    }

    [Test]
    public Task Extract()
    {
        var headers = new Dictionary<string, string>();
        var claims = BuildClaims();
        ClaimsAppender.Append(claims, headers, "prefix.");
        var result = ClaimsAppender.Extract(headers, "prefix.")
            .Select(_ => new
            {
                _.Type,
                _.Value
            });
        return Verify(result);
    }

    static IEnumerable<Claim> BuildClaims()
    {
        yield return new(ClaimTypes.Email, "User@foo.bar");
        yield return new(ClaimTypes.NameIdentifier, "User1");
        yield return new(ClaimTypes.NameIdentifier, "User2");
    }
}