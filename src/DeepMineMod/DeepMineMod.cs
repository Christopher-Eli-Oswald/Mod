using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace DeepMineMod;

public sealed class DeepMineMod : DataOnlyMod {
    public DeepMineMod(ModManifest manifest) : base(manifest) {
        Log.Info("DeepMineMod: loaded");
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }

    public override void RegisterDependencies(
        DependencyResolverBuilder depBuilder,
        ProtosDb protosDb,
        bool gameWasLoaded)
    {
        // Do not rely on assembly scanning for this controller. Explicit
        // registration guarantees that the DI container creates the brush and
        // supplies its input, cursor, terrain, and prototype dependencies.
        depBuilder.RegisterDependency<DeepMineBrushTool>();
        Log.Info("DeepMineMod: deep mine brush dependency registered");
    }

    public override void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) {
    }
}
