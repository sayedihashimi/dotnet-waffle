using Microsoft.Dnx.Runtime.Common.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dotnet_waffle {
    public class Program
    {
        public static int Main(string[] args) {
            try {
                return new Program().Run(args);
            }
            catch (Exception ex) {
                Console.Error.WriteLine("FAIL: {0}", ex);
                return 1;
            }
        }

        public int Run(string[] args) {
            //var templateManager = new TemplateSourceManager(new TemplateHelper.GetSourcesFilePath());
            var templateMan = new TemplateManager(new TemplateSourceManager(Helper.GetSourcesFilePath()));
            var app = new CommandLineApplication();
            app.Name = "dotnet-waffle";
            app.Description = "The dotnet-new2 command is used to create .NET Core projects using templates.";

            app.HelpOption("-?|-h|--help");

            app.Command("list", command => {
                command.Description = "shows the installed templates";
                command.HelpOption("-?|-h|--help");                
                command.OnExecute(() => {
                    // get and display the templates
                    // var sources = sourceManager.GetTemplateSources();
                    var installedTemplates = templateMan.GetInstalledTemplates();                    
                    PrintTemplates(installedTemplates);

                    return 0;
                });
            });

            app.Command("listsource", command => {
                command.Description = "shows the template sources that have been added";
                command.OnExecute(() => {
                    var sources = templateMan.SourceManager.GetTemplateSources();
                    PrintSources(sources);
                    return 0;
                });
            });
            
            app.Command("add", command => {
                command.Description = "Used to add a template source";
                command.HelpOption("-?|-h|--help");

                var pkgOption = command.Option("-p|--package <packagename>", "Name of the NuGet package that contains templates to install", CommandOptionType.SingleValue);
                var pkgVerOption = command.Option("-v|--version <version>", "Version of the NuGet package", CommandOptionType.SingleValue);

                var gitOption = command.Option("-g|--giturl <giturl>", "URL for the git repo which contains templates to install", CommandOptionType.SingleValue);
                var gitBranchOption = command.Option("-b|--gitbranchname <branchname>", "Name of the branch for the git repo", CommandOptionType.SingleValue);

                var pathOption = command.Option("-f|--folder <folder-or-file-path>", "Path to the folder to add templates from", CommandOptionType.SingleValue);

                command.OnExecute(() => {
                    if(pathOption.HasValue() && !string.IsNullOrWhiteSpace(pathOption.Value())){
                        string path = pathOption.Value();
                        if (string.IsNullOrWhiteSpace(path)) {
                            Console.WriteLine("path is empty");
                            command.ShowHelp();
                            return -1;
                        }

                        templateMan.SourceManager.AddTemplateSource(TemplateSource.NewFolderSource(path));
                        PrintTemplates(templateMan.GetInstalledTemplates());

                        Console.Write("folder selected [{0}]", path);
                        return 0;
                    }
                    else if(pkgOption.HasValue() && !string.IsNullOrWhiteSpace(pkgOption.Value())){
                        string pkgName = pkgOption.Value();
                        string pkgVersion = pkgVerOption.Value();

                        if (string.IsNullOrWhiteSpace(pkgName) || string.IsNullOrWhiteSpace(pkgVersion)) {
                            Console.WriteLine("package name or version missing");
                            command.ShowHelp();
                            return -1;
                        }

                        templateMan.SourceManager.AddTemplateSource(TemplateSource.NewNuGetSource(pkgName, pkgVersion));
                        PrintTemplates(templateMan.GetInstalledTemplates());

                        return 0;
                    }
                    else if(gitOption.HasValue() && !string.IsNullOrWhiteSpace(gitOption.Value())) {
                        string gitUrl = gitOption.Value();
                        string gitBranch = gitBranchOption.Value();

                        if (string.IsNullOrWhiteSpace(gitUrl) || string.IsNullOrWhiteSpace(gitBranch)) {
                            Console.WriteLine("git url or branch missing");
                            command.ShowHelp();
                            return -1;
                        }

                        templateMan.SourceManager.AddTemplateSource(TemplateSource.NewGitSource(new Uri(gitUrl), gitBranch));

                        Console.Write("git selected [{0},{1}]", gitOption.Value(), gitBranch);
                        return 0;
                    }
                    else {
                        command.ShowHelp();
                        return -1;
                    }
                    // check for git
                    // check for package
                    // check for folder/file

                    command.ShowHelp();
                    return 0;
                });
            });
            
            return app.Execute(args);
        }
        private void PrintTemplates(IEnumerable<Template> templates) {
            Console.WriteLine("--- Installed templates ---");
            if (!templates.Any()) {
                Console.WriteLine("No templates installed");
                return;
            }

            var maxNameLength = templates.Max(t => t.Name.Length);
            foreach (var template in templates) {
                var padding = new string(' ', maxNameLength - template.Name.Length);
                Console.WriteLine($"  - {template.Name} {padding} [{template.Source.GetSourceString()}]");
            }
        }
        private void PrintSources(IEnumerable<TemplateSource> sources) {
            Console.WriteLine("--- Installed template sources ---");
            if (!sources.Any()) {
                Console.WriteLine("No templates sources installed");
                return;
            }

            var maxNameLength = sources.Max(s => s.GetSourceString().Length);
            foreach (var source in sources) {
                var padding = new string(' ', maxNameLength - source.GetSourceString().Length);
                Console.WriteLine($"  - {source.Type} {padding} [{source.GetSourceString()}]");
            }
        }
        public static void OldMain(string[] args)
        {
            try {
                var pkgtemplate = GetTemplateFromPackage("DotnetSampleConsoleApp", "1.0.0", "microsoft.dotnet.console.rc2");
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

                string giturl = @"https://github.com/sayedihashimi/dotnet-waffle.git";
                string gitbranch = "master";
                string templatename = "microsoft.dotnet.console.rc2";
                template = GetTemplateFromGit(giturl, gitbranch, templatename);
                destFolder = @"C:\temp\dn-waffle\gitconsole";
                if (!string.IsNullOrWhiteSpace(destFolder) && Directory.Exists(destFolder)) { Directory.Delete(destFolder, true); }
                creator.CreateProject(template, destFolder, "MyNewConsoleProject", null);

                var settingsFile = @"C:\temp\dn-waffle\settings.json";
                var manager = new TemplateSourceManager(settingsFile);                
                var sourceFolder = @"C:\Data\mycode\dotnet-waffle\samples";
                manager.AddTemplateSource(TemplateSource.NewFolderSource(sourceFolder));

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

        private static Template GetTemplateFromGit(string gitUrl, string gitBranch, string templateName) {
            var templates = new TemplateManager(new TemplateSourceManager(Helper.GetSourcesFilePath())).GetTemplatesFromGit(gitUrl, gitBranch);
            Template result = (from t in templates
                               where t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase)
                               select t).FirstOrDefault();
            if (result == null) {
                Console.WriteLine(string.Format("Template not found [url=[{0}]],branch=[{1}],template-name=[{2}] ", gitUrl, gitBranch, templateName));
                return null;
            }

            return result;
        }

        private static Template GetTemplateFromPackage(string packageName,string packageVersion, string templateName) {
            List<Template> pkgTemplates = new TemplateManager(new TemplateSourceManager(Helper.GetSourcesFilePath())).GetTemplatesFromPackage(packageName, packageVersion).ToList();
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
