using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

public class RequestParserTests :
    TestBase
{
    [Test]
    public Task Optional()
    {
        var request = new FakeHttpRequest
        (
            headersDictionary: new()
            {
                {"MessageType", "TheMessageType"},
                {"MessageId", new Guid("00000000-0000-0000-0000-000000000001").ToString()},
            },
            body: Body(),
            form: Form()
        );
        return Verify(request);
    }

    [Test]
    public Task Simple()
    {
        var request = new FakeHttpRequest
        (
            headersDictionary: new()
            {
                {"Destination", "TheEndpoint"},
                {"MessageNamespace", "TheMessageNamespace"},
                {"MessageType", "TheMessageType"},
                {"MessageId", new Guid("00000000-0000-0000-0000-000000000001").ToString()},
                {HeaderNames.Referer, "TheReferer"}
            },
            body: Body(),
            form: Form()
        );
       return Verify(request);
    }

    static MemoryStream Body() =>
        new("{}"u8.ToArray());

    static Task Verify(FakeHttpRequest request)
    {
        var extract = RequestParser.Extract(request, Cancel.None).GetAwaiter().GetResult();
        return Verifier.Verify(new
        {
            extract.Attachments.Single().FileName,
            extract.Body,
            extract.ClientUrl,
            extract.Destination,
            extract.Id,
            extract.Type
        });
    }

    static FormCollection Form()
    {
        var attachmentBytes = "Attachment Text"u8.ToArray();
        return new(
            new()
            {
                {"message", "{}"}
            },
            new FormFileCollection
            {
                new FormFile(new MemoryStream(attachmentBytes), 0, attachmentBytes.Length, "attachment", "attachment.txt")
            });
    }
}