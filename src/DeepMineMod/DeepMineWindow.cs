using System;
using System.Collections.Generic;
using System.Linq;
using Mafi;
using Mafi.Core.Products;
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
/// Live-save deep resource editor. Select a mineable resource, activate the brush,
/// then paint a buried deposit at the configured depth while the surface stays intact.
/// </summary>
public sealed class DeepMineWindow : Window {
    private Option<ProductProto> m_selectedProduct = Option<ProductProto>.None;

    public DeepMineWindow(UiContext context, DeepMineBrushTool tool)
        : base("Deep Resource Editor".AsLoc())
    {
        ShortcutToShow(KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10));
        WindowSize(420.px(), Px.Auto).MakeMovable().EnablePinning();

        Dictionary<ProductProto, TerrainMaterialProto> materialByProduct = tool.Materials
            .Where(x => x.MinedProduct != null)
            .GroupBy(x => (ProductProto)x.MinedProduct)
            .ToDictionary(g => g.Key, g => g.First());

        var picker = new SingleProductPickerUi(
            () => materialByProduct.Keys.OrderBy(x => x.Strings.Name.TranslatedString),
            p => m_selectedProduct = p,
            () => m_selectedProduct,
            () => m_selectedProduct = Option<ProductProto>.None);

        var activateButton = new ButtonIcon(
                Button.General,
                "Assets/Unity/UserInterface/Toolbar/PaintBrush.svg",
                () => {
                    ProductProto selectedProduct = m_selectedProduct.ValueOrNull;
                    if (selectedProduct != null &&
                        materialByProduct.TryGetValue(selectedProduct, out TerrainMaterialProto material)) {
                        tool.SetMaterial(material);
                    }
                    context.InputMgr.ActivateNewController(tool);
                })
            .Medium()
            .Tooltip((
                "Paint a mineable resource deep underground without changing the surface. " +
                "Hold left mouse and drag to paint. Alt+wheel changes depth (default 50); " +
                "Ctrl+wheel changes deposit thickness (default 10); Shift+wheel changes brush radius (default 8); " +
                "right-click exits.").AsLoc());

        AddBodySingle(c => c.Gap(6.pt()),
            new Title("Mineable resource".AsLoc()).NoShrink(),
            picker,
            new Title("Deep underground brush".AsLoc()).NoShrink(),
            new Title("Alt + wheel: depth | Ctrl + wheel: thickness | Shift + wheel: radius".AsLoc()).NoShrink(),
            activateButton);

        Log.Info(
            $"DeepMineWindow: deep resource editor constructed with {materialByProduct.Count} resources; " +
            $"default depth={tool.Depth}, thickness={tool.Thickness}, radius={tool.Radius}");
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
                "Deep Resource Editor".AsLoc(),
                this,
                "Assets/Unity/UserInterface/Toolbar/PaintBrush.svg",
                1090f,
                _ => m_binding);

            Log.Info("DeepMineWindow.Controller: registered deep resource editor on F10 and toolbar");
        }
    }
}
