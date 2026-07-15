namespace RebirthProtocol.Domain
{
    // Fire-cadence gate for a held-trigger gun.
    public sealed class GunCycle
    {
        private float _cooldown;

        public void Tick(float dt)
        {
            _cooldown -= dt;
        }

        public bool TryFire()
        {
            if (_cooldown > 0f)
            {
                return false;
            }

            _cooldown = CombatTuning.Gun.FireInterval;
            return true;
        }
    }
}
