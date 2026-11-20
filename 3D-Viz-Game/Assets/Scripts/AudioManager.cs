using System;
using UnityEngine;

/*
To use: Call AudioManager.
Can play a clip of sfx, start/stop playing music.
Just drag the sound clips into designated sections.
The background musics are separate audio sources you need to add and drag in.
*/
public enum STATION
{
    CASHIER,
    COOK,
    DISHWASHING,
    STOCKER
};

public enum SOUND //adjust enum to your station's need!!!
{
    //Cashier

    //Cook

    //Dishwashing

    //Stocker
};


//Singleton Audio manager
[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class AudioManager : MonoBehaviour
{
    //Manager instance
    public static AudioManager Instance;

    public AudioSource source;
    [Header("Globals")]
    public AudioSource game_music;
    public AudioSource menu_music;
    [SerializeField] private StationAudioList UI;
    [Header("Stations")]
    [SerializeField] private StationAudioList[] stationList;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public static void PlayMusic(bool ismenu)
    {
        if (!Instance.game_music || !Instance.menu_music)
        {
            return;
        }

        if (ismenu)
        {
            Instance.game_music.Play();
        }
        else
        {
            Instance.menu_music.Play();
        }
    }

    public static void StopMusic(bool ismenu)
    {
        if (!Instance.game_music || !Instance.menu_music)
        {
            return;
        }

        if (ismenu)
        {
            Instance.game_music.Stop();
        }
        else
        {
            Instance.menu_music.Stop();
        }
    }

    public static void PlaySound(STATION station, SOUND soundType, float volume = 1)
    {
        int soundIndex = GetSoundIndex(station, soundType);
        Instance.source.PlayOneShot(Instance.stationList[(int)station].sounds[soundIndex], volume);
    }

    private static int GetSoundIndex(STATION station, SOUND sound)
    {
        int size = Instance.stationList[(int)station].sounds.Length;
        return (int)sound % size;
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        string[] stationNames = Enum.GetNames(typeof(STATION));
        Array.Resize(ref stationList, 4);

        for (int i = 0; i < 4; i++)
        {
            stationList[i].name = stationNames[i];
        }
    }
#endif
}

[Serializable]
public struct StationAudioList
{
    public AudioClip[] sounds { get => soundList; }
    [HideInInspector] public string name;
    [SerializeField] private AudioClip[] soundList;
}