using Assets.SimpleLocalization;
using UnityEngine;

/// <summary>
/// Инициализация систем до загрузки сцены: файловые сохранения (FBPP) и локализация.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        FBPP.Start(new FBPPConfig
        {
            SaveFileName = "saveData.txt",
            AutoSaveData = true,
            ScrambleSaveData = true
        });

        LocalizationManager.Read();
    }
}
