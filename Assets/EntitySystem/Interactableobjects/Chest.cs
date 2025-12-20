using EntitySystem;
using EntitySystem.Events;
using PlayerSystem;
using PlayerSystem.Tiling;
using UnityEngine;

public class Chest
    : MonoBehaviour, IInteractable, IEntityEventListener
{
    [Header("Reward")]
    [SerializeField] private Polyomino reward;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color focusColor = Color.yellow;

    [Header("Effect")]
    [SerializeField] private GameObject interactEffectPrefab;

    private bool opened;

    public Vector3 WorldPosition => transform.position;

    private void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        sr.color = normalColor;
    }

    private void Start()
    {
        var player = FindObjectOfType<PlayerSystem.Player>();
        if (player)
        {
            registerTarget(player);
            Debug.Log("Chest registered to player");
        }
    }

    // ===== Focus =====
    public void OnFocus()
    {
        if (opened) return;
        sr.color = focusColor;
    }

    public void OnDefocus()
    {
        if (opened) return;
        sr.color = normalColor;
    }

    // ===== IEntityEventListener =====
    public void registerTarget(Entity target, object args = null)
    {
        target.registerListener(this);
    }

    public void removeSelf() { }
    public void update(float deltaTime, Entity target) { }

    public void eventActive(EventArgs e)
    {
        Debug.Log("interact tried");
        if (opened) return;
        if (e is not InteractionEvent ie) return;
        if (ie.interactable != this) return;
        if (ie.entity is not PlayerSystem.Player player) return;

        opened = true;


        if (interactEffectPrefab)
        {
            Instantiate(
                interactEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        var poly = Instantiate(reward, player.inventory.transform, false);
        player.AddItemToInventory(poly);



        Destroy(gameObject);
    }
}
