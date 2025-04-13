using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Persistence
{    
    public static class GameSaveLoadSystem
    {
        const string DEFAULT_SCENE_NAME = "Testing Scene";
        const string NEW_GAME_NAME = "New Game";

        public static GameData gameData
        {
            get
            {
                if (gameData != null) return gameData;
                return gameData = new GameData(NEW_GAME_NAME, DEFAULT_SCENE_NAME);
            }
            set => gameData = value;
        }


        static IDataService<GameData> dataService
        {
            get
            {
                if (dataService != null) return dataService;
                return new GameFileDataService(new BinarySerializer());
            }
            set => dataService = value;
        }

        public static void NewGame()
        {
            gameData = new GameData(NEW_GAME_NAME, DEFAULT_SCENE_NAME);
            SceneManager.LoadScene(gameData.CurrentLevelName);
        }

        public static void SaveGame() => dataService.Save(gameData);
        public static void DeleteGame(string gameName) => dataService.Delete(gameName);
        public static void ReloadGame() => dataService.Load(gameData.Name);
        public static void LoadGame(string gameName)
        {
            gameData = dataService.Load(gameName);

            if (String.IsNullOrWhiteSpace(gameName))
                gameData.CurrentLevelName = DEFAULT_SCENE_NAME;

            SceneManager.LoadScene(gameData.CurrentLevelName);
        }
    }


    [Serializable]
    public class GameData : ISaveData
    {
        public string Name { get; set; }
        public string CurrentLevelName;

        public GameData(string name, string currentLevelName)
        {
            Name = name;
            CurrentLevelName = currentLevelName;
        }

    }
}
