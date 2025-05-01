using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _moveLoopSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip _startSceneBGM;
    [SerializeField] private AudioClip _lobbySceneBGM;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] private AudioClip _tankMoveLoopClip;
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
    void Start()
    {
        ApplySavedVolumes();

        if (_moveLoopSource != null)
        {
            _moveLoopSource.clip = _tankMoveLoopClip;
            _moveLoopSource.loop = true;
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

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] 재생할 BGM이 없습니다.");
            return;
        }

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

    public void StartTankMoveLoop()
    {
        if (_moveLoopSource != null && !_moveLoopSource.isPlaying)
        {
            _moveLoopSource.clip = _tankMoveLoopClip;
            _moveLoopSource.loop = true;
            _moveLoopSource.Play();
        }
    }

    public void StopTankMoveLoop()
    {
        if (_moveLoopSource != null && _moveLoopSource.isPlaying)
        {
            _moveLoopSource.Stop();
        }
    }

    public void PlayFire()
    {
        PlaySFX(_missileFireClip);
    }

    public void PlayExplosion()
    {
        PlaySFX(_missileExplosionClip);
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

    void ApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat("masterVolume", 0.7f);
        float bgm = PlayerPrefs.GetFloat("bgmVolume", 0.7f);
        float sfx = PlayerPrefs.GetFloat("sfxVolume", 0.7f);

        AudioMixer mixer = _audioMixer;

        mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(master, 0.0001f, 1f)) * 20f);
        mixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(bgm, 0.0001f, 1f)) * 20f);
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(sfx, 0.0001f, 1f)) * 20f);
    }
}
