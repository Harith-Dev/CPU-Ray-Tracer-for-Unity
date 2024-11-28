using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private Mesh bakedMesh;
    public MeshCollider meshCollider;

    void Start()
    {
        // Get the SkinnedMeshRenderer component
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        // Check if the SkinnedMeshRenderer is not found
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("SkinnedMeshRenderer not found on this GameObject.");
            return;
        }

        // Create a new baked mesh
        bakedMesh = new Mesh();

        // Add a MeshCollider if not already present
        meshCollider = gameObject.AddComponent<MeshCollider>();
    }

    void Update()
    {
        // Bake the skinned mesh
        skinnedMeshRenderer.BakeMesh(bakedMesh);

        // Update the mesh collider with the baked mesh
        meshCollider.sharedMesh = bakedMesh;
    }
}