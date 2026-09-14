using System.Collections;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [Header("Muzzle Flash Settings")]
    [Tooltip("How long the flash stays visible in seconds.")]
    public float flashDuration = 0.05f;

    [Tooltip("Visual mesh/quad representing the muzzle flash starburst.")]
    public GameObject flashVisual;

    [Tooltip("Point light illuminating the firearm and nearby environment.")]
    public Light flashLight;

    private Coroutine flashCoroutine;

    void Awake()
    {
        // Auto-wire components if not explicitly assigned
        if (flashVisual == null)
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                flashVisual = visualTransform.gameObject;
            }
            else
            {
                flashVisual = gameObject;
            }
        }

        if (flashLight == null)
        {
            flashLight = GetComponentInChildren<Light>(true);
        }

        // Ensure flash is hidden at startup
        SetFlashActive(false);
    }

    public void TriggerFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Randomize rotation around the barrel axis for organic visual variety
        if (flashVisual != null)
        {
            flashVisual.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        SetFlashActive(true);

        yield return new WaitForSeconds(flashDuration);

        SetFlashActive(false);
        flashCoroutine = null;
    }

    private void SetFlashActive(bool active)
    {
        if (flashVisual != null && flashVisual != gameObject)
        {
            flashVisual.SetActive(active);
        }
        else if (flashVisual == gameObject)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = active;
        }

        if (flashLight != null)
        {
            flashLight.enabled = active;
        }
    }
}

