using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WhitelistExecuter.Lib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IWhitelistExecuter
    {
        [OperationContract]
        ExecutionResult ExecuteCommand(string baseDir, Command command, string relativeWorkingDir);

        /// <summary>
        /// Returns a list of key-value pairs, where the key is a base path and the value is a list of subdirs in that base path.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<KeyValuePair<string, string[]>> GetPaths();
    }

    [DataContract]
    public enum Command
    {
        [EnumMember]
        GIT_STATUS,

        [EnumMember]
        GIT_FETCH,

        [EnumMember]
        GIT_DIFF,

        [EnumMember]
        GIT_PULL,

        [EnumMember]
        SERVICES_STATUS,

        [EnumMember]
        DEPLOY_SERVICES,
    }

    [DataContract]
    public class ExecutionResult
    {
        [DataMember]
        public string StandardOutput;

        [DataMember]
        public string StandardError;

        [DataMember]
        public int ExitCode;
    }
}
