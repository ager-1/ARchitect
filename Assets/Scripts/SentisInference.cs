using System;
using System.Collections;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SentisInference : MonoBehaviour
{
    [SerializeField] private ModelAsset onnxFile;
    [SerializeField] ARCameraManager arCameraManager;
    [SerializeField] Tensor<float> _savedTensor;
    [SerializeField] Tensor<float> outputTensor;
    Model runtimeModel;
    Worker worker;
    void Start()
    {
        runtimeModel = ModelLoader.Load(onnxFile);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }
    public void CaptureFrame()
    {
        if (arCameraManager == null) return; //making sure camera manager is assigned

        //trying to grab the latest frame from cpu
        if(arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            using (image) // 'using' ensure image.Dispose() is called automatically
            {
                int squareSize = Mathf.Min(image.width, image.height);
                int offsetX = (image.width - squareSize) / 2;
                int offsetY = (image.height - squareSize) / 2;
                var conversionParams = new XRCpuImage.ConversionParams //define how to transform the raw data into a usable texture
                {
                    inputRect = new RectInt(offsetX, offsetY, squareSize, squareSize), 
                    outputDimensions = new Vector2Int(224, 224),
                    outputFormat = TextureFormat.RGBA32, 
                    transformation = XRCpuImage.Transformation.None 
                };
                int size = image.GetConvertedDataSize(conversionParams); 
                var buffer = new NativeArray<byte>(size, Allocator.Temp); 
                image.Convert(conversionParams, buffer); 

                //creating a temporary texture2d object to hold the pixels in Unity
                Texture2D tempTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, TextureFormat.RGBA32, false);
                tempTexture.LoadRawTextureData(buffer); 
                tempTexture.Apply();

                TensorShape shape = new TensorShape(1, 3, 224, 224); 
                if (_savedTensor == null) _savedTensor = new Tensor<float>(shape);
                TextureConverter.ToTensor(tempTexture, _savedTensor, new TextureTransform());
                worker.Schedule(_savedTensor);
                DestroyImmediate(tempTexture);
                buffer.Dispose();
            }
        }
    }
    void GetModelOutput()
    {
        outputTensor = worker.PeekOutput() as Tensor<float>;
    }
    void OnDestroy()
    {
        if (_savedTensor != null) _savedTensor.Dispose();
        if (outputTensor != null) outputTensor.Dispose();
        if (worker != null) worker.Dispose();
    }
}
