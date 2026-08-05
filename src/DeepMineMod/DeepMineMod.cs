using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;

namespace DeepMineMod;

public sealed class DeepMineMod : DataOnlyMod {
    public DeepMineMod(ModManifest manifest) : base(manifest) {
        Log.Info("DeepMineMod: loaded; press F10 in-game to activate the deep deposit brush");
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }

    public override void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) {
    }
}
