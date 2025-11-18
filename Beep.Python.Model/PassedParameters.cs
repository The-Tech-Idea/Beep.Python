using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class PassedParameters
    {
        public PassedParameters() { }
        public bool Verbose { get; set; } = false;
        public bool Interactive { get; set; } = false;
        public bool IsInteractive { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string TypeDescription { get; set; }
        public string DescriptionDescription { get; set; }
        public string TypeName { get; set; }
        public string TypeDescriptionDescription { get; set; }
        public string Id { get; set; }
        public string EnvId { get; set; }
        public string EnvName { get; set; }
        public string EnvPath { get; set; }
        public string SessionId { get; set; }
        public string SessionName { get; set; }
        public string SessionDescription { get; set; }
        public string SessionType { get; set; }
        public string SessionTypeDescription { get; set; }
            public int Timeout { get; set; } = 30;
        public int TimeoutDescription { get; set; }
        public int TimeoutDescriptionDescription { get; set; }
            private string ErrorMessage { get; set; }
        public string ErrorDescription { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorHelpLink { get; set; }
        public string ErrorCodeDescription { get; set; }
        public DateTime ErrorDateTime { get; set; }
        public string ErrorDateTimeDescription { get; set; }
        public DateTime EventDateTime { get; set; }
        public string Message { get; set; }
        public Errors Flag { get; set; }
        public string Event { get; set; }
        public string EventType { get; set; }
        public Exception Ex { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; } = new Dictionary<string, object>();
    }
}
