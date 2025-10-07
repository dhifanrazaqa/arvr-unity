using UnityEngine;
using TensorFlowLite;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class TFLiteRunner : MonoBehaviour
{
    [Tooltip("Nama file model .tflite yang ada di dalam folder StreamingAssets")]
    public string modelFileName = "model_gender.tflite";

    private long[,] outputTensor = new long[1, 1];
    private Interpreter interpreter;

    IEnumerator Start()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
        Debug.Log("Mencoba memuat model dari path: " + modelPath);

        using (UnityWebRequest www = UnityWebRequest.Get(modelPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gagal memuat model dari StreamingAssets: " + www.error);
                yield break;
            }
            
            byte[] modelData = www.downloadHandler.data;

            if (modelData != null && modelData.Length > 0)
            {
                var options = new InterpreterOptions() { threads = 2 };
                interpreter = new Interpreter(modelData, options);
                interpreter.AllocateTensors();
                Debug.Log("TFLite Model Loaded Successfully via UnityWebRequest.");
            }
            else
            {
                Debug.LogError("Data model yang dimuat kosong atau korup.");
            }
        }
    }

    private float Sigmoid(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }

    public string Predict(Texture2D inputTexture)
    {
        if (interpreter == null)
        {
            Debug.LogError("ERROR: TFLite interpreter is null. Model mungkin gagal dimuat saat Start.");
            return null;
        }
        
        int inputWidth = 64;
        int inputHeight = 64;
        var inputTensor = new float[1, inputWidth, inputHeight, 3];

        var resizedTexture = ResizeTexture(inputTexture, inputWidth, inputHeight);
        for (int y = 0; y < inputHeight; y++)
        {
            for (int x = 0; x < inputWidth; x++)
            {
                Color32 pixel = resizedTexture.GetPixel(x, y);
                inputTensor[0, y, x, 0] = pixel.r / 255.0f;
                inputTensor[0, y, x, 1] = pixel.g / 255.0f;
                inputTensor[0, y, x, 2] = pixel.b / 255.0f;
            }
        }
        UnityEngine.Object.Destroy(resizedTexture);

        interpreter.SetInputTensorData(0, inputTensor);
        interpreter.Invoke();
        interpreter.GetOutputTensorData(0, outputTensor);

        float rawLogit = outputTensor[0, 0];
        float probability = Sigmoid(rawLogit);
        
        string prediction;
        double confidence;

        if (probability < 0.5f)
        {
            prediction = "Perempuan";
            confidence = 1 - probability;
        }
        else
        {
            prediction = "Laki-laki";
            confidence = probability;
        }
        
        return $"{{\"prediction\":\"{prediction}\",\"confidence\":{confidence}}}";
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
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

    void OnDestroy()
    {
        interpreter?.Dispose();
    }
}