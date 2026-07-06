using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    // 当前关卡编号
    // Level0填0，Level1填1……
    public int currentLevel;

    /// <summary>
    /// 解锁下一关
    /// </summary>
    public void UnlockNextLevel()
    {
        // 已经解锁到哪一关
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 0);

        // 如果当前关是玩家已解锁的最高关，则解锁下一关
        if (currentLevel >= unlockedLevel)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentLevel + 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LoadNextLevel()
    {
        string nextScene = "Level" + (currentLevel + 1);

        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// 完成当前关卡
    /// （推荐整个项目只调用这个函数）
    /// </summary>
    public void CompleteLevel()
    {
        UnlockNextLevel();
        LoadNextLevel();
    }
}