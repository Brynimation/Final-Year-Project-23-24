//This is mostly boilerplate code from here: https://docs.unity3d.com/Manual/NativePluginInterface.html
//And also lots of help from here: http://xdpixel.com/native-rendering-plugin-in-unity/
//And lots of help from here: https://github.com/Unity-Technologies/NativeRenderingPlugin
#include "IUnityGraphics.h" //unity interface
#include "d3d11.h" //DirectX version we need
#include "IUnityGraphicsD3D11.h" //Specific unity graphics interface
#include <assert.h>
#include <math.h>
#include <vector>
#include "D3D11RenderAPI.h"

static IUnityInterfaces* s_UnityInterfaces = nullptr;
static IUnityGraphics* s_Graphics = nullptr;
static UnityGfxRenderer s_RenderType = kUnityGfxRendererNull;


static void* s_VertexBufferHandle = NULL;
static int s_VertexBufferCount;
static void* s_IndexBufferHandle = NULL;
static std::vector<StarVertex> vertices;

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType type);
static void DoEventGraphicsDeviceD3D11(UnityGfxDeviceEventType type);
static void Render(IUnityInterfaces*interfaces, int eventid);
static void Initialise(IUnityInterfaces* interfaces, int eventid);
static bool initialised = false;



extern "C" //Any functions we want exposed to unity should be put in this extern "c" block
{
	//Below function is run when this plugin is loaded
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces) 
	{
		s_UnityInterfaces = interfaces;
		s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
		s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
		//Call OnGraphicsDeviceEvent(Initialize) manually on PluginLoad to not miss the event incase the device is already initialised
		OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
	}
	//Below function is run when this plugin is unloaded.
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() 
	{
		s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	//This is called for GL.IssuePluginEvent calls. EventID will be the 
	//integer passed to IssuePluginEvent
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API OnRenderEvent(int eventId) 
	{
		if (!initialised) 
		{
			Initialise(s_UnityInterfaces, eventId);
			initialised = true;
		}
		Render(s_UnityInterfaces, eventId);
	}
	//This method is run as a callback when the IssuePluginAndEventData method is called from c#
	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunction() 
	{
		return OnRenderEvent; 
	}

	/*
	References vs pointers: You cannot have null references, references can't be changed to refer to another object once declared,
	and a reference must be initialised when it is declared.
	If a variable name is a label attached to the variable's location in memory, then a reference is a second label attached to that 
	same memory location. You can therefore access the contents of said variable through either the variable name or the reference.
	int i = 17
	int& r = i; //"r is an integer reference initialised to i"
	*/
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetMeshDataFromUnity(void* vertexBufferHandle, int vertexCount, float* sourceVertices, void* indexBufferHandle, int* sourceIndices)
	{
		s_VertexBufferHandle = vertexBufferHandle;
		s_VertexBufferCount = vertexCount;
		s_IndexBufferHandle = indexBufferHandle;
		vertices.resize(vertexCount);

		for (int i = 0; i < vertexCount; i++) 
		{
			StarVertex& vert = vertices[i]; //vert is a StarVertex reference initialised to the ith element in our vertex buffer.
			vert.position[0] = sourceVertices[0];
			vert.position[1] = sourceVertices[1];
			vert.position[2] = sourceVertices[2];
			vert.id = sourceIndices[0];
			sourceVertices += 3;
			sourceIndices += 1;
		}
	}

}

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType type) 
{
	UnityGfxRenderer currentDeviceType = s_RenderType;

	switch (type) 
	{
		case kUnityGfxDeviceEventInitialize:
			//initialisation code
			break;
		case kUnityGfxDeviceEventShutdown:
			//shutdown code
			break;
		case kUnityGfxDeviceEventBeforeReset:
			break;
		case kUnityGfxDeviceEventAfterReset:
			break;
	}
}
static void Render(IUnityInterfaces* interfaces, int eventId) 
{
	ID3D11Device* pDevice = nullptr;
	IDXGISwapChain* pSwapChain = nullptr;
	ID3D11DeviceContext* pContext = nullptr;

	/*Create device, front and back buffers, swapchain and the rendering context*/
	D3D11CreateDeviceAndSwapChain(
		*pAdapter = nullptr, //IDXGIAdapter - in, optional - leaving this null means it chooses the default adapter
		DriverType = D3D_DRIVER_TYPE_HARDWARE, //Hardware device
		Software = nullptr, //HMODULE - A handle to a module - if you want to load a driver
		Flags = 0, //uint
		*pFeatureLevels = nullptr, //const D3D_FEATURE_LEVEL, - choose what feature levels we want to allow
		FeatureLevels = 0, //uint
		SDKVersion = D3D11_SDK_VERSION, //Macro expand
		*pSwapChainDesc = &sd, //in, optional - descriptor cons
		**ppSwapChain = &pSwapChain, //A pointer to a pointer - out, optional
		**ppDevice = &pDevice, //out, optional
		*pFeatureLevel = nullptr //out, optional
		**ppImmediateContext = &pContext
	);
}
////type alias - a FuncPtr is just a function that takes in a const char*.
//typedef void(*FuncPtr) (const char*);
//FuncPtr Debug;
//
///*The "extern" keyword has a number of different functionalities 
//depending on the context. Here, extern "C" specifies that the functions
//we are calling are both defined elsewhere and should be called using
//the C-language calling convention.
//A calling convention is a low-level scheme for how functions receive
//parameters from their caller, and how they return a result to their 
//caller.*/
//
///*UNITY_INTERFACE_EXPORT and UNITY_INTERFACE_API are both macros defined in the
//IUnityInterface.h file, which is an include in the IUnityGraphics.h file.
//UNITY_INTERFACE_EXPORT = _declspec(dllexport) (Allows us to export data, functions, classes and
//class members from the dll)
//UNITY_INTERFACE_API = _stdcall (defines how calls to the function will be made)
//*/
//static IUnityInterfaces* unityInterfaces = NULL;
//static IUnityGraphics* graphics = NULL;
//static UnityGfxRenderer rendererType = kUnityGfxRendererNull;
//
//namespace globals {
//	ID3D11Device* device = nullptr;
//	ID3D11DeviceContext* context = nullptr;
//}
//extern "C" {
//	UNITY_INTERFACE_EXPORT void SetDebugFunction(FuncPtr fp)
//	{
//		Debug = fp;
//	}
//	static void UNITY_INTERFACE_API OnRenderEvent(int eventId)
//	{
//		Debug("Hello world");
//	}
//
//	/*To handle main unity events, a plugin must export UnityPluginLoad and
//	UnityPluginUnload. These are both callback functions
//	which are called when the plugin is loaded/unloaded.*/
//
//	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
//	{
//		//auto is the same as "var" in c#
//		auto m_unityInterfaces = unityInterfaces;
//		IUnityGraphicsD3D11* d3d11 = m_unityInterfaces->Get<IUnityGraphicsD3D11>();
//		globals::device = d3d11->GetDevice();
//		globals::device->GetImmediateContext(&globals::context);
//	}
//	/*This function returns a UnityRenderingEvent. A UnityRenderingEvent is simply a void
//	function that takes a single integer parameter.*/
//	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API getEventFunction()
//	{
//		return OnRenderEvent;
//	}
//}