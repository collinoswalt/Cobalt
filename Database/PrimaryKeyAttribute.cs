using System;
namespace Cobalt.Database
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class PrimaryKeyAttribute : Attribute { }
}