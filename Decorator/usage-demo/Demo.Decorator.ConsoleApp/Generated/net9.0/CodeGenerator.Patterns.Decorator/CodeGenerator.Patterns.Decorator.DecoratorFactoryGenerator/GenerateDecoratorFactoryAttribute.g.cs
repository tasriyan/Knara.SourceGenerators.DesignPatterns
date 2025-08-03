
using System;

namespace CodeGenerator.Patterns.Decorator
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class GenerateDecoratorFactoryAttribute : Attribute
    {
        public string BaseName { get; set; } = "";
        public bool GenerateFactory { get; set; } = true;
    }
}