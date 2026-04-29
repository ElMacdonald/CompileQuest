using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float horizontal;
    public string moveState = "idle";
    private SpriteRenderer spr;
    public Sprite[] sprites;

    public Sprite[] rightWalkingSprites;
    public Sprite[] leftWalkingSprites;
    public Sprite[] jumpingSprites;
    public Sprite[] idleSprites;

    [Header("Death Animation")]
    [Tooltip("Frames played in order when the player hits a spike. Plays once then resets.")]
    public Sprite[] deathSprites;
    [Tooltip("How long each death frame is shown, in seconds.")]
    public float deathFrameTime = 0.1f;

    [Header("Spike Reset")]
    [Tooltip("Assign the ReadBox component from this scene so spike collisions can trigger a reset.")]
    public ReadBox readBox;

    [Header("Depth")]
    [Tooltip("Z position kept constant so the player never clips behind or in front of scene objects.")]
    public float playerZ = -3.25f;

    public float jumpTimer;
    public float jumpDelay;

    public float landDelay = .1f;
    public float landTimer;

    public float timeBetweenFrames = 0.1f;
    public float timer = 0f;
    public int currentFrame = 0;

    public float floatAmplitude = 0.05f;
    public float floatFrequency = 6f;
    public Vector3 basePos;
    public bool canTele = true;

    private bool isDead = false;
    private Rigidbody2D rb;

    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        readBox = FindObjectOfType<ReadBox>();
        basePos = transform.position;
        EnforceZ();
    }

    void Update()
    {
        if (isDead) return;

        jumpTimer += Time.deltaTime;
        handleAnims();
        EnforceZ();

        Ray2D ray = new Ray2D(transform.position, Vector2.down);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 2f);
        Debug.DrawRay(ray.origin, ray.direction * 1.5f, Color.red);

        if (hit.collider != null && hit.collider.tag == "Ground" && moveState == "jumping" && jumpTimer > jumpDelay)
        {
            moveState = "idle";
            timer = 1;
        }
    }

    void EnforceZ()
    {
        Vector3 pos = transform.position;
        if (pos.z != playerZ)
        {
            pos.z = playerZ;
            transform.position = pos;
        }
    }

    // -----------------------------------------------------------------------
    // Spike handling
    // -----------------------------------------------------------------------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("spike") && !isDead)
            StartCoroutine(DeathSequence());
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("spike") && !isDead)
            StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        isDead = true;
        SoundManager.Play("Death");
        if (readBox != null)
            readBox.StopCode();
        else
            Debug.LogWarning("[Movement] No ReadBox assigned — assign it in the Inspector.");

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        moveState = "dead";

        if (deathSprites != null && deathSprites.Length > 0)
        {
            foreach (Sprite frame in deathSprites)
            {
                spr.sprite = frame;
                yield return new WaitForSeconds(deathFrameTime);
            }
        }
        else
        {
            Debug.LogWarning("[Movement] No deathSprites assigned. Add them in the Inspector.");
            yield return new WaitForSeconds(0.5f);
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        isDead = false;
        moveState = "idle";

        if (readBox != null)
            readBox.FullReset();
    }

    // -----------------------------------------------------------------------
    // Animation
    // -----------------------------------------------------------------------

    public void handleAnims()
    {
        timer += Time.deltaTime;
        if (timer >= timeBetweenFrames)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % 4;
            switch (moveState)
            {
                case "left walk":
                    spr.sprite = leftWalkingSprites[currentFrame];
                    break;
                case "right walk":
                    spr.sprite = rightWalkingSprites[currentFrame];
                    break;
                case "jumping":
                    spr.sprite = jumpingSprites[currentFrame];
                    break;
                case "idle":
                    spr.sprite = idleSprites[currentFrame];
                    break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Movement commands
    // -----------------------------------------------------------------------

    public void MoveLeft(float distance)
    {
        moveState = "left walk";
        StartCoroutine(MoveCharacter(Vector3.left, distance));
    }

    public void MoveRight(float distance)
    {
        moveState = "right walk";
        StartCoroutine(MoveCharacter(Vector3.right, distance));
    }

    public void SetX(float x)
    {
        if (canTele)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            pos.z = playerZ;
            transform.position = pos;
        }
    }

    public void SetY(float y)
    {
        if (canTele)
        {
            Vector3 pos = transform.position;
            pos.y += (y - pos.y);
            pos.z = playerZ;
            transform.position = pos;
        }
        moveState = "idle";
    }

    public void JumpRight(float distance, float height)
    {
        StartCoroutine(MoveCharacter(Vector3.right, distance));
        Jump(height);
    }

    public void JumpLeft(float distance, float height)
    {
        StartCoroutine(MoveCharacter(Vector3.left, distance));
        Jump(height);
    }

    private IEnumerator MoveCharacter(Vector3 direction, float distance)
    {
        float moved = 0f;
        timer = 1f;
        float speed = distance;
        while (moved < distance)
        {
            float step = speed * Time.deltaTime;
            transform.Translate(new Vector3(direction.x * step, direction.y * step, 0f));
            EnforceZ();
            moved += step;
            yield return null;
        }
        moveState = "idle";
    }

    public void Jump(float height)
    {
        jumpTimer = 0f;
        timer = 1f;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, height), ForceMode2D.Impulse);
        moveState = "jumping";
    }
}
