using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem
{
    public abstract class Trigger:MonoBehaviour, IEntityEventListener
    {
        protected Entity entity { get; set; }
        public abstract void eventActive(EventArgs eventArgs);

        public void registerTarget(Entity target, object args = null)
        {
            this.entity = target;
            target.registerListener(this);
        }

        public void removeSelf()
        {
            this.entity.removeListener(this);
        }
        public abstract void update(float deltaTime, Entity target);

        // public bool tryAddEffect<T>(T effect, int stateIndex, int ax, int ay, out Board.Placement placement)
        //     where T : Polyomino, ITriggerEffect
        // {
        //     return this.board.TryPlace(effect, stateIndex, ax, ay, out placement);
        // }
        //
        // public bool tryRemoveEffect<T>(int ax, int ay, out T effect) where T : Polyomino, ITriggerEffect
        // {
        //     if (board.TryGetPlacementAt(ax, ay, out var placement))
        //     {
        //         var poly = board.getPolyomino(in placement);
        //         if (poly is ITriggerEffect t)
        //         {
        //             effect = (T)t;
        //             return true;
        //         }
        //     }
        //     effect = null;
        //     return false;
        // }
    }
}