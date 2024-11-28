using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using System.IO;

public class VideoRenderManager : MonoBehaviour
{
    public bool Render;
    public PlayableDirector playableDirector;
    public string outputPath = "CapturedVideo";
    public int frameRate = 30;
    public RayTracingCamera RTCam;
    public bool isCapturing = false;
    public int frameCount = 0;
    public int MaxSamples = 5;
    public float T;

    void Start()
    {
        StartCoroutine(CaptureVideo());
    }

    private void Update()
    {
        if (Render)
        {
            StartCoroutine(CaptureVideo());
            
        }
    }

    IEnumerator CaptureVideo()
    {
        isCapturing = true;
        //playableDirector.Play();
        yield return new WaitForEndOfFrame();

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);

        // Capture the total duration of the timeline
        double totalDuration = playableDirector.duration;

        float frameDuration = 1.0f / frameRate;
        // && frameCount < (totalDuration * frameRate)
        while (isCapturing)
        {
            // Capture the screen as a texture
            

           
            if (RTCam.AccumulatedFrames >= MaxSamples)
            {
                Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();

                // Encode to PNG
                byte[] bytes = frame.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(outputPath, $"frame_{frameCount:D04}.png"), bytes);
                Debug.Log($"Captured frame: {frameCount}");
                // Clean up
                Destroy(frame);

                frameCount++;
                RTCam.AccumulatedFrames = 1;
                //RTCam.Refrash = true;
                //RTCam.StaticScene = false;
                RTCam.UpdateFrame = true;
                playableDirector.time += frameDuration;
                playableDirector.Evaluate();
            }
            else
            {
                //RTCam.Refrash = false;
                //RTCam.StaticScene = true;
            }
            yield return new WaitForSeconds(frameDuration);
        }

        playableDirector.Stop();
    }

    public void StopCapture()
    {
        isCapturing = false;
    }
}