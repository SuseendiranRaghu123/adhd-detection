using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    MainScript ms;
    private Text keyLabel;

    // Mapping of button names to key labels
    private Dictionary<string, string> keyMappings = new Dictionary<string, string>()
    {
        {"Pink", "A"},
        {"Orange", "D"},
        {"Blue", "W"},
        {"Green", "Shift"},
        {"Center", "Space"}
    };

    void Start()
    {
        ms = MainScript.instance;
        gameObject.GetComponent<Button>().onClick.AddListener(btn);

        AddKeyLabel();
    }

    public void ApplySprite(Sprite sprite)
    {
        gameObject.GetComponent<Button>().GetComponent<Image>().sprite = sprite;
    }

    public void Disable()
    {
        gameObject.GetComponent<Button>().enabled = false;
    }

    public void Enable()
    {
        gameObject.GetComponent<Button>().enabled = true;
    }

    public void btn()
    {
        ms.CheckObject(gameObject);
    }

   private void AddKeyLabel()
{
    if (keyLabel == null)
    {
        GameObject labelObj = new GameObject("KeyLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, 60, 0); // Adjust as needed

        keyLabel = labelObj.AddComponent<Text>();
        keyLabel.alignment = TextAnchor.MiddleCenter;
        keyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // ✅ FIXED
        keyLabel.fontSize = 24;
        keyLabel.color = Color.black;
        keyLabel.resizeTextForBestFit = true;
        keyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyLabel.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = keyLabel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 40);
    }

    if (keyMappings.ContainsKey(gameObject.name))
    {
        keyLabel.text = keyMappings[gameObject.name];
    }
    else
    {
        keyLabel.text = "";
    }
}
}