using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mafi;
using Mafi.Unity;

namespace DeepMineMod;

/// <summary>
/// Finds terrain- and sandbox-related runtime services in the exact installed game build.
/// The output is written to the normal Captain of Industry log and is used to bind the
/// deep-deposit writer without hardcoding private type names that change between releases.
/// </summary>
[GlobalDependency(RegistrationMode.AsEverything, false, false)]
public sealed class TerrainApiProbe {
    private static readonly string[] TypeTerms = {
        "Terrain", "Heightmap", "TerrainDump", "TerrainMining", "TerrainMaterial",
        "Sandbox", "Designation", "Dumping", "Excavation", "LooseMaterial"
    };

    private static readonly string[] MethodTerms = {
        "Set", "Replace", "Fill", "Dump", "Mine", "Excavate", "Terrain", "Material",
        "Height", "Column", "Voxel", "Tile", "Designation", "Sandbox"
    };

    public TerrainApiProbe(DependencyResolver resolver) {
        try {
            Log.Info("DeepMineMod: terrain API probe starting");
            var candidates = new List<object>();

            foreach (object instance in resolver.AllResolvedInstances) {
                if (instance == null) continue;
                Type type = instance.GetType();
                if (TypeTerms.Any(term => type.FullName != null &&
                    type.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) {
                    candidates.Add(instance);
                }
            }

            foreach (object candidate in candidates.OrderBy(x => x.GetType().FullName)) {
                Type type = candidate.GetType();
                Log.Info($"DeepMineMod API TYPE: {type.Assembly.GetName().Name}::{type.FullName}");

                IEnumerable<MethodInfo> methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (MethodInfo method in methods
                    .Where(m => MethodTerms.Any(term =>
                        m.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.GetParameters().Length)) {
                    string parameters = string.Join(", ", method.GetParameters()
                        .Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                    Log.Info($"DeepMineMod API METHOD: {type.FullName}.{method.Name}({parameters}) -> {method.ReturnType.FullName}");
                }
            }

            Log.Info($"DeepMineMod: terrain API probe complete; {candidates.Count} candidate services found");
        }
        catch (Exception ex) {
            Log.Error($"DeepMineMod: terrain API probe failed: {ex}");
        }
    }
}
