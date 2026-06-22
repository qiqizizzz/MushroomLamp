/*
* ┌──────────────────────────────────┐
* │  描    述: 时间管理器                      
* │  类    名: TimerManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Timer
{
    public class TimerManager
    {
        private GameTimer timer;//游戏计时器

        public TimerManager()
        {
            timer = new GameTimer();
        }

        public void Register(float time, System.Action callback)
        {
            timer.Register(time, callback);
        }

        public void OnUpdate(float dt)
        {
            timer.onUpdate(dt);
        }
    }
}