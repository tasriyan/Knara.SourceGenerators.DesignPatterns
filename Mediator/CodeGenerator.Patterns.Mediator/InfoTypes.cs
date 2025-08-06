using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace CodeGenerator.Patterns.Mediator;

public enum HandlerType
{
    Query,
    Command,
    Notification,
    StreamQuery
}
public enum RequestType
{
    Query,
    Command,
    Notification,
    StreamQuery
}

public class PropertyInfo
{
    public string Type { get; }
    public string Name { get; }
    public string Initializer { get; }
    public PropertyInfo(
        string type,
        string name,
        string initializer)
    {
        Type = type;
        Name = name;
        Initializer = initializer;
    }
}

public class DiagnosticInfo
{
	public DiagnosticDescriptor Descriptor { get; }
	public Location Location { get; }
	public object[] Args { get; }

	public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params object[] args)
	{
		Descriptor = descriptor;
		Location = location;
		Args = args ?? [];
	}
}

public class RequestInfo
{
    public string ClassName { get; }
    public string Name { get; }
    public RequestType Type { get; }
    public string? ResponseType { get; }
    public string Namespace { get; }
    public List<PropertyInfo> Properties { get; }
    public bool IsRecord { get; }
    public RequestInfo(
        string className,
        string name,
        RequestType type,
        string? reponseType,
        string ns,
        List<PropertyInfo> properties,
        bool isRecord)
    {
        ClassName = className;
        Name = name;
        Type = type;
        ResponseType = reponseType;
        Namespace = ns;
        Properties = properties;
        IsRecord = isRecord;
    }
}

public class RequestInfoResult
{
	public RequestInfo? RequestInfo { get; }
	public List<DiagnosticInfo> Diagnostics { get; }

	public RequestInfoResult(RequestInfo? requestInfo, List<DiagnosticInfo> diagnostics)
	{
		RequestInfo = requestInfo;
		Diagnostics = diagnostics ?? [];
	}
}

public class HandlerInfo
{
    public string Namespace { get; }
    public string RequestType { get; }
    public string? Method { get; }
    public string? PublisherType { get; }
    public HandlerType Type { get; }
    public string HandlerName { get; }
    public string ServiceClassName { get; }
    
    public HandlerInfo(
        string serviceClassName,
        string handlerName,
        HandlerType type,
        string? requestType,
        string? publisherType,
        string? method,
        string ns)
    {
        ServiceClassName = serviceClassName;
        HandlerName = handlerName;
        Type = type;
        RequestType = requestType;
        PublisherType = publisherType;
        Method = method;
        Namespace = ns;
    }
}

public class HandlerInfoResult
{
	public HandlerInfo? HandlerInfo { get; }
	public List<DiagnosticInfo> Diagnostics { get; }

	public HandlerInfoResult(HandlerInfo? handlerInfo, List<DiagnosticInfo> diagnostics)
	{
		HandlerInfo = handlerInfo;
		Diagnostics = diagnostics ?? [];
	}
}

// NEW: Data structures for legacy method information
public class ParameterInfo
{
	public string Type { get; } 
	public string Name { get; }
	public string Initializer { get; }
	
	public ParameterInfo(
		string type,
		string name,
		string initializer)
	{
		Type = type;
		Name = name;
		Initializer = initializer;
	}
}

public record LegacyMethodInfo
{
	public string ServiceClassName { get; }
	public string MethodName { get; }
	public string HandlerName { get; }
	public string RequestName { get; }
	public List<ParameterInfo> Parameters { get; }
	public bool HasReturnType { get; }
	public string? ReturnType { get; }
	public string Namespace { get; }
				
	public LegacyMethodInfo(
				 string serviceClassName,
				 string methodName,
				 string handlerName,
				 string requestName,
				 List<ParameterInfo> parameters,
				 bool hasReturnType,
				 string? returnType,
				 string ns)
		{
			ServiceClassName = serviceClassName;
			MethodName = methodName;
			HandlerName = handlerName;
			RequestName = requestName;
			Parameters = parameters;
			HasReturnType = hasReturnType;
			ReturnType = returnType;
			Namespace = ns;
		}
}