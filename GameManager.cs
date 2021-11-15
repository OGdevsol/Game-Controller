using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    #region GlobalInstance

    public static GameManager localInstance;

    public static GameManager Instance
    {
        get
        {
            localInstance = GameObject.FindObjectOfType<GameManager>();
            return localInstance;
        }
    }

    #endregion

    #region UserInterface

    [Space(5)] [Header("---- UI References")]
    public GameObject[] reference;
    public GameObject rankUpPanel;
    public Text rankUp;
    [Space(3)]
    public GameObject victoryEffect;
    public GameObject defeatEffect;
    [HideInInspector] public int currentLevel = 0;
    private int levelCount;
    private int selectedLevel;

    [Space(10)] public Text levelNumber;
    public Text totalKill;

    
    [Space(5)] [Header("---- Scripts Reference")]
    public playercontroller playerController;
    private static int _levelCountStored;


    [Space(5)] [Header("---- Bomb Diffuse")]
    public Text bombSliderValue;
    public Slider bombSlider;
    public GameObject bombDiffuseBar;

    public GameObject bombDiffuseStatus;
    public GameObject flagModeStatus;

    public Text[] rankUpdTexts;

    #endregion

    #region Initlization

    private void Awake()
    {

        Time.timeScale = 1;

        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            currentLevel = PlayerPrefs.GetInt("FlagLevel");
            flagModeStatus.SetActive(true);
            bombDiffuseStatus.SetActive(false);
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            currentLevel = PlayerPrefs.GetInt("BombLevel");
            bombDiffuseStatus.SetActive(true);
            flagModeStatus.SetActive(false);
        }
        
        Debug.Log("Current Level "+currentLevel);
        
        playerController.transform.position =
            LevelsController.Instance.levelData[currentLevel - 1].playerSpwanPosition.position;
        playerrotate.Instance.rotationX = LevelsController.Instance.levelData[currentLevel - 1]
            .playerSpwanPosition.eulerAngles.y;
        weaponselector.Instance.grenade = 100;

        totalKill.text = LevelsController.Instance.levelData[currentLevel - 1].enemiesType.Count.ToString();
        
        EnableReference(0);
        levelNumber.text = currentLevel.ToString();

        
#if !UNITY_EDITOR
        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            Analyticsmanager.instance.LevelStartEvent("FML",currentLevel);
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            Analyticsmanager.instance.LevelStartEvent("BML",currentLevel);
        }

        AdsController.Instance.gameplay = true;
        AdsController.Instance.HideBanner();
        AdsController.Instance.HideLargeBanner();
#endif
        
}

    private void EnableReference(int index)
    {
        foreach (var item in reference)
        {
            item.SetActive(false);
        }
        reference[index].SetActive(true);
        
    }
    
    #endregion
    
    #region State Methods

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Pause()
    {
        EnableReference(1);
        Time.timeScale = 0;
#if !UNITY_EDITOR
            AdsController.Instance.Unity_InterstitialGame();
#endif
    }
    
    public void Resume()
    {
        Time.timeScale = 1;
        EnableReference(0);
    }
    
    public void Home()
    {
        StartCoroutine(LoadAsynchronously(1));
    }
    
    public void Next()
    {
        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            if (PlayerPrefs.GetInt("FlagLevel") >= 20)
            {
                PlayerPrefs.SetInt("FlagLevel",1); 
            }
            else
            {
                PlayerPrefs.SetInt("FlagLevel",currentLevel+1); 
            }
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            if (PlayerPrefs.GetInt("BombLevel") == 20)
            {
                PlayerPrefs.SetInt("BombLevel",1); 
            }
            else
            {
                PlayerPrefs.SetInt("BombLevel",currentLevel+1);
            }
        }
        StartCoroutine(LoadAsynchronously(2));
    }

    public void Reset()
    {
        InGameProperties.Instance.SfxVolumeOn();
        InGameProperties.Instance.MusicVoluneOn();
        InGameProperties.Instance.AutoShoot_Off();
    }
    
    #endregion
    
    #region Loading
    
    private IEnumerator LoadAsynchronously(int sceneIndex)
    {
        EnableReference(4);
        yield return new WaitForSecondsRealtime(1.5f);
        var async = SceneManager.LoadSceneAsync(sceneIndex);
        while (!async.isDone)
        {
            float progress = Mathf.Clamp01(async.progress / .9f);
            // LoadingBar.fillAmount = progress;
            // LoadingProgress.text = (progress * 100f).ToString("0") + "%";
            yield return null;
        }
    }

    #endregion
    
    #region Victory / Defeat!
    
    private  void FlagStats()
    {
        if (PlayerPrefs.HasKey("FlagLevelPurchased")) return;
        int curr = PlayerPrefs.GetInt("UnlockFlag");
        if (currentLevel > curr)
        {
            PlayerPrefs.SetInt("UnlockFlag", (curr + 1));
        }
        selectedLevel = PlayerPrefs.GetInt("UnlockFlag");
        if (currentLevel < 20)
        {
            PlayerPrefs.SetInt("SelectedFlagLevel",selectedLevel);
        }

    }
    
    private  void BombDiffuseStats()
    {
        if (PlayerPrefs.HasKey("BombLevelPurchased")) return;
        int curr = PlayerPrefs.GetInt("UnlockBomb");
        if (currentLevel > curr)
        {
            PlayerPrefs.SetInt("UnlockBomb", (curr + 1));
        }
        selectedLevel = PlayerPrefs.GetInt("UnlockBomb");
        if (currentLevel < 20)
        {
            PlayerPrefs.SetInt("SelectedBombLevel",selectedLevel);
        }

    }

    ///////////////////////////////// Level Complete
    
    public void GameComplete()
    {
        
        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            FlagStats();
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            BombDiffuseStats();
        }
        StartCoroutine(VictoryPanel());
        PlayerPrefs.SetInt("Cash",PlayerPrefs.GetInt("Cash")+LevelsController.Instance.levelCash[currentLevel-1]);
        
    }
    
    private IEnumerator VictoryPanel()
    {
        yield return new WaitForSeconds(0.2f);
        victoryEffect.SetActive(true);
        SoundController.instance.audioMusic.enabled = false;
        SoundController.instance.playFromPool(AudioType.LevelComplete);
        yield return new WaitForSeconds(3f);
        victoryEffect.SetActive(false);
        RankUpSystem();
        
        
#if !UNITY_EDITOR
        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            Analyticsmanager.instance.LevelCompleteEvent("FML",currentLevel);
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            Analyticsmanager.instance.LevelCompleteEvent("BML",currentLevel);
        }
        AdsController.Instance.Unity_InterstitialGame();
