using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.SimpleCooler
{
    public class SimpleCooler : Polyomino
    {
        public override string Name => "SimpleCooler";

        public override string Description =>
            "power*50만큼 회복한다. \n" +
            this.statBonusDescription;

        public override void trigger(Entity player, float power)
        {
            new HealEvent(player, (int)(power * 50)).trigger();
        }
    }
}