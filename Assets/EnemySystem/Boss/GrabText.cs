using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.6f;
    [SerializeField] private float floatSpeed = 1.2f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }
}

