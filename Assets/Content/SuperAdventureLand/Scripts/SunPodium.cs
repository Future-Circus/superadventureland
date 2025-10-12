namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using Text = TMPro.TMP_Text;

    public class SunPodium : Wackable
    {
        public Sun sun;
        public Text plaqueText;
        public override void ExecuteState()
        {
            switch (state) {
                case WackableState.START:
                    base.ExecuteState();
                    if (plaqueText == null) {
                        plaqueText = GetComponentInChildren<Text>();
                    }
                    plaqueText.text = sun ? $"<b>{sun.sunConfig.title}</b>\n-\n{sun.sunConfig.description}" : "DreamPark";
                    break;
                case WackableState.AIRBORNE:
                    base.ExecuteState();
                    sun?.SetState(SunState.HIDE);
                    break;
                case WackableState.IDLE:
                    base.ExecuteState();
                    sun?.SetState(SunState.DELAY_REVEAL);
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

    }
}
