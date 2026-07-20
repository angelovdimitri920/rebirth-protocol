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

        /// Run-layer hook (set by DuelManager): destroyed crates may drop
        /// an item pickup at their position.
        public System.Action<Vector3> OnDestroyed;

        public void Damage(int amount = 1)
        {
            HitsRemaining -= amount;
            if (HitsRemaining <= 0)
            {
                Break();
            }
        }

        public void DestroyOutright()
        {
            Break();
        }

        private void Break()
        {
            GameAudio.Sfx?.CrateBreak(transform.position);
            OnDestroyed?.Invoke(transform.position);
            OnDestroyed = null; // a crate breaks once; bomb + shots the same frame must not double-roll

            // Stop blocking shots NOW — Destroy is deferred to end of frame,
            // and a broken crate must not eat a later-ticked projectile that
            // frame (or for hundreds of steps in the harness's batched frames).
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(gameObject);
        }
    }
}
