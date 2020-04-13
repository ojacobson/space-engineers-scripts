using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    class DebugOutput
    {
        HashSet<string> DebugEnabled = new HashSet<string>();
        List<string> DebugMessages = new List<string>();

        public void Enable(string debugFlag)
        {
            DebugEnabled.Add(debugFlag);
        }

        bool Allow(string debugFlag)
        {
            return DebugEnabled.Contains(debugFlag);
        }

        public void Log(string debugFlag, string message)
        {
            if (Allow(debugFlag))
            {
                DebugMessages.Add(message);
            }
        }

        public void Log(string message)
        {
            DebugMessages.Add(message);
        }

        public DebugSession For(string debugFlag)
        {
            return new DebugSession(this, debugFlag);
        }

        public void Clear()
        {
            DebugMessages.Clear();
        }

        public string Output
        {
            get
            {
                return String.Join("\n", DebugMessages);
            }
        }
    }

    struct DebugSession
    {
        private DebugOutput DebugOutput;
        private string Category;

        public DebugSession(DebugOutput debugOutput, string category)
        {
            this.DebugOutput = debugOutput;
            this.Category = category;
        }

        public void Log(string message)
        {
            DebugOutput.Log(Category, message);
        }
    }
}
