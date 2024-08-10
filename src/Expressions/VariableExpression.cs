using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Symple.Expressions
{
    public class VariableExpression : INumericExpression
    {
        public VariableExpression(string name, string[] propertyNames)
        {
            Name = name;
            PropertyNames = propertyNames;
        }

        public string Name { get; }

        public string[] PropertyNames { get; }

        public object GetValue(Dictionary<string, object> variables)
        {
            var obj = variables.TryGetValue(Name, out var v) ? v : null;
            if (obj is null || !(PropertyNames?.Length > 0))
            {
                return obj;
            }

            foreach (var propertyName in PropertyNames)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    break;
                }

                obj = GetPropertyValue(obj, propertyName);

                if (obj is null)
                {
                    break;
                }
            }

            return obj;
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj is IDictionary dict)
            {
                return dict[propertyName];
            }

            // Reflection
            var type = obj.GetType();

            if (type.GetProperty(propertyName) is PropertyInfo pi)
            {
                return pi.GetValue(obj);
            }

            if (type.GetField(propertyName) is FieldInfo fi)
            {
                return fi.GetValue(obj);
            }

            // Property not found
            return null;
        }

        public string Render(Dictionary<string, object> variables)
        {
            var value = GetValue(variables);
            return value?.ToString() ?? "";
        }

        public bool AsBool(Dictionary<string, object> variables)
        {
            var value = GetValue(variables);

            if (value is null)
            {
                return false;
            }

            if (value is bool b) return b;
            if (value is string s) return s.Length > 0;
            if (value is IEnumerable e) return e.Cast<object>().Any();

            return !value.Equals(GetDefaultValue(value.GetType()));
        }

        private static object GetDefaultValue(Type t)
        {
            return t.IsValueType
                ? Activator.CreateInstance(t)
                : null;
        }

        public decimal? AsNumber(Dictionary<string, object> variables)
        {
            var v = GetValue(variables);

            if (v is decimal m) return m;
            if (v is double d) return (decimal)d;
            if (v is float f) return (decimal)f;
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is uint ui) return ui;
            if (v is ushort us) return us;
            if (v is short s) return s;
            if (v is byte b) return b;
            if (v is sbyte sb) return sb;

            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder().Append('$').Append(Name);

            if (PropertyNames?.Length > 0)
            {
                _ = sb.Append('.').Append(string.Join(".", PropertyNames));
            }

            return sb.ToString();
        }
    }
}
