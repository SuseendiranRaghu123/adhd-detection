using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoCollector : MonoBehaviour
{
    public GameObject inputPanel;
    public InputField ageInput;
    public InputField sexInput;
    public Button startButton;

    private void Start()
    {
        inputPanel.SetActive(true);
        startButton.onClick.AddListener(SubmitInfo);
    }

    void SubmitInfo()
    {
        string age = ageInput.text.Trim();
        string sex = sexInput.text.Trim();

        if (string.IsNullOrEmpty(age) || string.IsNullOrEmpty(sex))
        {
            Debug.LogWarning("Please enter all required fields.");
            return;
        }

        PlayerPrefs.SetString("Age", age);
        PlayerPrefs.SetString("Sex", sex);
        PlayerPrefs.Save();

        inputPanel.SetActive(false);

        Debug.Log($"User Info Saved - Age: {age}, Sex: {sex}");
    }
}
