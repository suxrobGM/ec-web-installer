using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC_OnlineInstaller.Models
{  
    [Serializable]
    public class ExistedModFilesException : Exception
    {
        public ExistedModFilesException() { }
        public ExistedModFilesException(string message, string existedModPath) : base(message)
        {
            ExistedModPath = existedModPath;
        }
        public ExistedModFilesException(string message, string existedModPath, Exception inner) : base(message, inner)
        {
            ExistedModPath = existedModPath;
        }
        protected ExistedModFilesException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public string ExistedModPath { get; }
    }
}
