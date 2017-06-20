using System.Text;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace Lykke.Service.CandlesHistory.Services.RabbitMq
{
    public class JsonMessageDeserializer<T> : IMessageDeserializer<T>
    {
        private readonly Encoding _encoding;

        public JsonMessageDeserializer() :
            this(Encoding.UTF8)
        {
        }

        public JsonMessageDeserializer(Encoding encoding)
        {
            _encoding = encoding;
        }

        public T Deserialize(byte[] data)
        {
            var json = _encoding.GetString(data);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}