#include <stdio.h>
#include "IUnityGraphics.h" //unity interface
#include "d3d11.h" //DirectX version we need
#include "IUnityGraphicsD3D11.h
#include <filesystem>
#include <d3dcompiler.inl>


class D3D11RenderAPI 
{
public:
	D3D11RenderAPI();
private:
	ID3D11Device* pDevice;// = nullptr;
	ID3D11Device* pContext;
	IDXGISwapChain* pSwapChain;// = nullptr;
	ID3D11DeviceContext* pContext;// = nullptr;
	ID3D11InputLayout* pLayout;
	ID3D11Buffer* pVertexBuffer;// = nullptr;
	ID3D11Buffer* pIndexBuffer;// = nullptr;
	ID3D11Buffer* pConstantBuffer;// = nullptr;
	ID3D11Buffer* pStreamOutputBuffer; //
	string shaderDir = current_path();
	string shaderFiles[3] = new string[]{ "VS.hlsl, GS.hlsl, PS.hlsl" };

	CreateD3D11RenderAPI();
	CreateResources();
	ReleaseResources();
};

	/*LPCWSTR - Long Pointer to Constant Wide String.Means the string is stored in a 2 byte character
	as opposed to the normal char data type.
	*pDefines - optional array of D3D_SHADER_MACRO structures defining the shader macros. Set to
	null if not used.
	*pIncludes - optional pointer to an ID3DInclude interface. ID3DInclude is an include interace
	* that the user implements to allow an application to call user-overridable method for
	* opening and closing shader #include files.
	*pEntryPoint - name of entry point function
	* pTarget - A pointer to a constant null-terminated string that specifies the shader target
	* or set of shader features to compile against
	* Flags1 - A combination of shader compile options that are combined using a bitwise OR.
	* Flags2 - A combination of effect compile options. When compiling a shader and not an effect
	* set this value to zero
	* ppCode - a pointer to a variable that recieves a pointer to the ID3DBlob interface that can
	* be used to access the compiled code.
	*/
HResult D3D11RenderAPI::CompileShader(LPCWSTR filePath)
{
	HResult vertexShaderByteCode = D3DCompile(filePath, )
}

D3D11RenderAPI* CreateD3D11RenderAPI()
{
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
		* *ppImmediateContext = &pContext
	);
};
void D3D11RenderAPI::CreateResources()
{
	pDevice->GetImmediateContext(&pContext);
	
	//Describes a buffer resource.
	/*Initialise and bind to vertex buffer
	A vertex buffer contains the vertex data used to define your geometry. This includes position
	coordinates, colour data, texture coordinate data, normal data and so forth.
	To access data from a vertex buffer you need to know which vertex to access. To do this, you need:
		- the offset (usually 0) - defines the number of elements in the buffer that are empty before
		the first vertex.
		-the BaseVertexLocation - its index, basically
	Before creating a vertex buffer we much define its layout by creating an ID3D11InputLayout interface
	by calling the method below. We can then bind it to the input assembler
	*/
	D3D11_INPUT_ELEMENT_DESC vertexBufferObject[] = //defines the StarVertex struct in our shader
	{
		//{semanticname, sematanticIndex, Format, inputslot, allignedByteOffset,InputslotClass, InstanceDataStepRate}
		{"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, },
		{"TEXCOORD0",0, DXGI_FORMAT_R32G32_FLOAT,},
		{"TEXCOORD1",1, DXGI_FORMAT_D32_FLOAT},
		{"SV_VERTEXID", 0, DXGI_FORMAT_R32_UINT,},
		{"COLOR", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, }
	};
	pDevice->CreateInputLayout(vertexBufferObject,5,)

	D3D11_BUFFER_DESC bufferDescriptor;
	desc.Usage = D3D11_USAGE_DEFAULT; //Buffer requires read/write access from/to the GPU
	desc.ByteWidth = 1024; //Size of the buffer, in bytes.
	desc.BindFlags = D3D11_BIND_VERTEX_BUFFER;//Bind a buffer as a vertex buffer to the input assembler stage
	pDevice->CreateBuffer(desc, NULL, &pVertexBuffer);

	/*Initialise and bind to index buffer
	Index buffers contain integer offsets into vertex buffers and are used to render primitives 
	more efficiently. An index buffer contains a sequential set of 16-bit or 32-bit indices. Each
	index identifies a vertex in the vertex buffer.
	*/

	desc.Usage = D3D11_USAGE_DEFAULT; //Buffer requires read/write access from/to the GPU
	desc.ByteWidth = D3D11_USAGE_DEFAULT;
	desc.ByteWidth = 1024;
	desc.BindFlags = D3D11_BIND_INDEX_BUFFER; //Bind a buffer as an index buffer to the input assembler stage
	desc.CPUAccessFlags = 0; //no need for the cpu to access this buffer.
	pDevice->CreateBuffer(&desc, NULL, &pIndexBuffer);

	/*Initialise and bind to the constant buffer*/
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.ByteWidth = 64;
	desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
	pDevice->CreateBuffer(&desc, NULL, &pConstantBuffer);

}



}