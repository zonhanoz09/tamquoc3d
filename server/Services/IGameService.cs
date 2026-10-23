using TamQuoc3D.Server.Contracts;

namespace TamQuoc3D.Server.Services;

public interface IGameService
{
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<object> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<object> GetWalletAsync(Guid userId, CancellationToken cancellationToken);
    Task<object> ClaimStaminaAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetHeroesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetFormationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<object> UpdateFormationAsync(Guid userId, FormationUpdateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetCampaignProgressAsync(Guid userId, CancellationToken cancellationToken);
    Task<object> ClearCampaignStageAsync(Guid userId, CampaignClearRequest request, CancellationToken cancellationToken);
    Task<object> StarUpHeroAsync(Guid userId, HeroStarUpRequest request, CancellationToken cancellationToken);
    Task<object> PullGachaAsync(Guid userId, GachaPullRequest request, CancellationToken cancellationToken);
    Task<object> SimulateBattleAsync(Guid userId, BattleSimulationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetMailAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, long>> GetDatabaseOverviewAsync(CancellationToken cancellationToken);
}
