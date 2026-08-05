using System;
using System.Collections.Generic;
using System.Linq;
using Mafi;
using Mafi.Core.Terrain;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Hud;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiStatic.Toolbar;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepMineMod;

public sealed class DeepMineWindow : Window {
    public DeepMineWindow(UiContext context, DeepMineBrushTool tool)
        : base("Deep Mine".AsLoc())
    {
        ShortcutToShow(KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10));
        WindowSize(360.px(), Px.Auto).MakeMovable().EnablePinning();

        List<TerrainMaterialProto> materials = tool.Materials.ToList();
        List<string> materialNames = materials
            .Select(x => x.Id.Value)
            .ToList();

        int selectedIndex = 0;
        TerrainMaterialProto selected = tool.SelectedMaterial;
        if (selected != null) {
            int existingIndex = materials.FindIndex(x => x.Id.Value == selected.Id.Value);
            if (existingIndex >= 0) selectedIndex = existingIndex;
        }

        var dropdown = new DropdownField(
            "Resource",
            materialNames,
            materialNames.Count == 0 ? -1 : selectedIndex);

        dropdown.RegisterValueChangedCallback(evt => {
            int index = materialNames.IndexOf(evt.newValue);
            if (index >= 0 && index < materials.Count) {
                tool.SetMaterial(materials[index]);
            }
        });

        var activateButton = new ButtonIcon(
                Button.General,
                "Assets/Unity/UserInterface/Toolbar/Flatten.svg",
                () => context.InputMgr.ActivateNewController(tool))
            .Medium()
            .Tooltip("Activate the deep-deposit brush. Shift+wheel changes radius, Ctrl+wheel changes thickness, Alt+wheel cycles resources.".AsLoc());

        AddBodySingle(c => c.Gap(6.pt()),
            new Title("Underground resource brush".AsLoc()).NoShrink(),
            dropdown,
            activateButton);

        Log.Info("DeepMineWindow: constructed with resource dropdown");
    }

    [GlobalDependency(RegistrationMode.AsEverything, false, false)]
    public sealed class Controller : WindowController<DeepMineWindow>, IToolbarItemController {
        private readonly KeyBindings m_binding =
            KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10);

        public bool IsVisible => true;
        public bool DeactivateShortcutsIfNotVisible => false;
        public event Action<IToolbarItemController> VisibilityChanged { add { } remove { } }

        public Controller(ControllerContext ctx, ToolbarHud toolbar)
            : base(ctx, ControllerConfig.LayersPanel)
        {
            ctx.InputManager.RegisterGlobalShortcut(_ => m_binding, this);
            toolbar.AddToolButton(
                "Deep Mine".AsLoc(),
                this,
                "Assets/Unity/UserInterface/Toolbar/Flatten.svg",
                1090f,
                _ => m_binding);

            Log.Info("DeepMineWindow.Controller: registered F10 and toolbar button");
        }
    }
}
