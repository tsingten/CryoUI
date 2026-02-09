using Cryo;
using Cryo;
using UnityEngine;

public class CryoDemo : CryoController
{
    private bool _windowOpen = true;
    private int _selectedTab = 0;
    private bool _checkbox1 = true;
    private bool _checkbox2 = false;
    private float _sliderValue = 0.5f;
    private int _sliderIntValue = 50;
    private string _inputText = "Hello CryoUI!";

    protected override void OnCryoUI()
    {
        if (CryoUI.BeginWindow("❄ CryoUI Demo", ref _windowOpen, new Vector2(50, 50), new Vector2(400, 500)))
        {
            CryoUI.BeginMenuBar();
            if (CryoUI.BeginMenu("文件"))
            {
                if (CryoUI.MenuItem("新建", "Ctrl+N")) Debug.Log("新建");
                if (CryoUI.MenuItem("打开", "Ctrl+O")) Debug.Log("打开");
                if (CryoUI.MenuItem("保存", "Ctrl+S")) Debug.Log("保存");
                CryoUI.EndMenu();
            }
            if (CryoUI.BeginMenu("编辑"))
            {
                if (CryoUI.MenuItem("撤销", "Ctrl+Z")) Debug.Log("撤销");
                if (CryoUI.MenuItem("重做", "Ctrl+Y")) Debug.Log("重做");
                CryoUI.EndMenu();
            }
            CryoUI.EndMenuBar();

            CryoUI.BeginTabBar("tabs");
            CryoUI.TabItem("控件", ref _selectedTab, 0);
            CryoUI.TabItem("设置", ref _selectedTab, 1);
            CryoUI.TabItem("关于", ref _selectedTab, 2);
            CryoUI.EndTabBar();

            CryoUI.Separator();

            switch (_selectedTab)
            {
                case 0: DrawControlsTab(); break;
                case 1: DrawSettingsTab(); break;
                case 2: DrawAboutTab(); break;
            }

            CryoUI.EndWindow();
        }
    }

    private void DrawControlsTab()
    {
        CryoUI.Label("基础控件演示");
        CryoUI.Spacing();

        if (CryoUI.Button("冰蓝按钮"))
            Debug.Log("按钮点击!");

        CryoUI.SameLine();

        if (CryoUI.Button("另一个按钮"))
            Debug.Log("另一个按钮!");

        CryoUI.Spacing();
        CryoUI.Checkbox("启用特效", ref _checkbox1);
        CryoUI.Checkbox("显示FPS", ref _checkbox2);

        CryoUI.Spacing();
        CryoUI.Slider("音量", ref _sliderValue, 0f, 1f);
        CryoUI.SliderInt("亮度", ref _sliderIntValue, 0, 100);

        CryoUI.Spacing();
        CryoUI.InputText("名称", ref _inputText, 180f);

        CryoUI.Spacing();
        CryoUI.Text($"当前帧率: {(1f / Time.deltaTime):F1} FPS", CryoStyle.IceCyan);
    }

    private void DrawSettingsTab()
    {
        CryoUI.Label("=== 设置页面 ===");
        CryoUI.Spacing();
        CryoUI.Text("这里可以放置各种设置选项");
    }

    private void DrawAboutTab()
    {
        CryoUI.Label("❄ CryoUI");
        CryoUI.Text("版本: 1.0.0", CryoStyle.IceCyan);
        CryoUI.Spacing();
        CryoUI.Text("一个冰蓝主题的即时模式 GUI 库");
        CryoUI.Text("灵感来自 Dear ImGui");
    }
}