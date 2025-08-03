
using System;

namespace CodeGenerator.Patterns.Decorator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DecoratorAttribute : Attribute
    {
        public string Type { get; set; } = "";
        public int Order { get; set; } = 0;
    }
}