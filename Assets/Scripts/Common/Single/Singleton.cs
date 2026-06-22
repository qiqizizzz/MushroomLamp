/*
* ┌──────────────────────────────────┐
* │  描    述: 单例基类                      
* │  类    名: Singleton.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;

namespace Common
{
    public class Singleton<T>
    {
        private static readonly T instance = Activator.CreateInstance<T>();

        public static T Instance
        {
            get { return instance; }
        }

        public virtual void Init()
        {
            
        }

        public virtual void Update(float dt)
        {
            
        }
        
        public virtual void Destroy()
        {
            
        }
    }
}