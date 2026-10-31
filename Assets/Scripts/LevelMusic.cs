using UnityEngine;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using System.Collections;
public class LevelMusic : MonoBehaviour
{

    string musicPath = "event:/Music_1";

    EventInstance lvlMusic;


    private void Start()
    {
        StartCoroutine(LoadAndPlay());
    }


    IEnumerator LoadAndPlay()
    {
        yield return new WaitUntil(() => RuntimeManager.HaveAllBanksLoaded);

        lvlMusic = RuntimeManager.CreateInstance(musicPath);

        lvlMusic.start();

    }

    

}
