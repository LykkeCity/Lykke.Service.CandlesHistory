// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class ConfigurationException : System.Exception
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(string message) :
            base(message)
        {
        }

        public ConfigurationException(string message, System.Exception inner) :
            base(message, inner)
        {
        }
    }
}