#endif
    }
    
    ///////////////////////////////// Level Fail
    
    public void GameFail()
    {
        StartCoroutine(DefeatPanel());
    }
    
    private IEnumerator DefeatPanel()
    {
        yield return new WaitForSeconds(0.2f);
        SoundController.instance.playFromPool(AudioType.LevelFail);
        defeatEffect.SetActive(true);
        yield return new WaitForSeconds(3f);
        defeatEffect.SetActive(false);
        EnableReference(3);
        
#if !UNITY_EDITOR
        if (PlayerPrefs.GetString("Mode") == "Flag")
        {
            Analyticsmanager.instance.LevelFailedEvent("FML",currentLevel);
        }
        else if (PlayerPrefs.GetString("Mode") == "BombDiffuse")
        {
            Analyticsmanager.instance.LevelFailedEvent("BML",currentLevel);
        }
        AdsController.Instance.Unity_InterstitialGame();
#endif
        Time.timeScale = 0;
    }

    #endregion

    #region Rankup

    private int rankUpValue;
    private void RankUpSystem()
    {
        if (PlayerPrefs.GetInt("TotalBodyShoot") % 5 == 0 && PlayerPrefs.GetString("Mode") == "Flag")
        {
            PlayerPrefs.SetInt("RankUp", PlayerPrefs.GetInt("RankUp") + 1);
            rankUpValue = PlayerPrefs.GetInt("RankUp");
            
            SoundController.instance.playFromPool(AudioType.Realod);
            
            rankUpPanel.SetActive(true);
            rankUp.text = $"{rankUpValue}";
            RankUpdTags();
            Invoke(nameof(RankUpDeactivate),2f);
        }
        else
        {
            EnableReference(2);
            Time.timeScale = 0;
        }
    }

    public void RankUpDeactivate()
    {
        rankUpPanel.SetActive(false);
        EnableReference(2);
        Time.timeScale = 0;
    }

    private void RankUpdTags()
    {
        switch (rankUpValue)
        {
            case 1:
                rankUpdTexts[0].text = rankUpdTexts[1].text = $"BRONZE";
                break;
            case 2:
                rankUpdTexts[0].text = rankUpdTexts[1].text = $"SILVER RANK";
                break;
            case 3:
                rankUpdTexts[0].text = rankUpdTexts[1].text = $"GOLD RANK";
                break;
            case 4:
                rankUpdTexts[0].text = rankUpdTexts[1].text = $"PLATINUM RANK";
                break;
            default:
                rankUpdTexts[0].text = rankUpdTexts[1].text = $"DIAMOND RANK";
                break;
        }
    }
    

    #endregion
    
}
