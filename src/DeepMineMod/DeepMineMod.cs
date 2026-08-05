using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace DeepMineMod;

/// <summary>
/// Main mod entry point. This implements IMod directly so Captain of Industry
/// invokes this mod's dependency and initialization lifecycle methods instead
/// of the non-virtual no-op implementations supplied by DataOnlyMod.
/// </summary>
public sealed class DeepMineMod : IMod {
    public ModManifest Manifest { get; }

    public DeepMineMod(ModManifest manifest) {
        Manifest = manifest;
        Log.Info("DeepMineMod: loaded");
    }

    public void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }

    public void RegisterDependencies(
        DependencyResolverBuilder depBuilder,
        ProtosDb protosDb,
        bool gameWasLoaded)
    {
        // DeepMineBrushTool carries GlobalDependency, so it is discovered by
        // the game's dependency scanner. Initialize() resolves it explicitly
        // because dependencies are otherwise created lazily.
        Log.Info("DeepMineMod: dependency registration reached");
    }

    public void EarlyInit(DependencyResolver resolver) {
        Log.Info("DeepMineMod: early initialization reached");
    }

    public void Initialize(DependencyResolver resolver, bool gameWasLoaded) {
        resolver.Resolve<DeepMineBrushTool>();
        Log.Info("DeepMineMod: deep mine brush initialized");
    }

    public void MigrateJsonConfig(
        VersionSlim savedVersion,
        Dict<string, object> savedValues)
    {
    }
}
