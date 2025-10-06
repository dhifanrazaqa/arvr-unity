using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Networking;
using System;

[System.Serializable]
public class ThemePrefabs
{
    public GameObject malePrefab;
    public GameObject femalePrefab;
}

[System.Serializable]
public class PredictionResponse
{
    public double confidence;
    public string prediction;
}


public class ThemeManager : MonoBehaviour
{
    [Header("Pengaturan AI")]
    public Toggle aiModeToggle;
    public TFLiteRunner tfliteRunner; 

    [Header("Referensi Komponen AR")]
    public ARFaceManager faceManager;

    [Header("Pengaturan Tema")]
    public List<ThemePrefabs> themes;

    [Header("Pengaturan UI")]
    public TextMeshProUGUI predictionText;
    public Button[] themeButtons;

    [Header("Pengaturan Server")]
    public string serverURL = "https://dhifanrazaqa-arvr-gender.hf.space/predict";
    
    private int currentThemeIndex = 0;
    private bool isFaceTracked = false;
    private bool isProcessing = false;

    void OnEnable()
    {
        faceManager.facesChanged += OnFacesChanged;
    }

    void OnDisable()
    {
        faceManager.facesChanged -= OnFacesChanged;
    }

    private void DisplayErrorOnUI(string errorMessage)
    {
        predictionText.text = $"ERROR:\n{errorMessage}";
        SetButtonsInteractable(false);
    }

    void Start()
    {
        aiModeToggle.onValueChanged.AddListener(OnAIModeChanged);
        OnAIModeChanged(aiModeToggle.isOn);
    }
    
    private void OnAIModeChanged(bool useLocalModel)
    {
        predictionText.text = useLocalModel ? "Mode AI: Lokal (Offline)" : "Mode AI: REST API (Online)";
    }

    void OnFacesChanged(ARFacesChangedEventArgs eventArgs)
    {
        if (eventArgs.added.Count > 0 && !isFaceTracked && !isProcessing)
        {
            isFaceTracked = true;
            StartCoroutine(ProcessPrediction());
        }
        else if (eventArgs.removed.Count > 0)
        {
            isFaceTracked = false;
            predictionText.text = "Arahkan kamera ke wajah...";
        }
    }
    
    public void SelectThemeAndPredict(int themeIndex)
    {
        currentThemeIndex = themeIndex;
        if (isFaceTracked && !isProcessing)
        {
            StartCoroutine(ProcessPrediction());
        }
    }

    private IEnumerator ProcessPrediction()
    {
        if (isProcessing) yield break;
        isProcessing = true;

        predictionText.text = "Menganalisis..."; 
        SetButtonsInteractable(false);

        yield return new WaitForEndOfFrame();
        
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        string resultJson = null;

        if (aiModeToggle.isOn)
        {
            resultJson = tfliteRunner.Predict(screenTexture);
        }
        else
        {
            yield return StartCoroutine(UploadImage(screenTexture, (response, error) => {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning("API Error: " + error + ". Fallback to TFLite.");
                    resultJson = tfliteRunner.Predict(screenTexture);
                }
                else
                {
                    resultJson = response;
                }
            }));
        }
        
        Destroy(screenTexture);

        if (!string.IsNullOrEmpty(resultJson))
        {
            UpdateFaceFilter(resultJson);
        }
        else
        {
            predictionText.text = "Gagal mendapatkan hasil prediksi.";
        }

        isProcessing = false;
        SetButtonsInteractable(true);
    }
    
    private void UpdateFaceFilter(string responseJson)
    {
        PredictionResponse response = JsonUtility.FromJson<PredictionResponse>(responseJson);
        ThemePrefabs selectedTheme = themes[currentThemeIndex];
        GameObject prefabToSet = null;

        if (response.prediction.ToLower() == "perempuan")
        {
            prefabToSet = selectedTheme.femalePrefab;
        }
        else
        {
            prefabToSet = selectedTheme.malePrefab;
        }

        if (prefabToSet != null && faceManager.facePrefab != prefabToSet)
        {
            faceManager.facePrefab = prefabToSet;
            
            string formattedConfidence = response.confidence.ToString("P1");
            predictionText.text = $"Prediksi: {response.prediction} ({formattedConfidence})\nFilter: {prefabToSet.name}";
            
            StartCoroutine(ResetFaceManager());
        }
        else if(prefabToSet != null)
        {
            string formattedConfidence = response.confidence.ToString("P1");
            predictionText.text = $"Prediksi: {response.prediction} ({formattedConfidence})\nFilter: {faceManager.facePrefab.name}";
        }
        else
        {
             predictionText.text = "Filter untuk gender ini belum di-assign!";
        }
    }

    private IEnumerator ResetFaceManager()
    {
        faceManager.enabled = false;
        yield return null; 
        faceManager.enabled = true;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth)
    {
        float aspectRatio = (float)source.height / source.width;
        int newHeight = Mathf.RoundToInt(newWidth * aspectRatio);
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readableTexture = new Texture2D(newWidth, newHeight);
        readableTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readableTexture;
    }

    private IEnumerator UploadImage(Texture2D imageToUpload, Action<string, string> onComplete)
    {
        var resizedTexture = ResizeTexture(imageToUpload, 480);
        byte[] imageData = resizedTexture.EncodeToJPG(75);
        Destroy(resizedTexture);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageData, "capture.jpg", "image/jpeg");

        using (var www = UnityWebRequest.Post(serverURL, form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(www.downloadHandler.text, null);
            }
            else
            {
                onComplete?.Invoke(null, www.error);
            }
        }
    }
    
    private void SetButtonsInteractable(bool isInteractable)
    {
        foreach (var button in themeButtons)
        {
            button.interactable = isInteractable;
        }
    }
}