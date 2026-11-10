using UnityEngine;

namespace BattleChess
{
    /// <summary>Sits next to the Animator so clip events named "Impact" reach the PieceActor.</summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        public PieceActor actor;
        public void Impact() { if (actor != null) actor.OnAnimationImpact(); }
        public void Footstep() { if (SoundBank.I != null) SoundBank.I.Play("step", 0.35f); }
    }
}
