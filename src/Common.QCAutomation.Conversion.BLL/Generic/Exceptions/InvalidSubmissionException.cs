using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Exceptions
{
    public class InvalidSubmissionException : Exception
    {
        public InvalidSubmissionException(string message) : base(message) { }
    }
}
