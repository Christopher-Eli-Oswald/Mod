using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace DeepMineMod;

public sealed class DeepMineMod : DataOnlyMod, IMod {
    public DeepMineMod(ModManifest manifest) : base(manifest) {
        Log.Info("DeepMineMod: loaded");
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }

    void IMod.RegisterDependencies(
        DependencyResolverBuilder depBuilder,
        ProtosDb protosDb,
        bool gameWasLoaded)
    {
        // DeepMineBrushTool is also marked as a GlobalDependency, but registering
        // it explicitly keeps the mod self-contained and version-stable.
        depBuilder.RegisterDependency<DeepMineBrushTool>();
        Log.Info("DeepMineMod: deep mine brush dependency registered");
    }

    void IMod.EarlyInit(DependencyResolver resolver) {
    }

    void IMod.Initialize(DependencyResolver resolver, bool gameWasLoaded) {
        // Dependency resolution is lazy. Force creation here so the brush
        // constructor registers its shortcut with the Unity input manager.
        resolver.Resolve<DeepMineBrushTool>();
        Log.Info("DeepMineMod: deep mine brush initialized");
    }

    public override void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) {
    }
}
