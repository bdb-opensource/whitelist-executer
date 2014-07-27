using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using log4net;

namespace WhitelistExecuter.Lib
{
    public class WhitelistExecuter : IWhitelistExecuter
    {
        protected static readonly ILog _logger = log4net.LogManager.GetLogger("WhitelistExecuter");

        protected static readonly string[] _allowedBaseDirs = AllowedBaseDirs();

        protected static readonly Dictionary<string, string[]> _paths = GetPathsFromFilesystem();

        protected class AppKeys
        {
            public const string BASE_DIRS = "BaseDirs";

            public const string GIT_EXE = "GitExe";

            public const string PROCESS_TIMEOUT_SECONDS = "ProcessTimeoutSeconds";

            public const string SERVICES_FILE_PATH = "ServicesFilePath";
        }

        #region IWhitelistExecuter Members

        public ExecutionResult ExecuteCommand(string baseDir, Command command, string relativeWorkingDir)
        {
            var commandInfo = String.Format("command: {0} in {1}/{2}", command.ToString(), baseDir, relativeWorkingDir);
            _logger.InfoFormat("Preparing to execute " + commandInfo);
            try
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
                    case Command.GIT_DIFF: return RunGit("diff");
                    case Command.DEPLOY_SERVICES: return RunScript(absPath);
                    case Command.SERVICES_STATUS: return ServicesStatus(absPath);
                    default:
                        throw new ArgumentException("Unsupported command: " + command.ToString(), "command");
                }
            }
            catch (Exception e)
            {
                _logger.Error("Exception while executing " + commandInfo, e);
                throw;
            }
        }

        public List<KeyValuePair<string, string[]>> GetPaths()
        {
            return _paths.Select(x => x).ToList();
        }

        #endregion

        #region Protected Methods

        private ExecutionResult ServicesStatus(string path)
        {
            var services = GetServiceControllers(path);

            return new ExecutionResult()
            {
                ExitCode = 0,
                StandardOutput = String.Empty,
                StandardError = String.Join(Environment.NewLine, services.Select(x => String.Format("{0}: {1}", x.ServiceName, x.Status.ToString())))
            };
        }

        protected ExecutionResult RunScript(string path)
        {
            var services = GetServiceControllers(path);

            var fetchRes = RunGit("fetch");

            var statusPre = this.ServicesStatus(path);
            foreach (var service in services)
            {
                if (service.CanStop)
                {
                    service.Stop();
                }
            }
            foreach (var service in services)
            {
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }

            var statusPost = this.ServicesStatus(path);
            var pullRes = RunGit("pull");
            foreach (var service in services.Reverse())
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
            }
            return ConcatExecutionResults(statusPre, statusPost, fetchRes, pullRes, this.ServicesStatus(path));
        }

        private static ServiceController[] GetServiceControllers(string path)
        {
            var serviceNames = File.ReadAllLines(Path.Combine(path, ConfigurationManager.AppSettings[AppKeys.SERVICES_FILE_PATH]));
            var services = ServiceController.GetServices().Where(x => serviceNames.Any(n => n.Equals(x.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
            return services;
        }

        private static ExecutionResult ConcatExecutionResults(params ExecutionResult[] results)
        {
            return new ExecutionResult()
            {
                ExitCode = results[results.Length - 1].ExitCode,
                StandardError = results.Aggregate(String.Empty, (a, b) => a + Environment.NewLine + b.StandardError),
                StandardOutput = results.Aggregate(String.Empty, (a, b) => a + Environment.NewLine + b.StandardOutput)
            };
        }

        protected static string[] AllowedBaseDirs()
        {
            return ConfigurationManager.AppSettings[AppKeys.BASE_DIRS]
                .Split(';')
                .Select(x => x.Trim())
                .ToArray();
        }

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
            return ExecuteCommand(ConfigurationManager.AppSettings[AppKeys.GIT_EXE], args);
        }

        protected static ExecutionResult ExecuteCommand(string exePath, string args)
        {
            var startInfo = new ProcessStartInfo(exePath, args)
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
                    _logger.Error("Timeout when running: " + exePath + " " + args + " in directory " + Directory.GetCurrentDirectory());
                    _logger.Error("Execution stdout:" + stdOut);
                    _logger.Error("Execution stderr:" + stdError);
                    throw new Exception("Execution timed out: " + exePath + " " + args);
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
