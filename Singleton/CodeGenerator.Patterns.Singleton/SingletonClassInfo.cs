using System.Collections.Generic;

namespace CodeGenerator.Patterns.Singleton;

public enum SingletonStrategy
{
    Lazy = 0,
    Eager = 1,
    LockFree = 2,
    DoubleCheckedLocking = 3
}

public class SingletonClassInfoResult
{
	public SingletonClassInfo? SingletonClassInfo { get; }
	public List<DiagnosticInfo> Diagnostics { get; }

	public SingletonClassInfoResult(SingletonClassInfo? singletonClassInfo, List<DiagnosticInfo> diagnostics)
	{
		SingletonClassInfo = singletonClassInfo;
		Diagnostics = diagnostics ?? [];
	}
}

public class SingletonClassInfo
{
    public string ClassName { get; }
    public string Namespace { get; }
    public SingletonStrategy Strategy { get; }
    public bool HasInitializeMethod { get; }
    public bool IsGeneric { get; }
    public List<string> TypeParameters { get; }
    public List<string> TypeConstraints { get; }
    
    public SingletonClassInfo(string className, 
                                string ns,
                                SingletonStrategy strategy,
                                bool hasInitializeMethod,
                                bool isGeneric,
                                List<string> typeParameters,
                                List<string> typeConstraints)
    {
        ClassName = className;
        Namespace = ns;
        Strategy = strategy;
        HasInitializeMethod = hasInitializeMethod;
        IsGeneric = isGeneric;
        TypeParameters = typeParameters ?? [];
        TypeConstraints = typeConstraints ?? [];
    }
}