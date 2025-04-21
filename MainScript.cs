using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class GameLogEntry {
    public int round;
    public string expectedButton;
    public string clickedButton;
    public float timeIssued;
    public float timeClicked;
    public bool correct;
    public string sessionId;
}

[System.Serializable]
public class RawScore {
    public string sessionId;
    public int totalTrials;
    public int omissions;
    public int commissions;
    public float hitRT;
    public float hitSE;
    public int perseverations;
    public float hitRTBlock;
    public float hitSEBlock;
    public float dPrime;
    public float beta;
    public float varSE;
    public float hitRTISI;
    public float hitSEISI;
    public int sex;
    public int age;
}

[System.Serializable]
public class CombinedData {
    public List<GameLogEntry> Items;
    public List<RawScore> Metrics;
}

public class MainScript : MonoBehaviour {
    public static MainScript instance { get; private set; }

    private string userId;
    private string sessionId;
    private List<GameObject> buttonsList = new List<GameObject>();
    private List<GameObject> buttonOrder = new List<GameObject>();
    private GameObject finalScreen;
    private int rounds = 1;
    private int counter = 0;

    public Sprite[] buttonImages;
    public Sprite[] newButtonImages;
    public Text currentRounds;
    public GameObject Failed;
    public GameObject button;
    public Transform mainUI;

    System.Random rnd = new System.Random();
    private List<GameLogEntry> gameLogs = new List<GameLogEntry>();
    private float previousClickTime;
    private Coroutine inactivityCoroutine;

    private int round {
        get { return rounds; }
        set {
            rounds = value;
            currentRounds.text = "Round: " + rounds;
        }
    }

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        userId = PlayerPrefs.GetString("UserID", "Guest");
        sessionId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        SpawnButtons(new Vector3(0, 180, 0), new Vector2(260, 100));
        SpawnButtons(new Vector3(-180, 0, 0), new Vector2(100, 260));
        SpawnButtons(new Vector3(0, -180, 0), new Vector2(260, 100));
        SpawnButtons(new Vector3(180, 0, 0), new Vector2(100, 260));
        SpawnButtons(new Vector3(0, 0, 0), new Vector2(225, 225));
        FinalScreen();

        buttonsList[0].name = "Blue";
        buttonsList[1].name = "Pink";
        buttonsList[2].name = "Green";
        buttonsList[3].name = "Orange";
        buttonsList[4].name = "Center";

        for (int i = 0; i < buttonsList.Count; i++) {
            buttonsList[i].GetComponent<ButtonScript>().ApplySprite(buttonImages[i]);
        }

        AddObject();
    }

    void Update() {
        if (buttonsList.Count == 5) {
            if (Input.GetKeyDown(KeyCode.A)) CheckObject(buttonsList[1]);
            else if (Input.GetKeyDown(KeyCode.D)) CheckObject(buttonsList[3]);
            else if (Input.GetKeyDown(KeyCode.W)) CheckObject(buttonsList[0]);
            else if (Input.GetKeyDown(KeyCode.LeftShift)) CheckObject(buttonsList[2]);
            else if (Input.GetKeyDown(KeyCode.Space)) CheckObject(buttonsList[4]);
        }
    }

    void AddObject() {
        DisableButtons();
        GameObject rndBtn = buttonsList[rnd.Next(buttonsList.Count)];
        buttonOrder.Clear();
        buttonOrder.Add(rndBtn);
        counter = 0;
        ShowOrder();
    }

