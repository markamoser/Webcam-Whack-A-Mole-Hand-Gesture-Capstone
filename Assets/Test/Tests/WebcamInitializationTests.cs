using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WebcamInitializationTests
{
    [UnityTest]
    public IEnumerator Webcam_WhenDevicesExist_SuccessfullyInitializesAndPlays()
    {
        
        if (WebCamTexture.devices.Length == 0)
        {
            Assert.Ignore("Skipping test: No physical webcam connected to this machine.");
            yield break;
        }

        
        string deviceName = WebCamTexture.devices[0].name;
        WebCamTexture webcamTexture = new WebCamTexture(deviceName);

        
        webcamTexture.Play();

         
        yield return new WaitForSeconds(0.5f);

        
        Assert.IsTrue(webcamTexture.isPlaying, "The webcam texture failed to enter the 'playing' state.");
        
        
        Assert.Greater(webcamTexture.width, 16, "Webcam width is invalid. The camera may have failed to initialize.");
        Assert.Greater(webcamTexture.height, 16, "Webcam height is invalid. The camera may have failed to initialize.");

        
        webcamTexture.Stop();
    }
}
