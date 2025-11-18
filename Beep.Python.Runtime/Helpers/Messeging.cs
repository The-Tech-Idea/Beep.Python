using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Helpers
{
    public static class Messaging
    {
        public static void AddLogMessage(string source, string message, DateTime timestamp, int userId, string additionalInfo, Errors error)
        {
            // Implementation for logging the message

        }
        public static string GetPackageStatusMessage(string packageName, string status)
        {
            return $"Package: {packageName}, Status: {status}";
        }
        public static string GetAlgorithmParamsMessage(string algorithmName, string parameterName, string parameterValue)
        {
            return $"Algorithm: {algorithmName}, Parameter: {parameterName}, Value: {parameterValue}";
        }
        public static string GetEnvironmentInfoMessage(string envName, string envPath, string pythonVersion)
        {
            return $"Environment Name: {envName}, Path: {envPath}, Python Version: {pythonVersion}";
        }
    }
}
