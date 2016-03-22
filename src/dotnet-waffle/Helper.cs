using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class Helper
    {
        public static string GetTempDirectory() {
            string assemblyDir = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
            string tempDir = Path.Combine(assemblyDir, ".templatetemp");
            if (!Directory.Exists(tempDir)) {
                Directory.CreateDirectory(tempDir);
            }

            return tempDir;
        }

        public static string GetSourcesFilePath() {
            return Path.Combine(GetTempDirectory(), "sources.json");
        }

        public static string GetNewTempWorkingDir() {
            var path = Path.Combine(GetTempDirectory(), DateTime.UtcNow.Ticks.ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetRootGitDir() {
            string gitDir = Path.Combine(GetTempDirectory(), "git");
            if (!Directory.Exists(gitDir)) {
                Directory.CreateDirectory(gitDir);
            }

            return gitDir;
        }

        private static string GetGitRepoNameFor(string gitUrl) {
            string repoName = string.Empty;
            int startindex = gitUrl.LastIndexOf("/");
            if (startindex > 0) {
                repoName = gitUrl.Substring(startindex + 1).Replace(".git", string.Empty);
            }
            else {
                throw new InvalidOperationException(string.Format("Unable to get repo name from url [{0}]", gitUrl));
            }

            return repoName;
        }

        public static string GetLocalGitExpectedDirFor(string gitUrl, string branchName) {
            return Path.Combine(GetRootGitDir(), GetGitRepoNameFor(gitUrl));
        }

        public static string EnsureGitIsClonedLocally(string gitUrl,string branchName) {
            string expectedPath = GetLocalGitExpectedDirFor(gitUrl, branchName);
            if (Directory.Exists(expectedPath)) {
                return expectedPath;
            }
            string repoName = GetGitRepoNameFor(gitUrl);
            string commandArgs = string.Format("clone {0} --branch {1} --single-branch {2}",gitUrl,branchName,repoName);

            var processInfo = new ProcessStartInfo("git.exe", commandArgs);
            processInfo.WorkingDirectory = GetRootGitDir();
            var cloneCmd = Process.Start(processInfo);
            cloneCmd.WaitForExit();

            int exitCode = cloneCmd.ExitCode;

            if (!Directory.Exists(expectedPath)) {
                throw new InvalidOperationException(string.Format("Unable to get repo from [url={0},branch={1}]",gitUrl,gitUrl));
            }

            return expectedPath;
        }

        public static string UpdateGitRepo(string gitUrl, string branchName) {
            string gitpath = EnsureGitIsClonedLocally(gitUrl, branchName);

            string commandArgs = "pull";

            var processInfo = new ProcessStartInfo("git.exe", commandArgs);
            processInfo.WorkingDirectory = GetRootGitDir();
            var pullCmd = Process.Start(processInfo);
            pullCmd.WaitForExit();

            return gitpath;
        }

        public static string RestorePackage(string packageName, string packageVersion, bool ignoreExitCode = true) {
            string expectedPackagePath = Path.Combine(GetNuGetPackagesPath(), packageName, packageVersion);
            if (Directory.Exists(expectedPackagePath)) {
                return expectedPackagePath;
            }

            // create a project.json with the package name listed and then restore it
            string dir = GetNewTempWorkingDir();
            try {
                string tempprojjsonpath = Path.Combine(dir, "project.json");
                string projJsonContent = @"
{
  ""dependencies"": {
    ""<pkgname>"": ""<pkgversion>""
  },
  ""frameworks"": {
    ""netstandardapp1.5"": {
      ""imports"": ""dnxcore50""
    }
  }
}";
                projJsonContent = projJsonContent.Replace("<pkgname>", packageName).Replace("<pkgversion>", packageVersion);
                File.WriteAllText(tempprojjsonpath, projJsonContent);

                // run restore
                var processInfo = new ProcessStartInfo("dotnet.exe", "restore -f C:\\temp\\nuget");
                processInfo.WorkingDirectory = Path.GetDirectoryName(dir);
                var restore = Process.Start(processInfo);
                restore.WaitForExit();

                int exitCode = restore.ExitCode;
                if (exitCode != 0 && !ignoreExitCode) {
                    throw new InvalidOperationException(string.Format("Exit code for restore was not 0 [{exit code = 0}]", exitCode));
                }

                // string expectedPackagePath = string.Format(@"{0}{1}\{2}",GetNuGetPackagesPath(), packageName, packageVersion);
                
                if (!Directory.Exists(expectedPackagePath)) {
                    throw new InvalidOperationException(string.Format("NuGet package not found at [{0}]", expectedPackagePath));
                }

                return expectedPackagePath;
            }
            finally {
                if(!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
            
        }
        public static string GetNuGetPackagesPath() {
            return Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages\");
        }
        public static IList<string> GetFiles(string rootDir, IList<string> includes, IList<string> excludes) {
            string includeStr = null;
            string excludeStr = null;

            if (includes != null) {
                var sb = new StringBuilder();
                foreach (var item in includes) {
                    sb.Append(item);
                    sb.Append("; ");
                }
                includeStr = sb.ToString();
            }
            if (excludes != null) {
                var sb = new StringBuilder();
                foreach (var item in excludes) {
                    sb.Append(item);
                    sb.Append(";");
                }
                excludeStr = sb.ToString();
            }

            return GetFiles(rootDir, includeStr, excludeStr);
        }

        public static IList<string> GetFiles(string rootDir, string include, string exclude) {
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

        public static IEnumerable<string> Search(string root, string searchPattern) {
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
