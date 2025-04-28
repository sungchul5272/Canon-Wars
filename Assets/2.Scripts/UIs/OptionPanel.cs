using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class OptionPanel : MonoBehaviour
{
    [SerializeField] Slider _masterSlider;
    [SerializeField] Text _masterPercentText;
    [SerializeField] Slider _sfxSlider;
    [SerializeField] Text _sfxPercentText;
    [SerializeField] Dropdown resolutionDropdown;
    [SerializeField] AudioMixer audioMixer;

    [SerializeField] Button _confirmBtn;
    [SerializeField] Button[] _closeBtns;
 
    float _prevMasterVolume;
    float _prevSfxVolume;
    int _prevResolutionIndex;
    int _defaultResolutionIndex = 0;

    Resolution[] _resolutions;

    void Start()
    {
        _masterSlider.onValueChanged.AddListener(OnChangeMasterVolume);
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
        // 해상도 먼저 초기화
        InitResolutionOptions();

        // 저장값 불러오기
        _prevMasterVolume = PlayerPrefs.GetFloat("masterVolume", 0.7f);
        _prevSfxVolume = PlayerPrefs.GetFloat("sfxVolume", 0.7f);
        _prevResolutionIndex = PlayerPrefs.GetInt("resolutionIndex", _defaultResolutionIndex);

        // UI 반영
        _masterSlider.value = _prevMasterVolume;
        _sfxSlider.value = _prevSfxVolume;
        resolutionDropdown.value = _prevResolutionIndex;

        // 퍼센트 텍스트 초기화
        UpdateVolumeText();

        //ApplyMixerVolumes();                              오디오 리소스 확보후 구현
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

        //audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
    }

    public void OnChangeSFXVolume(float value)
    {
        int percent = Mathf.RoundToInt(value * 100);
        _sfxPercentText.text = percent + "%";

        //audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }

    public void OnChangeResolution(int index)
    {
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.Windowed);
    }

    public void OnClickConfirm()
    {
        PlayerPrefs.SetFloat("masterVolume", _masterSlider.value);
        PlayerPrefs.SetFloat("sfxVolume", _sfxSlider.value);
        PlayerPrefs.SetInt("resolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    public void OnClickClose()
    {
        _masterSlider.value = _prevMasterVolume;
        _sfxSlider.value = _prevSfxVolume;
        resolutionDropdown.value = _prevResolutionIndex;

        UpdateVolumeText();  // 텍스트도 복원

        //ApplyMixerVolumes();

        gameObject.SetActive(false);
    }

    void ApplyMixerVolumes()
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(_masterSlider.value) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(_sfxSlider.value) * 20);
    }

    void UpdateVolumeText()
    {
        _masterPercentText.text = Mathf.RoundToInt(_masterSlider.value * 100) + "%";
        _sfxPercentText.text = Mathf.RoundToInt(_sfxSlider.value * 100) + "%";
    }
}
