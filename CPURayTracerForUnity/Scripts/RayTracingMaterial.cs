
using UnityEngine;

public class RayTracingMaterial : MonoBehaviour
{
    public bool AutoSetVulues;
    public bool NoColider;
    public bool LowPolyModel;
    public Color Color = Color.white;
    [Header("Emissive")]
    public Texture2D GlowTexture;
    public float EmissiveIntinsity;
    [Header("Texture & UV")]
    public bool IsTexture;
    [Space(4)]
    public Texture2D AlbedoMap;
    public Texture2D NormalMap;
    public float NormalMapIntensity;
    public Vector2 TilingAlbedo = Vector2.one;
    public Vector2 OffsetAlbedo = Vector2.one;
    public Vector2 TilingNormal = Vector2.one;
    public Vector2 OffsetNormal = Vector2.one;
    [Header("Reflaction")]
    //public float RefracteVulue;
    [Range(0f,1f)]
    public float Smoothness;
    [Range(0f, 1f)]
    public float Roughness;
    public bool ClearCoat;
    [Header("Glass & Refraction")]
    [Range(0f, 1f)]
    public float GlassIntinsty = 0.0f;
    [Range(0f, 3.5f)]
    public float RefracteIndex = 1.5f;
    [Range(0f, 1f)]
    public float RefracteIndexAir = 1.0f;
    [Range(0f, 1f)]
    public float RefracteBlurry = 0f;
    [Header("Light Setting")]
    public bool IsLight;
    public float LightIntensity;       
    [Header("Fresnal")]
    public bool Fresnal;
    public Color Color_1 = Color.white;
    public Color Color_2 = Color.white;
    [Range(0f, 0.1f)]
    public float FresnalEffactWight;
    private void Start()
    {

        if (AutoSetVulues == true)
        {
            Color = transform.GetComponent<SkinnedMeshRenderer>().material.color;
            AlbedoMap = transform.GetComponent<SkinnedMeshRenderer>().material.mainTexture as Texture2D;
            if (transform.GetComponent<SkinnedMeshRenderer>().material.GetInteger("Emission") == 1)
            {
                EmissiveIntinsity = 5;
            }
        }       
    }
   
}
