using UnityEngine;

public class SoundRipple : MonoBehaviour
{
    public float maxRadius = 10f;
    public float expandSpeed = 20f;
    
    private float currentScale = 0f;
    private Material mat;
    private Color originalColor;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        originalColor = mat.color;
        transform.localScale = new Vector3(0, 0.01f, 0); // Start tiny
    }

    void Update()
    {
        // Expand the ring
        currentScale += expandSpeed * Time.deltaTime;
        transform.localScale = new Vector3(currentScale, 0.01f, currentScale);

        // Fade out the transparency as it expands
        float alpha = 1f - (currentScale / maxRadius);
        originalColor.a = Mathf.Max(alpha, 0f); // Prevent negative alpha
        mat.color = originalColor;

        // Destroy when it reaches max size
        if (currentScale >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}