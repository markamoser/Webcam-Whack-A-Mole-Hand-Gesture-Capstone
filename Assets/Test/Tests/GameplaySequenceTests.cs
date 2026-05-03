using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class GameplaySequenceTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Load the game "Additively" so it sits on top of the Test Runner's empty void
        SceneManager.LoadScene("GamePrototype", LoadSceneMode.Additive);
        yield return null; 
    }

    [UnityTest]
    public IEnumerator E2E_InitializationSequence_SuccessfullyStartsGameAndRegistersHits()
    {
        GameObject gameManagerObj = GameObject.Find("GameManager");
        Assert.IsNotNull(gameManagerObj, "Could not find the GameManager in the scene.");
        
        GameManagerLogic gameManager = gameManagerObj.GetComponent<GameManagerLogic>();

        RingStartAreaLogic[] rings = gameManagerObj.GetComponentsInChildren<RingStartAreaLogic>(); //Ensure the two starting rings are present in the scene
        Assert.GreaterOrEqual(rings.Length, 2, "GameManager did not spawn the two starting rings."); // Simulate the player hitting both rings to progress the game past the initialization sequence

        rings[0].l = true; // Simulate hitting the left ring
        rings[1].r = true; // Simulate hitting the right ring

        yield return new WaitForSeconds(4.5f); // Wait for the game to process the hits and transition to the next state

        Assert.AreEqual("", gameManager.startmessage.text, "The game failed to progress past the initialization sequence."); // Check that the start message has been cleared, indicating the game has progressed

        gameManager.UpdateHits();

        yield return null;

        Assert.AreEqual("Hits x 1", gameManager.hitcount.text, "The scoreboard UI did not update when a hit was registered."); // Check that the hit count has been updated to reflect the registered hit
    }

    
    [UnityTearDown]
    public IEnumerator Teardown() 
    {
        
        yield return SceneManager.UnloadSceneAsync("GamePrototype"); // Unload the game scene to clean up after the test
    }
}