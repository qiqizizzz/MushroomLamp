/*
* ┌──────────────────────────────────┐
* │  描    述: 框架协程宿主，用于非 MonoBehaviour 类执行协程
* │  类    名: GameAppRunner.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using UnityEngine;

namespace Common
{
    public class GameAppRunner : MonoBehaviour
    {
        private static GameAppRunner _instance;

        // 执行无宿主对象的协程
        public static void Run(IEnumerator routine)
        {
            if (routine == null) return;

            if (_instance == null)
            {
                GameObject go = new GameObject(nameof(GameAppRunner));
                if (GameApp.RootTf != null)
                    go.transform.SetParent(GameApp.RootTf, false);
                else
                    DontDestroyOnLoad(go);

                _instance = go.AddComponent<GameAppRunner>();
            }

            _instance.StartCoroutine(routine);
        }
    }
}
