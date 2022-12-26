using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MandelbrotSetComputeShader : MonoBehaviour
{
    [Header("Mandelbrot set variables")]
    [SerializeField] float width;
    [SerializeField] float height;

    [SerializeField] float realOrigin;
    [SerializeField] float complexOrigin;

    [SerializeField] float increment;
    [SerializeField] int maxIterations;
    [SerializeField] float zoom;

    [Header("Compute Shader variables")]
    [SerializeField] ComputeShader shader;
    [SerializeField] ComputeBuffer buffer; //compute buffers are used to pass data from unity memory to the gpu
    [SerializeField] RenderTexture texture;
    [SerializeField] RawImage image;

    //We need a struct that contains all the data we want to pass between unity and the compute shader
    [System.Serializable]
    struct MandelbrotData
    {
        public float width;
        public float height;
        public float real;
        public float imaginary;

        public int screenWidth;
        public int screenHeight;
        public MandelbrotData(float w, float h, float r, float i, int sw, int sh)
        {
            width = w;
            height = h;
            real = r;
            imaginary = i;
            screenWidth = sw;
            screenHeight = sh;
        }
    }
    [SerializeField] MandelbrotData[] data; //This will be fed into the compute buffer
    void Start()
    {
        width = 4.5f;
        /*As our canvas is set to match the screen's width, we set the height based on the width*/
        height = width * Screen.height / Screen.width;
        realOrigin = -2.0f;
        complexOrigin = -1.25f;

        /*Create the array containing only the first iteration of our Mandelbrot set.*/
        data = new MandelbrotData[1];

        data[0] = new MandelbrotData(width, height, realOrigin, complexOrigin, Screen.width, Screen.height);

        //public ComputeBuffer(int count, int stride) - count is the number of elements, stride is the size of each element in bytes.
        //A MandelbrotData element contains 4 floats (8 bytes each) and 2 ints (4 bytes each).
        int stride = 4 * 4 + 2 * 4;
        buffer = new ComputeBuffer(data.Length, stride);

        /*A RenderTexture is a special kind of texture that unity creates and updates at runtime.  
         They're textures that can be renderered to.*/
        texture = new RenderTexture(Screen.width, Screen.height, 0);
        texture.enableRandomWrite = true;
        texture.Create();
        Mandelbrot();
    }

    void Mandelbrot() 
    {
        int kernalIndex = shader.FindKernel("CSMain");
        buffer.SetData(data); //Feed our data into the compute buffer.
        shader.SetBuffer(kernalIndex, "buffer", buffer);

        shader.SetInt("maxIterations", maxIterations);
        shader.SetTexture(kernalIndex, "Result", texture);
        shader.Dispatch(kernalIndex, Screen.width / 24, Screen.height / 24, 1);

        RenderTexture.active = texture;
        image.material.mainTexture = texture;
    }
    /*Usually c# and unity will do garbage collection and memory cleanup,
     but since HLSL is a low-level language we need to manually free up
    memory.*/
    private void OnDestroy()
    {
        buffer.Dispose(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
