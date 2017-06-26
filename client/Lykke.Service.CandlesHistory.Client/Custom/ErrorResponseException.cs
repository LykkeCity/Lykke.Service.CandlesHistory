using System;
using Lykke.Service.CandlesHistory.Client.Models;

namespace Lykke.Service.CandlesHistory.Client.Custom
{
    public class ErrorResponseException : Exception
    {
        public ErrorResponse Error { get; }

        public ErrorResponseException(ErrorResponse error)
        {
            Error = error;
        }

        public ErrorResponseException(ErrorResponse error, Exception inner) :
            base(string.Empty, inner)
        {
            Error = error;
        }
    }
}