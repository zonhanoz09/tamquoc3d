using System.Security.Claims;
using TamQuoc3D.Server.Contracts;
using TamQuoc3D.Server.Services;

namespace TamQuoc3D.Server.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var player = endpoints.MapGroup("/api").RequireAuthorization().WithTags("Player data");
        player.MapGet("/profile", async (ClaimsPrincipal principal, IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetProfileAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetProfile")
            .WithSummary("Get the authenticated player's profile.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        player.MapGet("/wallet", async (ClaimsPrincipal principal, IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetWalletAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetWallet")
            .WithSummary("Get the authenticated player's wallet.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        player.MapPost("/wallet/claim-stamina", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.ClaimStaminaAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("ClaimStamina")
            .WithSummary("Claim stamina recovery for the authenticated player.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        player.MapGet("/heroes", async (ClaimsPrincipal principal, IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetHeroesAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetHeroes")
            .WithSummary("List heroes owned by the authenticated player.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/heroes/star-up", async (
                HeroStarUpRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.StarUpHeroAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("StarUpHero")
            .WithSummary("Raise an owned hero's star, paying the server-side gold cost.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/shop/items", async (
                IGameService service,
                CancellationToken ct) => TypedResults.Ok(await service.GetShopItemsAsync(ct)))
            .WithName("GetShopItems")
            .WithSummary("List the server-defined shop catalog.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/shop/buy", async (
                ShopBuyRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.BuyShopItemAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("BuyShopItem")
            .WithSummary("Buy a shop item at the server-side price.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/inventory", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.GetInventoryAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetInventory")
            .WithSummary("List inventory items with quantity > 0.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/inventory/use", async (
                UseItemRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.UseItemAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("UseItem")
            .WithSummary("Use a consumable inventory item on a hero.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/arena/profile", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.GetArenaProfileAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetArenaProfile")
            .WithSummary("Get the player's arena rank profile and team power.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/arena/opponents", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.GetArenaOpponentsAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetArenaOpponents")
            .WithSummary("List nearby-MMR arena opponents with real team power.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/tower/progress", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.GetTowerProgressAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetTowerProgress")
            .WithSummary("Get the player's tower progress.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/tower/clear", async (
                TowerClearRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.ClearTowerFloorAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("ClearTowerFloor")
            .WithSummary("Claim the server-side reward for the next tower floor.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/formations", async (ClaimsPrincipal principal, IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetFormationsAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetFormations")
            .WithSummary("List formations belonging to the authenticated player.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/formations", async (
                FormationUpdateRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.UpdateFormationAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("UpdateFormation")
            .WithSummary("Create or update a player's formation.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/campaign/progress", async (
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.GetCampaignProgressAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetCampaignProgress")
            .WithSummary("Get campaign progress for the authenticated player.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/campaign/clear", async (
                CampaignClearRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.ClearCampaignStageAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("ClearCampaignStage")
            .WithSummary("Record campaign completion and grant rewards.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/gacha/pull", async (
                GachaPullRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.PullGachaAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("PullGacha")
            .WithSummary("Spend gold on one or ten hero pulls.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapPost("/battle/simulate", async (
                BattleSimulationRequest request,
                ClaimsPrincipal principal,
                IGameService service,
                CancellationToken ct) =>
                TypedResults.Ok(await service.SimulateBattleAsync(AuthEndpoints.GetUserId(principal), request, ct)))
            .WithName("SimulateBattle")
            .WithSummary("Simulate a battle and persist its replay.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        player.MapGet("/mail", async (ClaimsPrincipal principal, IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetMailAsync(AuthEndpoints.GetUserId(principal), ct)))
            .WithName("GetMail")
            .WithSummary("List unexpired mail for the authenticated player.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/db-overview", async (IGameService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetDatabaseOverviewAsync(ct)))
            .RequireAuthorization()
            .WithName("GetDatabaseOverview")
            .WithSummary("Return counts for known game database tables.")
            .WithTags("Operations");
        return endpoints;
    }
}
