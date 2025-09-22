namespace EntitySystem.StatSystem
{
    public interface IStat
    {
        public void registerBuff(IBuff buff);
        public void removeBuff(IBuff buff);
        
        public int maxHp { get; }
        public int nowHp { get; }
        public int calculateTrueDamage(AtkTagSet tags, int coefficient);
        public int calculateTakenDamage(AtkTagSet tags, int damage);
        public void takeDamage(int damage);
    }
}