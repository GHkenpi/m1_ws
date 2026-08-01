using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR空間内で右コントローラーを使って回答するアンケートパネル管理クラス。
/// 3つの必須項目（DV1: 時間推定, DV2: 時間経過速度, DV3: ベクション強度）の入力と送信を制御。
/// </summary>
public class VRQuestionnaireUI : MonoBehaviour
{
    [Header("UI Controls")]
    public TMP_InputField timeEstimateInput; // DV1 (直感的な秒数推定)
    public Slider passageOfTimeSlider;        // DV2 (VAS 0-100)
    public TextMeshProUGUI passageValueText;
    public Slider vectionSlider;               // DV3 (VAS 0-100)
    public TextMeshProUGUI vectionValueText;
    public Button submitButton;
    public TextMeshProUGUI titleText;

    private Action<float, float, float> _onSubmittedCallback;

    private void Awake()
    {
        if (passageOfTimeSlider != null)
        {
            passageOfTimeSlider.onValueChanged.AddListener(val => {
                if (passageValueText != null) passageValueText.text = $"{val:F0}";
            });
        }

        if (vectionSlider != null)
        {
            vectionSlider.onValueChanged.AddListener(val => {
                if (vectionValueText != null) vectionValueText.text = $"{val:F0}";
            });
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
    }

    public void Show(int trialIndex, int totalTrials, Action<float, float, float> onSubmitted)
    {
        _onSubmittedCallback = onSubmitted;
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = $"Trial Questionnaire ({trialIndex} / {totalTrials})";
        }

        // 初期リセット
        if (timeEstimateInput != null) timeEstimateInput.text = "";
        if (passageOfTimeSlider != null) passageOfTimeSlider.value = 50f;
        if (vectionSlider != null) vectionSlider.value = 50f;
        if (passageValueText != null) passageValueText.text = "50";
        if (vectionValueText != null) vectionValueText.text = "50";

        // HMD正面へUIを自動追従・配置
        PositionInFrontOfCamera();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnSubmitClicked()
    {
        float dv1 = 0f;
        if (timeEstimateInput != null && !string.IsNullOrEmpty(timeEstimateInput.text))
        {
            float.TryParse(timeEstimateInput.text, out dv1);
        }

        float dv2 = passageOfTimeSlider != null ? passageOfTimeSlider.value : 50f;
        float dv3 = vectionSlider != null ? vectionSlider.value : 50f;

        Hide();
        _onSubmittedCallback?.Invoke(dv1, dv2, dv3);
    }

    /// <summary>VRカメラの正面2mにパネルを配置</summary>
    private void PositionInFrontOfCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 forward = mainCam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            transform.position = mainCam.transform.position + forward * 1.8f + Vector3.up * 0.1f;
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
