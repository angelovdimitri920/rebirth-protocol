using System;
using System.Linq;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Persists the player's loadout by part ids (PlayerPrefs), mirroring the
    // prototype's localStorage save. Unknown/missing ids fall back per-slot.
    public static class LoadoutStore
    {
        private const string KeyPrefix = "rebirth-protocol.loadout.";

        public static void Save(Loadout l)
        {
            PlayerPrefs.SetString(KeyPrefix + "body", l.Body.Id);
            PlayerPrefs.SetString(KeyPrefix + "rightArm", l.HasGun ? "gun:" + l.Gun.Id : "melee:" + l.Melee.Id);
            PlayerPrefs.SetString(KeyPrefix + "leftArm", l.HasBomb ? "bomb:" + l.Bomb.Id : "shield:" + l.Shield.Id);
            PlayerPrefs.SetString(KeyPrefix + "legs", l.Legs.Id);
            PlayerPrefs.SetString(KeyPrefix + "pod", l.Pod.Id);
            PlayerPrefs.Save();
        }

        public static Loadout Load()
        {
            var fallback = PartsCatalog.DefaultLoadout();
            var loadout = new Loadout
            {
                Body = ById(PartsCatalog.Bodies, Get("body"), p => p.Id) ?? fallback.Body,
                Legs = ById(PartsCatalog.Legs, Get("legs"), p => p.Id) ?? fallback.Legs,
                Pod = ById(PartsCatalog.Pods, Get("pod"), p => p.Id) ?? fallback.Pod
            };

            var rightArm = Get("rightArm");
            if (rightArm.StartsWith("melee:", StringComparison.Ordinal))
            {
                loadout.Melee = ById(PartsCatalog.MeleeWeapons, rightArm.Substring(6), p => p.Id) ?? PartsCatalog.MeleeWeapons[0];
            }
            else
            {
                var id = rightArm.StartsWith("gun:", StringComparison.Ordinal) ? rightArm.Substring(4) : "";
                loadout.Gun = ById(PartsCatalog.Guns, id, p => p.Id) ?? fallback.Gun;
            }

            var leftArm = Get("leftArm");
            if (leftArm.StartsWith("shield:", StringComparison.Ordinal))
            {
                loadout.Shield = ById(PartsCatalog.Shields, leftArm.Substring(7), p => p.Id) ?? PartsCatalog.Shields[0];
            }
            else
            {
                var id = leftArm.StartsWith("bomb:", StringComparison.Ordinal) ? leftArm.Substring(5) : "";
                loadout.Bomb = ById(PartsCatalog.Bombs, id, p => p.Id) ?? fallback.Bomb;
            }

            return loadout;
        }

        private static string Get(string key) => PlayerPrefs.GetString(KeyPrefix + key, "");

        private static T ById<T>(T[] catalog, string id, Func<T, string> idOf) where T : class
        {
            return string.IsNullOrEmpty(id) ? null : catalog.FirstOrDefault(p => idOf(p) == id);
        }
    }
}
