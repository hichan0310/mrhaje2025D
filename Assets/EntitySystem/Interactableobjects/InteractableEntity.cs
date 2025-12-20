using EntitySystem;
using EntitySystem.Events;
using PlayerSystem;
using UnityEngine;

public abstract class InteractionListener
    : MonoBehaviour, IEntityEventListener
{
    protected IInteractable interactable;
    protected Entity targetEntity; // ∫∏≈Î Player

    protected virtual void Awake()
    {
        interactable = GetComponent<IInteractable>();
        if (interactable == null)
        {
            Debug.LogError($"{name} has InteractionListener but no IInteractable");
        }
    }

    public void registerTarget(Entity target, object args = null)
    {
        targetEntity = target;
        target.registerListener(this);
    }

    public void removeSelf()
    {
        if (targetEntity != null)
            targetEntity.removeListener(this);

        Destroy(this);
    }

    public virtual void update(float deltaTime, Entity target) { }

    public void eventActive(EventArgs e)
    {
        if (e is not InteractionEvent ie) return;
        if (ie.interactable != interactable) return;

        OnInteract(ie.entity);
    }

    protected abstract void OnInteract(Entity interactor);
}
