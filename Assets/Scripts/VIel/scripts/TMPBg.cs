using TMPro;
using UnityEngine;

public class tmpbg : MonoBehaviour
{
    public TMP_Text myText;

    void Start()
    {
        if (myText != null)
        {
            Color bgColor = new Color(1f, 0f, 0f, 0.5f); // semi-transparent red
            myText.text = $"<mark=#{ColorUtility.ToHtmlStringRGBA(bgColor)}>This has a background!</mark>";
        }
    }
}
