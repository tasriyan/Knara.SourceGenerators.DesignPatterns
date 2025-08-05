using System.Runtime.CompilerServices;
using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;

namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures;

[StreamQuery(Name = "UsersStreamQuery", ResponseType = typeof(User))]
public record BulkUsersRequestWithBuffer(string? EmailFilter, int? BufferSize = null);

[StreamQueryHandler(
	Name = "UsersStreamHandler", 
	RequestType = typeof(UsersStreamQuery))]
public class BulkUsersService
{
	private readonly IUserRepository _repository;

	public BulkUsersService(IUserRepository repository)
	{
		_repository = repository;
	}

	public async IAsyncEnumerable<User> GetAsync(BulkUsersRequestWithBuffer request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var bufferSize = GetEffectiveBufferSize(request);
		
		await foreach (var user in _repository.GetFilteredUsersAsync(request.EmailFilter, 
			               bufferSize,
			               cancellationToken))
		{
			yield return user;
		}
	}
	
	private int GetEffectiveBufferSize(BulkUsersRequestWithBuffer bufferedRequest)
	{
		if (bufferedRequest.BufferSize.HasValue)
		{
			return bufferedRequest.BufferSize.Value;
		}
		
		return 50; 
	}
}

//after
// public class UsersStreamHandler : IStreamQueryHandler<UsersStreamQuery, User>
// {
// 	private readonly BulkUsersService _service;
// 	private const int BufferSize = 50;
// 	private const bool AllowBufferOverride = true;
//
// 	public UsersStreamHandler(BulkUsersService service)
// 	{
// 		_service = service;
// 	}
//
// 	public async IAsyncEnumerable<Demo.Mediator.ConsoleApp.Core.User> Handle(UsersStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
// 	{
// 		var originalRequest = new BulkUsersRequestWithBuffer(query.EmailFilter, query.BufferSize);
//
// 		// Determine effective buffer size
// 		var effectiveBufferSize = AllowBufferOverride && query.BufferSize.HasValue ? query.BufferSize.Value : BufferSize;
//
// 		if (effectiveBufferSize > 0)
// 		{
// 			var skip = 0;
// 			List<User> batch;
// 			do
// 			{
// 				batch = (await _service.GetAsync(originalRequest, cancellationToken))
// 					.Skip(skip)
// 					.Take(effectiveBufferSize)
// 					.ToList();
//
// 				foreach (var user in batch)
// 				{
// 					cancellationToken.ThrowIfCancellationRequested();
// 					yield return user;
// 				}
//
// 				skip += batch.Count;
// 			}while (batch.Count == effectiveBufferSize);
// 		}
// 		else
// 		{
// 			await foreach (var item in _service.GetAsync(originalRequest, cancellationToken))
// 			{
// 				yield return item;
// 			}
// 		}
// 	}
// }

