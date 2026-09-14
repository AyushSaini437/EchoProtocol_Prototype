using System.Collections;
using UnityEngine;

public class BulletHole : MonoBehaviour
{
    [Header("Lifetime Settings")]
    [Tooltip("Total lifetime in seconds before the bullet hole is destroyed.")]
    public float lifetime = 15f;

    [Tooltip("Duration in seconds over which the bullet hole fades out before destruction.")]
    public float fadeDuration = 3f;

    private Material mat;
    private Color initialColor;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            initialColor = mat.color;
            StartCoroutine(FadeAndDestroy());
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        float waitTime = Mathf.Max(0f, lifetime - fadeDuration);
        yield return new WaitForSeconds(waitTime);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(initialColor.a, 0f, elapsed / fadeDuration);
            if (mat != null)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}

