using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private AudioClip menuAudio;

    private void Start()
    {
        AudioManager.Instance.PlayBackgroundMusic(menuAudio);
    }

    private void OnEnable()
    {
        AudioManager.Instance.PlayBackgroundMusic(menuAudio);
    }
}
