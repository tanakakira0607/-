using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 10;
    private int currentHp;

    public Slider hpSlider;

    // ダメージ演出用
    public Image flashImage;
    public float flashDuration = 0.2f;

    void Start()
    {

        Debug.Log(hpSlider);
        Debug.Log(flashImage);
        currentHp = maxHp;

        hpSlider.maxValue = maxHp;
        hpSlider.value = currentHp;

        // 最初は透明
        flashImage.color = new Color(1f, 0f, 0f, 0f);
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        hpSlider.value = currentHp;

        // 赤フラッシュ
        StartCoroutine(FlashCoroutine());

        if (currentHp == 0)
        {
            Debug.Log("ゲームオーバー！");
            Time.timeScale = 0f;
        }
    }

    IEnumerator FlashCoroutine()
    {
        flashImage.color = new Color(1f, 0f, 0f, 0.3f);

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                0.3f,
                0f,
                timer / flashDuration
            );

            flashImage.color =
                new Color(1f, 0f, 0f, alpha);

            yield return null;
        }

        flashImage.color = new Color(1f, 0f, 0f, 0f);
    }
}