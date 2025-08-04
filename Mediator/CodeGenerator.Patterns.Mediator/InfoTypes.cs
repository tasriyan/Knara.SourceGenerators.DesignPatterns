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
    Notification
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