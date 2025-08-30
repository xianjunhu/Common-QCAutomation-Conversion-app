using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Exceptions
{
    public class InvalidConfigurationException : Exception
    {
        private string _configurationKey;

        public InvalidConfigurationException(string configurationKey)
            : base($"Invalid configuration value by '{configurationKey}' key.")
        {
            _configurationKey = configurationKey;
        }
    }
}
