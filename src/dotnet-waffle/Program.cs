using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle {
    public class Program
    {
        public static void Main(string[] args)
        {
            try {
                var pkgtemplate = CreateTemplateFromPackage("DotnetSampleConsoleApp", "1.0.0", "microsoft.dotnet.console.rc2");
                if(pkgtemplate != null) {
                    string targetfolder = @"c:\temp\dn-waffle\frompkg";
                    if (!string.IsNullOrWhiteSpace(targetfolder) && Directory.Exists(targetfolder)) {
                        Directory.Delete(targetfolder, true);
                    }
                    var tcreator = new TemplateCreator();
                    tcreator.CreateProject(pkgtemplate, targetfolder, "MyNewConsoleProject", null);
                }

                var template = CreateTemplate();
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(template, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine(result);

                string destFolder = @"c:\temp\dn-waffle\webapi";
                if ( !string.IsNullOrWhiteSpace(destFolder) && Directory.Exists(destFolder)) {
                    Directory.Delete(destFolder, true);
                }
                var creator = new TemplateCreator();
                creator.CreateProject(template, destFolder, "MyNewProject", null);

                var manager = new TemplateSourceManager();
                var settingsFile = @"C:\temp\dn-waffle\settings.json";
                var sourceFolder = @"C:\Data\mycode\dotnet-waffle\samples";
                manager.AddTemplateSource(settingsFile, TemplateSource.NewFolderSource(sourceFolder));

                destFolder = @"c:\temp\dn-waffle\console";
                if (!string.IsNullOrWhiteSpace(destFolder) && Directory.Exists(destFolder)) {
                    Directory.Delete(destFolder, true);
                }
                template = Template.BuildFromFile(@"C:\Users\sayedha\Documents\Visual Studio 2015\Projects\SamplesForDotnetWaffle\src\SampleConsoleApp\waffle.json");
                creator.CreateProject(template, destFolder, "MyNewConsoleApp", null);
                
            }
            catch(Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private static Template CreateTemplateFromPackage(string packageName,string packageVersion, string templateName) {
            List<Template> pkgTemplates = new TemplateManager().GetTemplatesFromPackage(packageName, packageVersion).ToList();
            Template result = (from t in pkgTemplates
                               where t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase)
                               select t).FirstOrDefault();
            if(result == null) {
                Console.WriteLine(string.Format("Template not found [pkg=[{0}]],ver=[{1}],template-name=[{2}] ", packageName, packageVersion, templateName));
                return null;
            }

            return result;
        }

        private static Template CreateTemplate() {
            var template = new Template("microsoft.aspnet.web.empty.rc2", TemplateType.ProjectTemplate, @"C:\Data\mycode\pecan-waffle\templates\aspnet5\WebApiProject");
            template.Alias.Add("empty-web");

            template.Replacements.Add(new Replacement("WebApiProject", "$ProjectName", "MyWebProject"));
            template.Replacements.Add(new Replacement(@"..\..\artifacts", "$Artifacts", @"..\..\artifacts"));
            template.Replacements.Add(new Replacement("a9914dea-7cf2-4216-ba7e-fecb82baa627", "$ProjectGuid", "$NewGuid()"));

            template.Excludes.Add(@"artifacts\*");
            template.Excludes.Add(@".vs\*");
            template.Excludes.Add(@"bin\*");
            template.Excludes.Add(@"debug\*");
            template.Excludes.Add("*.user");
            template.Excludes.Add("*.suo");
            template.Excludes.Add("project.lock.json");
            template.Excludes.Add("pw-templateinfo.ps1");
            template.PathReplacemets.Add(new Replacement("WebApiProject", "$ProjectName", "MyWebProject"));

            return template;
        }        
    }
}
