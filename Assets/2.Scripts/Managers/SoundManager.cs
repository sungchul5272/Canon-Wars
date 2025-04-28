using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip _startSceneBGM;
    [SerializeField] private AudioClip _lobbySceneBGM;
    [SerializeField] private AudioClip[] _ingameBGMs;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] private AudioClip _tankMoveClip;
    [SerializeField] private AudioClip _missileFireClip;
    [SerializeField] private AudioClip _missileExplosionClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _loseClip;
    [SerializeField] private AudioClip _drawClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayStartSceneBGM()
    {
        PlayBGM(_startSceneBGM);
    }

    public void PlayLobbySceneBGM()
    {
        PlayBGM(_lobbySceneBGM);
    }

    public void PlayIngameBGM(int mapIndex)
    {
        if (_ingameBGMs != null && mapIndex >= 0 && mapIndex < _ingameBGMs.Length)
        {
            PlayBGM(_ingameBGMs[mapIndex]);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Àß¸øµÈ ¸Ê ÀÎµ¦½ºÀÔ´Ï´Ù.");
        }
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlayButtonClick()
    {
        PlaySFX(_buttonClickClip);
    }

    public void PlayTankMove()
    {
        PlaySFX(_tankMoveClip);
    }

    public void PlayFire()
    {
        PlaySFX(_missileFireClip);
    }

    public void PlayExplosion()
    {
        PlaySFX(_missileFireClip);
    }

    public void PlayWin()
    {
        PlaySFX(_winClip);
    }

    public void PlayLose()
    {
        PlaySFX(_loseClip);
    }

    public void PlayDraw()
    {
        PlaySFX(_drawClip);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }


    public void SetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        _sfxSource.volume = volume;
    }
}
