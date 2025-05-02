using UnityEngine;
using UnityEngine.UI;

public class DotSpinner : MonoBehaviour
{
    public Image[] dots; 
    public float interval = 0.1f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;

    private int currentIndex = 0;

    void OnEnable()
    {
        InvokeRepeating(nameof(AnimateDots), 0f, interval);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(AnimateDots));
    }

    void AnimateDots()
    {
        for (int i = 0; i < dots.Length; i++)
        {
            float alpha = (i == currentIndex) ? maxAlpha : minAlpha;
            var color = dots[i].color;
            color.a = alpha;
            dots[i].color = color;
        }

        currentIndex = (currentIndex + 1) % dots.Length;
    }
}
