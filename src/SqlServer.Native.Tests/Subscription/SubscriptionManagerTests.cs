public class SubscriptionManagerTests :
    TestBase
{
    [Test]
    public async Task Create()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await Verify(SqlConnection)
            .SchemaFilter(_ => _.Name == "Subscription");
    }

    [Test]
    public async Task Drop()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await manager.Drop();
        await Verify(SqlConnection)
            .SchemaFilter(_ => _.Name == "Subscription");
    }

    [Test]
    public async Task Subscribe()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await manager.Subscribe("endpoint1", "address1", "topic1");
        await manager.Subscribe("endpoint2", "address2", "topic1");
        await Verify(manager.GetSubscribers("topic1"));
    }

    [Test]
    public async Task NoMatchingTopic()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await manager.Subscribe("endpoint1", "address1", "topic1");
        await manager.Subscribe("endpoint2", "address2", "topic2");
        await Verify(manager.GetSubscribers("topic3"));
    }

    [Test]
    public async Task SingleTopic()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await manager.Subscribe("endpoint1", "address1", "topic1");
        await manager.Subscribe("endpoint2", "address2", "topic1");
        await manager.Subscribe("endpoint3", "address3", "topic3");
        await Verify(manager.GetSubscribers("topic1"));
    }

    [Test]
    public async Task MultipleTopic()
    {
        var manager = new SubscriptionManager("Subscription", SqlConnection);
        await manager.Drop();
        await manager.Create();
        await manager.Subscribe("endpoint1", "address1", "topic1");
        await manager.Subscribe("endpoint2", "address2", "topic2");
        await manager.Subscribe("endpoint3", "address3", "topic3");
        await manager.Subscribe("endpoint4", "address4", "topic4");
        await Verify(manager.GetSubscribers("topic1", "topic4"));
    }
}
