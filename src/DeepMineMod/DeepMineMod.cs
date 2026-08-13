using Mafi;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace DeepMineMod;

/// <summary>
/// Mod entry point. Runtime services and UI controllers are discovered through
/// their GlobalDependency attributes, matching the working PlaceResourceMod pattern.
/// </summary>
public sealed class DeepMineMod : DataOnlyMod {
    public DeepMineMod(ModManifest manifest) : base(manifest) {
        Log.Info("DeepMineMod: constructed");
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }
}
