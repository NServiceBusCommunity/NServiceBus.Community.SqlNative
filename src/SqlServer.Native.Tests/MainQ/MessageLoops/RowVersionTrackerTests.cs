public class RowVersionTrackerTests :
    TestBase
{
    [Test]
    public async Task Run()
    {
        await SqlConnection.DropTable(null, "RowVersionTracker");
        var tracker = new RowVersionTracker();
        await tracker.CreateTable(SqlConnection);
        var initial = await tracker.Get(SqlConnection);
        await Assert.That(initial).IsEqualTo(1);
        await tracker.Save(SqlConnection,4);
        var after = await tracker.Get(SqlConnection);
        await Assert.That(after).IsEqualTo(4);
    }
}
