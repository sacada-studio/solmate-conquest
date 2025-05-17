using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Pipes : MonoBehaviour
{
    public float speed = 5f;
    private float leftEdge;
    [SerializeField] private SpriteRenderer pipeA;
    [SerializeField] private SpriteRenderer pipeB;
    [SerializeField] private Sprite[] pipeSprites;
    [SerializeField] private GameObject coin;

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
        
        // Randomly select a sprite for each pipe
        int randomIndexA = Random.Range(0, pipeSprites.Length);
        int randomIndexB = Random.Range(0, pipeSprites.Length);
        pipeA.sprite = pipeSprites[randomIndexA];
        pipeB.sprite = pipeSprites[randomIndexB];
        
        if (Random.Range(0, 100) > 30)
        {
            coin.SetActive(false);
        }
    }

    private void Update()
    {
        if (_gameManager.isDead) return;
        
        transform.position += Vector3.left * speed * Time.deltaTime;
        
        if (transform.position.x < leftEdge)
        {
            Destroy(gameObject);
        }
    }
}
