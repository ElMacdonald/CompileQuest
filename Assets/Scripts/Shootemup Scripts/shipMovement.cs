using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class shipMovement : MonoBehaviour
{
    private float horizontalInput;
    private float verticalInput;
    public float speed;
    public GameObject projectilePrefab;
    private Transform firePoint;             
    private float projectileSpeed = 15f;
    private float fireSpeed = .15f;
    private float canFire = 0f;
    private Vector3 movement;
    public float power = 1f;
    public GameObject textmesh;
    public GameObject bombText;
    public GameObject livesText;
    public float lives;
    public float bombs;
    private bool invincible;

    void Start()
    {
        speed = 4f;
        updatePower();
        updateBomb();
        updateLives();
    }

    void Update(){
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        firePoint = this.transform;
        if(Input.GetKeyDown(KeyCode.LeftShift)){
            //speed = 2.0f;
        }
        else if(Input.GetKeyUp(KeyCode.LeftShift)){
            //speed = 4f;
        }
        if(Input.GetButton("Shoot" )&& Time.time > canFire){
            FireProjectile(power);
            canFire = Time.time + fireSpeed;
            SoundManager.Play("laser");
        }
    }
    void FixedUpdate()
    {
        movement = new Vector3(horizontalInput, verticalInput, 0).normalized;
        transform.Translate(movement * speed * Time.deltaTime);
    }

    void FireProjectile(float power)
    {
        if(power >= 1f && power < 2f){
            GameObject newProjectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = newProjectile.GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.up * projectileSpeed;
        }else if(power >= 2f && power < 3f){
            GameObject newProjectile1 = Instantiate(projectilePrefab, firePoint.position + (Vector3.left/4), Quaternion.identity);
            Rigidbody2D rb1 = newProjectile1.GetComponent<Rigidbody2D>();
            rb1.velocity = Vector2.up * projectileSpeed;
            GameObject newProjectile2 = Instantiate(projectilePrefab, firePoint.position + (Vector3.right/4), Quaternion.identity);
            Rigidbody2D rb2 = newProjectile2.GetComponent<Rigidbody2D>();
            rb2.velocity = Vector2.up * projectileSpeed;
        }else if(power >= 3f && power < 4f){
            for(float i = 0f; i < 3f; i++){
            GameObject newProjectile1 = Instantiate(projectilePrefab, firePoint.position + new Vector3(-.333f + (i/3), 0, 0), Quaternion.identity);
            Rigidbody2D rb1 = newProjectile1.GetComponent<Rigidbody2D>();
            rb1.velocity = Vector2.up * projectileSpeed;
            }
        }
        else
        {
            GameObject newProjectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = newProjectile.GetComponent<Rigidbody2D>();
            rb.velocity = Vector2.up * projectileSpeed;
        }
    }

    void OnTriggerEnter2D(Collider2D other){
        switch(other.gameObject.tag){
            case "Power":
                if(power < 4.00f){
                    power += .05f;
                }
                updatePower();
                Destroy(other.gameObject);
                break;
            case "Bomb":
                if(bombs < 8f){
                    bombs += 1f;
                }
                updateBomb();
                break;
            case "Lives":
                if(lives < 8f){
                    lives += 1f;
                }
                updateLives();
                break;
            case "Enemy":
                if(!invincible){
                    Debug.Log("damage");
                }
                break;
            case "EnemyBullet":
                if(!invincible){
                    Debug.Log("damage");
                }
                break;
        }
    }

    
    void updatePower(){
        

    }

    void updateBomb(){
       
    }
    void updateLives(){
        
    }
}