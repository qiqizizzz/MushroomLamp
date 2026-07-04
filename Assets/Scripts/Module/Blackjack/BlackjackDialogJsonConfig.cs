using System;

namespace Module.Blackjack
{
    [Serializable]
    public class BlackjackSpeakerDialogJsonData
    {
        public string normalText;
        public string bustedText;
    }

    [Serializable]
    public class BlackjackDialogJsonConfig
    {
        public BlackjackSpeakerDialogJsonData devil;
        public BlackjackSpeakerDialogJsonData angel;
    }
}
