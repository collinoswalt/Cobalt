using System;
namespace Cobalt.Models.Editor
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DisplayNameAttribute : Attribute {
        public string Value { get; set; }
        public DisplayNameAttribute(string Value) {
            this.Value = Value;
        }
    }
}