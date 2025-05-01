using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class StartSceneManager : MonoBehaviour
{
    [Header("User Interection Property")]
    [SerializeField] InputField _idField;
    [SerializeField] InputField _passwordField;
    [SerializeField] InputField _nickNameField;

    [Space(5)]
    [SerializeField] Button _loginBtn;
    [SerializeField] Button _signUpBtn;
    [SerializeField] Button _exitBtn;
    [SerializeField] Button _searchPWBtn;
    [SerializeField] Button _createAccountBtn;

    [Space(10)]
    [Header("SYSTEM")]
    [SerializeField] FirebaseManager _fm;

    [Space(10)]
    [Header("UI")]
    [SerializeField] Image _maskImage;

    [Space(10)]
    [SerializeField] GameObject _infoWindowObject;
    [SerializeField] Text _infoText;

    [Space(10)]
    [SerializeField] GameObject _createAccountWindow;


    void Start()
    {
        SoundManager.Instance.PlayStartSceneBGM();
        //메인 디폴트 화면의 Button Action 부여
        {
            //회원가입 Button.
            _signUpBtn.onClick.AddListener(

                delegate
                {
                    SoundManager.Instance.PlayButtonClick();
                    Check_Empty_BaseInputField(delegate
                    {
                        //password 찾기를 이용
                        _fm.Get_UserPW(_idField.text,
                            delegate
                            {

                                //이미 동일한 ID가 존재한다면.
                                if (!string.IsNullOrEmpty(_fm.userVO.UserID))
                                {
                                    _maskImage.raycastTarget = true;
                                    _infoWindowObject.SetActive(true);
                                    _infoText.text = "이미 존재하는 아이디야!! 다른 아이디를 선택해줘!";
                                }
                                //존재하지 않는다면 생성 화면 open
                                else
                                {
                                    _maskImage.raycastTarget = true;
                                    _createAccountWindow.SetActive(true);
                                }
                            });
                    });
                });


            _loginBtn.onClick.AddListener(
                delegate
                {
                    SoundManager.Instance.PlayButtonClick();
                    Check_Empty_BaseInputField(delegate
                    {
                        OnLoginClick();
                    });
                });

            _searchPWBtn.onClick.AddListener(
                delegate
                {
                    Check_Empty_BaseInputField(delegate
                    {
                        //password 찾기를 이용
                        _fm.Get_UserPW(_idField.text,
                            delegate
                            {
                                //이미 동일한 ID가 존재한다면.
                                if (!string.IsNullOrEmpty(_fm.userVO.UserID))
                                {
                                    _maskImage.raycastTarget = true;
                                    _infoWindowObject.SetActive(true);
                                    _infoText.text = $"비밀번호 : {_fm.userVO.UserPW}";
                                }
                                //존재하지 않는다면.
                                else
                                {
                                    _maskImage.raycastTarget = true;
                                    _infoWindowObject.SetActive(true);
                                    _infoText.text = "없는 ID야. 다시 확인해줘!";
                                }
                            });
                    });
                });


            _exitBtn.onClick.AddListener(OnExitClick);
        }

        //서브 팝업 화면 Button Action 부여.
        {
            _createAccountBtn.onClick.AddListener(
                delegate
                {
                    SoundManager.Instance.PlayButtonClick();
                    if (string.IsNullOrEmpty(_nickNameField.text))
                    {
                        _maskImage.raycastTarget = true;
                        _infoWindowObject.SetActive(true);
                        _infoText.text = "사용할 닉네임을 설정해줘!!";
                        return;
                    }

                    _fm.Create_UserAccount(_idField.text, _passwordField.text, _nickNameField.text);
                });
        }
    }


    /// <summary>
    /// 버튼 작동 전, ID InputField와 PW InputField의 string Empty를 체크.
    /// </summary>
    void Check_Empty_BaseInputField(Action action)
    {
        if (string.IsNullOrEmpty(_idField.text))
        {
            _maskImage.raycastTarget = true;
            _infoWindowObject.SetActive(true);
            _infoText.text = "아이디를 먼저 입력해줘!!";
            return;
        }

        if (string.IsNullOrEmpty(_passwordField.text))
        {
            _maskImage.raycastTarget = true;
            _infoWindowObject.SetActive(true);
            _infoText.text = "비밀번호를 먼저 입력해줘!!";
            return;
        }

        action.Invoke();
    }

    void OnLoginClick()
    {
        SoundManager.Instance.PlayButtonClick();
        string email = _idField.text;
        string password = _passwordField.text;

        _fm.Login(email, password,
        delegate
        {
            _maskImage.raycastTarget = true;
            _infoWindowObject.SetActive(true);
            _infoText.text = "일치하지 않는 아이디 혹은 비밀번호야! 확인해줘!";
        },
        delegate
        {
            SceneManager.LoadScene("2.LobbyScene"); 
            //씬 전환 소스를 여기에 넣으면 됩니다.
            //로그인 되어서 VO Setting 후 다음 씬으로 넘어갑니다.
        });
    }

    void OnSignUpClick()
    {
        SoundManager.Instance.PlayButtonClick();
        Debug.Log("회원가입버튼 클릭");
    }

    void OnExitClick()
    {
        SoundManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
