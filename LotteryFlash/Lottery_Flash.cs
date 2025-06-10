using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LotteryFlash : MonoBehaviour
{
    public Button spinButton; // 抽奖按钮
    public TextMeshProUGUI resultText; // 结果显示文本
    public Sprite[] prizeSprites; // 物品Sprite数组
    public AudioSource flashSound; // 切换音效（可选）
    public AudioSource winSound; // 中奖音效（可选）
    public float flashDuration = 5f; // 动画总时长
    public float flashInterval = 0.03f; // 快速闪过间隔（更流畅）
    private Image prizeImage; // 显示物品的Image组件
    private bool isFlashing = false; // 是否正在抽奖
    private string[] prizeNames = { "Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Grand Prize" }; // 奖项名称
    private int[] weights = { 30, 20, 15, 15, 10, 5, 3, 2 }; // 奖项权重

    void Start()
    {
        prizeImage = GetComponent<Image>();
        if (!prizeImage) { Debug.LogError("PrizeImage 缺少 Image 组件！"); return; }
        if (!spinButton) { Debug.LogError("SpinButton 未分配！"); return; }
        if (!resultText) { Debug.LogError("ResultText 未分配！"); return; }
        if (!resultText.GetComponent<CanvasGroup>()) { Debug.LogError("ResultText 缺少 CanvasGroup 组件！"); return; }
        if (prizeSprites.Length != prizeNames.Length || prizeSprites.Length != weights.Length)
        { Debug.LogError("PrizeSprites、PrizeNames 和 Weights 数组长度必须一致！"); return; }

        spinButton.onClick.AddListener(StartFlash);
        resultText.text = "点击开始抽奖！";
        prizeImage.sprite = prizeSprites[0];
        resultText.GetComponent<CanvasGroup>().alpha = 0f;
        prizeImage.color = Color.white;
    }

    public void StartFlash()
    {
        if (!isFlashing && prizeImage && resultText)
        {
            isFlashing = true;
            spinButton.interactable = false;
            resultText.text = "抽奖中...";
            StartCoroutine(FlashAnimation());
        }
        else
        {
            Debug.LogError("无法开始抽奖：PrizeImage 或 ResultText 未分配！");
        }
    }

    IEnumerator FlashAnimation()
    {
        float elapsedTime = 0f;
        int prizeIndex = GetWeightedRandomIndex();
        float currentInterval = flashInterval;

        // 快速闪过阶段（前60%时间）
        while (elapsedTime < flashDuration * 0.6f)
        {
            int randomIndex = Random.Range(0, prizeSprites.Length);
            prizeImage.sprite = prizeSprites[randomIndex];
            if (flashSound && !flashSound.isPlaying) flashSound.Play();
            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // 减速阶段（后40%时间，平滑调整间隔）
        float slowDownTime = flashDuration * 0.4f;
        float maxInterval = flashInterval * 6f; // 最大间隔
        while (elapsedTime < flashDuration)
        {
            int randomIndex = Random.Range(0, prizeSprites.Length);
            prizeImage.sprite = prizeSprites[randomIndex];
            if (flashSound && !flashSound.isPlaying) flashSound.Play();
            currentInterval = Mathf.Lerp(flashInterval, maxInterval, Mathf.Pow((elapsedTime - flashDuration * 0.6f) / slowDownTime, 2f));
            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // 最终停止（淡入+轻微抖动）
        prizeImage.color = new Color(1f, 1f, 1f, 0f);
        prizeImage.sprite = prizeSprites[prizeIndex];
        if (winSound) winSound.Play();

        // 淡入并抖动
        yield return StartCoroutine(FadeInPrize());

        // 缩放动画
        StartCoroutine(ScaleAnimation());

        // 显示结果
        resultText.text =  prizeNames[prizeIndex];
        Debug.Log($"中奖索引: {prizeIndex}, Sprite: {prizeSprites[prizeIndex].name}, 名称: {prizeNames[prizeIndex]}");
        StartCoroutine(FadeInResult());
        PlayerPrefs.SetString("LastPrize", prizeNames[prizeIndex]);
        PlayerPrefs.Save();

        isFlashing = false;
        spinButton.interactable = true;
    }

    // 淡入最终物品（带轻微抖动）
    IEnumerator FadeInPrize()
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Vector3 originalPos = prizeImage.transform.localPosition;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            t = t * t * (3f - 2f * t); // EaseInOut 曲线
            prizeImage.color = new Color(1f, 1f, 1f, t);
            // 轻微抖动
            prizeImage.transform.localPosition = originalPos + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            yield return null;
        }
        prizeImage.color = Color.white;
        prizeImage.transform.localPosition = originalPos;
    }

    // 缩放动画（优化缓动）
    IEnumerator ScaleAnimation()
    {
        float scaleTime = 0.4f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.2f;

        for (int i = 0; i < 3; i++)
        {
            elapsed = 0f;
            while (elapsed < scaleTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleTime;
                t = t * t * (3f - 2f * t); // EaseInOut 曲线
                prizeImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < scaleTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleTime;
                t = t * t * (3f - 2f * t);
                prizeImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
        }
        prizeImage.transform.localScale = originalScale;
    }

    // 淡入结果文本
    IEnumerator FadeInResult()
    {
        CanvasGroup canvasGroup = resultText.GetComponent<CanvasGroup>();
        if (!canvasGroup) { Debug.LogError("CanvasGroup 缺失于 ResultText！"); yield break; }
        float fadeTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            t = t * t * (3f - 2f * t); // EaseInOut 曲线
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    // 加权随机选择
    int GetWeightedRandomIndex()
    {
        int totalWeight = 0;
        foreach (int weight in weights) totalWeight += weight;
        int randomValue = Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Length; i++)
        {
            if (randomValue < weights[i]) return i;
            randomValue -= weights[i];
        }
        return 0;
    }
}