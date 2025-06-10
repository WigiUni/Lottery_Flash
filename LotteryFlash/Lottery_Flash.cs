using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LotteryFlash : MonoBehaviour
{
    public Button spinButton; // �齱��ť
    public TextMeshProUGUI resultText; // �����ʾ�ı�
    public Sprite[] prizeSprites; // ��ƷSprite����
    public AudioSource flashSound; // �л���Ч����ѡ��
    public AudioSource winSound; // �н���Ч����ѡ��
    public float flashDuration = 5f; // ������ʱ��
    public float flashInterval = 0.03f; // ���������������������
    private Image prizeImage; // ��ʾ��Ʒ��Image���
    private bool isFlashing = false; // �Ƿ����ڳ齱
    private string[] prizeNames = { "Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Grand Prize" }; // ��������
    private int[] weights = { 30, 20, 15, 15, 10, 5, 3, 2 }; // ����Ȩ��

    void Start()
    {
        prizeImage = GetComponent<Image>();
        if (!prizeImage) { Debug.LogError("PrizeImage ȱ�� Image �����"); return; }
        if (!spinButton) { Debug.LogError("SpinButton δ���䣡"); return; }
        if (!resultText) { Debug.LogError("ResultText δ���䣡"); return; }
        if (!resultText.GetComponent<CanvasGroup>()) { Debug.LogError("ResultText ȱ�� CanvasGroup �����"); return; }
        if (prizeSprites.Length != prizeNames.Length || prizeSprites.Length != weights.Length)
        { Debug.LogError("PrizeSprites��PrizeNames �� Weights ���鳤�ȱ���һ�£�"); return; }

        spinButton.onClick.AddListener(StartFlash);
        resultText.text = "�����ʼ�齱��";
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
            resultText.text = "�齱��...";
            StartCoroutine(FlashAnimation());
        }
        else
        {
            Debug.LogError("�޷���ʼ�齱��PrizeImage �� ResultText δ���䣡");
        }
    }

    IEnumerator FlashAnimation()
    {
        float elapsedTime = 0f;
        int prizeIndex = GetWeightedRandomIndex();
        float currentInterval = flashInterval;

        // ���������׶Σ�ǰ60%ʱ�䣩
        while (elapsedTime < flashDuration * 0.6f)
        {
            int randomIndex = Random.Range(0, prizeSprites.Length);
            prizeImage.sprite = prizeSprites[randomIndex];
            if (flashSound && !flashSound.isPlaying) flashSound.Play();
            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // ���ٽ׶Σ���40%ʱ�䣬ƽ�����������
        float slowDownTime = flashDuration * 0.4f;
        float maxInterval = flashInterval * 6f; // �����
        while (elapsedTime < flashDuration)
        {
            int randomIndex = Random.Range(0, prizeSprites.Length);
            prizeImage.sprite = prizeSprites[randomIndex];
            if (flashSound && !flashSound.isPlaying) flashSound.Play();
            currentInterval = Mathf.Lerp(flashInterval, maxInterval, Mathf.Pow((elapsedTime - flashDuration * 0.6f) / slowDownTime, 2f));
            elapsedTime += currentInterval;
            yield return new WaitForSeconds(currentInterval);
        }

        // ����ֹͣ������+��΢������
        prizeImage.color = new Color(1f, 1f, 1f, 0f);
        prizeImage.sprite = prizeSprites[prizeIndex];
        if (winSound) winSound.Play();

        // ���벢����
        yield return StartCoroutine(FadeInPrize());

        // ���Ŷ���
        StartCoroutine(ScaleAnimation());

        // ��ʾ���
        resultText.text =  prizeNames[prizeIndex];
        Debug.Log($"�н�����: {prizeIndex}, Sprite: {prizeSprites[prizeIndex].name}, ����: {prizeNames[prizeIndex]}");
        StartCoroutine(FadeInResult());
        PlayerPrefs.SetString("LastPrize", prizeNames[prizeIndex]);
        PlayerPrefs.Save();

        isFlashing = false;
        spinButton.interactable = true;
    }

    // ����������Ʒ������΢������
    IEnumerator FadeInPrize()
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Vector3 originalPos = prizeImage.transform.localPosition;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            t = t * t * (3f - 2f * t); // EaseInOut ����
            prizeImage.color = new Color(1f, 1f, 1f, t);
            // ��΢����
            prizeImage.transform.localPosition = originalPos + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            yield return null;
        }
        prizeImage.color = Color.white;
        prizeImage.transform.localPosition = originalPos;
    }

    // ���Ŷ������Ż�������
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
                t = t * t * (3f - 2f * t); // EaseInOut ����
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

    // �������ı�
    IEnumerator FadeInResult()
    {
        CanvasGroup canvasGroup = resultText.GetComponent<CanvasGroup>();
        if (!canvasGroup) { Debug.LogError("CanvasGroup ȱʧ�� ResultText��"); yield break; }
        float fadeTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            t = t * t * (3f - 2f * t); // EaseInOut ����
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    // ��Ȩ���ѡ��
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