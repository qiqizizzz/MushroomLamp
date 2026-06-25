using System.Linq;
using MVC;
using UnityEngine;

namespace Module.Debug
{
    public class GMPanelDebugger : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool visible;
        [SerializeField] private Vector2 scrollPosition;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || GameApp.ViewManager == null)
                return;

            const float panelWidth = 320f;
            const float panelHeight = 420f;
            var rect = new Rect(16f, 16f, panelWidth, panelHeight);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("GM 面板工具");
            GUILayout.Label("F1 开/关");
            GUILayout.Space(6f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var viewInfo in GameApp.ViewManager.ViewInfos.OrderBy(item => item.Key))
            {
                var viewTypeName = ((ViewType)viewInfo.Key).ToString();
                bool isOpen = GameApp.ViewManager.IsOpen(viewInfo.Key);
                string label = $"{viewTypeName} [{(isOpen ? "Open" : "Close")}]";

                if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    if (isOpen)
                        GameApp.ViewManager.Close(viewInfo.Key);
                    else
                        GameApp.ViewManager.Open(viewInfo.Key);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Close All", GUILayout.Height(30f)))
                GameApp.ViewManager.CloseAll();

            GUILayout.EndArea();
        }
    }
}
