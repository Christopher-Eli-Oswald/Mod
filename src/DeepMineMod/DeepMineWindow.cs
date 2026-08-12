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

public sealed class DeepMineWindow : Window {
    private Option<ProductProto> m_selectedProduct = Option<ProductProto>.None;

    public DeepMineWindow(UiContext context, DeepMineBrushTool tool)
        : base("Deep Mine".AsLoc())
    {
        ShortcutToShow(KeyBindings.FromKey(KbCategory.Tools, ShortcutMode.Game, KeyCode.F10));
        WindowSize(384.px(), Px.Auto).MakeMovable().EnablePinning();

        var materialByProduct = tool.Materials
            .Where(x => !x.IgnoreInEditor && x.MinedProduct != null)
            .GroupBy(x => x.MinedProduct)
            .ToDictionary(g => g.Key, g => g.First());

        TerrainMaterialProto initialMaterial = tool.SelectedMaterial;
        if (initialMaterial != null && initialMaterial.MinedProduct != null) {
            m_selectedProduct = initialMaterial.MinedProduct;
        }

        var picker = new SingleProductPickerUi(
            () => materialByProduct.Keys.OrderBy(x => x.Strings.Name.TranslatedString),
            p => m_selectedProduct = p,
            () => m_selectedProduct,
            () => m_selectedProduct = Option<ProductProto>.None);

        var activateButton = new ButtonIcon(
                Button.General,
                "Assets/Unity/UserInterface/Toolbar/Flatten.svg",
                () => {
                    ProductProto selectedProduct = m_selectedProduct.ValueOrNull;
                    if (selectedProduct != null && materialByProduct.TryGetValue(selectedProduct, out TerrainMaterialProto material)) {
                        tool.SetMaterial(material);
                    }
                    context.InputMgr.ActivateNewController(tool);
                })
            .Medium()
            .Tooltip("Activate the deep-deposit brush with the selected resource. Shift+wheel changes radius; Ctrl+wheel changes thickness.".AsLoc());

        AddBodySingle(c => c.Gap(6.pt()),
            new Title("Underground resource".AsLoc()).NoShrink(),
            picker,
            activateButton);

        Log.Info($"DeepMineWindow: constructed with {materialByProduct.Count} selectable underground resources");
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
