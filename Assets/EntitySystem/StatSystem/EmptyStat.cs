namespace EntitySystem.StatSystem
{
    public class EmptyStat:IStat
    {
        public void registerBuff(IBuff buff)
        {
            
        }

        public void removeBuff(IBuff buff)
        {
            
        }

        int IStat.maxHp => 1;

        int IStat.nowHp => 1;

        public int calculateTrueDamage(AtkTagSet tags, int coefficient)
        {
            return 0;
        }

        public int calculateTakenDamage(AtkTagSet tags, int damage)
        {
            return 0;
        }

        public void takeDamage(int damage)
        {
            
        }
    }
}