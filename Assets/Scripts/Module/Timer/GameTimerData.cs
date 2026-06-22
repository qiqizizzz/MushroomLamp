/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏计时器数据                      
* │  类    名: GameTimerData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;

namespace Module.Timer
{
    public class GameTimerData
    {
        private float time;
        private Action callback;

        public GameTimerData(float time, Action callback)
        {
            this.time = time;
            this.callback = callback;
        }

        public bool OnUpdate(float dt)
        {
            time -= dt;

            if (time <= 0)
            {
                callback?.Invoke();
                return true;
            }

            return false;
        }
    }
}