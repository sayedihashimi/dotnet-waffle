using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle {
    public class Template {

        public Template() {
            Type = TemplateType.ProjectTemplate;
        }

        public Template(string name, TemplateType type, string sourceFolder) {
            if (type != TemplateType.ProjectTemplate) { throw new NotImplementedException(); }
            Name = name;
            Type = type;
            Source = TemplateSource.NewFolderSource(sourceFolder);
        }        

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TemplateType Type { get; set; } = TemplateType.ProjectTemplate;

        public bool CreateNewFolder { get; set; } = true;

        public IList<string> Alias { get; set; } = new List<string>();

        public IList<Replacement> Replacements { get; set; } = new List<Replacement>();

        public IList<Replacement> PathReplacemets { get; set; } = new List<Replacement>();

        public IList<string> Excludes { get; set; } = new List<string>();

        public TemplateSource Source { get; set; }

        [JsonIgnore]
        public string TemplateFilePath { get; set; }

        public static Template BuildFromFile(string filePath) {
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Template file not found", filePath); }

            var template = JsonConvert.DeserializeObject<Template>(File.ReadAllText(filePath));

            template.TemplateFilePath = filePath;
            if(template.Source == null) {
                template.Source = TemplateSource.NewFolderSource(new FileInfo(filePath).DirectoryName);
            }

            return template;
        }
    }
}
