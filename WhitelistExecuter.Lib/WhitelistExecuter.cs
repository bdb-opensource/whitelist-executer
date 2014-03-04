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
        protected static ILog _logger = log4net.LogManager.GetLogger("Executer");

        protected class AppKeys
        {
            public const string BASE_DIR = "BaseDir";

            public const string GIT_EXE = "GitExe";

            public const string PROCESS_TIMEOUT_SECONDS = "ProcessTimeoutSeconds";
        }

        #region IWhitelistExecuter Members

        public ExecutionResult ExecuteCommand(Command command, string relativeWorkingDir)
        {
            var baseDir = ConfigurationManager.AppSettings[AppKeys.BASE_DIR];
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

        public string[] GetPaths()
        {
            var baseDir = ConfigurationManager.AppSettings[AppKeys.BASE_DIR];
            Directory.SetCurrentDirectory(baseDir);
            var baseDirInfo = new DirectoryInfo(baseDir);
            return baseDirInfo.GetDirectories(".git", SearchOption.AllDirectories)
                              .Select(x => 
                                  String.Join(Path.DirectorySeparatorChar.ToString(), 
                                              ParentsUpTo(x.Parent, baseDirInfo).Reverse().Select(p => p.Name)))
                              .Where(x => false == String.IsNullOrWhiteSpace(x))
                              .ToArray();
        }

        #endregion

        #region Protected Methods

        protected IEnumerable<DirectoryInfo> ParentsUpTo(DirectoryInfo subPath, DirectoryInfo basePath)
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
