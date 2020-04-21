using System;
using System.Web;
using System.Collections.Generic;
using System.Reflection;

using Cobalt.Database;

namespace Cobalt.Models.Editor
{
    public class EditField
    {
        public Object Owner { get; set; }
        public String Name { get; set; }
        public Type FieldType { get; set; }
        public PropertyInfo Property { get; set; }
        public bool ReadOnly { get{
            if(_ReadOnly == null) {
                _ReadOnly =
                    Property.GetCustomAttribute(typeof(ReadOnlyAttribute))      != null ||
                    Property.GetCustomAttribute(typeof(AutoIncrementAttribute)) != null;
            }
            return _ReadOnly.Value;
        }}
        private bool? _ReadOnly;

        private static HashSet<Type> NumericTypes = new HashSet<Type> {
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
        };
        public String ValueString { get {
            return Owner != null
                ? Property.GetGetMethod().Invoke(Owner, null)?.ToString() ?? ""
                : "";
        }}

        public Object Value { get {
            return Owner != null
                ? Property?.GetGetMethod()?.Invoke(Owner, null)
                : null;
        }}

        public String GetInput() {
            String S = "";

            if(FieldType == typeof(string))
                S = GetTextInput();
            else if(NumericTypes.Contains(FieldType))
                S = GetNumberInput();
            else if(FieldType == typeof(DateTime))
                S = GetDateTimeInput();
            else if(FieldType.IsEnum)
                S = GetEnumDropdown();

            return S;
        }

        public String GetTextInput() {
            if(Property.GetCustomAttribute(typeof(RichTextAttribute)) != null)
                return $@"<textarea name=""{Name}"" {(ReadOnly ? "readonly" : "")} placeholder=""{Name}"">{ValueString}</textarea>";
            return $@"<input name=""{Name}"" value=""{ValueString}"" {(ReadOnly ? "readonly" : "")} placeholder=""{Name}""/>";
        }

        public String GetNumberInput() {
            return $@"<input name=""{Name}"" {(ReadOnly ? "readonly" : "")} type=""number"" value=""{ValueString}"" placeholder=""{Name}""/>";
        }

        public String GetDateInput() {
            return $@"<input name=""{Name}"" {(ReadOnly ? "readonly" : "")} type=""date"" value=""{ValueString}""/>";
        }

        public String GetDateTimeInput() {
            var Time = Value != null
                ? ((DateTime)Value)
                : DateTime.Now;

            var DateTimeValue = Time.ToString("yyyy-MM-ddThh:mm:ss");
            return $@"<input name=""{Name}"" {(ReadOnly ? "readonly" : "")} type=""datetime-local"" {(Value != null ? "value=\"" + DateTimeValue + "\"" : "")} />";
        }

        public String GetEnumDropdown() {
            var Select = $@"<select name=""{Name}"">";
            foreach(var EnumName in FieldType.GetEnumNames()) {
                var DisplayName = (
                    FieldType
                        .GetMember(EnumName)[0]
                        .GetCustomAttribute(typeof(DisplayNameAttribute))
                        as DisplayNameAttribute
                    )?.Value
                    ?? EnumName;
                Select += $@"<option value=""{EnumName}"" {(EnumName == Value.ToString() ? "selected" : "")}>{DisplayName}</option>";
            }
            Select += "</select>";
            return Select;
        }
        
        public static List<EditField> FieldFromModel<T>(Object Model) {
            List<EditField> Fields = new List<EditField>();
            foreach(var Property in typeof(T).GetProperties()) {
                if(Property.GetCustomAttribute(typeof(HiddenAttribute)) != null)
                    continue;
                Fields.Add(new EditField() {
                    Owner = Model,
                    Name = Property.Name,
                    FieldType = Property.PropertyType,
                    Property = Property
                });
            }
            return Fields;
        }
    }
}