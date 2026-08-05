using System;
using Mafi;
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

namespace DeepMineMod;

/// <summary>
/// Small launcher window for the deep-deposit brush. The constructor takes
/// DeepMineBrushTool directly; this is intentional because it forces the lazy
/// dependency resolver to instantiate the tool, exactly like PlaceResourceMod.
/// </summary>
public sealed class DeepMineWindow : Window {
    public DeepMineWindow(UiContext context, DeepMineBrushTool tool)
        : base("Deep Mine".AsLoc())
    {
        ShortcutToShow(KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10));
        WindowSize(320.px(), Px.Auto).MakeMovable().EnablePinning();

        AddBodySingle(c => c.Gap(4.pt()),
            new Title("Underground resource brush".AsLoc()).NoShrink(),
            new ButtonIcon(
                    Button.General,
                    "Assets/Unity/UserInterface/Toolbar/Flatten.svg",
                    () => context.InputMgr.ActivateNewController(tool))
                .Medium()
                .Tooltip("Activate the deep-deposit brush. Alt+wheel changes material, Shift+wheel changes radius, Ctrl+wheel changes thickness.".AsLoc()));

        Log.Info("DeepMineWindow: constructed; brush dependency resolved");
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
