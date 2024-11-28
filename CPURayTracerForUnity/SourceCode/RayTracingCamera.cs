//Simple CPU RayTracer for Unity 
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor.UI;
using UnityEngine.Rendering.Universal;



public struct ScreenRaysInfo
{
    public bool IsHit;    
    public Vector3 HitPoint;
    public Vector3 HitNormal;    
}

public struct PixelData
{
    public int RaysShadowSamplesProccesed;
    public int PixelsRaysHitLight;    
    public int RayTracingGiSamples;    
}

public struct RT_Mat_Info
{
    public Color color;
    public bool IsLight;
    public float LightIntensity;
}

public enum RaysMothod
{
    RayCast,
    LineCast,
}

public enum DebugMode
{
    None,
    Shadow,
    Global_Illumination,
    Reflaction,
    Refraction
}



public class RayTracingCamera : MonoBehaviour
{
    [Header("Rendering Info")]
    public int AccumulatedFrames = 0;
    public float TimeToRenderLastFrame;    
    [Header("Rendering Setting")]
    public bool RayTracing = false;
    public Vector2Int ScreenSize = new Vector2Int(200,100);//SimpleSize
    public RawImage DisplayIn_RawImage;//RawImage Well Display the resulte in
    public bool DisableScript;        
    public bool UpdateFrame;
    public bool RecalculateCamRaysEveryFrame;//Procces Camera Rays every frame
    //Buffer
    Color32[] PixelsColor;
    Color32[] PixelsColor_Shadow;
    private Vector3[] SSRayDir;    
    Color32[] AlbedoColor_Buffer;
    PixelData[] PixelData;
    Color32[] PixelsColor_Global_Illumination;
    RayTracingMaterial[] PointsLightInfo;      
    Color32[] EmissivePixels;
    private Texture2D DisplayTexture;
    private Texture2D FilterTexture;
    Color32[] LastFrame = null;
    private NativeArray<Ray> RaysCam;
    private NativeArray<RaycastCommand> RaysCamInfoCommend;    
    private NativeArray<Ray> RaysHitLight;
    private NativeArray<RaycastCommand> RayHitPixelsInfoJob;
    private JobHandle raycastLightJobHandle;
    //ChackAngleIfWellHitToskip
    private NativeArray<Vector3> NormalsVectors;
    private NativeArray<Vector3> LightsDirection;
    private NativeArray<bool> Resulte_If_Weel_Hit;
    private JobHandle ChackLightAngleToSkipJobHandle;
    public bool StaticScene = false;    
    RaycastHit[] RayHitPixelInfo;
    ScreenRaysInfo[] ScreenSpaceRaysInfo;
    //ShadowInfo
    [Header("Random Genrator Setting")]
    public bool useSeed;
   
    [Header("Ray Tracing Shadow")]
    public bool RayTracingShadow = true;    
    public bool RealTimePreviewShadow;    
    public bool ShadowMothod;
    public int RaysCasted;
    public int RaysHitedLight;            
    [Header("Ray Tracing Global ilomimtion")]
    public bool Global_ilomimtion;
    public bool BounceTolight;
    public float MultyVulue = 1.5f;
    public float Intinsity_Gi = 1;    
    public int MaxRandomGiVulue = 0;            
    private NativeArray<Ray> RaysGi;
    private NativeArray<RaycastCommand> RaysGiInfoCommend;
    
    
    //DenoiserJob
    private NativeArray<int> CurrentFrameCopy;
    private NativeArray<Color32> PixelsColorCopy;
    private NativeArray<Color32> LastFrameCopy;
    private JobHandle DenoiserJobHandle;    
    GameObject[] Objects;
    Vector3[] PointsLight;    
    int RealVerNem = 0;
    private float FixRayNormalVector = 0.0001f;
    int[] All_Hit_Object_ID;
    int[] All_Objects_ID;
    RayTracingMaterial[] ShadowRayHit_RT_Mat;
    
    [Header("Ray Tracing Reflaction & Refraction")]
    public bool RayTracingReflaction;
    public bool RenderShadow;
    public bool RenderGi;    
    Color32[] PixelsColor_Reflaction;    

    [Header("Depth Of Feild")]
    [Range(0f,.6f)]
    public float BluringIntinsity = 0.0f;
    public float FocusDistance = 10.0f;
    public bool FocusCunter;    
    public float BrightnessVulue = 1.2f;

    [Header("Denoiser Setting")]
    private bool Denoiser = true;        
    public bool Refrash;
   
    [Header("Sky Setting & Environment")]
    public Texture2D SkyTexture;
    public bool Gi_Effict_With_Sky_Texture = true;
    public Color SkyColor = Color.black;
    
    [Header("Debug Mode")]
    public DebugMode Debug_Mode;
    [Header("Other...")]    
    public bool RefrashLight;    
    public float RandomSamplesDir = 0;    
    Vector3[] PointsVer;
    RayTracingMaterial[] PointsInfo;
    public bool OptimazeBySkipTexture;
    //LightScatterSetting
    public Vector2 SetMinMax = new Vector2(0.0f, 1.0f);
    public float ScatterLightIntensity = 0.003f;
    public float ScatterLightIntensityColor = 0.003f;




    private void Start()
    {
        
    }
    //
    [System.Obsolete]
    void Update()
    {
        if (DisableScript == false)
        {
            //Rendering Info
            TimeToRenderLastFrame = ((Time.deltaTime * 1000f));
            int PixelsCount = ((int)(ScreenSize.x * ScreenSize.y));
            if (PixelsColor == null)
            {
                PixelsColor = new Color32[PixelsCount];

            }

            if (Input.GetKeyDown(KeyCode.RightAlt))
            {
                RaycastHit hit;
                Vector3 Pointmouse = Input.mousePosition;
                Ray rayFocus = Camera.main.ScreenPointToRay(Pointmouse);
                bool ishit;
                if (FocusCunter)
                {
                    ishit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 1000);
                }
                else
                {
                    ishit = Physics.Raycast(rayFocus, out hit, 1000);
                }
                if (ishit == true)
                {
                    FocusDistance = hit.distance;
                }
            }
            if (useSeed == true)
            {
                Random.InitState(AccumulatedFrames);
            }
            else
            {
                Random.InitState(AccumulatedFrames + ((int)(Time.frameCount)));
            }
            if (Refrash == true)
            {
                PixelsColor = new Color32[(int)(ScreenSize.x * ScreenSize.y)];

            }
            EmissivePixels = new Color32[PixelsColor.Length];

            if (Refrash == true)
            {
                PixelsColor_Shadow = new Color32[PixelsColor.Length];
                PixelsColor_Global_Illumination = new Color32[PixelsColor.Length];
                PixelsColor_Reflaction = new Color32[PixelsColor.Length];
            }


            if (UpdateFrame == true)
            {
                Refrash = true;
                StaticScene = false;
                UpdateFrame = false;
            }


            //
            if (Refrash == true)
            {
                LastFrame = new Color32[((int)(ScreenSize.x * ScreenSize.y))];
                AccumulatedFrames++;
                
            }

            if (!FocusCunter)
            {
                if (Input.GetMouseButtonDown(4))
                {
                    RaycastHit hit;
                    Vector3 Pointmouse = Input.mousePosition;
                    Ray rayFacus = Camera.main.ScreenPointToRay(Pointmouse);
                    bool ishit;

                    ishit = Physics.Raycast(rayFacus, out hit);

                    if (ishit == true)
                    {
                        FocusDistance = hit.distance;
                    }
                }
            }
            else
            {
                RaycastHit hit;
                Vector3 Pointmouse = Input.mousePosition;
                Ray rayFacus = Camera.main.ScreenPointToRay(Pointmouse);
                bool ishit;

                ishit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 1000);

                if (ishit == true)
                {
                    FocusDistance = hit.distance;
                }
            }


