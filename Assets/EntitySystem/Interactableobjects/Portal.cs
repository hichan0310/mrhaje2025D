using EntitySystem;
using EntitySystem.Events;
using PlayerSystem;
using UnityEngine;

public class Portal
    : MonoBehaviour, IInteractable, IEntityEventListener
{
    [Header("Teleport")]
    [SerializeField] private Vector3 targetPosition;

    [Header("Focus Hint")]
    [SerializeField] private GameObject focusHintPrefab;
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 1.2f, 0f);

    private GameObject focusHintInstance;

    public Vector3 WorldPosition => transform.position;

    private void Start()
    {
        // Player 등록 (이벤트 시스템 그대로 활용)
        var player = FindObjectOfType<Player>();
        if (player)
        {
            registerTarget(player);
        }

        // F 말풍선 생성 (비활성)
        if (focusHintPrefab)
        {
            focusHintInstance = Instantiate(
                focusHintPrefab,
                transform.position + hintOffset,
                Quaternion.identity,
                transform // ★ 반드시 포탈의 자식
            );

            // 항상 위에 보이게
            var canvas = focusHintInstance.GetComponent<Canvas>();
            if (canvas)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 1000;
            }

            focusHintInstance.SetActive(false);
        }
    }


    public void OnFocus()
    {
        if (focusHintInstance)
            focusHintInstance.SetActive(true);
    }

    public void OnDefocus()
    {
        if (focusHintInstance)
            focusHintInstance.SetActive(false);
    }

    // ===============================
    // IEntityEventListener
    // ===============================
    public void registerTarget(Entity target, object args = null)
    {
        target.registerListener(this);
    }

    public void removeSelf() { }

    public void update(float deltaTime, Entity target) { }

    public void eventActive(EventArgs e)
    {
        if (e is not InteractionEvent ie) return;
        if (ie.interactable != this) return;
        if (ie.entity is not Player player) return;

        // 텔레포트
        player.transform.position = targetPosition;

        // 말풍선 정리
        if (focusHintInstance)
            focusHintInstance.SetActive(false);
    }
}
