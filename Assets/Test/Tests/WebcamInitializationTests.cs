using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WebcamInitializationTests // Tests to verify that the webcam initializes and starts playing correctly
{
    [UnityTest]
    public IEnumerator Webcam_WhenDevicesExist_SuccessfullyInitializesAndPlays() 
    {
        
        if (WebCamTexture.devices.Length == 0) // If no webcam devices are found, skip the test to avoid false failures
        {
            Assert.Ignore("Skipping test: No physical webcam connected to this machine."); 
            yield break;
        }

        
        string deviceName = WebCamTexture.devices[0].name; // Use the first available webcam device
        WebCamTexture webcamTexture = new WebCamTexture(deviceName); // Initialize the webcam texture with the selected device
        
        webcamTexture.Play();

         
        yield return new WaitForSeconds(0.5f); // Wait briefly to allow the webcam to initialize and start playing

        
        Assert.IsTrue(webcamTexture.isPlaying, "The webcam texture failed to enter the 'playing' state."); // Check if the webcam is playing
        
        
        Assert.Greater(webcamTexture.width, 16, "Webcam width is invalid. The camera may have failed to initialize."); // Check if the webcam has a valid width (indicating it initialized properly)
        Assert.Greater(webcamTexture.height, 16, "Webcam height is invalid. The camera may have failed to initialize."); // Check if the webcam has a valid height (indicating it initialized properly)

        
        webcamTexture.Stop();
    }
}