            if (RayTracing == true || RecalculateCamRaysEveryFrame == true)
            {
                AccumulatedFrames++;

                if (Refrash == true)
                {

                    DisplayTexture = new Texture2D(ScreenSize.x, ScreenSize.y);
                    DisplayTexture.filterMode = FilterMode.Point;

                    FilterTexture = new Texture2D(((int)ScreenSize.x), ((int)ScreenSize.y));
                    FilterTexture.filterMode = FilterMode.Point;

                    DisplayIn_RawImage.texture = FilterTexture;

                    RayHitPixelInfo = new RaycastHit[(int)(ScreenSize.x * ScreenSize.y)];

                }

                RaysCasted = 0;
                RaysHitedLight = 0;
                //ProccesMineData

                //
                //
                //
                LastFrame = DisplayTexture.GetPixels32();
                if (Refrash == true)
                {
                    ScreenSpaceRaysInfo = new ScreenRaysInfo[((int)(ScreenSize.x * ScreenSize.y))];

                    AlbedoColor_Buffer = new Color32[PixelsColor.Length];



                }

                if (Refrash == true || RefrashLight == true)
                {

                    //LastFrame = PixelsColor;
                    Objects = GameObject.FindGameObjectsWithTag("Object");
                    //InterPointLightPos();
                    //GetLightsPoint
                    PointsVer = new Vector3[10000];
                    PointsInfo = new RayTracingMaterial[10000];
                    RealVerNem = 0;
                    for (int i = 0; i < Objects.Length; i++)
                    {
                        //

                        if (Objects[i].transform.GetComponent<RayTracingMaterial>().IsLight)
                        {

                            int MeshVer = MeshVertecesCounter(Objects[i]);

                            for (int j = 0; j < MeshVer; j++)
                            {

                                PointsVer[j + RealVerNem] = GetLightPoints(j, Objects[i], false);
                                RealVerNem++;
                                PointsInfo[j + RealVerNem] = Objects[i].transform.GetComponent<RayTracingMaterial>();

                            }

                        }
                    }

                    PointsLight = new Vector3[RealVerNem];
                    PointsLightInfo = new RayTracingMaterial[RealVerNem];
                    int ReCurrectRays = 0;
                    int ReCurrectRaysInfo = 0;
                    for (int i = 0; i < PointsVer.Length; i++)
                    {
                        if (PointsVer[i] != new Vector3(0, 0, 0))
                        {
                            ReCurrectRays++;
                            PointsLight[ReCurrectRays - 1] = PointsVer[i];
                        }
                        if (PointsInfo[i] != null)
                        {
                            ReCurrectRaysInfo++;
                            PointsLightInfo[ReCurrectRaysInfo - 1] = PointsInfo[i];
                        }
                    }

                }

                if (Refrash == true)
                {
                    All_Hit_Object_ID = new int[PixelsCount];
                    All_Objects_ID = new int[Objects.Length];
                    ShadowRayHit_RT_Mat = new RayTracingMaterial[PixelsCount];

                    //GetObjects_ID
                    for (int i = 0; i < Objects.Length; i++)
                    {
                        //AllOBJID[i] = AllOBJ[i].transform.GetInstanceID();
                        All_Objects_ID[i] = Objects[i].GetComponent<Collider>().GetInstanceID();
                    }
                    //Create Pixels Data <--------

                    PixelData = new PixelData[PixelsCount];

                    SSRayDir = new Vector3[PixelsCount];
                }

                if (StaticScene == false || RecalculateCamRaysEveryFrame == true)
                {
                    //

                    PixelsColor = new Color32[((int)PixelsCount)];

                    RayHitPixelInfo = new RaycastHit[(int)PixelsCount];

                    Camera cam = Camera.main;
                    Transform camT = cam.transform;


                    RaysCam = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                    RaysCamInfoCommend = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);

                    Vector3[] PixelsRayDir = RaysCamDirection(out Vector3[] RaysComeFrom);
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        RaysCamInfoCommend[i] = new RaycastCommand(camT.position + RaysComeFrom[i], PixelsRayDir[i], 200);

                    }
                    CameraRays_Job CamRays_Job = new CameraRays_Job()
                    {
                        RaysCam = RaysCam,
                        RaysCamCommend = RaysCamInfoCommend,
                    };

                    JobHandle CamRays_Handle_Jon = new JobHandle();
                    CamRays_Handle_Jon = CamRays_Job.Schedule(RayHitPixelsInfoJob.Length, 64);
                    CamRays_Handle_Jon.Complete();



