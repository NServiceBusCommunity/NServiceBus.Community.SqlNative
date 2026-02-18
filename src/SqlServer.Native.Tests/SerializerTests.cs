public class SerializerTests
{
    [Test]
    public Task Dictionary()
    {
        var serialized = Serializer.SerializeDictionary(
            new Dictionary<string, string>
            {
                {"key", "value"},
                {@"a\b", @"a\b"},
                {@"a\\b", @"a\\b"},
                {"a\"b", "a\"b"},
                {"a/b", "a/b"},
                {"a//b", "a//b"},
                {@"a\/b", @"a\/b"}
            });
        return Verify(Serializer.DeSerializeDictionary(serialized));
    }

    [Test]
    public Task List()
    {
        var serialized = Serializer.SerializeList(
        [
            "value",
            @"a\b",
            @"a\\b",
            "a\"b",
            "a/b",
            "a//b",
            @"a\/b"
        ]);
        return Verify(Serializer.DeSerializeList(serialized));
    }
}