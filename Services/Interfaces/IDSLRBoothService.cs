using System.Threading.Tasks;

namespace MiddleBooth.Services.Interfaces
{
    public interface IDSLRBoothService
    {
        bool CheckDSLRBoothPath();
        Task<bool> LaunchDSLRBooth();
        Task SetDSLRBoothTopmost(bool isTopmost);
        bool IsDSLRBoothRunning();
    }
}