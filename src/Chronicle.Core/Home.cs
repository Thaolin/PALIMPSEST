namespace Chronicle.Core;

public enum HomeMaterialState
{
    HearthstoneRaised = 1,
}

public sealed record HomeState(
    string HoldingId,
    string DisplayName,
    WorldAddress Address,
    long FoundedTick,
    long FoundingIncarnationId,
    HomeMaterialState Material);

public readonly record struct HomeSiteSnapshot(
    WorldAddress Address,
    WorldGround Ground,
    WorldFeature? Feature,
    string? DurableIdentity,
    bool IsEligible,
    string Reason);

public readonly record struct ReturnRouteSnapshot(
    WorldAddress Destination,
    bool IsTraversable,
    bool Arrived,
    WorldAddress? NextAddress,
    UInt128 RemainingSteps);

public sealed record HomeContextSnapshot(
    HomeState? Home,
    HomeSiteSnapshot CurrentSite,
    ReturnRouteSnapshot? ReturnRoute);
