using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public abstract class Weapon:MonoBehaviour
    {
        public Player player;

        public abstract void Fire(IStat stat);
    }
}