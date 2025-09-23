using Knara.SourceGenerators.DesignPatterns.Mediator;

namespace Demo.Mediator.DotNet4.Core;

[StreamQuery(Name = "UsersStreamQuery", ResponseType = typeof(User))]
public class BulkUsersRequestWithBuffer
{
    public string? EmailFilter { get; }
    public int? BufferSize { get; }
    
    public BulkUsersRequestWithBuffer(string? emailFilter, int? bufferSize = null)
    {
        EmailFilter = emailFilter;
        BufferSize = bufferSize;
    }
}