namespace RebirthProtocol.Domain
{
    // Fire-cadence gate for a held-trigger gun.
    public sealed class GunCycle
    {
        private readonly float _fireInterval;
        private float _cooldown;

        public GunCycle(float fireInterval = 0.38f)
        {
            _fireInterval = fireInterval;
        }

        public void Tick(float dt)
        {
            _cooldown -= dt;
        }

        /// Trigger Coil boon: an on-hit proc wipes the remaining cooldown.
        public void ResetCooldown()
        {
            _cooldown = 0f;
        }

        public bool TryFire()
        {
            if (_cooldown > 0f)
            {
                return false;
            }

            _cooldown = _fireInterval;
            return true;
        }
    }
}
