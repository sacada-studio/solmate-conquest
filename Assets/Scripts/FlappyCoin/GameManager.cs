using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private int _score;
    [SerializeField] private TMP_Text scoreTxt;
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] private AudioClip collect;
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private string mainMenuSceneName = "HomeScene"; // Scene to return to after game over
    public bool isDead = false;
    private bool _processingGameOver = false;
    
    // Reference to AccountManager (will be found automatically via singleton)
    private AccountManager _accountManager;

    private void Start()
    {
        // Find AccountManager reference (should exist as a singleton)
        _accountManager = AccountManager.Instance;
        if (_accountManager == null)
        {
            Debug.LogWarning("AccountManager not found! Blockchain points will not be recorded.");
        }
        else
        {
            Debug.Log("AccountManager found and ready to record points.");
        }
        
        AudioManager.Instance.PlayBackgroundMusic(bgMusic);
        _score = 0;
        isDead = false;
        _processingGameOver = false;
    }

    public async void GameOver()
    {
        if (isDead || _processingGameOver) return;
        
        isDead = true;
        _processingGameOver = true;
        
        AudioManager.Instance.PlaySfx(gameOver);
        AudioManager.Instance.StopMusic();
        
        Debug.Log($"Game Over! Final score: {_score}");
        
        // Show game over UI or play animations here
        // Wait a moment before adding points to blockchain
        await System.Threading.Tasks.Task.Delay(1500);
        
        // Add points to blockchain if score > 0
        if (_score > 0 && _accountManager != null)
        {
            Debug.Log($"Adding {_score} points to blockchain...");
            try
            {
                // Try to add points but use a timeout to prevent hanging
                var addPointsTask = _accountManager.AddGamePoints(_score);
                
                // Wait for the transaction but with a 10-second timeout
                var timeoutTask = System.Threading.Tasks.Task.Delay(10000); // 10 seconds timeout
                await System.Threading.Tasks.Task.WhenAny(addPointsTask, timeoutTask);
                
                if (addPointsTask.IsCompleted && !addPointsTask.IsFaulted)
                {
                    bool success = await addPointsTask;
                    if (success)
                    {
                        Debug.Log("Successfully added points to blockchain!");
                    }
                    else
                    {
                        Debug.LogWarning("Transaction failed but continuing to close the scene");
                    }
                }
                else
                {
                    Debug.LogWarning("Blockchain transaction timed out or failed, continuing to close the scene");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during blockchain transaction: {ex.Message}");
                Debug.LogWarning("Continuing to close the scene despite transaction error");
            }
        }
        else
        {
            if (_score <= 0)
            {
                Debug.Log("No points earned, skipping blockchain update");
            }
            else
            {
                Debug.LogWarning("AccountManager not available, points won't be recorded");
            }
        }
        
        // Always proceed to closing the scene, regardless of transaction status
        Debug.Log($"Returning to scene: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void IncreaseScore()
    {
        _score++;
        scoreTxt.text = _score.ToString();
        AudioManager.Instance.PlaySfx(collect);
    }
}