                    for (int x = 0; x < ScreenSize.x; x++)
                    {
                        for (int y = 0; y < ScreenSize.y; y++)
                        {

                            int X_Fix = Screen.width / ScreenSize.x;
                            int Y_Fix = Screen.height / ScreenSize.y;
                            int indexVulue = ColcolateIndex(x, y);

                            Ray ray;

                            Vector3 RandomDirectionVulues = Random.onUnitSphere * RandomSamplesDir;
                            ray = cam.ScreenPointToRay(new Vector3(x * X_Fix + RandomDirectionVulues.x, y * Y_Fix + RandomDirectionVulues.y, 0));


                            ray = RaysCam[indexVulue];
    



                            bool IsHit;

                            IsHit = Physics.Raycast(ray, out RayHitPixelInfo[indexVulue]);

                            SSRayDir[indexVulue] = ray.direction;
                            //Debug.DrawRay(transform.position, ray.direction, Color.blue);
                            if (IsHit == true)
                            {

                                ScreenSpaceRaysInfo[indexVulue].IsHit = IsHit;
                                if (IsHit == true)
                                {


                                    RaycastHit hit;

                                    hit = RayHitPixelInfo[indexVulue];
                                    Vector2 hitUV = RayHitPixelInfo[indexVulue].textureCoord;

                                    ScreenSpaceRaysInfo[indexVulue].HitPoint = hit.point;
                                    if (hit.transform.GetComponent<RayTracingMaterial>().IsTexture == true)
                                    {
                                        Vector2 UV = hitUV;
                                        if (hit.transform.GetComponent<RayTracingMaterial>().AlbedoMap != null)
                                        {
                                            Texture2D BaseMap = hit.transform.GetComponent<RayTracingMaterial>().AlbedoMap;
                                            Vector2 tiling = hit.transform.GetComponent<RayTracingMaterial>().TilingAlbedo;
                                            Vector2 offset = hit.transform.GetComponent<RayTracingMaterial>().OffsetAlbedo;
                                            Vector2 adjustedUV = (UV * tiling) + offset;

                                            // Calculate pixel coordinates
                                            int UV_X = Mathf.RoundToInt(adjustedUV.x * BaseMap.width);
                                            int UV_Y = Mathf.RoundToInt(adjustedUV.y * BaseMap.height);

                                            // Ensure UV_X and UV_Y are within valid bounds
                                            //UV_X = Mathf.Clamp(UV_X, 0, BaseMap.width - 1);
                                            //UV_Y = Mathf.Clamp(UV_Y, 0, BaseMap.height - 1);
                                            Color BaseTextureColor = BaseMap.GetPixel(UV_X, UV_Y);
                                            PixelsColor[indexVulue] = BaseTextureColor;
                                            AlbedoColor_Buffer[indexVulue] = BaseTextureColor;
                                        }
                                        else
                                        {
                                            if (Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Fresnal == true)
                                            {
                                                PixelsColor[indexVulue] = FresnalColor(RayHitPixelInfo[indexVulue], ray);
                                                AlbedoColor_Buffer[indexVulue] = FresnalColor(RayHitPixelInfo[indexVulue], ray);
                                            }
                                            else
                                            {
                                                PixelsColor[indexVulue] = Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Color;
                                                AlbedoColor_Buffer[indexVulue] = Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Color;
                                            }
                                        }
                                        //NormalMap

                                        ScreenSpaceRaysInfo[indexVulue].HitNormal = hit.normal;
                                        
                                    }
                                    else
                                    {
                                        if (RayHitPixelInfo[indexVulue].transform != null)
                                        {
                                            if (Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Fresnal == true)
                                            {
                                                PixelsColor[indexVulue] = FresnalColor(RayHitPixelInfo[indexVulue], ray);
                                                AlbedoColor_Buffer[indexVulue] = FresnalColor(RayHitPixelInfo[indexVulue], ray);
                                            }
                                            else
                                            {
                                                PixelsColor[indexVulue] = Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Color;
                                                AlbedoColor_Buffer[indexVulue] = Find_OBJ_Hit_By_ID(ray).GetComponent<RayTracingMaterial>().Color;
                                            }
                                            ScreenSpaceRaysInfo[indexVulue].HitNormal = hit.normal;
                                        }
                                    }
                                }

                            }
                            else
                            {
                                PixelsColor[indexVulue] = SkyRayDir(SSRayDir[indexVulue], SkyTexture);
                                AlbedoColor_Buffer[indexVulue] = SkyRayDir(SSRayDir[indexVulue], SkyTexture);
                                RayHitPixelInfo[indexVulue].point = new Vector3(0, 0, 0);
                            }
                        }
                    }
                    if (RaysCam.IsCreated == true)
                    {
                        RaysCam.Dispose();
                        RaysCamInfoCommend.Dispose();
                    }

                }

                if (RayTracingShadow == true)
                {
                    if (StaticScene == true)
                    {
                        RayTracingMaterial RT_Mat_Black = new RayTracingMaterial();
                        RT_Mat_Black.IsLight = false;

                        for (uint i = 0; i < PixelsCount; i++)
                        {
                            PixelsColor[i] = AlbedoColor_Buffer[i];
                        }


                        RayHitPixelsInfoJob = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);
                        RaysHitLight = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                        bool[] RaysWellHit = new bool[PixelsCount];

                        //rayCasting
                        Vector3[] LightDir = new Vector3[PixelsCount];
                        int[] RandomHitPoint = new int[PixelsCount];
                        for (int i = 0; i < PixelsCount; i++)
                        {
                            RandomHitPoint[i] = Random.Range(0, PointsLight.Length);
                            //Debug.Log(RandomHitPoint);
                            LightDir[i] = (PointsLight[RandomHitPoint[i]] - RayHitPixelInfo[i].point).normalized;
                        }
                        //SetDataToSkiping(UsingAngle)
                        NormalsVectors = new NativeArray<Vector3>(PixelsCount, Allocator.TempJob);
                        LightsDirection = new NativeArray<Vector3>(PixelsCount, Allocator.TempJob);
                        Resulte_If_Weel_Hit = new NativeArray<bool>(PixelsCount, Allocator.TempJob);

                        for (int i = 0; i < PixelsCount; i++)
                        {
                            NormalsVectors[i] = ScreenSpaceRaysInfo[i].HitNormal;
                            LightsDirection[i] = LightDir[i];
                        }

                        ChackLightAngleToSkipJob chackLightAngleToSkipJob = new ChackLightAngleToSkipJob
                        {
                            NormalsVectors = NormalsVectors,
                            LightsDirection = LightsDirection,
                            Resulte_If_Weel_Hit = Resulte_If_Weel_Hit,
                        };

                        ChackLightAngleToSkipJobHandle = chackLightAngleToSkipJob.Schedule(PixelsCount, 64);
                        ChackLightAngleToSkipJobHandle.Complete();

                        for (int i = 0; i < PixelsCount; i++)
                        {
                            RaysWellHit[i] = Resulte_If_Weel_Hit[i];
                            if (RayHitPixelInfo[i].point == new Vector3(0, 0, 0) || RaysWellHit[i] == false)
                            {
                                RayHitPixelsInfoJob[i] = new RaycastCommand(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0);
                            }
                            else
                            {
                                RayHitPixelsInfoJob[i] = new RaycastCommand(ScreenSpaceRaysInfo[i].HitPoint + (ScreenSpaceRaysInfo[i].HitNormal * FixRayNormalVector), LightDir[i], 1000);
                                PixelData[i].RaysShadowSamplesProccesed++;
                            }
                        }
                        
                        //End Job <-----
                        NormalsVectors.Dispose();
                        LightsDirection.Dispose();
                        Resulte_If_Weel_Hit.Dispose();
                        //
                        RayCastingJob rayCastingJob = new RayCastingJob
                        {
                            RaysHitLightJob = RaysHitLight,
                            PixelsHitPos = RayHitPixelsInfoJob,
                        };

                        raycastLightJobHandle = rayCastingJob.Schedule(RayHitPixelsInfoJob.Length, 64);
                        raycastLightJobHandle.Complete();

                        RaycastHit hit;
                        for (int i = 0; i < PixelsCount; i++)
                        {

                            if (ScreenSpaceRaysInfo[i].IsHit == true && RaysHitLight[i].origin != Vector3.zero || RaysWellHit[i] == true)
                            {
                                if (ScreenSpaceRaysInfo[i].IsHit == true)
                                {

                                    if (RaysHitLight[i].origin != Vector3.zero)
                                    {
                                        if (PointsLightInfo[RandomHitPoint[i]].NoColider == true && PointsLightInfo[RandomHitPoint[i]].IsLight == true)
                                        {
                                            
                                            bool ishit = Physics.Raycast(RaysHitLight[i], out hit);
                                            if (ishit == false)
                                            {
                                                ShadowRayHit_RT_Mat[i] = PointsLightInfo[RandomHitPoint[i]];
                                            }
                                            else
                                            {
                                                ShadowRayHit_RT_Mat[i] = RT_Mat_Black;
                                            }
                                        }
                                        else
                                        {
                                            ShadowRayHit_RT_Mat[i] = Find_OBJ_Hit_By_ID(RaysHitLight[i]).GetComponent<RayTracingMaterial>();
                                        }

                                    }
                                    else
                                    {
                                        ShadowRayHit_RT_Mat[i] = PointsLightInfo[RandomHitPoint[i]];
                                    }

                                }

                            }
                            else
                            {
                                ShadowRayHit_RT_Mat[i] = RT_Mat_Black;
                            }
                        }

                        if (RayTracingShadow)
                        {
                            for (int x = 0; x < ScreenSize.x; x++)
                            {
                                for (int y = 0; y < ScreenSize.y; y++)
                                {
                                    int indexVulue = ColcolateIndex((int)x, (int)y);


                                    if (ScreenSpaceRaysInfo[indexVulue].HitPoint != new Vector3(0, 0, 0))
                                    {

                                        if (ShadowRayHit_RT_Mat[indexVulue] != null)
                                        {
                                            RayTracingMaterial rayTracingMaterialPointLight = PointsLightInfo[RandomHitPoint[indexVulue]];

                                            if (rayTracingMaterialPointLight.NoColider == false)
                                            {
                                                RayTracingMaterial ShadowRayInfo_RT_Mat = ShadowRayHit_RT_Mat[indexVulue];
                                                if (ShadowRayInfo_RT_Mat.IsLight == true)
                                                {
                                                    Vector3 PointRayHit = ScreenSpaceRaysInfo[indexVulue].HitPoint;
                                                    Vector3 NormalVector = RayHitPixelInfo[indexVulue].normal;
                                                    RaysHitedLight++;
                                                    float DirecteLightInSerfaceVulue = Vector3.Angle(NormalVector, LightDir[indexVulue]);
                                                    float CulDis = Vector3.Distance(PointRayHit, PointsLight[RandomHitPoint[indexVulue]]);
                                                    //Color DirectScatterVulue = Color.Lerp(PixelsColor_Shadow[indexVulue], Color.black, DirecteLightInSerfaceVulue * ScatterLightIntensity * ((CulDis * 2f) / rayTracingMaterialPointLight.LightIntensity));
                                                    Color DirectScatterVulue = Color.Lerp(AlbedoColor_Buffer[indexVulue], Color.black, DirecteLightInSerfaceVulue * ScatterLightIntensity * ((CulDis * 2f) / rayTracingMaterialPointLight.LightIntensity));
                                                    float BrightVulue = Mathf.Lerp(0f, 1f, rayTracingMaterialPointLight.LightIntensity / CulDis);
                                                    float SetMin_MaxVulueToScatterColorLight = Mathf.Lerp(SetMinMax.x, SetMinMax.y, DirecteLightInSerfaceVulue * ScatterLightIntensityColor * ((CulDis * 2f) / rayTracingMaterialPointLight.LightIntensity));
                                                    Color ScatterColorLightinObj = Color.Lerp(rayTracingMaterialPointLight.Color, DirectScatterVulue, SetMin_MaxVulueToScatterColorLight);
                                                    PixelsColor_Shadow[indexVulue] = Color.Lerp(Color.black, ScatterColorLightinObj, BrightVulue);
                                                   
                                                    PixelData[indexVulue].PixelsRaysHitLight++;

                                                }
                                               
                                            }
                                            else
                                            {
                                                RayTracingMaterial ShadowRayInfo_RT_Mat = ShadowRayHit_RT_Mat[indexVulue];

                                                if (ShadowRayInfo_RT_Mat.Color != Color.black)
                                                {
                                                    Vector3 PointRayHit = ScreenSpaceRaysInfo[indexVulue].HitPoint;
                                                    Vector3 NormalVector = RayHitPixelInfo[indexVulue].normal;
                                                    RaysHitedLight++;
                                                    float DirecteLightInSerfaceVulue = Vector3.Angle(NormalVector, LightDir[indexVulue]);
                                                    float CulDis = Vector3.Distance(PointRayHit, PointsLight[RandomHitPoint[indexVulue]]);
                                                    Color DirectScatterVulue = Color.Lerp(PixelsColor_Shadow[indexVulue], Color.black, DirecteLightInSerfaceVulue * ScatterLightIntensity * ((CulDis * 2f) / rayTracingMaterialPointLight.LightIntensity));
                                                    float BrightVulue = Mathf.Lerp(0f, 1f, rayTracingMaterialPointLight.LightIntensity / CulDis);
                                                    float SetMin_MaxVulueToScatterColorLight = Mathf.LerpUnclamped(SetMinMax.x, SetMinMax.y, DirecteLightInSerfaceVulue * ScatterLightIntensityColor * ((CulDis * 2f) / rayTracingMaterialPointLight.LightIntensity));
                                                    Color ScatterColorLightinObj = Color.Lerp(rayTracingMaterialPointLight.Color, DirectScatterVulue, SetMin_MaxVulueToScatterColorLight);
                                                    PixelsColor_Shadow[indexVulue] = AlbedoColor_Buffer[indexVulue];
                                                    //
                                                }
                                                else
                                                {
                                                    PixelsColor[indexVulue] = Color.black;
                                                }


                                            }
                                        }
                                        else
                                        {
                                            PixelsColor[indexVulue] = Color.black;

                                        }
                                    }

                                }
                            }
                            RaysHitLight.Dispose();
                            RayHitPixelsInfoJob.Dispose();

                        }
                    }
                }
                if (RayTracingShadow == true)
                {

                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (RayHitPixelInfo[i].transform != null)
                        {
                            float DividedVulueForShadow;
                            if (PixelData[i].RaysShadowSamplesProccesed == 0)
                            {
                                DividedVulueForShadow = 1;
                            }
                            else
                            {
                                DividedVulueForShadow = PixelData[i].RaysShadowSamplesProccesed * 1f;
                            }

                            if (PixelData[i].PixelsRaysHitLight == PixelData[i].RaysShadowSamplesProccesed)
                            {
                            }
                            else
                            {
                                PixelsColor_Shadow[i] = Color32.Lerp(Color.black, PixelsColor_Shadow[i], PixelData[i].PixelsRaysHitLight / DividedVulueForShadow);
                            }
                        }
                    }

                }


                if (Global_ilomimtion == true)
                {
                    //Global Illumntion
                    RaysGi = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                    RaysGiInfoCommend = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);



                    bool[] PixelsWellRender = new bool[PixelsColor.Length];

                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        int Ran = Random.Range(0, MaxRandomGiVulue);
                        if (RayHitPixelInfo[i].transform != null)
                        {
                            if (ScreenSpaceRaysInfo[i].IsHit == true && Ran == 0 && RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness < 1f && RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty < 1f)
                            {
                                PixelsWellRender[i] = true;
                            }
                        }
                        else
                        {
                            PixelsWellRender[i] = false;
                        }

                    }

                    Vector3[] RandomSamplingGiDirection = new Vector3[PixelsColor.Length];
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].IsHit == true && PixelsWellRender[i] == true)
                        {
                            RandomSamplingGiDirection[i] = GenerateDirection(ScreenSpaceRaysInfo[i].HitNormal, 0.5f);
                        }
                    }
                    //SetJobToComplte
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].HitPoint != Vector3.zero && ScreenSpaceRaysInfo[i].IsHit == true && PixelsWellRender[i] == true)
                        {

                            RaysGiInfoCommend[i] = new RaycastCommand(ScreenSpaceRaysInfo[i].HitPoint + (ScreenSpaceRaysInfo[i].HitNormal * FixRayNormalVector), RandomSamplingGiDirection[i], 1000);
                        }
                        else
                        {
                            RaysGiInfoCommend[i] = new RaycastCommand(Vector3.zero, Vector3.zero, 1000);
                        }
                    }

                    Global_Illumination_ReSTIR_Job Gi_Job = new Global_Illumination_ReSTIR_Job
                    {
                        RaysGi = RaysGi,
                        RaysGiCommend = RaysGiInfoCommend,
                    };
                    JobHandle Gi_Handle_Jon = new JobHandle();
                    Gi_Handle_Jon = Gi_Job.Schedule(RayHitPixelsInfoJob.Length, 64);
                    Gi_Handle_Jon.Complete();


                    //Bounce to Light----------->
                    NativeArray<Ray> RaysBounceGi = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                    NativeArray<RaycastCommand> RaysBounceGiInfoCommend = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);
                    //
                    int RandomSampleslight = Random.Range(0, PointsLight.Length);

                    RaycastHit[] hits = new RaycastHit[PixelsCount];
                    for (int i = 0; i < PixelsCount; i++)
                    {
                        bool isHits = Physics.Raycast(RaysGi[i], out hits[i]);
                    }

                    if (BounceTolight == true)
                    {
                        //GetFirestRandomBounceSampling
                        Vector3 PointLightDirCalculate;
                        Vector3 Bounce2Dir;
                        for (int i = 0; i < PixelsColor.Length; i++)
                        {
                            if (ScreenSpaceRaysInfo[i].IsHit == true && PixelsWellRender[i] == true)
                            {

                                PointLightDirCalculate = PointsLight[RandomSampleslight] - hits[i].point;

                                if (hits[i].transform != null)
                                {
                                    if (hits[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty == 0f)
                                    {
                                        Bounce2Dir = PointLightDirCalculate.normalized;
                                        RaysBounceGiInfoCommend[i] = new RaycastCommand(hits[i].point + (hits[i].normal * FixRayNormalVector), Bounce2Dir, 1000);
                                    }
                                    else
                                    {
                                        Bounce2Dir = CalculateRefraction(RaysGi[i].direction, hits[i].normal, hits[i].transform.GetComponent<RayTracingMaterial>().RefracteIndexAir, hits[i].transform.GetComponent<RayTracingMaterial>().RefracteIndex);
                                        RaysBounceGiInfoCommend[i] = new RaycastCommand(hits[i].point + (hits[i].normal * -0.01f), Bounce2Dir, 1000);
                                    }
                                    if (hits[i].transform.GetComponent<RayTracingMaterial>().Smoothness != 0f)
                                    {
                                        Bounce2Dir = Vector3.Reflect(RaysGi[i].direction, hits[i].normal);
                                        RaysBounceGiInfoCommend[i] = new RaycastCommand(hits[i].point + (hits[i].normal * 0.01f), Bounce2Dir, 1000);
                                    }
                                }
                                else
                                {
                                    Bounce2Dir = PointLightDirCalculate.normalized;

                                }

                            }
                            else
                            {
                                RaysBounceGiInfoCommend[i] = new RaycastCommand(Vector3.zero, Vector3.zero, 1000);
                            }
                        }
                        //
                        Global_Illumination_ReSTIR_Job GiBounce_Job = new Global_Illumination_ReSTIR_Job
                        {
                            RaysGi = RaysBounceGi,
                            RaysGiCommend = RaysBounceGiInfoCommend,
                        };
                        JobHandle GiBounce_Handle_Jon = new JobHandle();
                        GiBounce_Handle_Jon = GiBounce_Job.Schedule(RayHitPixelsInfoJob.Length, 64);
                        GiBounce_Handle_Jon.Complete();
                    }
                    //ProccingRaysResulte--------------------------------------------------------------------------------
                    RaycastHit hit;
                    RaycastHit hitBounce;
                    bool HitResulteBounce = false;
                    bool RealHitResulteBounce;
                    RayTracingMaterial RT_Mat_BounceObjectHited;
                    Color32 PointLightColor;
                    Color32 GiResulte;
                    Color SkyRefracteCol;
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].IsHit == true && PixelsWellRender[i] == true)
                        {

                            bool HitResulte = Physics.Raycast(RaysGi[i], out hit);
                            //Bounce
                            hitBounce = hit;
                            HitResulteBounce = false;
                            RealHitResulteBounce = HitResulte;
                            if (BounceTolight == true)
                            {
                                HitResulteBounce = Physics.Raycast(RaysBounceGi[i], out hitBounce);
                            }
                            if (HitResulteBounce == true)
                            {
                                RT_Mat_BounceObjectHited = Find_OBJ_Hit_By_ID(RaysBounceGi[i]).GetComponent<RayTracingMaterial>();
                                if (RT_Mat_BounceObjectHited.IsLight == true)
                                {
                                    HitResulteBounce = true;
                                }
                                else
                                {
                                    HitResulteBounce = false;
                                }
                            }

                            if (BounceTolight == true)
                            {
                                PointLightColor = PointsLightInfo[RandomSampleslight].Color;
                                GiResulte = ProccesGiRays_Global_Illumination(HitResulte, hit, HitResulteBounce, hitBounce, ScreenSpaceRaysInfo[i].HitPoint, ScreenSpaceRaysInfo[i].HitNormal, PointsLight[RandomSampleslight], PointLightColor, BounceTolight, RaysGi[i].direction);
                                PixelData[i].RayTracingGiSamples++;

                                if (hits[i].transform != null && PixelsWellRender[i])
                                {
                                    if (hits[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty != 0 || hits[i].transform.GetComponent<RayTracingMaterial>().Smoothness != 0)
                                    {
                                        if (hitBounce.transform != null)
                                        {                                            
                                            if (hitBounce.transform.GetComponent<RayTracingMaterial>().EmissiveIntinsity != 0)
                                            {
                                                GiResulte = GetTextureCol(hitBounce, false);                                            
                                            }
                                        }
                                        else
                                        {


                                            SkyRefracteCol = SkyRayDir(RaysBounceGi[i].direction, SkyTexture);
                                           
                                            Color.RGBToHSV(SkyRefracteCol, out float HRef, out float SRef, out float Vref);
                                            GiResulte = Color.LerpUnclamped(Color.black, SkyRefracteCol, Mathf.Pow(Vref, 7.5f));
                                        }
                                    }
                                }
                                if (PixelsWellRender[i])
                                {
                                    PixelsColor_Global_Illumination[i] = Color.LerpUnclamped(GiResulte, PixelsColor_Global_Illumination[i], 1f / (PixelData[i].RayTracingGiSamples + 1f));
                                }
                            }
                            else
                            {
                                if (PixelsWellRender[i])
                                {
                                    PointLightColor = PointsLightInfo[RandomSampleslight].Color;

                                    PixelsColor_Global_Illumination[i] = ProccesGiRays_Global_Illumination(HitResulte, hit, HitResulte, hit, ScreenSpaceRaysInfo[i].HitPoint, ScreenSpaceRaysInfo[i].HitNormal, PointsLight[RandomSampleslight], PointLightColor, BounceTolight, RaysGi[i].direction);
                                }
                            }





                            Color.RGBToHSV(AlbedoColor_Buffer[i], out float H2, out float S2, out float V2);
                            Color.RGBToHSV(PixelsColor_Global_Illumination[i], out float H3, out float S3, out float V3);
                            Color32 PixelMeinColor = Color.HSVToRGB(H2, S2, V3);
                            Color32 Gi_Combine_Objecte_Hited = Color.LerpUnclamped(AlbedoColor_Buffer[i] * new Color(V3, V3, V3), PixelsColor_Global_Illumination[i], 0.5f);
                            PixelsColor_Global_Illumination[i] = Gi_Combine_Objecte_Hited;

                        }
                                                
                    }
                    RaysGi.Dispose();
                    RaysGiInfoCommend.Dispose();
                    RaysBounceGi.Dispose();
                    RaysBounceGiInfoCommend.Dispose();

                }
                //RT Reflactions-------------------------->
                if (RayTracingReflaction == true)
                {
                    NativeArray<Ray> RayReflaction = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                    NativeArray<RaycastCommand> RayCommendReflaction = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);

                    Vector3[] RandomSamplingReflaction = new Vector3[PixelsColor.Length];
                    bool[] PixelWellReflacte = new bool[PixelsColor.Length];

                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (RayHitPixelInfo[i].transform != null)
                        {
                            if (ScreenSpaceRaysInfo[i].IsHit == true && RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness != 0 || (ScreenSpaceRaysInfo[i].IsHit == true && RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty != 0))
                            {
                                PixelWellReflacte[i] = true;
                            }
                        }
                        else
                        {
                            PixelWellReflacte[i] = false;
                        }
                    }

                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].IsHit == true && PixelWellReflacte[i] == true)
                        {
                            if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness != 0)
                            {
                                Vector3 ReflacteDir = GenarateReflacteDir(ScreenSpaceRaysInfo[i].HitNormal, SSRayDir[i]);
                                Vector3 RandomDirectionVulues = Random.onUnitSphere * RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Roughness;
                                RandomSamplingReflaction[i] = ReflacteDir + RandomDirectionVulues;
                            }
                            if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty != 0)
                            {
                                //Vector3 ReflacteDir = GenarateReflacteDir(ScreenSpaceRaysInfo[i].HitNormal, SSRayDir[i]);
                                Vector3 ReflacteDir = CalculateRefraction(SSRayDir[i], ScreenSpaceRaysInfo[i].HitNormal, RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().RefracteIndexAir, RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().RefracteIndex);
                                Vector3 RandomDirectionVulues = Random.onUnitSphere * RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().RefracteBlurry;
                                RandomSamplingReflaction[i] = ReflacteDir + RandomDirectionVulues;
                            }
                        }
                    }
                    //SetJobToComplte
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].HitPoint != Vector3.zero && ScreenSpaceRaysInfo[i].IsHit == true && PixelWellReflacte[i] == true)
                        {
                            if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness != 0)
                            {
                                RayCommendReflaction[i] = new RaycastCommand(ScreenSpaceRaysInfo[i].HitPoint + (ScreenSpaceRaysInfo[i].HitNormal * FixRayNormalVector), RandomSamplingReflaction[i], 1000);
                            }
                            if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty != 0)
                            {
                                RayCommendReflaction[i] = new RaycastCommand(ScreenSpaceRaysInfo[i].HitPoint + (ScreenSpaceRaysInfo[i].HitNormal * -0.01f), RandomSamplingReflaction[i], 1000);
                            }
                        }
                        else
                        {
                            RayCommendReflaction[i] = new RaycastCommand(Vector3.zero, Vector3.zero, 1000);
                        }
                    }

                    Global_Illumination_ReSTIR_Job Reflacte_Job = new Global_Illumination_ReSTIR_Job
                    {
                        RaysGi = RayReflaction,
                        RaysGiCommend = RayCommendReflaction,
                    };
                    JobHandle Reflacte_Handle_Jon = new JobHandle();
                    Reflacte_Handle_Jon = Reflacte_Job.Schedule(RayHitPixelsInfoJob.Length, 64);
                    Reflacte_Handle_Jon.Complete();
                    //

                    //Bounce to Light----------->
                    NativeArray<Ray> RaysReflacteBounceToLight = new NativeArray<Ray>(PixelsCount, Allocator.TempJob);
                    NativeArray<RaycastCommand> RaysReflacteBounceToLightInfoCommend = new NativeArray<RaycastCommand>(PixelsCount, Allocator.TempJob);
                    //-------->
                    //GenrateRandomSampling
                    int[] RandomSampleslight = new int[PixelsColor.Length];
                    for (int i = 0; i < PixelsCount; i++)
                    {
                        RandomSampleslight[i] = Random.Range(0, PointsLight.Length);
                    }
                    if (RenderShadow == true)
                    {
                        
                        
                        //GetFirestRandomBounceSampling
                        RaycastHit[] hits = new RaycastHit[PixelsCount];
                        for (int i = 0; i < PixelsCount; i++)
                        {
                            if (PixelWellReflacte[i] == true)
                            {
                                bool isHits = Physics.Raycast(RayReflaction[i], out hits[i]);
                            }
                        }

                        for (int i = 0; i < PixelsColor.Length; i++)
                        {
                            if (PixelWellReflacte[i] == true)
                            {
                                Vector3 PointLightDirCalculate = PointsLight[RandomSampleslight[i]] - hits[i].point;
                                Vector3 PointLightDir = PointLightDirCalculate.normalized;
                                RaysReflacteBounceToLightInfoCommend[i] = new RaycastCommand(hits[i].point + (hits[i].normal * FixRayNormalVector), PointLightDir, 1000);
                            }
                            else
                            {
                                RaysReflacteBounceToLightInfoCommend[i] = new RaycastCommand(Vector3.zero, Vector3.zero, 1000);
                            }
                        }

                        Global_Illumination_ReSTIR_Job ReflacteBounce_Job = new Global_Illumination_ReSTIR_Job
                        {
                            RaysGi = RaysReflacteBounceToLight,
                            RaysGiCommend = RaysReflacteBounceToLightInfoCommend,
                        };
                        JobHandle GiBounce_Handle_Jon = new JobHandle();
                        GiBounce_Handle_Jon = ReflacteBounce_Job.Schedule(RayHitPixelsInfoJob.Length, 64);
                        GiBounce_Handle_Jon.Complete();
                    }
                    //--------->
                    //ProccesReflacteResulte
                    for (int i = 0; i < PixelsColor.Length; i++)
                    {
                        if (ScreenSpaceRaysInfo[i].IsHit == true && PixelWellReflacte[i] == true)
                        {
                            RaycastHit hit;
                            bool HitResulte = Physics.Raycast(RayReflaction[i], out hit);
                            //Bounce
                            RaycastHit hitBounce = hit;
                            bool HitResulteBounce = false;
                            if (RenderShadow == true && ScreenSpaceRaysInfo[i].IsHit == true && PixelWellReflacte[i] == true)
                            {
                                HitResulteBounce = Physics.Raycast(RaysReflacteBounceToLight[i], out hitBounce);
                            }
                            if (HitResulteBounce == true)
                            {
                                RayTracingMaterial RT_Mat_BounceObjectHited = Find_OBJ_Hit_By_ID(RaysReflacteBounceToLight[i]).GetComponent<RayTracingMaterial>();
                                if (RT_Mat_BounceObjectHited.IsLight == true)
                                {
                                    HitResulteBounce = true;
                                }
                                else
                                {
                                    HitResulteBounce = false;
                                }
                            }

                            if (RenderShadow == true)
                            {
                                Color32 PointLightColor = PointsLightInfo[RandomSampleslight[i]].Color;
                                Color32 GiResulte;
                                Color32 ReflacteProccesCol;
                                if (HitResulte == true)
                                {

                                    if (hitBounce.transform.GetComponent<RayTracingMaterial>().IsLight == true)
                                    {

                                        GiResulte = GetTextureCol(hit, false);
                                        float AngleWithLight = Vector3.Angle(hitBounce.normal, RaysReflacteBounceToLightInfoCommend[i].direction);
                                        float DisRef = Vector3.Distance(hitBounce.point, hit.point);
                                        GiResulte = Color.LerpUnclamped(GiResulte, Color.black, (DisRef / (hitBounce.transform.GetComponent<RayTracingMaterial>().LightIntensity * 3)));
                                        PixelsColor_Reflaction[i] = Color.LerpUnclamped(GiResulte, PixelsColor_Reflaction[i], 1f / (AccumulatedFrames));

                                    }
                                    else
                                    {
                                        PixelsColor_Reflaction[i] = Color.LerpUnclamped(Color.black, PixelsColor_Reflaction[i], 1f / (AccumulatedFrames));
                                    }
                                    if (hit.transform != null)
                                    {
                                        if (hit.transform.GetComponent<RayTracingMaterial>().EmissiveIntinsity != 0)
                                        {

                                            //GiResulte = hit.transform.GetComponent<RayTracingMaterial>().Color;
                                            GiResulte = GetTextureCol(hit, OptimazeBySkipTexture);
                                            //EmissivePixels[i] = Color.Lerp(GiResulte, LastFrame[i],1f / CurrentFrame);  
                                            //
                                            PixelsColor_Reflaction[i] = Color.LerpUnclamped(GiResulte, PixelsColor_Reflaction[i], 1f / (AccumulatedFrames));
                                        }
                                    }

                                    ReflacteProccesCol = PixelsColor_Reflaction[i];
                                }
                                else
                                {
                                    Color32 SkyRayCol;

                                    SkyRayCol = SkyRayDir(RayReflaction[i].direction, SkyTexture);
                                    if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty == 0)
                                    {
                                        ReflacteProccesCol = SkyRayCol;

                                    }
                                    else
                                    {
                                        //ReflacteProccesCol = Color.LerpUnclamped(PixelsColor_Reflaction[i], SkyRayCol, RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty);
                                        ReflacteProccesCol = SkyRayCol;
                                    }
                                    PixelsColor_Reflaction[i] = ReflacteProccesCol;
                                    //PixelsColor_Reflaction[i] = Color.Lerp(ReflacteProccesCol, PixelsColor_Reflaction[i], 1f / (CurrentFrame + 1));
                                }
                               
                            }
                            else
                            {
                                Color32 PointLightColor = PointsLightInfo[RandomSampleslight[i]].Color;
                                PixelsColor_Reflaction[i] = ProccesGiRays_Global_Illumination(HitResulte, hit, HitResulte, hit, ScreenSpaceRaysInfo[i].HitPoint, ScreenSpaceRaysInfo[i].HitNormal, PointsLight[RandomSampleslight[i]], PointLightColor, BounceTolight, Vector3.zero);
                                Color32 ReflacteProccesCol;
                                if (HitResulte == true)
                                {
                                    if (RenderShadow == false)
                                    {
                                        ReflacteProccesCol = Color.LerpUnclamped(PixelsColor_Reflaction[i], PixelsColor_Global_Illumination[i], RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness);
                                    }
                                    else
                                    {
                                        ReflacteProccesCol = Color.LerpUnclamped(PixelsColor_Reflaction[i], PixelsColor_Global_Illumination[i], RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness);
                                    }
                                }
                                else
                                {
                                    Color32 SkyRayCol = SkyRayDir(RayReflaction[i].direction, SkyTexture);
                                    ReflacteProccesCol = Color.LerpUnclamped(PixelsColor[i], SkyRayCol, RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness);
                                }
                            }

                        }
                        else
                        {

                        }
                    }
                    RayReflaction.Dispose();
                    RayCommendReflaction.Dispose();
                    RaysReflacteBounceToLight.Dispose();
                    RaysReflacteBounceToLightInfoCommend.Dispose();

                }



            }


            //FilterEmmisiveLight
            for (int i = 0; i < PixelsColor.Length; i++)
            {
                if (RayHitPixelInfo[i].transform != null)
                {
                    if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().EmissiveIntinsity != 0)
                    {
                        EmissivePixels[i] = Color.LerpUnclamped(Color.black, GetTextureCol(RayHitPixelInfo[i], false), RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().EmissiveIntinsity / 5f);
                        PixelsColor[i] = EmissivePixels[i];
                    }
                }
            }




            Texture2D EmissiveTexture = new Texture2D(DisplayTexture.width, DisplayTexture.height);
            EmissiveTexture.SetPixels32(EmissivePixels);
            EmissiveTexture.Apply();



            //-------------------------> Combine all (Shadow ,global illumination ,Reflaction ,Refraction) To Pixels Color (Arrey)
            Color32 BlackCol32 = Color.black;            
            int DebugMode_int = DebugModeTest(Debug_Mode);
            for (uint i = 0; i < PixelsColor.Length; i++)
            {
                if (DebugMode_int == 0)
                {
                    if (EmissivePixels[i].r == BlackCol32.r && EmissivePixels[i].g == BlackCol32.g && EmissivePixels[i].g == BlackCol32.g)
                    {


                        if (RayHitPixelInfo[i].transform != null)
                        {
                            Color.RGBToHSV(PixelsColor_Shadow[i], out float H, out float S, out float V);
                            Color Color_Lighting;
                            if (Global_ilomimtion)
                            {
                                Color_Lighting = Color.LerpUnclamped(PixelsColor_Global_Illumination[i], PixelsColor_Shadow[i], V * Intinsity_Gi);
                            }
                            else
                            {
                                Color_Lighting = Color.LerpUnclamped(Color.black, PixelsColor_Shadow[i], V);

                            }
                            if (RayTracingReflaction == true)
                            {
                                Color32 ReflactionAndRefraction;
                                if (RayHitPixelInfo[i].transform != null)
                                {

                                    //ReflactionAndRefraction = Color.LerpUnclamped(PixelsColor_Global_Illumination[i], PixelsColor[i], V * Intinsity_Gi);
                                    ReflactionAndRefraction = PixelsColor_Reflaction[i];
                                    if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty == 0)
                                    {
                                        if (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().ClearCoat)
                                        {
                                            Color.RGBToHSV(ReflactionAndRefraction, out float H5, out float S5, out float V5);
                                            float ClearCoatFix = Mathf.Clamp(Mathf.Abs(10f - (RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness * 10f)), 1f, 10f);
                                            Color ColorClearCoatReflacte = Color.HSVToRGB(H5, S5, Mathf.Pow(V5, ClearCoatFix));
                                            ReflactionAndRefraction = ColorClearCoatReflacte;
                                            ReflactionAndRefraction = Color.LerpUnclamped(ColorClearCoatReflacte, Color_Lighting, 1f - Mathf.Pow(V5, ClearCoatFix));
                                        }
                                        else
                                        {
                                            ReflactionAndRefraction = Color.Lerp(PixelsColor_Reflaction[i], Color_Lighting, 1f - RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Smoothness);
                                        }

                                    }
                                    else
                                    {

                                        ReflactionAndRefraction = Color.Lerp(PixelsColor_Reflaction[i], Color_Lighting, 1f - RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().GlassIntinsty);
                                    }
                                    PixelsColor[i] = ReflactionAndRefraction;
                                }

                            }
                            else
                            {
                                PixelsColor[i] = Color_Lighting;
                            }
                        }
                        if (!RayTracingShadow && !RayTracingReflaction && !Global_ilomimtion)
                        {
                            PixelsColor[i] = RayHitPixelInfo[i].transform.GetComponent<RayTracingMaterial>().Color;
                        }
                    }

                }
                else
                {
                    PixelsColor[i] = EmissivePixels[i];
                }
                if (DebugMode_int == 1)
                {

                    PixelsColor[i] = PixelsColor_Shadow[i];
                }
                if (DebugMode_int == 2)
                {

                    PixelsColor[i] = PixelsColor_Global_Illumination[i];
                }
                if (DebugMode_int == 3)
                {

                    PixelsColor[i] = PixelsColor_Reflaction[i];
                }



            }

            Color32[] PostProccesImage = null;

            //DenoiserJobSetData <--------------------------------------------------------------------------------------------
            if (Denoiser == true)
            {
                if (StaticScene == true)
                {                    
                    PixelsColorCopy = new NativeArray<Color32>(PixelsCount, Allocator.TempJob);
                    NativeArray<Color32> LastFrameCopy = new NativeArray<Color32>(LastFrame.Length, Allocator.TempJob);                    
                    NativeArray<Color32> CurrentPixelsColor = new NativeArray<Color32>(PixelsCount, Allocator.TempJob);                    
                    NativeArray<Color32> PostProccesImageCopy = new NativeArray<Color32>(PixelsColor.Length, Allocator.TempJob);

                    //LastFrameHitPoints                    
                    for (int i = 0; i < PixelsCount; i++)
                    {
                        CurrentPixelsColor[i] = PixelsColor[i];
                        LastFrameCopy[i] = LastFrame[i];                        
                        PostProccesImageCopy[i] = PixelsColor[i];                                          
                    }
                                 
                    DenoiserJob denoiserJob = new DenoiserJob
                    {
                        PixelsColor = PixelsColorCopy,
                        CurrentFrame = AccumulatedFrames,
                        CurrentPixelsColor = CurrentPixelsColor,
                        LastFrame = LastFrameCopy,                        
                        PostProccesImage = PostProccesImageCopy,
                        BrightnessVulue = BrightnessVulue,                                                              

                    };

                    DenoiserJobHandle = denoiserJob.Schedule(PixelsCount, 64);
                    DenoiserJobHandle.Complete();

                    PixelsColor = PixelsColorCopy.ToArray();
                    PostProccesImage = PostProccesImageCopy.ToArray();

                    PixelsColorCopy.Dispose();
                    CurrentFrameCopy.Dispose();
                    CurrentPixelsColor.Dispose();
                    LastFrameCopy.Dispose();
                    PostProccesImageCopy.Dispose();

                    FilterTexture.SetPixels32(PostProccesImage);
                    FilterTexture.Apply();

                    DisplayIn_RawImage.texture = FilterTexture;

                }
                else
                {
                    AccumulatedFrames = 0;

                    FilterTexture.SetPixels32(PixelsColor);
                    FilterTexture.Apply();

                }

            }
           
            if (Denoiser == false && RayTracing == true)
            {
                PostProccesImage = PixelsColor;
            }
            if (RayTracing == true)
            {
                DisplayTexture.SetPixels32(PixelsColor);
                DisplayTexture.Apply();
                Refrash = false;
            }

        }

    }


    //(CompileSynchronously = true)
    [BurstCompile(CompileSynchronously = true)]
    public struct RayCastingJob : IJobParallelFor
    {
        public NativeArray<Ray> RaysHitLightJob;
        [ReadOnly] public NativeArray<RaycastCommand> PixelsHitPos;
        public void Execute(int index)
        {
            if (PixelsHitPos[index].from != new Vector3(0, 0, 0))
            {
                RaysHitLightJob[index] = new Ray(PixelsHitPos[index].from, PixelsHitPos[index].direction);
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct Global_Illumination_ReSTIR_Job : IJobParallelFor
    {
        [WriteOnly] public NativeArray<Ray> RaysGi;
        [ReadOnly] public NativeArray<RaycastCommand> RaysGiCommend;
        public void Execute(int index)
        {
            if (RaysGiCommend[index].from != Vector3.zero)
            {
                RaysGi[index] = new Ray(RaysGiCommend[index].from, RaysGiCommend[index].direction);
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct CameraRays_Job : IJobParallelFor
    {
        [WriteOnly] public NativeArray<Ray> RaysCam;
        [ReadOnly] public NativeArray<RaycastCommand> RaysCamCommend;
        public void Execute(int index)
        {
            if (RaysCamCommend[index].from != Vector3.zero)
            {
                RaysCam[index] = new Ray(RaysCamCommend[index].from, RaysCamCommend[index].direction);
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct ChackLightAngleToSkipJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> NormalsVectors;
        [ReadOnly] public NativeArray<Vector3> LightsDirection;
        [WriteOnly] public NativeArray<bool> Resulte_If_Weel_Hit;

        public void Execute(int index)
        {
            bool Resulte = true;

            float Angle_Between_Light_Normal = Vector3.Angle(NormalsVectors[index], LightsDirection[index]);
            if (Angle_Between_Light_Normal >= 90f)
            {
                Resulte = false;
            }

            Resulte_If_Weel_Hit[index] = Resulte;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct DenoiserJob : IJobParallelFor
    {
        [ReadOnly] public int CurrentFrame;
        [WriteOnly] public NativeArray<Color32> PixelsColor;
        [ReadOnly] public NativeArray<Color32> CurrentPixelsColor;
        [ReadOnly] public NativeArray<Color32> LastFrame;                
        [WriteOnly] public NativeArray<Color32> PostProccesImage;
        [ReadOnly] public float BrightnessVulue;

        public void Execute(int index)
        {
            Color32 Denoise = Color.black;

            Denoise = Color.LerpUnclamped(LastFrame[index], CurrentPixelsColor[index], 1f / CurrentFrame);


            //denoiseHSV
            float H, S, V;
            Color.RGBToHSV(Denoise, out H, out S, out V);

            PixelsColor[index] = Color.HSVToRGB(H, S, V);

            PostProccesImage[index] = Color.HSVToRGB(H, S, V * BrightnessVulue);
        }
    }

    Color32 ProccesGiRays_Global_Illumination(bool IsHit, RaycastHit hit, bool IsHitBounce, RaycastHit HitBounce, Vector3 SSHitPoint, Vector3 SSHitNormal, Vector3 PointLightPos, Color32 LightCol, bool Bounce,Vector3 GiRayDir)
    {
        Color32 Resulte = Color.black;
        

        RayTracingMaterial RT_Mat_OBJ_Hit;

        GameObject OBJ = null;

        if (IsHit == true)
        {
            int ID_Object_Hited = hit.colliderInstanceID;
            for (int j = 0; j < Objects.Length; j++)
            {
                if (ID_Object_Hited == All_Objects_ID[j])
                {
                    OBJ = Objects[j];
                }
            }
        }

        Vector3 PointRandomSampleHited = hit.point;
        Vector3 PointBouncedSampleHited = HitBounce.point;

        //
        if (IsHit == true)
        {
            if (Bounce == false)
            {
                RT_Mat_OBJ_Hit = OBJ.GetComponent<RayTracingMaterial>();

                if (RT_Mat_OBJ_Hit.EmissiveIntinsity == 0)
                {
                    //Color32 ObjectHitColor = RT_Mat_OBJ_Hit.Color;
                    Color32 ObjectHitColor = GetTextureCol(hit,OptimazeBySkipTexture);
                    float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                    Resulte = Color.LerpUnclamped(Resulte, ObjectHitColor, DistanceToObject / MultyVulue);
                    Resulte = Color.Lerp(LightCol, Resulte, 0.5f);

                }
                else
                {
                    if (RT_Mat_OBJ_Hit.GlowTexture == null)
                    {
                        Color32 ObjectHitColorEmissive = RT_Mat_OBJ_Hit.Color;
                        float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                        Resulte = Color.LerpUnclamped(ObjectHitColorEmissive, Color.black, DistanceToObject / RT_Mat_OBJ_Hit.EmissiveIntinsity);
                    }
                    else
                    {
                        Texture2D GlowMap = RT_Mat_OBJ_Hit.GlowTexture;
                        Vector2 UV = hit.textureCoord;
                        int UV_X = Mathf.RoundToInt(UV.x * GlowMap.width);
                        int UV_Y = Mathf.RoundToInt(UV.y * GlowMap.height);
                        Color32 ObjectHitColorEmissive = GlowMap.GetPixel(UV_X, UV_Y);
                        if (ObjectHitColorEmissive != Color.black)
                        {
                            float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                            Resulte = Color.LerpUnclamped(ObjectHitColorEmissive, Color.black, DistanceToObject / RT_Mat_OBJ_Hit.EmissiveIntinsity);
                        }
                        else
                        {
                            Color32 ObjectHitColor = RT_Mat_OBJ_Hit.Color;
                            float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                            Resulte = Color.LerpUnclamped(Resulte, ObjectHitColor, DistanceToObject / MultyVulue);
                        }
                    }
                }
 
            }
            else
            {
                RT_Mat_OBJ_Hit = OBJ.GetComponent<RayTracingMaterial>();
                if (RT_Mat_OBJ_Hit.EmissiveIntinsity == 0)
                {

                    if (IsHitBounce)
                    {

                        Color32 ColorObjectHited_Bounced = LightCol;
                        float DisRS_BS = Vector3.Distance(PointRandomSampleHited, PointBouncedSampleHited);
                        Color32 ResulteTestBounced;
                        if (HitBounce.transform.GetComponent<RayTracingMaterial>().IsLight)
                        {
                            ResulteTestBounced = Color.LerpUnclamped(GetTextureCol(hit, OptimazeBySkipTexture), Color.black, DisRS_BS / (MultyVulue * HitBounce.transform.GetComponent<RayTracingMaterial>().LightIntensity));

                            float ColBright = GetColorBrightness(ResulteTestBounced);
                            ResulteTestBounced = Color.LerpUnclamped(LightCol, ResulteTestBounced, 0.5f);
                            Resulte = new Color(ResulteTestBounced.r * ColBright, ResulteTestBounced.g * ColBright, ResulteTestBounced.b * ColBright);

                        }
                        else
                        {

                            Resulte = Color.black;

                        }
                    }
                    else
                    {
                        Resulte = Color.black;
                    }
                    
                }
                else
                {
                    //If Gi Ray Hit Emissive Serface
                    if (RT_Mat_OBJ_Hit.GlowTexture == null)
                    {
                        Color32 ObjectHitColorEmissive = GetTextureCol(hit,false);
                        float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                        Resulte = Color.LerpUnclamped(ObjectHitColorEmissive, Color.black, DistanceToObject / RT_Mat_OBJ_Hit.EmissiveIntinsity);
                    }
                    else
                    {
                        Texture2D GlowMap = RT_Mat_OBJ_Hit.GlowTexture;
                        Vector2 UV = hit.textureCoord;
                        int UV_X = Mathf.RoundToInt(UV.x * GlowMap.width);
                        int UV_Y = Mathf.RoundToInt(UV.y * GlowMap.height);
                        Color32 ObjectHitColorEmissive = GlowMap.GetPixel(UV_X, UV_Y);
                        if (ObjectHitColorEmissive != Color.black)
                        {
                            float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                            Resulte = Color.LerpUnclamped(ObjectHitColorEmissive, Color.black, DistanceToObject / RT_Mat_OBJ_Hit.EmissiveIntinsity);
                        }
                        else
                        {
                            Color32 ObjectHitColor = RT_Mat_OBJ_Hit.Color;
                            float DistanceToObject = Vector3.Distance(hit.point, SSHitPoint);
                            Resulte = Color.LerpUnclamped(Resulte, ObjectHitColor, DistanceToObject / MultyVulue);
                        }
                    }
                }
            }
        }
        else
        {
            if (!Gi_Effict_With_Sky_Texture)
            {
                Resulte = SkyColor;
            }
            else
            {
                Resulte = SkyRayDir(GiRayDir,SkyTexture);
            }
        }


        return Resulte;
    }

    Vector3 GenerateDirection(Vector3 normal, float minDotProduct)
    {
        Vector3 randomDirection;
        float dotProduct;

        do
        {
            randomDirection = Random.onUnitSphere;
            dotProduct = Vector3.Dot(randomDirection, normal);
        } while (dotProduct < minDotProduct);

        return randomDirection;
    }

    float GetColorBrightness(Color color)
    {
        //return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return v;
    }
    Vector3[] RaysCamDirection(out Vector3[] RaysComeFrom)
    {
        Vector3[] Resultes = new Vector3[PixelsColor.Length];
        RaysComeFrom = new Vector3[PixelsColor.Length];
        Camera cam = Camera.main;
        Transform camT = cam.transform;

        float NearClipPlaneF;
        if (BluringIntinsity == 0)
        {
            NearClipPlaneF = Camera.main.nearClipPlane;
        }
        else
        {
            NearClipPlaneF = FocusDistance;
        }
        
        float PlaneHeight = NearClipPlaneF * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float PlaneWidth = PlaneHeight * cam.aspect;

        Vector3 BottomLeftLocal = new Vector3(-PlaneWidth / 2, -PlaneHeight / 2, NearClipPlaneF);

        for (int x = 0; x < ScreenSize.x; x++)
        {
            for (int y = 0; y < ScreenSize.y; y++)
            {
                int indexVulue = ColcolateIndex(x, y);

                float tx = (float)x / (ScreenSize.x - 1);
                float ty = (float)y / (ScreenSize.y - 1);
                Vector3 PointStart = new Vector3(Random.Range(-BluringIntinsity, BluringIntinsity), Random.Range(-BluringIntinsity, BluringIntinsity), Random.Range(-BluringIntinsity, BluringIntinsity)) ;

                Vector3 PointLocal = BottomLeftLocal + new Vector3(PlaneWidth * tx, PlaneHeight * ty);
                Vector3 point = camT.position + camT.right * PointLocal.x + camT.up * PointLocal.y + camT.forward * PointLocal.z;
                Vector3 dirFinel = (point - camT.position).normalized;


                Vector3 RandomDirectionVulues = Random.onUnitSphere * RandomSamplesDir;
                Vector3 dirRay = dirFinel + RandomDirectionVulues;

                Resultes[indexVulue] = dirRay.normalized;
                RaysComeFrom[indexVulue] = PointStart;
            }
        }

        //RaysComeFrom
        return Resultes;
    }

    bool Chack_Normal_and_Light_Angle(Vector3 NormalVector, Vector3 LightDirection)
    {
        bool Resulte = true;

        float Angle_Between_Light_Normal = Vector3.Angle(NormalVector, LightDirection);
        if (Angle_Between_Light_Normal >= 90f)
        {
            Resulte = false;
        }

        return Resulte;
    }

    GameObject Find_OBJ_Hit_By_ID(Ray ray)
    {
        GameObject OBJ = null;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            int ID_Object_Hited = hit.colliderInstanceID;
            for (int j = 0; j < Objects.Length; j++)
            {
                if (ID_Object_Hited == All_Objects_ID[j])
                {
                    OBJ = Objects[j];
                }
            }

        }
        return OBJ;
    }
    //
    RT_Mat_Info ConvertRTMat_To_RT_Mat(RayTracingMaterial rayTracingMaterial)
    {
        RT_Mat_Info RT_Mat = new RT_Mat_Info();
        RT_Mat.color = rayTracingMaterial.Color;
        RT_Mat.IsLight = rayTracingMaterial.IsLight;
        RT_Mat.LightIntensity = rayTracingMaterial.LightIntensity;
        return RT_Mat;
    }

    void InterPointLightPos()
    {
        Vector3[] PointsVer = new Vector3[10000];
        RealVerNem = 0;
        for (int i = 0; i < Objects.Length; i++)
        {
            if (Objects[i] != null)
            {
                if (Objects[i].transform.GetComponent<RayTracingMaterial>().IsLight)
                {
                    int MeshVer = MeshVertecesCounter(Objects[i]);
                    for (int j = 0; j < MeshVer; j++)
                    {
                        bool IsLowPolyLight = false;
                        if (PointsLightInfo[j].LowPolyModel == true)
                        {
                            IsLowPolyLight = true;
                        }

                        PointsVer[i] = GetLightPoints(j, Objects[i], IsLowPolyLight);
                        RealVerNem++;
                    }
                }
            }
        }
        PointsLight = PointsVer;
    }

    int ColcolateIndex(int x, int y)
    {
        int index = y * DisplayTexture.width + x;
        return index;
    }

    int MeshVertecesCounter(GameObject OBJ)
    {
        SkinnedMeshRenderer skinnedMesh = OBJ.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = skinnedMesh.sharedMesh;

        int VerticesMesh = mesh.vertices.Length;
        return VerticesMesh;
    }

    Vector3 GetLightPoints(int i, GameObject OBJ, bool LowVerticesMode)
    {
        SkinnedMeshRenderer skinnedMesh = OBJ.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = skinnedMesh.sharedMesh;
        Vector3 ScaleMesh = OBJ.transform.localScale;

        Vector3[] Vertices = mesh.vertices;
        int[] Tringles = mesh.triangles;
        Vector3 newVector = Vector3.zero;
        if (LowVerticesMode != false)
        {
            newVector = OBJ.transform.rotation * Vector3.Scale(Vertices[i], ScaleMesh) + OBJ.transform.position;
        }
        else
        {
            int selectedTriangle = Random.Range(0, Tringles.Length / 3) * 3;
            Vector3 vertex1 = Vertices[Tringles[selectedTriangle]]; 
            Vector3 vertex2 = Vertices[Tringles[selectedTriangle + 1]];
            Vector3 vertex3 = Vertices[Tringles[selectedTriangle + 2]];  

            Vector3 faceNormal = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;
            
            float u = Random.Range(0f, 1f);
            float v = Random.Range(0f, 1f);

            if (u + v > 1)
            {
                u = 1 - u;
                v = 1 - v;
            }

            Vector3 randomPoint = vertex1 + u * (vertex2 - vertex1) + v * (vertex3 - vertex1);
            Vector3 inwardPoint = randomPoint - faceNormal * 0.1f;
            newVector = OBJ.transform.rotation * Vector3.Scale(inwardPoint, ScaleMesh) + OBJ.transform.position;
        }

        return newVector;
    }

    public Vector3 CalculateRefraction(Vector3 incomingRayDirection, Vector3 normal, float refractiveIndexAir = 1f, float refractiveIndexGlass = 1.5f)
    {

        incomingRayDirection.Normalize();
        normal.Normalize();


        float dotProduct = Vector3.Dot(incomingRayDirection, normal);


        float eta = refractiveIndexAir / refractiveIndexGlass;


        float k = 1.0f - eta * eta * (1.0f - dotProduct * dotProduct);

        if (k < 0.0f)
        {

            return Vector3.zero;
        }


        Vector3 refractedDirection = eta * incomingRayDirection - (eta * dotProduct + Mathf.Sqrt(k)) * normal;
        refractedDirection.Normalize();

        return refractedDirection;
    }


    Color32 SkyRayDir(Vector3 Dir, Texture2D SkyTexture)
    {        
        Dir.Normalize();
        float theta = Mathf.Atan2(Dir.z, Dir.x);
        float phi = Mathf.Acos(Dir.y);

        float u = (theta + Mathf.PI) / (2 * Mathf.PI);
        float v = phi / Mathf.PI; 

        float uPixel = 1f - (u * SkyTexture.width);
        float vPixel = 1f - (v * SkyTexture.height);
        Color dst = Color.black;
        if (SkyTexture != null)
        {
            dst = SkyTexture.GetPixel(((int)uPixel), ((int)vPixel));
        }   
        return dst;
    }

    float FresnalAngle(RaycastHit hit, Vector3 ViewDir)
    {
        float Resulte = 0;
        RayTracingMaterial RT_Mat = hit.transform.GetComponent<RayTracingMaterial>();
        if (RT_Mat != null)
        {
            Vector3 Normal = hit.normal;
            Resulte = (Vector3.Angle(Normal, ViewDir) * RT_Mat.FresnalEffactWight);
        }
        return Resulte;
    }

    Color GetTextureCol(RaycastHit hit, bool Skip)
    {
        Color Res;
        if (!Skip)
        {
            Vector2 UV = hit.textureCoord;
            if (hit.transform.GetComponent<RayTracingMaterial>().AlbedoMap != null)
            {
                Texture2D BaseMap = hit.transform.GetComponent<RayTracingMaterial>().AlbedoMap;
                Vector2 tiling = hit.transform.GetComponent<RayTracingMaterial>().TilingAlbedo;
                Vector2 offset = hit.transform.GetComponent<RayTracingMaterial>().OffsetAlbedo;
                Vector2 adjustedUV = (UV * tiling) + offset;

                // Calculate pixel coordinates
                int UV_X = Mathf.RoundToInt(adjustedUV.x * BaseMap.width);
                int UV_Y = Mathf.RoundToInt(adjustedUV.y * BaseMap.height);

                // Ensure UV_X and UV_Y are within valid bounds
                //UV_X = Mathf.Clamp(UV_X, 0, BaseMap.width - 1);
                //UV_Y = Mathf.Clamp(UV_Y, 0, BaseMap.height - 1);
                Color BaseTextureColor = BaseMap.GetPixel(UV_X, UV_Y);
                Res = BaseTextureColor;
            }
            else
            {
                Res = hit.transform.GetComponent<RayTracingMaterial>().Color;
            }
        }
        else
        {
            Res = hit.transform.GetComponent<RayTracingMaterial>().Color;
        }
        return Res;
    }

    Color32 FresnalColor(RaycastHit hit, Ray ray)
    {
        Color32 Resulte = Color.black;
        RayTracingMaterial RT_Mat = hit.transform.GetComponent<RayTracingMaterial>();
        if (RT_Mat != null)
        {
            Vector3 Normal = hit.normal;
            Vector3 ViewDir = ray.direction;
            float FresnalPower = (Vector3.Angle(Normal, ViewDir) * RT_Mat.FresnalEffactWight);
            Resulte = Color.Lerp(RT_Mat.Color_1, RT_Mat.Color_2, FresnalPower);
        }
        return Resulte;
    }
   
    Vector3 GenarateReflacteDir(Vector3 NormalVector, Vector3 RayDir)
    {
        Vector3 ReflacteDir = Vector3.zero;
        return Vector3.Reflect(RayDir, NormalVector);
    }
    
    
    private int DebugModeTest(DebugMode debugMode)
    {
        switch (debugMode)
        {
            case DebugMode.None:
                return 0;
            case DebugMode.Shadow:
                return 1;
            case DebugMode.Global_Illumination:
                return 2;
            case DebugMode.Reflaction:
                return 3;
            case DebugMode.Refraction:
                return 4;
            default:
                return 0;
        }
    }

}
