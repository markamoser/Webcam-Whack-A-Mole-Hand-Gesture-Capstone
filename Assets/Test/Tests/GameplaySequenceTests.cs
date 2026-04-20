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

        RingStartAreaLogic[] rings = gameManagerObj.GetComponentsInChildren<RingStartAreaLogic>();
        Assert.GreaterOrEqual(rings.Length, 2, "GameManager did not spawn the two starting rings.");

        rings[0].l = true; 
        rings[1].r = true;

        yield return new WaitForSeconds(4.5f);

        Assert.AreEqual("", gameManager.startmessage.text, "The game failed to progress past the initialization sequence.");

        gameManager.UpdateHits();

        yield return null;

        Assert.AreEqual("Hits x 1", gameManager.hitcount.text, "The scoreboard UI did not update when a hit was registered.");
    }

    
    [UnityTearDown]
    public IEnumerator Teardown()
    {
        
        yield return SceneManager.UnloadSceneAsync("GamePrototype");
    }
}