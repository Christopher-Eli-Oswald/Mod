using System;
using System.Linq;
using System.Reflection;
using Mafi;
using Mafi.Unity;

namespace DeepMineMod;

/// <summary>
/// One-shot reflection trace for the map-editor terrain/resource implementation in the
/// exact installed Captain of Industry build. This intentionally scans types from the
/// loaded Mafi.Unity assembly (rather than resolved gameplay services) because map-editor
/// tools are registered only in the MapEditor scene.
/// </summary>
[GlobalDependency(RegistrationMode.AsEverything, false, false)]
public sealed class MapEditorApiProbe {
    private static readonly string[] InterestingTypeTerms = {
        "Resource", "Terrain", "Material", "Brush", "Paint", "Tool", "Feature", "Editor"
    };

    private static readonly string[] InterestingMemberTerms = {
        "Resource", "Terrain", "Material", "Brush", "Paint", "Place", "Apply", "Add",
        "Set", "Replace", "Layer", "Height", "Depth", "Thickness", "Feature", "Undo"
    };

    public MapEditorApiProbe() {
        try {
            Assembly unityAssembly = typeof(IUnityInputMgr).Assembly;
            Log.Info("DeepMineMod MAPEDITOR: ===== BEGIN EXACT MAP EDITOR API TRACE =====");
            Log.Info($"DeepMineMod MAPEDITOR: assembly={unityAssembly.FullName}");

            Type[] types = unityAssembly.GetTypes()
                .Where(t => t.FullName != null &&
                    t.FullName.StartsWith("Mafi.Unity.MapEditor", StringComparison.Ordinal))
                .Where(t => InterestingTypeTerms.Any(term =>
                    t.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(t => t.FullName)
                .ToArray();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                                       BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.DeclaredOnly;

            foreach (Type type in types) {
                Log.Info($"DeepMineMod MAPEDITOR TYPE: {type.FullName}");

                foreach (ConstructorInfo ctor in type.GetConstructors(flags)
                    .OrderBy(c => c.GetParameters().Length)) {
                    Log.Info($"DeepMineMod MAPEDITOR CTOR: {type.FullName}({formatParameters(ctor.GetParameters())})");
                }

                foreach (MethodInfo method in type.GetMethods(flags)
                    .Where(m => InterestingMemberTerms.Any(term =>
                        m.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        m.GetParameters().Any(p => p.ParameterType.FullName != null &&
                            InterestingMemberTerms.Any(term => p.ParameterType.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))))
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.GetParameters().Length)) {
                    Log.Info($"DeepMineMod MAPEDITOR METHOD: {type.FullName}.{method.Name}({formatParameters(method.GetParameters())}) -> {method.ReturnType.FullName}");
                }

                foreach (FieldInfo field in type.GetFields(flags)
                    .Where(f => InterestingMemberTerms.Any(term =>
                        f.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (f.FieldType.FullName != null && f.FieldType.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)))
                    .OrderBy(f => f.Name)) {
                    Log.Info($"DeepMineMod MAPEDITOR FIELD: {field.FieldType.FullName} {type.FullName}.{field.Name}");
                }

                foreach (PropertyInfo property in type.GetProperties(flags)
                    .Where(p => InterestingMemberTerms.Any(term =>
                        p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (p.PropertyType.FullName != null && p.PropertyType.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)))
                    .OrderBy(p => p.Name)) {
                    Log.Info($"DeepMineMod MAPEDITOR PROPERTY: {property.PropertyType.FullName} {type.FullName}.{property.Name}");
                }
            }

            Log.Info($"DeepMineMod MAPEDITOR: traced {types.Length} candidate map-editor types");
            Log.Info("DeepMineMod MAPEDITOR: ===== END EXACT MAP EDITOR API TRACE =====");
        }
        catch (ReflectionTypeLoadException ex) {
            Log.Error($"DeepMineMod MAPEDITOR: type load failure: {ex}");
            foreach (Exception loader in ex.LoaderExceptions ?? Array.Empty<Exception>()) {
                Log.Error($"DeepMineMod MAPEDITOR: loader exception: {loader}");
            }
        }
        catch (Exception ex) {
            Log.Error($"DeepMineMod MAPEDITOR: trace failed: {ex}");
        }
    }

    private static string formatParameters(ParameterInfo[] parameters) {
        return string.Join(", ", parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"));
    }
}
