using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services
{
    public interface ISnapshotSerializer
    {
        Task SerializeAsync();
        Task<bool> DeserializeAsync();
    }
}