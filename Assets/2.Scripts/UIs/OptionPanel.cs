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
        _masterSlider.onValueChanged.AddListener(value =>
        {
            UpdateVolumeText(_masterPercentText, value);
            SetMixerVolume("MasterVolume", value);
        });

        _bgmSlider.onValueChanged.AddListener(value =>
        {
            UpdateVolumeText(_bgmPercentText, value);
            SetMixerVolume("BGMVolume", value);
        });

        _sfxSlider.onValueChanged.AddListener(value =>
        {
            UpdateVolumeText(_sfxPercentText, value);
            SetMixerVolume("SFXVolume", value);
        });

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

        ApplyMixerVolumes();
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

    public void OnChangeResolution(int index)
    {
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.Windowed);
    }

    public void OnClickConfirm()
    {
        SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetFloat("masterVolume", _masterSlider.value);
        PlayerPrefs.SetFloat("bgmVolume", _bgmSlider.value);
        PlayerPrefs.SetFloat("sfxVolume", _sfxSlider.value);
        PlayerPrefs.SetInt("resolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();

        gameObject.SetActive(false);
    }

    public void OnClickClose()
    {
        SoundManager.Instance.PlayButtonClick();
        _masterSlider.value = _prevMasterVolume;
        _bgmSlider.value = _prevBgmVolume;
        _sfxSlider.value = _prevSfxVolume;
        resolutionDropdown.value = _prevResolutionIndex;

        ApplyMixerVolumes();
        gameObject.SetActive(false);
    }

    void ApplyMixerVolumes()
    {
        SetMixerVolume("MasterVolume", _masterSlider.value);
        SetMixerVolume("BGMVolume", _bgmSlider.value);
        SetMixerVolume("SFXVolume", _sfxSlider.value);

        UpdateVolumeText(_masterPercentText, _masterSlider.value);
        UpdateVolumeText(_bgmPercentText, _bgmSlider.value);
        UpdateVolumeText(_sfxPercentText, _sfxSlider.value);
    }

    void SetMixerVolume(string paramName, float sliderValue)
    {
        if (audioMixer != null)
        {
            float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat(paramName, volume);
        }
    }

    void UpdateVolumeText(Text textField, float value)
    {
        textField.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
