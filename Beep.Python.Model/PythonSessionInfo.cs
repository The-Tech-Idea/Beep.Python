using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public enum PythonSessionStatus
    {
        Active,
        Inactive,
        Terminated
    }
    public class PythonSessionInfo:Entity
    {
        public PythonSessionInfo() {
            _id = Guid.NewGuid().ToString(); 
             }
        private string _id;
        public string SessionId
        {
            get { return _id; }
            set
            {
                _id = value;
               SetProperty(ref _id, value);
            }
        }
        private string _sessionName;
        public string SessionName
        {
            get { return _sessionName; }
            set
            {
                _sessionName = value;
                SetProperty(ref _sessionName, value);
            }
        }
        private DateTime _startedAt;
        public DateTime StartedAt
        {
            get { return _startedAt; }
            set
            {
                _startedAt = value;
                SetProperty(ref _startedAt, value);
            }
        }
        private DateTime? DateTime;
        public DateTime? EndedAt
        {
            get { return DateTime; }
            set
            {
                DateTime = value;
                SetProperty(ref DateTime, value);
            }
        }
        private bool _wasSuccessful;
        public bool WasSuccessful
        {
            get { return _wasSuccessful; }
            set
            {
                _wasSuccessful = value;
                SetProperty(ref _wasSuccessful, value);
            }
        }
        private string _notes;
        public string Notes
        {
            get { return _notes; }
            set
            {
                _notes = value;
                SetProperty(ref _notes, value);
            }
        }
        private string _virtualEnvironmentId;
        public string VirtualEnvironmentId
        {
            get { return _virtualEnvironmentId; }
            set
            {
                _virtualEnvironmentId = value;
                SetProperty(ref _virtualEnvironmentId, value);
            }
        }
        private string _username;
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                SetProperty(ref _username, value);
            }
        }
        private PythonSessionStatus _status= PythonSessionStatus.Active;
        public PythonSessionStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                SetProperty(ref _status, value);
            }
        }
    }

}
