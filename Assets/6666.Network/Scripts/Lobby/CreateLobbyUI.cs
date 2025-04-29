using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyUI : MonoBehaviour
{
    public Toggle privateToggle;
    public TMP_Dropdown gameModeDropDown;
    public TMP_InputField nameInputField;
    public Button createButton;
    public Button backButton;

    void Start()
    {
        LobbyManager instance = LobbyManager.Instance;

        // 로비 이름 입력
        nameInputField.onValueChanged.AddListener((_) =>
        {
            // 텍스트가 있으면 버튼 활성화
            createButton.interactable = !string.IsNullOrEmpty(nameInputField.text);
        });

        // 로비 생성
        createButton.onClick.AddListener(() =>
        {
            EGameMode gameMode = (EGameMode)Enum.Parse(typeof(EGameMode), $"Mode{gameModeDropDown.options[gameModeDropDown.value].text}");
            instance.CreateLobby(gameMode, nameInputField.text, privateToggle.isOn, string.Empty);
        });
    }

    void OnEnable()
    {
        nameInputField.text = string.Empty;
        createButton.interactable = false;

        // 뒤로가기
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            LobbyManager.Instance.startLobbyUI.gameObject.SetActive(true);
        });
    }
}
