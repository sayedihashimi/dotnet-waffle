using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle {
    public class TemplateManager {
        private IList<Template> templates { get; set; }

        public IEnumerable<Template> AddTemplatesFromFolder(string folderPath, bool recurse, string filePattern = "dn-template*.json") {
            List<Template> templates = new List<Template>();

            if (!Directory.Exists(folderPath)) { throw new DirectoryNotFoundException(string.Format("Template dir not found [{0}]", folderPath)); }

            var options = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(folderPath, filePattern, options);

            foreach(var file in files) {
                try {
                    var template = Template.BuildFromFile(file);
                    templates.Add(template);
                }
                catch(Exception ex) {
                    // TODO: replace with something better
                    Console.WriteLine(string.Format("Unable to add template from file [{0}]. Error: [{1}]",file,ex.ToString()));
                }
            }

            return templates;
        }

        public Template AddTemplate(string filePath) {
            var template = Template.BuildFromFile(filePath);
            templates.Add(template);
            return template;
        }

        public IEnumerable<Template> GetTemplates() {
            return templates;
        }
    }
}
