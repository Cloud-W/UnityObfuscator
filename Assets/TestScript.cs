using UnityEngine;

/// <summary>
/// Sample script to test obfuscation
/// This script will be obfuscated during the build process
/// </summary>
public class TestScript : MonoBehaviour
{
    // This field should be preserved because of [SerializeField]
    [SerializeField]
    private string playerName = "Test Player";
    
    // This field can be obfuscated
    private int score = 0;
    
    // String literals can be encrypted if enabled
    private const string SecretMessage = "This is a secret message!";
    
    void Start()
    {
        Debug.Log("TestScript started");
        Debug.Log(SecretMessage);
        InitializePlayer();
    }
    
    void Update()
    {
        // This method name will be preserved (Unity callback)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IncrementScore();
        }
    }
    
    // This method name can be obfuscated
    private void InitializePlayer()
    {
        Debug.Log($"Initializing player: {playerName}");
        score = 0;
    }
    
    // This method name can be obfuscated
    public void IncrementScore()
    {
        score++;
        Debug.Log($"Score: {score}");
    }
    
    // This property can be obfuscated
    public int CurrentScore
    {
        get { return score; }
        set { score = value; }
    }
}

/// <summary>
/// A nested class for testing type obfuscation
/// </summary>
public class GameData
{
    public string GameName = "My Game";
    public int Level = 1;
    
    public string GetGameInfo()
    {
        return $"{GameName} - Level {Level}";
    }
}
