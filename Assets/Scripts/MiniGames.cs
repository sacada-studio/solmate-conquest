using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGames : MonoBehaviour
{
    [SerializeField] private AudioClip menuAudio;
    
    private void OnEnable()
    {
        AudioManager.Instance.PlayBackgroundMusic(menuAudio);
    }
    
    public void StartFlappyCoin()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("FlappyCoin");
    }
}
