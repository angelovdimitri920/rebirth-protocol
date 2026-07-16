using RebirthProtocol.Battle.Audio;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Destructible cover: a few gun hits (prototype: 3) or any bomb blast
    // removes it. Cover you can shoot through eventually — digging an enemy
    // out of hiding is a real tactic, not a stalemate.
    public sealed class CrateHealth : MonoBehaviour
    {
        public int HitsRemaining = 3;

        public void Damage(int amount = 1)
        {
            HitsRemaining -= amount;
            if (HitsRemaining <= 0)
            {
                GameAudio.Sfx?.CrateBreak();
                Destroy(gameObject);
            }
        }

        public void DestroyOutright()
        {
            GameAudio.Sfx?.CrateBreak();
            Destroy(gameObject);
        }
    }
}
