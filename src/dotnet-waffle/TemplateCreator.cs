using FileReplacer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class TemplateCreator
    {
        public void CreateProject(Template template, string folderPath, string projectName, IDictionary<string,string>properties) {
            if (template == null) { throw new ArgumentNullException(nameof(template)); }

            Dictionary<string, string> allProperties = new Dictionary<string, string>();
            if(properties != null) {
                foreach(var key in properties.Keys) {
                    allProperties[key] = properties[key];
                }
            }

            if (!string.IsNullOrWhiteSpace(projectName)) {
                allProperties["ProjectName"] = projectName;
            }

            string templateWorkingDir = GetNewTempWorkingDir();

            try {
                // copy template source files to working dir
                string templateSourceRoot = GetLocalTemplateSourceFolder(template);
                var templateFiles = GetTemplateSourceFiles(template);
                CopyFiles(templateSourceRoot, templateFiles, templateWorkingDir);

                // perform replacements in working directory
                Dictionary<string, string> replacements = new Dictionary<string, string>();
                foreach(var rep in template.Replacements) {
                    string repKey = rep.Key;
                    string unevalRepValue = rep.Value;
                    string repValue = repValue = GetReplacementValue(rep, properties);

                    if (!string.IsNullOrEmpty(repValue)) {
                        replacements.Add(repKey, repValue);
                    }
                }

                var replacer = new RobustReplacer();
                replacer.ReplaceInFiles(templateWorkingDir, @"*.*", null, replacements);

                // update file and folder names
                foreach(var pathRep in template.PathReplacemets) {
                    var repkey = EvaluateString(pathRep.Key, properties);
                    var repvalue = GetReplacementValue(pathRep, properties);
                    if (string.IsNullOrWhiteSpace(repvalue)) {
                        continue;
                    }

                    // see if there are any directories that match
                    var dirToUpdate = Directory.GetDirectories(templateWorkingDir, repkey, SearchOption.AllDirectories);
                    foreach(var dir in dirToUpdate) {
                        if (!Directory.Exists(dir)) { continue; }

                        var newPath = dir.Replace(repkey, repvalue);
                        if (Directory.Exists(newPath)) {
                            throw new InvalidOperationException(string.Format("Directory to move to already exists. [olddir=[{0}],newdir=[{1}])", dir, newPath));
                        }
                        Directory.Move(dir, newPath);
                    }

                    // update filenames
                    var filesToUpdate = Directory.GetFiles(templateWorkingDir, repkey, SearchOption.AllDirectories);
                    foreach(var file in filesToUpdate) {
                        if (!File.Exists(file)) { continue; }

                        string destFile = file.Replace(repkey, repvalue);
                        string destFolder = new FileInfo(destFile).DirectoryName;
                        if (!Directory.Exists(destFolder)) {
                            Directory.CreateDirectory(destFolder);
                        }
                        File.Move(file, destFile);
                    }
                }

                // copy to final dest
                string actualDestFolder = folderPath;
                if (template.CreateNewFolder) {
                    actualDestFolder = Path.Combine(folderPath, projectName);
                    if (!Directory.Exists(actualDestFolder)) {
                        Directory.CreateDirectory(actualDestFolder);
                    }
                }
                
                CopyFiles(templateWorkingDir, Directory.GetFiles(templateWorkingDir, "*", SearchOption.AllDirectories), actualDestFolder);

            }
            finally {
                if (Directory.Exists(templateWorkingDir)) {
                    Directory.Delete(templateWorkingDir, true);
                }
            }
        }

        private string GetReplacementValue(Replacement replacement, IDictionary<string, string> properties) {
            string repKey = replacement.Key;
            string unevalRepValue = replacement.Value;
            string repValue = EvaluateString(unevalRepValue, properties);
            if (string.IsNullOrWhiteSpace(repValue) && !string.IsNullOrWhiteSpace(replacement.DefaultValue)) {
                repValue = EvaluateString(replacement.DefaultValue, properties);
            }

            return repValue;
        }

        private string EvaluateString(string str, IDictionary<string, string> properties) {
            if(str == null) { return null; }
            string result = str;

            string keyname = str;
            if (str.StartsWith("$")) {
                keyname = keyname.Substring(1);

                result = null;
                string propValue;
                if (properties != null && properties.TryGetValue(keyname, out propValue)) {
                    result = propValue;
                }

                // see if it's a guid and create a new one
                if (keyname != null && keyname.Equals("NewGuid()", StringComparison.OrdinalIgnoreCase)) {
                    result = Guid.NewGuid().ToString();
                }
            }

            return result;
        }
        protected IList<string> GetTemplateSourceFiles(Template template) {
            if(template.Source.SourceFiles !=null && template.Source.SourceFiles.Count > 0) {
                throw new NotImplementedException();
            }

            string templatesourcefolder = GetLocalTemplateSourceFolder(template);
            return GetFiles(templatesourcefolder, null, template.Excludes);
        }

        public void CopyFiles(string rootdir,IList<string> filesToCopy,string destFolder) {
            string rootDirFullPath = Path.GetFullPath(rootdir);
            foreach(string file in filesToCopy) {
                string filepath = file;
                if (!Path.IsPathRooted(filepath)) {
                    filepath = Path.Combine(rootdir, filepath);
                }

                string relpath = GetRelativePath(rootDirFullPath, filepath);
                string destfilepath = Path.Combine(destFolder, relpath);
                string itemfolder = new FileInfo(destfilepath).DirectoryName;
                if (!Directory.Exists(itemfolder)) {
                    Directory.CreateDirectory(itemfolder);
                }
                File.Copy(filepath, destfilepath);
            }
        }
        private bool IsDirectory(string path) {
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                return true;
            }
            return false;
        }
        private string GetRelativePath(string fromPath, string toPath) {
            string fromPathToUse = Path.GetFullPath(fromPath);
            if (IsDirectory(fromPathToUse)) {
                fromPathToUse += Path.DirectorySeparatorChar;
            }

            string toPathToUse = Path.GetFullPath(toPath);
            if (IsDirectory(toPathToUse)) {
                toPathToUse += Path.DirectorySeparatorChar;
            }

            var fromUri = new Uri(fromPathToUse);
            var toUri = new Uri(toPathToUse);

            string relPath = toPath;

            if (fromUri.Scheme.Equals(toUri.Scheme)) {
                var relUri = fromUri.MakeRelativeUri(toUri);
                relPath = Uri.UnescapeDataString(relUri.ToString());

                if (string.Equals(toUri.Scheme, "file", StringComparison.OrdinalIgnoreCase)) {
                    relPath = relPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                }
            }

            if (string.IsNullOrWhiteSpace(relPath)) {
                relPath = string.Format(".{0}",Path.DirectorySeparatorChar);
            }

            return relPath;

//           $fromPathToUse = (Resolve - Path $fromPath).Path
//        if ((Get - Item $fromPathToUse) -is [System.IO.DirectoryInfo]){
//            $fromPathToUse += [System.IO.Path]::DirectorySeparatorChar
//          }

//        $toPathToUse = (Resolve - Path $toPath).Path
//        if ((Get - Item $toPathToUse) -is [System.IO.DirectoryInfo]){
//            $toPathToUse += [System.IO.Path]::DirectorySeparatorChar
//          }

//        [uri]$fromUri = New-Object -TypeName 'uri' -ArgumentList $fromPathToUse
//  [uri]$toUri = New-Object -TypeName 'uri' -ArgumentList $toPathToUse

//  [string]$relPath = $toPath
//        #if the Scheme doesn't match just return toPath
//        if($fromUri.Scheme -eq $toUri.Scheme){
//            [uri]$relUri = $fromUri.MakeRelativeUri($toUri)
//            $relPath = [Uri]::UnescapeDataString($relUri.ToString())

//            if([string]::Equals($toUri.Scheme, [Uri]::UriSchemeFile, [System.StringComparison]::OrdinalIgnoreCase)){
//                $relPath = $relPath.Replace([System.IO.Path]::AltDirectorySeparatorChar,[System.IO.Path]::DirectorySeparatorChar)
//            }
//        }

//        if([string]::IsNullOrWhiteSpace($relPath)){
//            $relPath = ('.{0}' -f [System.IO.Path]::DirectorySeparatorChar)
//        }

//#'relpath:[{0}]' -f $relPath | Write-verbose

//# return the result here
//        $relPath
        }

        protected string GetLocalTemplateSourceFolder(Template template) {
            if (template == null) { throw new ArgumentNullException(nameof(template)); }

            if(template.Source.Type == SourceType.Git || template.Source.Type == SourceType.Package) {
                throw new NotImplementedException();
            }

            bool isRelpath = Path.IsPathRooted(template.Source.SourceFolder);

            string localsourcepath = template.Source.SourceFolder;
            if (string.IsNullOrWhiteSpace(localsourcepath)) {
                localsourcepath = "";
            }

            if (!Path.IsPathRooted(localsourcepath)) {
                string templateDir = new FileInfo(template.TemplateFilePath).DirectoryName;
                localsourcepath = Path.Combine(templateDir, template.Source.SourceFolder);
            }

            if (!Directory.Exists(localsourcepath)) {
                throw new DirectoryNotFoundException(string.Format("template directory not found at [{0}]", localsourcepath));
            }

            return localsourcepath;
        }

        private string GetTempDirectory() {
            string assemblyDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
            string tempDir = Path.Combine(assemblyDir, ".templatetemp");
            if (!Directory.Exists(tempDir)) {
                Directory.CreateDirectory(tempDir);
            }

            return tempDir;
        }

        private string GetNewTempWorkingDir() {
            var path = Path.Combine(GetTempDirectory(), DateTime.UtcNow.Ticks.ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        private IList<string>GetFiles(string rootDir, IList<string> includes, IList<string> excludes) {
            string includeStr = null;
            string excludeStr = null;

            if(includes != null) {
                var sb = new StringBuilder();
                foreach(var item in includes) {
                    sb.Append(item);
                    sb.Append(";");
                }
                includeStr = sb.ToString();
            }
            if(excludes != null) {
                var sb = new StringBuilder();
                foreach(var item in excludes) {
                    sb.Append(item);
                    sb.Append(";");
                }
                excludeStr = sb.ToString();
            }

            return GetFiles(rootDir, includeStr, excludeStr);
        }

        private IList<string>GetFiles(string rootDir, string include, string exclude) {
            if (string.IsNullOrWhiteSpace(include)) {
                include = "*.*";
            }

            string rootDirFullPath = Path.GetFullPath(rootDir);

            // search for all include files
            List<string> pathsToInclude = new List<string>();
            List<string> pathsToExclude = new List<string>();

            if (!string.IsNullOrEmpty(include)) {
                string[] includeParts = include.Split(';');
                foreach (string includeStr in includeParts) {
                    var results = Search(rootDirFullPath, includeStr);
                    foreach (var result in results) {
                        if (!pathsToInclude.Contains(result)) {
                            pathsToInclude.Add(result);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(exclude)) {
                string[] excludeParts = exclude.Split(';');
                foreach (string excludeStr in excludeParts) {
                    var results = Search(rootDirFullPath, excludeStr);
                    foreach (var result in results) {
                        if (!pathsToExclude.Contains(result)) {
                            pathsToExclude.Add(result);
                        }
                    }
                }
            }

            int numFilesExcluded = pathsToInclude.RemoveAll(p => pathsToExclude.Contains(p));

            return pathsToInclude.ToList();
        }

        static IEnumerable<string> Search(string root, string searchPattern) {
            // taken from: http://stackoverflow.com/a/438316/105999
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(root);
            while (dirs.Count > 0) {
                string dir = dirs.Dequeue();

                // files
                string[] paths = null;
                try {
                    paths = Directory.GetFiles(dir, searchPattern);
                }
                catch (Exception) { } // swallow

                if (paths != null && paths.Length > 0) {
                    foreach (string file in paths) {
                        yield return file;
                    }
                }

                // sub-directories
                paths = null;
                try {
                    paths = Directory.GetDirectories(dir);
                }
                catch (Exception) { } // swallow

                if (paths != null && paths.Length > 0) {
                    foreach (string subDir in paths) {
                        dirs.Enqueue(subDir);
                    }
                }
            }
        }
    }
}
