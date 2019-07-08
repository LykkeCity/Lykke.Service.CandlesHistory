// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Service.CandlesHistory.Client.Models;
using Newtonsoft.Json;

namespace Lykke.Service.CandlesHistory.Client.Custom
{
    [Serializable]
    public class ErrorResponseException : Exception
    {
        public ErrorResponse Error { get; }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };

        public ErrorResponseException(ErrorResponse error) :
            base(GetMessage(error))
        {
            Error = error;
        }

        public ErrorResponseException(ErrorResponse error, Exception inner) :
            base(GetMessage(error), inner)
        {
            Error = error;
        }

        private static string GetMessage(ErrorResponse error)
        {
            return JsonConvert.SerializeObject(error, SerializerSettings);
        }
    }
}
