using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    public float gravity = -9.8f;
    public float strength = 5f;
    public Sprite[] sprites;
    private int spriteIndex = 0;
    private Rigidbody2D rb;
    public float maxUpAngle = 45f;
    public float maxDownAngle = -90f;
    public float rotationSpeed = 5f;
    private GameManager _gameManager;
    [SerializeField] private AudioClip jump;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
        rb = GetComponent<Rigidbody2D>();
        _gameManager = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        if (_gameManager.isDead)
        {
            direction.y += 2 * gravity * Time.deltaTime;
            rb.position += new Vector2(direction.x, direction.y) * Time.deltaTime;

            Quaternion downRotation = Quaternion.Euler(0, 0, maxDownAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, downRotation, Time.deltaTime * rotationSpeed);
            return;
        }
        
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && !_gameManager.isDead)
        {
            direction = Vector3.up * strength;
        }
        
        direction.y += gravity * Time.deltaTime;
        rb.position += new Vector2(direction.x, direction.y) * Time.deltaTime;
        float targetAngle = Mathf.Clamp(direction.y * rotationSpeed, maxDownAngle, maxUpAngle);

        Quaternion desiredRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
    }


    private void AnimateSprite()
    {
        spriteIndex++;
        if (spriteIndex >= sprites.Length)
        {
            spriteIndex = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("collided with " + other.gameObject.name);
        if (other.gameObject.CompareTag("Obstacle"))
        {
            FindObjectOfType<GameManager>().GameOver();
        } else if (other.gameObject.CompareTag("Scoring"))
        {
            FindObjectOfType<GameManager>().IncreaseScore();
            other.gameObject.SetActive(false);
        }
    }
}
