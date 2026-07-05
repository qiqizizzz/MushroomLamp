using UnityEngine;

namespace Common.UI
{
    // 关闭弹层后屏蔽底层点击，避免误触下层按钮
    public static class UiClickGuard
    {
        private static int _blockUntilFrame = -1;

        public static void BlockUntilNextFrame()
        {
            BlockForFrames(1);
        }

        public static void BlockForFrames(int frames)
        {
            _blockUntilFrame = Time.frameCount + Mathf.Max(1, frames);
        }

        public static bool IsBlocked => Time.frameCount <= _blockUntilFrame;
    }
}
