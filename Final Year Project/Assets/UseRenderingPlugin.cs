using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class UseRenderingPlugin : MonoBehaviour
{
    /* Native plugin rendering events are only called if a plugin is used
	 by some active C# script. */
    [DllImport("RenderingPlugin")]

    //Functions declared with the extern keyword reference a function defined in our RenderingPlugin DLL.
    private static extern IntPtr GetRenderEventFunction();
    [DllImport("RenderingPlugin")]
    private static extern IntPtr SetMeshDataFromUnity(IntPtr vertexBuffer, int vertexCount, IntPtr sourceVerts, IntPtr indexBuffer, IntPtr sourceIndices);
    private void SendMeshDataToPlugin()
    {
        //Get a reference to the mesh whose data is being sent to the plugin
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;
        /*For the plugin to be able to modify the vertex buffer, for many platforms
         in order for that to work we must mark the mesh as dynamic. This makes the 
        buffers CPU writable. By default, they are only GPU readable*/
        mesh.MarkDynamic();
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] indices = mesh.GetIndices(0);

        /*Managed memory is cleaned up by a Garbage Collector (GC). Garbage collection is a form of automatic memory management.
         The garbage collector attempts to reclaim memory which was allocated by the program, but is no longer referenced.
        Unmanaged memory is cleaned up either explicitly by your program, or by the Operating System. GCHandle provides a way
        of accessing a Managed object from unmanaged memory
        A handle is an abstract reference to a resource provided by a third party (ie, the Windows Operating System). When the handle
        has been allocated, you can use it to prevent the object from being collected by the garbage collector when the the unmanaged
        client (our c++ code) holds its only reference (ie, when no references exist in our C# code). Without this reference, an object
        may be collected before completing its work on behalf of the unmanaged client (our c++ code).
        So here, we're allocating handles (references) to our vertex data, our uv data and our index data so it can be used by the c++
        native rendering plugin code.
        GCHandleType.Pinned has two effects: - The object can no longer be collected by the garbage collector AND the address of the object
        can be retrieved. Use the GCHandle.Free() method to free the allocated handle asap.
         */
        GCHandle gcVertices = GCHandle.Alloc(verts, GCHandleType.Pinned);
        GCHandle gcUVs = GCHandle.Alloc(uvs, GCHandleType.Pinned);
        GCHandle gcIndices = GCHandle.Alloc(indices, GCHandleType.Pinned);

        SetMeshDataFromUnity(mesh.GetNativeVertexBufferPtr(0), verts.Count(), gcVertices.AddrOfPinnedObject(), mesh.GetNativeIndexBufferPtr(), gcIndices.AddrOfPinnedObject());
        gcVertices.Free();
        gcIndices.Free();
    }
    IEnumerator Start() 
    {
        SendMeshDataToPlugin();
        yield return StartCoroutine("RenderPipeline");
    }

    IEnumerator RenderPipeline() 
    {
        while (true) 
        {
            yield return new WaitForEndOfFrame();

            //Issue a plugin event with an arbitrary integer identifier. The plugin can distinguish between different things it needs to do based on this id.
            GL.IssuePluginEvent(GetRenderEventFunction(), 0);
        }
    }
}

