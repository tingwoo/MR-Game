using Unity.Netcode;
using UnityEngine;

public class NetworkedMusicPlayer : NetworkBehaviour
{
    [SerializeField] private AudioClip backgroundMusic;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            
            audioSource.loop = true; 
            
            audioSource.Play();
        }
    }
    
    // 選用：當物件被銷毀時停止播放
    public override void OnNetworkDespawn()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}