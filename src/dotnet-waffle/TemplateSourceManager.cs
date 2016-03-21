using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class TemplateSourceManager
    {
        public List<TemplateSource> GetTemplatesFrom(string settingsFilePath) {
            List<TemplateSource> results = null;
            if (File.Exists(settingsFilePath)) {
                var contents = File.ReadAllText(settingsFilePath);
                results = JsonConvert.DeserializeObject<List<TemplateSource>>(contents);
            }
            return results;
        }

        public void SaveTemplates(string settingsFilePath, List<TemplateSource> sources) {
            var str = JsonConvert.SerializeObject(sources);
            File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(sources,Formatting.Indented));
        }

        public void AddTemplateSource(string settingsFilePath, TemplateSource sourceToAdd) {
            List<TemplateSource> existingTemplates = null;            
            if (File.Exists(settingsFilePath)) {
                try {
                    existingTemplates = GetTemplatesFrom(settingsFilePath);
                }
                catch(Exception ex) {
                    Console.WriteLine(string.Format("Unable to read the settings file from [{0}]. Overwriting with new source only. Error: {1}", settingsFilePath, ex.ToString()));
                }
            }

            List<TemplateSource> sources = new List<TemplateSource>();
            sources.Add(sourceToAdd);

            // add back existing templates, and don't add any duplicates
            if(existingTemplates != null) {
                foreach(var template in existingTemplates) {
                    if (template.Equals(sourceToAdd)) {
                        continue;
                    }
                    sources.Add(template);
                }
            }
            
            SaveTemplates(settingsFilePath, sources);
        }

        public void RemoveTemplateSource(string settingsFilePath, TemplateSource sourceToRemove) {
            List<TemplateSource> existingTemplates = null;
            if (File.Exists(settingsFilePath)) {
                try {
                    existingTemplates = GetTemplatesFrom(settingsFilePath);
                }
                catch (Exception ex) {
                    Console.WriteLine(string.Format("Unable to read the settings file from [{0}]. Overwriting with new source only. Error: {1}", settingsFilePath, ex.ToString()));
                }
            }

            if(existingTemplates == null) {
                existingTemplates = new List<TemplateSource>();
            }

            if (existingTemplates.Contains(sourceToRemove)) {
                existingTemplates.Remove(sourceToRemove);
                SaveTemplates(settingsFilePath, existingTemplates);
            }
            else {
                Console.WriteLine("Source to remove was not found in sources");
            }
        }
    }
}
