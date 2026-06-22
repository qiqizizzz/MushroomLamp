/*
* ┌──────────────────────────────────┐
* │  描    述: 资源加载与对象池管理器
* │  类    名: ResManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public static class ResManager
    {
        private static readonly Dictionary<string, Queue<GameObject>> _pool;
        private static readonly Dictionary<GameObject, Queue<GameObject>> _prefabPool;

        static ResManager()
        {
            _pool = new Dictionary<string, Queue<GameObject>>();
            _prefabPool = new Dictionary<GameObject, Queue<GameObject>>();
        }

        // 同步加载实例，资源路径对应 Resources 下的路径
        public static GameObject Instantiate(string path, Transform parent = null)
        {
            GameObject prefab = LoadAsset<GameObject>(path);
            if (prefab == null)
            {
                QLog.Error($"[{nameof(ResManager)}] 资源加载失败：{path}");
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            return go;
        }

        // 异步加载实例，资源路径对应 Resources 下的路径
        public static void InstantiateAsync(string path, Action<GameObject> onCompleted, Transform parent = null)
        {
            GameAppRunner.Run(instantiateAsyncCoroutine(path, onCompleted, parent));
        }

        // 卸载实例
        public static bool UnLoadInstance(GameObject go)
        {
            if (go == null) return false;

            UnityEngine.Object.Destroy(go);
            return true;
        }

        // 同步加载资源
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }

        // 异步加载资源
        public static void LoadAssetAsync<T>(string path, Action<T> onCompleted) where T : UnityEngine.Object
        {
            GameAppRunner.Run(loadAssetAsyncCoroutine(path, onCompleted));
        }

        // 卸载资源引用
        public static void UnLoadAsset<T>(T obj) where T : UnityEngine.Object
        {
            if (obj != null)
                Resources.UnloadAsset(obj);
        }

        // 同步从对象池加载实例
        public static GameObject InstantiateFromPool(string path, Transform parent = null)
        {
            if (_pool.TryGetValue(path, out Queue<GameObject> queue))
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj == null) continue;

                    obj.SetActive(true);
                    if (parent != null)
                        obj.transform.SetParent(parent, false);
                    return obj;
                }
            }

            return Instantiate(path, parent);
        }

        // 异步从对象池加载实例
        public static void InstantiateFromPoolAsync(string path, Action<GameObject> onCompleted, Transform parent = null)
        {
            GameObject obj = InstantiateFromPool(path, parent);
            onCompleted?.Invoke(obj);
        }

        // 释放实例到对象池
        public static void ReleaseToPool(string path, GameObject obj, int maxPoolSize = 20)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(getOrCreatePoolRoot(), false);

            if (!_pool.ContainsKey(path))
                _pool[path] = new Queue<GameObject>();

            if (_pool[path].Count < maxPoolSize)
                _pool[path].Enqueue(obj);
            else
                UnityEngine.Object.Destroy(obj);
        }

        // 清理单个对象池
        public static void ClearPool(string path)
        {
            if (!_pool.TryGetValue(path, out Queue<GameObject> queue)) return;

            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }

            _pool.Remove(path);
        }

        // 清理所有对象池
        public static void ClearAllPools()
        {
            foreach (string key in new List<string>(_pool.Keys))
                ClearPool(key);

            ClearAllPrefabPools();
        }

        // 同步从预制体对象池加载实例
        public static GameObject InstantiateFromPool(GameObject prefab, Transform parent = null)
        {
            if (prefab == null) return null;

            if (_prefabPool.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj == null) continue;

                    obj.SetActive(true);
                    if (parent != null)
                        obj.transform.SetParent(parent, false);
                    return obj;
                }
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            return go;
        }

        // 释放实例到预制体对象池
        public static void ReleaseToPool(GameObject prefab, GameObject obj, int maxPoolSize = 20)
        {
            if (prefab == null || obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(getOrCreatePoolRoot(), false);

            if (!_prefabPool.ContainsKey(prefab))
                _prefabPool[prefab] = new Queue<GameObject>();

            if (_prefabPool[prefab].Count < maxPoolSize)
                _prefabPool[prefab].Enqueue(obj);
            else
                UnityEngine.Object.Destroy(obj);
        }

        // 清理单个预制体对象池
        public static void ClearPrefabPool(GameObject prefab)
        {
            if (prefab == null) return;
            if (!_prefabPool.TryGetValue(prefab, out Queue<GameObject> queue)) return;

            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }

            _prefabPool.Remove(prefab);
        }

        // 清理所有预制体对象池
        public static void ClearAllPrefabPools()
        {
            foreach (GameObject key in new List<GameObject>(_prefabPool.Keys))
                ClearPrefabPool(key);

            _prefabPool.Clear();
        }

        // 异步加载实例协程
        private static IEnumerator instantiateAsyncCoroutine(string path, Action<GameObject> onCompleted, Transform parent)
        {
            ResourceRequest request = Resources.LoadAsync<GameObject>(path);
            yield return request;

            GameObject prefab = request.asset as GameObject;
            if (prefab == null)
            {
                QLog.Error($"[{nameof(ResManager)}] 资源加载失败：{path}");
                onCompleted?.Invoke(null);
                yield break;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            onCompleted?.Invoke(go);
        }

        // 异步加载资源协程
        private static IEnumerator loadAssetAsyncCoroutine<T>(string path, Action<T> onCompleted) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            yield return request;
            onCompleted?.Invoke(request.asset as T);
        }

        // 获取或创建对象池根节点
        private static Transform getOrCreatePoolRoot()
        {
            Transform rootTf = GameApp.RootTf;
            Transform poolTf = rootTf == null ? null : rootTf.Find("Pools");
            if (poolTf != null)
                return poolTf;

            GameObject poolObj = new GameObject("Pools");
            if (rootTf != null)
                poolObj.transform.SetParent(rootTf, false);

            poolObj.transform.localPosition = Vector3.zero;
            poolObj.transform.localRotation = Quaternion.identity;
            poolObj.transform.localScale = Vector3.one;
            return poolObj.transform;
        }
    }

}
