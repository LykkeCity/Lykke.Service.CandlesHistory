using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Lykke.Service.CandlesHistory.Core.Domain.Candles
{
    public interface IFailedCandlesEnvelope
    {
        DateTime ProcessingMoment { get; }
        string Exception { get; }
        IEnumerable<ICandle> Candles { get; }
    }

    public class FailedCandlesEnvelope : IFailedCandlesEnvelope
    {
        public DateTime ProcessingMoment { get; set; }
        public string Exception { get; set; }
        public IEnumerable<ICandle> Candles { get; set; }

        public static FailedCandlesEnvelope Create(IFailedCandlesEnvelope src)
        {
            return new FailedCandlesEnvelope
            {
                ProcessingMoment = src.ProcessingMoment,
                Exception = src.Exception,
                Candles = src.Candles.Select(Candle.Create)
            };
        }
    }
}