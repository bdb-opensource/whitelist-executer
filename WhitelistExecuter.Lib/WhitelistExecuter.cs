using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;

namespace WhitelistExecuter.Lib
{
    public class WhitelistExecuter : IWhitelistExecuter
    {
        protected static readonly ILog _logger = log4net.LogManager.GetLogger("Executer");

        protected static readonly string[] _allowedBaseDirs = AllowedBaseDirs();

        protected static readonly Dictionary<string, string[]> _paths = GetPathsFromFilesystem();

        protected class AppKeys
        {
            public const string BASE_DIRS = "BaseDirs";

            public const string GIT_EXE = "GitExe";

            public const string PROCESS_TIMEOUT_SECONDS = "ProcessTimeoutSeconds";
        }

        #region IWhitelistExecuter Members

        public ExecutionResult ExecuteCommand(string baseDir, Command command, string relativeWorkingDir)
        {
            if (false == _allowedBaseDirs.Any(x => x.Equals(baseDir, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException("Base dir: " + baseDir + " is not whitelisted.");
            }

            Directory.SetCurrentDirectory(baseDir);
            var absPath = Path.GetFullPath(Path.Combine(baseDir, relativeWorkingDir));
            if ((false == absPath.StartsWith(baseDir)) || (Path.IsPathRooted(relativeWorkingDir)))
            {
                throw new ArgumentException("Expecting relative sub-path, not: " + relativeWorkingDir, "relativeWorkingDir");
            }
            Directory.SetCurrentDirectory(absPath);
            switch (command)
            {
                case Command.GIT_FETCH: return RunGit("fetch");
                case Command.GIT_PULL: return RunGit("pull");
                case Command.GIT_STATUS: return RunGit("status");
                default:
                    throw new ArgumentException("Unsupported command: " + command.ToString(), "command");
            }
        }

        private static string[] AllowedBaseDirs()
        {
            return ConfigurationManager.AppSettings[AppKeys.BASE_DIRS]
                .Split(';')
                .Select(x => x.Trim())
                .ToArray();
        }

        public List<KeyValuePair<string, string[]>> GetPaths()
        {
            return _paths.Select(x => x).ToList();
        }

        #endregion

        #region Protected Methods

        protected static Dictionary<string, string[]> GetPathsFromFilesystem()
        {
            var result = new Dictionary<string, string[]>();
            foreach (var baseDir in _allowedBaseDirs)
            {
                Directory.SetCurrentDirectory(baseDir);
                var baseDirInfo = new DirectoryInfo(baseDir);
                result.Add(baseDir,
                    baseDirInfo.GetFileSystemInfos(".git", SearchOption.AllDirectories)
                                  .Select(x => new DirectoryInfo(Path.GetDirectoryName(x.FullName)))
                                  .Select(x =>
                                      String.Join(Path.DirectorySeparatorChar.ToString(),
                                                  ParentsUpTo(x, baseDirInfo).Reverse().Select(p => p.Name)))
                                  .ToArray());
            }
            return result;
        }

        protected static IEnumerable<DirectoryInfo> ParentsUpTo(DirectoryInfo subPath, DirectoryInfo basePath)
        {
            var cur = subPath;
            while ((null != cur) && (cur.FullName != basePath.FullName))
            {
                yield return cur;
                cur = cur.Parent;
            }
        }

        protected ExecutionResult RunGit(string args)
        {
            var startInfo = new ProcessStartInfo(ConfigurationManager.AppSettings[AppKeys.GIT_EXE], args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            using (var process = Process.Start(startInfo))
            {
                var hasExited = process.WaitForExit(1000 * Int32.Parse(ConfigurationManager.AppSettings[AppKeys.PROCESS_TIMEOUT_SECONDS]));
                var stdOut = process.StandardOutput.ReadToEnd();
                var stdError = process.StandardError.ReadToEnd();
                if (false == hasExited)
                {
                    _logger.Error("Timeout when running: git " + args + " in directory " + Directory.GetCurrentDirectory());
                    _logger.Error("Execution stdout:" + stdOut);
                    _logger.Error("Execution stderr:" + stdError);
                    throw new Exception("Execution timed out: git " + args);
                }
                return new ExecutionResult
                {
                    StandardError = stdError,
                    StandardOutput = stdOut,
                    ExitCode = process.ExitCode
                };
            }
        }

        #endregion
    }
}
