using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private Image FadeOutImage;
    [SerializeField] private GameObject EndingUI;
    [SerializeField] private Button EndGame;
    [SerializeField] private Button RestartButton;

    public float totalPlayTime { get; private set; }
    public TextMeshProUGUI timeText;
    private bool isEnd;


    private void Awake()
    {
        EndGame.onClick.AddListener(() => Application.Quit());
        RestartButton.onClick.AddListener(ReloadCurrentScene);
        isEnd = false;
        totalPlayTime = 0;
    }
    private void Update()
    {
        if (isEnd == false)
        {
        totalPlayTime += Time.unscaledDeltaTime;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(FadeOutAndShowEnding());
            Cursor.visible = true;
        }
    }

    public void ReloadCurrentScene()
    {
        // 현재 활성화된 씬의 빌드 인덱스를 가져옵니다.
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 가져온 빌드 인덱스를 사용해 씬을 다시 로드합니다.
        SceneManager.LoadScene(currentSceneIndex);
    }

    private IEnumerator FadeOutAndShowEnding()
    {
        isEnd = true;
        FormatTime();
        float fadeDuration = 2f;
        float elapsedTime = 0f;
        Color color = FadeOutImage.color;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            FadeOutImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        FadeOutImage.color = color;
        
        EndingUI.SetActive(true);
    }
    private void FormatTime()
    {
        int minutes = Mathf.FloorToInt(totalPlayTime / 60);
        int seconds = Mathf.FloorToInt(totalPlayTime % 60);
        timeText.text = string.Format("총 걸린 시간: {0:00}:{1:00}", minutes, seconds);
    }

}
