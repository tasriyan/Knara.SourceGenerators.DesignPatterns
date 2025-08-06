using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeGenerator.Patterns.Mediator;

    // CQRS-style interfaces (EXISTING)
    public interface IQuery<out TResponse> { }
    public interface ICommand { }
    public interface ICommand<out TResponse> { }
    public interface IStreamQuery<out TResponse> { }

    public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface IStreamQueryHandler<in TQuery, TResponse> where TQuery : IStreamQuery<TResponse>
    {
	    IAsyncEnumerable<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
    }

    // NEW: Basic MediatR-style interfaces for legacy retrofitting
    public interface IRequest { }
    public interface IRequest<out TResponse> { }

    public interface IRequestHandler<in TRequest> where TRequest : IRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken = default);
    }

    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
    }

    // Extended mediator interface supporting both patterns
    public interface IMediator
    {
        // CQRS-style methods (EXISTING)
        Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
        Task Send(ICommand command, CancellationToken cancellationToken = default);
        Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
        Task Publish<TEvent>(TEvent eventObj, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default);
        
        // NEW: MediatR-style methods for legacy support
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        Task Send(IRequest request, CancellationToken cancellationToken = default);
    }
