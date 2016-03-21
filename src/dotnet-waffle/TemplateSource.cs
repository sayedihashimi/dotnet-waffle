using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class TemplateSource
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceType Type { get; set; }

        // root path to template folder
        public string SourceFolder { get; set; }

        // if sourcefiles are specified then all other files are ignored
        public IList<string> SourceFiles { get; set; }

        // can specify a folder under Path for the actual sources
        // mosly used for git scenarios
        public string SourceRelPath { get; set; }

        // only used if SourceType == NuGet
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }

        // only used if SourceType == git
        protected Uri GitUrl { get; set; }
        protected string GitBranch { get; set; }
        

        public static TemplateSource NewFolderSource(string path) {
            return new TemplateSource() {
                SourceFolder = path
            };
        }

        public static TemplateSource NewGitSource(Uri url, string branchName, string relPath) {
            throw new NotImplementedException();
        }

        public static TemplateSource NewNuGetSource(string id, string version) {
            throw new NotImplementedException();
        }
    }
}
