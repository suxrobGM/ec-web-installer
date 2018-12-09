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
        public ExistedModFilesException(string message) : base(message) { }
        public ExistedModFilesException(string message, Exception inner) : base(message, inner) { }
        protected ExistedModFilesException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
