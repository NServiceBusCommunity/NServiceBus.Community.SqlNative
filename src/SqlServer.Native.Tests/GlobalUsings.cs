global using System.Security.Claims;
global using Microsoft.Data.SqlClient;
global using Microsoft.Extensions.DependencyInjection;
global using NServiceBus.SqlServer.HttpPassthrough;
global using NServiceBus.Transport.SqlServerNative;

[assembly: ParallelLimiter<NoParallelLimit>]

public record NoParallelLimit : TUnit.Core.Interfaces.IParallelLimit
{
    public int Limit => 1;
}
