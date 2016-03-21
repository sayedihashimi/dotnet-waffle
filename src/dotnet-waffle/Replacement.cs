using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class Replacement
    {
        public Replacement() { }
        public Replacement(string key,string value,string defaultValue) {
            if (string.IsNullOrWhiteSpace(key)) {
                new TemplateException(string.Format("Replacement key is null or empty"));
            }

            Key = key;
            Value = value;
            DefaultValue = defaultValue;
        }
        public Replacement(string key,string value) : this(key, value, null) {
        }

        public string Key { get; set; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
    }
}
