using UnityEngine;

namespace Cryo
{
    [DefaultExecutionOrder(-31000)]
    public class CryoController : MonoBehaviour
    {
        private Vector2 _lastMousePosition;
        private static CryoInputManager _inputManager;

        public static bool IsMouseOverUI => CryoContext.Current?.WantCaptureMouse ?? false;

        protected virtual void Awake()
        {
            // 自动创建输入管理器
            if (_inputManager == null && FindFirstObjectByType<CryoInputManager>() == null)
            {
                var go = new GameObject("[CryoUI Input Manager]");
                _inputManager = go.AddComponent<CryoInputManager>();
            }
        }

        protected virtual void Update()
        {
            var ctx = CryoContext.Current;

            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            ctx.MouseDelta = currentMousePos - _lastMousePosition;
            _lastMousePosition = currentMousePos;

            ctx.MousePosition = currentMousePos;
            ctx.MouseDown = Input.GetMouseButton(0);
            ctx.MouseClicked = Input.GetMouseButtonDown(0);
            ctx.MouseReleased = Input.GetMouseButtonUp(0);

            // ★ 键盘输入处理
            ctx.InputText = Input.inputString;
            ctx.HasKeyboardInput = !string.IsNullOrEmpty(ctx.InputText);

            // ★ 特殊按键处理
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ctx.BackspacePressed = true;
            }
            else
            {
                ctx.BackspacePressed = false;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ctx.EnterPressed = true;
            }
            else
            {
                ctx.EnterPressed = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ctx.EscapePressed = true;
            }
            else
            {
                ctx.EscapePressed = false;
            }

            ctx.BeginFrame();
            OnCryoUI();
            ctx.EndFrame();
            ctx.HotId = 0;
        }

        protected virtual void OnCryoUI() { }
    }
}