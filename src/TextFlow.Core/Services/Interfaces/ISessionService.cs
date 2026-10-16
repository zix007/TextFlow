using System.Threading.Tasks;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface ISessionService
{
    Task SaveSessionAsync(SessionModel session);
    Task<SessionModel?> LoadSessionAsync();
    Task ClearSessionAsync();
}
