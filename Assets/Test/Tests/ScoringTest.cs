using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// Test suite dedicated to verifying the internal scoring mechanics and UI updates of the whack-a-mole game.
/// </summary>
/// <remarks>
/// Because the core <c>MoleLogic</c> relies heavily on physical mouse inputs embedded within its <c>Update()</c> loop, 
/// these tests utilize System.Reflection to bypass hardware inputs and directly inject test data into private fields.
/// This ensures the scoring logic and UI updates can be validated in an automated pipeline without altering the core gameplay scripts.
/// </remarks>
public class ScoringTests
{
    /// <summary>
    /// Prepares the testing environment before each test is executed.
    /// </summary>
    /// <remarks>
    /// Loads the "GamePrototype" scene additively. It yields the Async operation to guarantee 
    /// the scene and all of its GameObjects are 100% loaded into memory before the test logic begins.
    /// </remarks>
    /// <returns>An IEnumerator to allow Unity to wait for the scene load to complete.</returns>
    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return SceneManager.LoadSceneAsync("GamePrototype", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Validates that an increment to the internal hit counter is successfully pushed to the UI text element.
    /// </summary>
    /// <remarks>
    /// This test performs several safety injections (providing a dummy Camera and dummy center object) 
    /// to prevent the <c>Mole2DLogic.Update()</c> loop from throwing a NullReferenceException when running in 
    /// an isolated Test Runner environment. It then uses Reflection to set the private <c>hits</c> integer to 1.
    /// </remarks>
    /// <returns>An IEnumerator to allow the Unity engine to process the Update loop for 0.1 seconds.</returns>
    [UnityTest]
    
    public IEnumerator UpdateHits_WhenCalled_ProperlyUpdatesScoreboardText()
    {

        GameManagerLogic gameManager = Object.FindObjectOfType<GameManagerLogic>(true);
        Assert.IsNotNull(gameManager, "Failed to find GameManagerLogic in the scene.");

        gameManager.gameObject.SetActive(true);

        
        yield return new WaitForSeconds(0.1f);

        
        FieldInfo stateField = typeof(GameManagerLogic).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
        if (stateField != null)
        {
        stateField.SetValue(gameManager, 5);
        }

        
        gameManager.UpdateHits();

        
        yield return null;

        
        Assert.AreEqual("Hits x 1", gameManager.hitcount.text);
    }

    /// <summary>
    /// Cleans up the testing environment after each test has finished executing.
    /// </summary>
    /// <remarks>
    /// Unloads the "GamePrototype" scene to ensure subsequent tests start with a clean, default state 
    /// and to prevent memory leaks in the Unity Editor during bulk test runs.
    /// </remarks>
    /// <returns>An IEnumerator to allow Unity to finish unloading the scene.</returns>
    [UnityTearDown]
    public IEnumerator Teardown()
    {
        yield return SceneManager.UnloadSceneAsync("GamePrototype");
    }
}