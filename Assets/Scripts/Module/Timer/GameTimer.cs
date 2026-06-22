/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏时间                      
* │  类    名: GameTimer.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Module.Timer
{
    public class GameTimer
    {
        private readonly List<GameTimerData> timers;

        public GameTimer()
        {
            timers = new List<GameTimerData>();
        }

        //注册新计时器
        public void Register(float time, System.Action callback)
        {
            timers.Add(new GameTimerData(time, callback));
        }
        
        public void onUpdate(float dt)
        {
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                if (timers[i].OnUpdate(dt))
                {
                    timers.RemoveAt(i);
                }
            }
        }

        public void Break() => timers.Clear();
        public int Count() => timers.Count;
    }
}