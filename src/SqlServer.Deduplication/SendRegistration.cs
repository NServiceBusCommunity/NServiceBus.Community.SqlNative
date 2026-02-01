using NServiceBus.Pipeline;
using NServiceBus.Transport.SqlServerDeduplication;

class SendRegistration(Table table, Func<Cancel, Task<SqlConnection>> connectionBuilder) :
        RegisterStep(stepId: $"{AssemblyHelper.Name}Send",
            behavior: typeof(SendBehavior),
            description: "Saves the outgoing message id to a secondary store to allow message deduplication",
            factoryMethod: _ => new SendBehavior(table, connectionBuilder));