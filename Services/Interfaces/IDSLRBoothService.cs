using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IDSLRBoothService
    {
        bool CheckDSLRBoothPath();
        Task<bool> LaunchDSLRBooth();
        Task SetDSLRBoothVisibility(bool isVisible);
        bool IsDSLRBoothRunning();
    }
}