private void ShowOrder()
{
    StartCoroutine(BlinkButton(buttonOrder[0]));
}


    IEnumerator InactivityTimeout() {
        yield return new WaitForSeconds(1.5f);
        if (counter < buttonOrder.Count) {
            gameLogs.Add(new GameLogEntry {
                round = round,
                expectedButton = buttonOrder[counter].name,
                clickedButton = "None",
                timeIssued = 0f,
                timeClicked = 0f,
                correct = false,
                sessionId = sessionId
            });

            counter++;
            EndOfRound();
        }
    }

    public void CheckObject(GameObject obj) {
        if (counter >= buttonOrder.Count) return;

        if (inactivityCoroutine != null) {
            StopCoroutine(inactivityCoroutine);
            inactivityCoroutine = null;
        }

        float responseTime = Time.time - previousClickTime;
        previousClickTime = Time.time;

        bool isCorrect = (obj == buttonOrder[counter]);

        gameLogs.Add(new GameLogEntry {
            round = round,
            expectedButton = buttonOrder[counter].name,
            clickedButton = obj.name,
            timeIssued = 0f,
            timeClicked = responseTime,
            correct = isCorrect,
            sessionId = sessionId
        });

        counter++;
        EndOfRound();
    }

    void EndOfRound() {
        counter = 0;
        round++;

        if (round <= 100) {
            AddObject();
        } else {
            EndSession();
        }
    }

    private async void EndSession() {
        DisableButtons();
        finalScreen.SetActive(true);
        await System.Threading.Tasks.Task.Delay(5000);
        finalScreen.SetActive(false);

        UploadToS3(); // 🔥 Call your S3 uploader here
    }

    private void SpawnButtons(Vector3 pos, Vector2 newSize) {
        GameObject _button = Instantiate(button, pos, Quaternion.identity);
        _button.GetComponent<RectTransform>().sizeDelta = newSize;
        buttonsList.Add(_button);
        _button.transform.SetParent(mainUI, false);
    }

    private void FinalScreen() {
        finalScreen = Instantiate(Failed, Vector3.zero, Quaternion.identity);
        finalScreen.SetActive(false);
        finalScreen.transform.SetParent(mainUI, false);
    }

    private void DisableButtons() {
        foreach (var btn in buttonsList)
            btn.GetComponent<ButtonScript>().Disable();
    }

    private void EnableButtons() {
        foreach (var btn in buttonsList)
            btn.GetComponent<ButtonScript>().Enable();
    }

    // 🔥 Upload JSON to S3 through your API Gateway + Lambda
    private void UploadToS3() {
        CombinedData data = new CombinedData {
            Items = gameLogs,
            Metrics = new List<RawScore> { CalculateMetrics(gameLogs) }
        };

        string jsonPayload = JsonUtility.ToJson(data);
        StartCoroutine(PostJsonToS3(jsonPayload));
    }

    private IEnumerator PostJsonToS3(string jsonData) {
        string url = "https://your-api-gateway-endpoint.amazonaws.com/prod/upload"; // 🔁 Replace with actual endpoint

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log("Data uploaded to S3 successfully");
        } else {
            Debug.LogError("Upload failed: " + request.error);
        }
    }

    private RawScore CalculateMetrics(List<GameLogEntry> logs, int blockSize = 20) {
        int omissions = logs.Count(x => x.clickedButton == "None");
        int commissions = logs.Count(x => x.clickedButton != x.expectedButton && !x.correct && x.clickedButton != "None");

        var hitLogs = logs.Where(x => x.correct).ToList();
        var rtList = hitLogs.Select(x => x.timeClicked).Where(rt => rt > 0).ToList();

        float meanRT = rtList.Count > 0 ? rtList.Average() : 0f;
        float stdDevRT = rtList.Count > 0 ? Mathf.Sqrt(rtList.Average(rt => Mathf.Pow(rt - meanRT, 2))) : 0f;

        int perseverations = logs.Count(x => x.timeClicked < 0.1f && x.timeClicked > 0);

        List<float> isiList = new List<float>();
        for (int i = 1; i < rtList.Count; i++) {
            isiList.Add(rtList[i] - rtList[i - 1]);
        }

        float isiMean = isiList.Count > 0 ? isiList.Average() : 0f;
        float isiSE = isiList.Count > 1 ? Mathf.Sqrt(isiList.Average(x => Mathf.Pow(x - isiMean, 2))) : 0f;

        List<float> rtBlock = new List<float>();
        List<float> seBlock = new List<float>();

        for (int i = 0; i < rtList.Count; i += blockSize) {
            var blockRTs = rtList.Skip(i).Take(blockSize).ToList();
            if (blockRTs.Count > 0) {
                float blockMean = blockRTs.Average();
                float blockSE = Mathf.Sqrt(blockRTs.Average(rt => Mathf.Pow(rt - blockMean, 2)));
                rtBlock.Add(blockMean);
                seBlock.Add(blockSE);
            }
        }

        float rtBlockAvg = rtBlock.Count > 0 ? rtBlock.Average() : 0f;
        float seBlockAvg = seBlock.Count > 0 ? seBlock.Average() : 0f;
        float varSE = seBlock.Count > 1 ? Mathf.Sqrt(seBlock.Average(se => Mathf.Pow(se - seBlock.Average(), 2))) : 0f;

        float hitRate = (float)hitLogs.Count / logs.Count;
        float falseAlarmRate = (float)commissions / logs.Count;

        hitRate = Mathf.Clamp(hitRate, 0.01f, 0.99f);
        falseAlarmRate = Mathf.Clamp(falseAlarmRate, 0.01f, 0.99f);

        float zHit = Z(hitRate);
        float zFA = Z(falseAlarmRate);

        float dPrime = zHit - zFA;
        float beta = Mathf.Exp((zFA * zFA - zHit * zHit) / 2f);

        return new RawScore {
            sessionId = logs[0].sessionId,
            totalTrials = logs.Count,
            omissions = omissions,
            commissions = commissions,
            hitRT = meanRT,
            hitSE = stdDevRT,
            perseverations = perseverations,
            hitRTBlock = rtBlockAvg,
            hitSEBlock = seBlockAvg,
            varSE = varSE,
            dPrime = dPrime,
            beta = beta,
            hitRTISI = isiMean,
            hitSEISI = isiSE,
            sex = 1,
            age = 14
        };
    }

    private float Z(float p) {
        return Mathf.Sqrt(2) * ErfInv(2 * p - 1);
    }

    private float ErfInv(float x) {
        float a = 0.147f;
        float ln = Mathf.Log(1 - x * x);
        float first = (2 / (Mathf.PI * a)) + (ln / 2);
        float second = ln / a;
        return Mathf.Sign(x) * Mathf.Sqrt(Mathf.Sqrt(first * first - second) - first);
    }
    IEnumerator BlinkButton(GameObject btn)
{
    yield return new WaitForSeconds(0.2f); // wait before blinking

    int index = buttonsList.IndexOf(btn);
    btn.GetComponent<ButtonScript>().ApplySprite(newButtonImages[index]);

    yield return new WaitForSeconds(0.5f); // blink duration

    btn.GetComponent<ButtonScript>().ApplySprite(buttonImages[index]);

    EnableButtons();
    previousClickTime = Time.time;

    if (inactivityCoroutine != null) StopCoroutine(inactivityCoroutine);
    inactivityCoroutine = StartCoroutine(InactivityTimeout());
}

}
