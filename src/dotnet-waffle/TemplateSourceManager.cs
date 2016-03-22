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
        public TemplateSourceManager(string sourcesFilePath) {
            SourcesFilePath = sourcesFilePath;
        }

        private string SourcesFilePath { get; set; }

        public List<TemplateSource> GetTemplateSources() {
            List<TemplateSource> results = null;
            if (File.Exists(SourcesFilePath)) {
                var contents = File.ReadAllText(SourcesFilePath);
                results = JsonConvert.DeserializeObject<List<TemplateSource>>(contents);
            }
            return results;
        }

        public void SaveTemplates(List<TemplateSource> sources) {
            var str = JsonConvert.SerializeObject(sources);
            File.WriteAllText(SourcesFilePath, JsonConvert.SerializeObject(sources,Formatting.Indented));
        }

        public void AddTemplateSource(TemplateSource sourceToAdd) {
            List<TemplateSource> existingTemplates = null;            
            if (File.Exists(SourcesFilePath)) {
                try {
                    existingTemplates = GetTemplateSources();
                }
                catch(Exception ex) {
                    Console.WriteLine(string.Format("Unable to read the settings file from [{0}]. Overwriting with new source only. Error: {1}", SourcesFilePath, ex.ToString()));
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
            
            SaveTemplates(sources);
        }

        public void RemoveTemplateSource(TemplateSource sourceToRemove) {
            List<TemplateSource> existingTemplates = null;
            if (File.Exists(SourcesFilePath)) {
                try {
                    existingTemplates = GetTemplateSources();
                }
                catch (Exception ex) {
                    Console.WriteLine(string.Format("Unable to read the settings file from [{0}]. Overwriting with new source only. Error: {1}", SourcesFilePath, ex.ToString()));
                }
            }

            if(existingTemplates == null) {
                existingTemplates = new List<TemplateSource>();
            }

            if (existingTemplates.Contains(sourceToRemove)) {
                existingTemplates.Remove(sourceToRemove);
                SaveTemplates(existingTemplates);
            }
            else {
                Console.WriteLine("Source to remove was not found in sources");
            }
        }

        public void UpdateRemoteTemplateSources(List<TemplateSource>templates) {
            if(templates == null) { return; }

            var gittemplates = (from t in templates
                                where t.GitUrl != null && !string.IsNullOrWhiteSpace(t.GitUrl.AbsoluteUri)
                                select t).ToList();

            if (!gittemplates.Any()) { return; }

            foreach(var template in gittemplates) {
                Console.WriteLine($"Updating templates for [{template.GitUrl.AbsoluteUri}] branch={template.GitBranch}");
                Helper.UpdateGitRepo(template.GitUrl.AbsoluteUri, template.GitBranch);
            }
        }
    }
}
