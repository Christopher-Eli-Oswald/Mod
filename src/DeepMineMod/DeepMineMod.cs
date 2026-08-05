using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Mods;

namespace DeepMineMod;

public sealed class DeepMineMod : DataOnlyMod {
    private static readonly string[] ProbeTerms = {
        "Terrain",
        "Sandbox",
        "Mining",
        "Dump",
        "Designation",
        "Heightmap",
        "Material"
    };

    public DeepMineMod(ModManifest manifest) : base(manifest) {
        Log.Info("DeepMineMod: loaded");
        ProbeGameApi();
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator) {
        Log.Info("DeepMineMod: prototype registration complete");
    }

    public override void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) {
    }

    private static void ProbeGameApi() {
        Log.Info("DeepMineMod API: ===== BEGIN TERRAIN API PROBE =====");

        try {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                         .Where(a => a.GetName().Name.StartsWith("Mafi", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(a => a.GetName().Name)) {
                foreach (Type type in GetLoadableTypes(assembly)
                             .Where(IsRelevantType)
                             .OrderBy(t => t.FullName)) {
                    Log.Info($"DeepMineMod API: TYPE {type.Assembly.GetName().Name} :: {type.FullName}");

                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.DeclaredOnly;

                    foreach (ConstructorInfo constructor in type.GetConstructors(flags)) {
                        Log.Info($"DeepMineMod API:   CTOR {FormatMethod(constructor)}");
                    }

                    foreach (MethodInfo method in type.GetMethods(flags)
                                 .Where(IsRelevantMethod)
                                 .OrderBy(m => m.Name)) {
                        Log.Info($"DeepMineMod API:   METHOD {FormatMethod(method)}");
                    }

                    foreach (PropertyInfo property in type.GetProperties(flags)
                                 .Where(IsRelevantMember)
                                 .OrderBy(p => p.Name)) {
                        Log.Info($"DeepMineMod API:   PROPERTY {property.PropertyType.FullName} {property.Name}");
                    }

                    foreach (FieldInfo field in type.GetFields(flags)
                                 .Where(IsRelevantMember)
                                 .OrderBy(f => f.Name)) {
                        Log.Info($"DeepMineMod API:   FIELD {field.FieldType.FullName} {field.Name}");
                    }
                }
            }
        } catch (Exception ex) {
            Log.Warning($"DeepMineMod API: probe failed: {ex}");
        }

        Log.Info("DeepMineMod API: ===== END TERRAIN API PROBE =====");
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            return ex.Types.Where(t => t != null);
        } catch {
            return Array.Empty<Type>();
        }
    }

    private static bool IsRelevantType(Type type) {
        string name = type.FullName ?? type.Name;
        return ProbeTerms.Any(term => name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsRelevantMethod(MethodInfo method) {
        if (ProbeTerms.Any(term => method.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) {
            return true;
        }

        string returnType = method.ReturnType.FullName ?? method.ReturnType.Name;
        if (ProbeTerms.Any(term => returnType.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) {
            return true;
        }

        return method.GetParameters().Any(parameter => {
            string parameterType = parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
            return ProbeTerms.Any(term => parameterType.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        });
    }

    private static bool IsRelevantMember(MemberInfo member) {
        return ProbeTerms.Any(term => member.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string FormatMethod(MethodBase method) {
        string parameters = string.Join(", ", method.GetParameters()
            .Select(p => $"{p.ParameterType.FullName ?? p.ParameterType.Name} {p.Name}"));

        if (method is MethodInfo info) {
            return $"{info.ReturnType.FullName ?? info.ReturnType.Name} {method.Name}({parameters})";
        }

        return $"{method.Name}({parameters})";
    }
}
