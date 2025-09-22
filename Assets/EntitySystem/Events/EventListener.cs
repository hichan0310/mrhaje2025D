namespace EntitySystem.Events
{
    [System.Serializable]
    public abstract class EventArgs
    {
        public string name { get; protected set; }
        public abstract void trigger();
    }
    
    public interface IEntityEventListener
    {
        public void eventActive<T>(T eventArgs) where T : EventArgs;
        public void registrarTarget(Entity target, object args=null);
        public void removeSelf();
        public void update(float deltaTime, Entity target);
    }

    public interface ITimeInfo { public float time { get; } }
    public interface IPercentInfo { public float percent { get; } }
    public interface IDamageInfo { public int damage { get; } }
    public interface IEntityInfo { public Entity entity { get; } }
}