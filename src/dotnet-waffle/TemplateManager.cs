using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle {
    public class TemplateManager {
        public TemplateManager(TemplateSourceManager sourceManager) {
            SourceManager = sourceManager;
        }
        private TemplateSourceManager SourceManager { get; set; }
           
        public IEnumerable<Template> GetTemplatesFromFolder(string folderPath, bool recurse, string filePattern = "waffle*.json") {
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

        public IEnumerable<Template>GetTemplatesFromPackage(string packageName,string packageVersion) {
            string pkgPath = Helper.RestorePackage(packageName, packageVersion);
            return GetTemplatesFromFolder(pkgPath, true);
        }
        
        public IEnumerable<Template>GetTemplatesFromGit(string gitUri,string branchName) {
            string gitpath = Helper.EnsureGitIsClonedLocally(gitUri, branchName);
            return GetTemplatesFromFolder(gitpath, true);
        }

        public IEnumerable<Template>GetTemplateFromSource(TemplateSource source) {
            switch (source.Type) {
                case SourceType.Folder:
                    return GetTemplatesFromFolder(source.SourceFolder, true);
                case SourceType.Package:
                    return GetTemplatesFromPackage(source.PackageName, source.PackageVersion);
                case SourceType.Git:
                    return GetTemplatesFromGit(source.GitUrl.AbsoluteUri, source.GitBranch);
                default:
                    throw new InvalidOperationException(string.Format("Unknown source type [{0}]", source.Type));
            }
        }

        public IEnumerable<Template> GetInstalledTemplates() {
            IList<TemplateSource> sources = SourceManager.GetTemplateSources();
            List<Template> templates = new List<Template>();

            if (sources != null) {
                foreach (var source in sources) {
                    var sourceTemplates = GetTemplateFromSource(source);
                    if (sourceTemplates != null) {
                        templates.AddRange(sourceTemplates);
                    }
                }
            }
            return templates;
        }
    }
}
