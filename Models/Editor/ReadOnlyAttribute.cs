using System;
namespace Cobalt.Models.Editor
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReadOnlyAttribute : EditorAttribute { }
}