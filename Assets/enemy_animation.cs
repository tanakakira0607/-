using UnityEngine;

public class enemy_animation : MonoBehaviour
{

    Rigidbody2D rb;

    public Sprite[] idlSprites;

    float time = 0;
    int idx = 0;
    SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.unscaledDeltaTime;

        if (time > 0.3f)
        {
            time = 0;

            // ƒWƒƒƒ“ƒv’†
           
            // ‘Ò‹@’†
            
                spriteRenderer.sprite =
                    idlSprites[idx];
            

            idx = 1 - idx;
        }
    }
}
