using System.Runtime.CompilerServices;
using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
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

		public async IAsyncEnumerable<User> GetAsync(BulkUsersRequestWithBuffer request, 
				[EnumeratorCancellation] CancellationToken cancellationToken = default)
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
}

