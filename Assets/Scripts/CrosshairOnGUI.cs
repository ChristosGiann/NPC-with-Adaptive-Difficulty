using UnityEngine;

public class CrosshairOnGUI : MonoBehaviour
{
    public int size = 14;      // overall size
    public int thickness = 2;  // line thickness
    Texture2D tex;

    void Awake()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.black);
        tex.Apply();
    }

    void OnGUI()
    {
        float x = (Screen.width - size) * 0.5f;
        float y = (Screen.height - size) * 0.5f;

        // horizontal
        GUI.DrawTexture(new Rect(x, y + (size - thickness) * 0.5f, size, thickness), tex);
        // vertical
        GUI.DrawTexture(new Rect(x + (size - thickness) * 0.5f, y, thickness, size), tex);
    }
}
