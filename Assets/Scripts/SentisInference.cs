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
    Model runtimeModel;
    Worker worker;
    void Start()
    {
        runtimeModel = ModelLoader.Load(onnxFile);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }
    void ExecuteModel()
    {

    }
    public void CaptureFrame()
    {
        if (arCameraManager == null) return; //making sure camera manager is assigned

        //trying to grab the latest frame from cpu
        if(arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            using (image) // 'using' ensure image.Dispose() is called automatically
            {
                var conversionParams = new XRCpuImage.ConversionParams //define how to transform the raw data into a usable texture
                {
                    inputRect = new RectInt(0, 0, image.width, image.height), //use the entire source image area
                    outputDimensions = new Vector2Int(224, 224), //downscale the image
                    outputFormat = TextureFormat.RGBA32, //convert raw sensor data into a standard color format
                    transformation = XRCpuImage.Transformation.None //keep the orientation standard
                };
                int size = image.GetConvertedDataSize(conversionParams); //calculate how much memory is needed for the converted pixels
                var buffer = new NativeArray<byte>(size, Allocator.Temp); //allocate temporary highspeed memory for the pixel buffer
                image.Convert(conversionParams, buffer); // perform the actual conversion from raw data to RGBA bytes

                //creating a temporary texture2d object to hol the pixels in Unity
                Texture2D tempTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, TextureFormat.RGBA32, false);
                tempTexture.LoadRawTextureData(buffer); //upload the converted byte buffer to texture
                tempTexture.Apply();//finalize the texture to be read

                TensorShape shape = new TensorShape(1, 3, 224, 224); //define the shape(Batch:1,Channels:3,Width:224,Height:224)
                if (_savedTensor == null) _savedTensor = new Tensor<float>(shape);//if variable empty, initialize it as a new tensor float
                TextureConverter.ToTensor(tempTexture, _savedTensor, new TextureTransform());//copy texture to tensor variable and keep the orientation standard
                DestroyImmediate(tempTexture);//cleanup temporary objects to prevent memory leaks
                buffer.Dispose();
            }
        }
    }
}
