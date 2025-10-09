using EntitySystem.StatSystem;

namespace EntitySystem
{
    public interface IBuff
    {
        public bool isStable { get; }
        public void applyBuff(IStat stat);
        public void removeSelf();
    }
}