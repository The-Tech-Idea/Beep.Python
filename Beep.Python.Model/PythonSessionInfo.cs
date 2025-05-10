using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class PythonSessionInfo
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public bool WasSuccessful { get; set; }
        public string Notes { get; set; }
        public string VirtualEnvironmentId { get; set; }
        public string Username { get; set; }
    }

}
