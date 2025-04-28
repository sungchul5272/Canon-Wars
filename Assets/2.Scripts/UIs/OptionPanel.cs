using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class OptionPanel : MonoBehaviour
{
    [Header("슬라이더 + 텍스트")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Text _masterPercentText;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Text _bgmPercentText;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Text _sfxPercentText;

    [Header("해상도")]
    [SerializeField] private Dropdown resolutionDropdown;

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("버튼")]
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button[] _closeBtns;

    private float _prevMasterVolume;
    private float _prevBgmVolume;
    private float _prevSfxVolume;
    private int _prevResolutionIndex;
    private int _defaultResolutionIndex = 0;

    private Resolution[] _resolutions;

    void Start()
    {
        _masterSlider.onValueChanged.AddListener(OnChangeMasterVolume);
        _bgmSlider.onValueChanged.AddListener(OnChangeBGMVolume);
        _sfxSlider.onValueChanged.AddListener(OnChangeSFXVolume);
        resolutionDropdown.onValueChanged.AddListener(OnChangeResolution);

        _confirmBtn.onClick.AddListener(OnClickConfirm);
        foreach (Button btn in _closeBtns)
        {
            btn.onClick.AddListener(OnClickClose);
        }
    }

    void OnEnable()
    {
        InitResolutionOptions();

        _prevMasterVolume = PlayerPrefs.GetFloat("masterVolume", 0.7f);
        _prevBgmVolume = PlayerPrefs.GetFloat("bgmVolume", 0.7f);
        _prevSfxVolume = PlayerPrefs.GetFloat("sfxVolume", 0.7f);
        _prevResolutionIndex = PlayerPrefs.GetInt("resolutionIndex", _defaultResolutionIndex);

        _masterSlider.value = _prevMasterVolume;
        _bgmSlider.value = _prevBgmVolume;
        _sfxSlider.value = _prevSfxVolume;
        resolutionDropdown.value = _prevResolutionIndex;

        UpdateVolumeText();

        if (audioMixer != null)
        {
            ApplyMixerVolumes();
        }
        else
        {
            Debug.LogWarning("[OptionPanel] AudioMixer가 연결되지 않았습니다.");
        }
    }

    private void InitResolutionOptions()
    {
        _resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        for (int i = 0; i < _resolutions.Length; i++)
        {
            string resStr = _resolutions[i].width + " x " + _resolutions[i].height;
            options.Add(resStr);

            if (_resolutions[i].width == 1920 && _resolutions[i].height == 1080)
                _defaultResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
    }

    public void OnChangeMasterVolume(float value)
    {
        int percent = Mathf.RoundToInt(value * 100);
        _masterPercentText.text = percent + "%";

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        }
    }

    public void OnChangeBGMVolume(float value)
    {
        int percent = Mathf.RoundToInt(value * 100);
        _bgmPercentText.text = percent + "%";

        if (audioMixer != null)
        {
            audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
        }
    }

    public void OnChangeSFXVolume(float value)
    {
        int percent = Mathf.RoundToInt(value * 100);
        _sfxPercentText.text = percent + "%";

        if (audioMixer != null) 
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        }
    }

    public void OnChangeResolution(int index)
    {
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.Windowed);
    }

    public void OnClickConfirm()
    {
        PlayerPrefs.SetFloat("masterVolume", _masterSlider.value);
        PlayerPrefs.SetFloat("bgmVolume", _bgmSlider.value);
        PlayerPrefs.SetFloat("sfxVolume", _sfxSlider.value);
        PlayerPrefs.SetInt("resolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    public void OnClickClose()
    {
        _masterSlider.value = _prevMasterVolume;
        _bgmSlider.value = _prevBgmVolume;
        _sfxSlider.value = _prevSfxVolume;
        resolutionDropdown.value = _prevResolutionIndex;

        UpdateVolumeText();

        if (audioMixer != null)
        {
            ApplyMixerVolumes();
        }

        gameObject.SetActive(false);
    }

    void ApplyMixerVolumes()
    {
        if (audioMixer != null) 
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(_masterSlider.value) * 20);
            audioMixer.SetFloat("BGMVolume", Mathf.Log10(_bgmSlider.value) * 20);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(_sfxSlider.value) * 20);
        }
    }

    void UpdateVolumeText()
    {
        _masterPercentText.text = Mathf.RoundToInt(_masterSlider.value * 100) + "%";
        _bgmPercentText.text = Mathf.RoundToInt(_bgmSlider.value * 100) + "%";
        _sfxPercentText.text = Mathf.RoundToInt(_sfxSlider.value * 100) + "%";
    }
}
