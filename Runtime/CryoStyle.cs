using UnityEngine;

namespace Cryo
{
    /// <summary>
    /// CryoUI 冰蓝主题样式
    /// </summary>
    public class CryoStyle
    {
        public float FontSize = 15f;

        // ★ 冰蓝主题色
        public static readonly Color32 IceBlue = new Color32(100, 180, 255, 255);
        public static readonly Color32 IceCyan = new Color32(120, 220, 255, 255);
        public static readonly Color32 FrostWhite = new Color32(220, 240, 255, 255);
        public static readonly Color32 DeepIce = new Color32(30, 60, 100, 255);
        public static readonly Color32 DarkIce = new Color32(20, 40, 70, 255);

        // 文字
        public Color32 Text = FrostWhite;
        public Color32 TextDim = new Color32(140, 180, 210, 255);
        public Color32 TextHighlight = IceCyan;

        // 窗口
        public Color32 WindowBackground = new Color32(15, 30, 50, 230);
        public Color32 WindowBorder = new Color32(60, 120, 180, 180);
        public Color32 TitleBarBackground = new Color32(30, 70, 120, 240);
        public Color32 TitleBarGradient = new Color32(20, 50, 90, 240);
        public Color32 CloseButtonNormal = new Color32(180, 80, 80, 200);
        public Color32 CloseButtonHovered = new Color32(220, 100, 100, 255);

        // 按钮
        public Color32 ButtonNormal = new Color32(40, 80, 130, 200);
        public Color32 ButtonHovered = new Color32(60, 110, 170, 230);
        public Color32 ButtonActive = new Color32(80, 140, 200, 255);
        public Color32 ButtonBorder = new Color32(80, 150, 220, 150);

        // Checkbox
        public Color32 CheckboxUnchecked = new Color32(30, 50, 80, 200);
        public Color32 CheckboxChecked = new Color32(60, 140, 220, 255);
        public Color32 CheckboxBorder = new Color32(80, 140, 200, 180);
        public Color32 CheckboxHovered = new Color32(100, 180, 255, 200);

        // Slider
        public Color32 SliderTrack = new Color32(25, 45, 75, 200);
        public Color32 SliderFill = new Color32(60, 140, 220, 255);
        public Color32 SliderHandle = new Color32(180, 220, 255, 255);
        public Color32 SliderHandleHovered = new Color32(220, 240, 255, 255);

        // InputText
        public Color32 InputBackground = new Color32(20, 40, 70, 220);
        public Color32 InputBorder = new Color32(60, 120, 180, 180);
        public Color32 InputBorderFocused = new Color32(100, 180, 255, 255);
        public Color32 InputCursor = IceCyan;

        // Tab
        public Color32 TabNormal = new Color32(25, 50, 80, 180);
        public Color32 TabHovered = new Color32(40, 80, 130, 200);
        public Color32 TabActive = new Color32(50, 100, 160, 230);
        public Color32 TabUnderline = IceCyan;

        // TreeNode
        public Color32 TreeNodeHovered = new Color32(50, 90, 140, 150);

        // Dropdown & Menu
        public Color32 DropdownNormal = new Color32(30, 55, 90, 220);
        public Color32 DropdownHovered = new Color32(45, 80, 130, 230);
        public Color32 DropdownBackground = new Color32(20, 40, 70, 245);
        public Color32 DropdownOptionHovered = new Color32(50, 100, 170, 255);
        public Color32 DropdownSelected = IceCyan;

        public Color32 MenuBarBackground = new Color32(20, 45, 75, 230);
        public Color32 MenuBackground = new Color32(25, 50, 85, 250);
        public Color32 MenuHovered = new Color32(50, 90, 150, 255);
        public Color32 MenuItemHovered = new Color32(60, 110, 180, 255);

        // Header
        public Color32 HeaderNormal = new Color32(35, 65, 105, 220);
        public Color32 HeaderHovered = new Color32(50, 90, 145, 240);

        // Separator
        public Color32 Separator = new Color32(60, 110, 170, 120);

        // Scrollbar
        public Color32 ScrollbarTrack = new Color32(20, 40, 70, 150);
        public Color32 ScrollbarThumb = new Color32(60, 120, 180, 200);
        public Color32 ScrollbarThumbHovered = new Color32(80, 150, 220, 255);
    }
}