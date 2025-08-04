using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.Core;
using System.Runtime.CompilerServices;

namespace Demo.Mediator.ConsoleApp.VerticalSlices;

/*
 * GET USERS STREAM FEATURE - High-Performance Streaming Query Pattern Demonstration
 *
 * This file demonstrates the STREAMING QUERY pattern within the mediator architecture:
 *
 * STREAMING QUERY PATTERN (Large Dataset Processing):
 *    - GetUsersStreamQuery: Represents a streaming request that returns IAsyncEnumerable<User>
 *    - GetUsersStreamQueryHandler: Handles streaming query execution with yield return pattern
 *    - Uses IStreamQuery<User> interface for operations that stream large datasets
 *    - Implements IAsyncEnumerable<User> for memory-efficient data streaming
 *
 * PERFORMANCE OPTIMIZATIONS:
 *    - IAsyncEnumerable enables processing large datasets without loading all into memory
 *    - yield return pattern provides lazy evaluation and reduced memory footprint
 *    - EnumeratorCancellation attribute enables proper cancellation token propagation
 *    - Configurable PageSize for controlling batch processing (default: 100)
 *
 * The generated mediator (GeneratedMediator.g.cs) routes this stream query via:
 *    - CreateStream<TResponse>() method using pattern matching
 *    - Direct handler invocation returning IAsyncEnumerable
 *
 * Handler is registered in DI container via AddMediatorHandlers() extension method.
 */
public class GetUsersStreamQuery : IStreamQuery<User>
{
	public string? EmailFilter { get; set; }
	public DateTime? CreatedAfter { get; set; }
	public int PageSize { get; set; } = 100;
}

[StreamQueryHandler]
public partial class GetUsersStreamQueryHandler : IStreamQueryHandler<GetUsersStreamQuery, User>
{
	private readonly IUserRepository _repository;

	public GetUsersStreamQueryHandler(IUserRepository repository)
	{
		_repository = repository;
	}

	public async IAsyncEnumerable<User> Handle(GetUsersStreamQuery query, CancellationToken cancellationToken = default)
	{
		await foreach (var user in _repository.GetUsersStreamAsync(
			query.EmailFilter, query.CreatedAfter, query.PageSize, cancellationToken))
		{
			yield return user;
		}
	}
}
