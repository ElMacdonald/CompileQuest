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

    [Header("Spike Reset")]
    [Tooltip("Assign the ReadBox component from this scene so spike collisions can trigger a reset.")]
    public ReadBox readBox;

    [Header("Platform Detection")]
    [Tooltip("How far upward the checkPlatform() raycast reaches.")]
    public float platformCheckDistance = 5f;

    // Tracks whether checkPlatform() found a platform above the player.
    // Set by ReadBox when evaluating checkPlatform(), read by Jump().
    [HideInInspector] public bool platformAbove = false;
    [HideInInspector] public Collider2D platformAboveCollider = null;

    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        basePos = transform.position;
    }

    void Update()
    {
        jumpTimer += Time.deltaTime;
        handleAnims();

        Ray2D ray = new Ray2D(transform.position, Vector2.down);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 2f);
        Debug.DrawRay(ray.origin, ray.direction * 1.5f, Color.red);

        if (hit.collider != null && hit.collider.tag == "Ground" && moveState == "jumping" && jumpTimer > jumpDelay)
        {
            moveState = "idle";
            timer = 1;
        }
    }

    // -----------------------------------------------------------------------
    // checkPlatform() — called by ReadBox when it parses checkPlatform()
    // Casts a ray upward and returns true if a ThroughPlatform is above the player.
    // Also stores the collider so Jump() can temporarily disable it.
    // -----------------------------------------------------------------------
    public bool CheckPlatformAbove()
    {
        platformAbove = false;
        platformAboveCollider = null;

        // Cast upward, ignoring the player's own collider
        Collider2D playerCol = GetComponent<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[10];
        int count = Physics2D.Raycast(transform.position, Vector2.up, filter, hits, platformCheckDistance);

        Debug.DrawRay(transform.position, Vector2.up * platformCheckDistance, Color.cyan, 0.5f);

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == playerCol) continue;
            if (h.collider.CompareTag("ThroughPlatform"))
            {
                platformAbove = true;
                platformAboveCollider = h.collider;
                Debug.Log("[Movement] ThroughPlatform detected above: " + h.collider.gameObject.name);
                return true;
            }
        }

        Debug.Log("[Movement] No ThroughPlatform detected above.");
        return false;
    }

    // -----------------------------------------------------------------------
    // Jump — if platformAbove is set, temporarily disables the platform
    // collider so the player passes through upward, then re-enables it
    // once the player is above the platform so they can land on top.
    // -----------------------------------------------------------------------
    public void Jump(float height)
    {
        jumpTimer = 0f;
        timer = 1f;
        GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, 0);
        GetComponent<Rigidbody2D>().AddForce(new Vector2(0, height), ForceMode2D.Impulse);
        moveState = "jumping";

        if (platformAbove && platformAboveCollider != null)
            StartCoroutine(PassThroughPlatform(platformAboveCollider));

        // Clear after consuming — avoids accidentally reusing it on the next jump
        platformAbove = false;
        platformAboveCollider = null;
    }

    // Disables the platform collider, waits until the player is above it, then re-enables it.
    private IEnumerator PassThroughPlatform(Collider2D platform)
    {
        platform.enabled = false;
        Debug.Log("[Movement] Platform collider disabled for pass-through.");

        // Wait until the player's bottom edge is above the platform's top edge
        Collider2D playerCol = GetComponent<Collider2D>();
        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;

            float playerBottom = playerCol.bounds.min.y;
            float platformTop  = platform.bounds.max.y;

            if (playerBottom > platformTop)
                break;

            yield return null;
        }

        platform.enabled = true;
        Debug.Log("[Movement] Platform collider re-enabled — player is above.");
    }

    // -----------------------------------------------------------------------
    // Spike handling
    // -----------------------------------------------------------------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("spike"))
            HitSpike();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("spike"))
            HitSpike();
    }

    void HitSpike()
    {
        if (readBox != null)
            readBox.breakAndReset();
        else
            Debug.LogWarning("[Movement] Spike hit detected but no ReadBox assigned. Assign it in the Inspector.");
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
            transform.position = pos;
        }
    }

    public void SetY(float y)
    {
        Debug.Log("attempting y set");
        if (canTele)
        {
            Debug.Log(moveState);
            Vector3 pos = transform.position;
            pos.y += (y - pos.y);
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
            transform.Translate(direction * step);
            moved += step;
            yield return null;
        }
        moveState = "idle";
    }
}
