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
    public InputField timeEstimateInput; // DV1 (直感的な秒数推定)
    public Slider passageOfTimeSlider;        // DV2 (VAS 0-100)
    public UnityEngine.UI.Text passageValueText;
    public Slider vectionSlider;               // DV3 (VAS 0-100)
    public UnityEngine.UI.Text vectionValueText;
    public Button submitButton;
    public UnityEngine.UI.Text titleText;

    private Action<float, float, float> _onSubmittedCallback;

    private bool _uiBuilt = false;

    private void Awake()
    {
        EnsureUIBuilt();
    }

    public void EnsureUIBuilt()
    {
        if (_uiBuilt) return;
        _uiBuilt = true;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(800, 600);
            rect.localScale = new Vector2(0.002f, 0.002f);
        }

        // 背景パネル作成
        GameObject bgObj = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(transform, false);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // タイトル
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        titleObj.transform.SetParent(transform, false);
        var titleTxt = titleObj.GetComponent<UnityEngine.UI.Text>();
        titleTxt.text = "Trial Questionnaire";
        titleTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleTxt.fontSize = 28;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 220);
        titleRect.sizeDelta = new Vector2(700, 60);

        // Q1: Time Estimate Text & Input
        CreateQuestionLabel("Q1: How long did the tunnel last? (seconds)", new Vector2(0, 150));
        GameObject q1InputObj = CreateInputField(new Vector2(0, 100), "Enter estimated seconds...");
        timeEstimateInput = q1InputObj.GetComponent<TMP_InputField>();

        // Q2: Passage of Time Slider
        CreateQuestionLabel("Q2: How fast did time pass for you? (0: Slow ~ 100: Fast)", new Vector2(0, 30));
        var passageGroup = CreateSlider(new Vector2(0, -20));
        passageOfTimeSlider = passageGroup.slider;
        passageValueText = passageGroup.valText;

        // Q3: Vection Slider
        CreateQuestionLabel("Q3: Did you feel like you were moving? (0: Not at all ~ 100: Intensely)", new Vector2(0, -90));
        var vectionGroup = CreateSlider(new Vector2(0, -140));
        vectionSlider = vectionGroup.slider;
        vectionValueText = vectionGroup.valText;

        // Submit Button
        GameObject btnObj = new GameObject("SubmitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(transform, false);
        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        submitButton = btnObj.GetComponent<Button>();
        submitButton.onClick.AddListener(OnSubmitClicked);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -230);
        btnRect.sizeDelta = new Vector2(240, 60);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxt = btnTxtObj.GetComponent<UnityEngine.UI.Text>();
        btnTxt.text = "SUBMIT (次へ)";
        btnTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        btnTxt.fontSize = 22;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;
    }

    private void CreateQuestionLabel(string text, Vector2 pos)
    {
        GameObject obj = new GameObject("Q_Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        obj.transform.SetParent(transform, false);
        var txt = obj.GetComponent<UnityEngine.UI.Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = new Color(0.95f, 0.95f, 1.0f);
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(700, 40);
    }

    private GameObject CreateInputField(Vector2 pos, string placeholderText)
    {
        GameObject inputObj = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        inputObj.transform.SetParent(transform, false);
        Image img = inputObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform r = inputObj.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(300, 45);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObj.transform.SetParent(inputObj.transform, false);
        var txt = textObj.GetComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;

        InputField inputField = inputObj.GetComponent<InputField>();
        inputField.textComponent = txt;
        inputField.contentType = InputField.ContentType.DecimalNumber;
        return inputObj;
    }

    private (Slider slider, UnityEngine.UI.Text valText) CreateSlider(Vector2 pos)
    {
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(transform, false);
        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 50f;
        RectTransform sr = sliderObj.GetComponent<RectTransform>();
        sr.anchoredPosition = pos;
        sr.sizeDelta = new Vector2(500, 30);

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(sliderObj.transform, false);
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform bgr = bg.GetComponent<RectTransform>();
        bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.sizeDelta = Vector2.zero;

        // Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one; far.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = new Color(0.2f, 0.7f, 1.0f);
        RectTransform fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.sizeDelta = Vector2.zero;
        slider.fillRect = fr;

        // Handle Area & Handle
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform har = handleArea.GetComponent<RectTransform>();
        har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one; har.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        RectTransform hr = handle.GetComponent<RectTransform>();
        hr.sizeDelta = new Vector2(25, 35);
        slider.handleRect = hr;
        slider.targetGraphic = handle.GetComponent<Image>();

        // Value Text
        GameObject valObj = new GameObject("ValText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        valObj.transform.SetParent(transform, false);
        var valTxt = valObj.GetComponent<UnityEngine.UI.Text>();
        valTxt.text = "50";
        valTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        valTxt.fontSize = 22;
        valTxt.alignment = TextAnchor.MiddleLeft;
        valTxt.color = Color.yellow;
        RectTransform vr = valObj.GetComponent<RectTransform>();
        vr.anchoredPosition = new Vector2(290, pos.y);
        vr.sizeDelta = new Vector2(100, 40);

        slider.onValueChanged.AddListener(v => valTxt.text = $"{v:F0}");

        return (slider, valTxt);
    }

    public void Show(int trialIndex, int totalTrials, Action<float, float, float> onSubmitted)
    {
        _onSubmittedCallback = onSubmitted;
        EnsureUIBuilt();
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

    /// <summary>VRカメラの正面1.8mにパネルを配置</summary>
    private void PositionInFrontOfCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindObjectOfType<Camera>();
        }

        if (mainCam != null)
        {
            Vector3 forward = mainCam.transform.forward;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.y = 0;
            forward.Normalize();

            transform.position = mainCam.transform.position + forward * 1.8f;
            transform.rotation = Quaternion.LookRotation(forward);
            Debug.Log($"[VRQuestionnaireUI] Panel positioned in front of camera at: {transform.position}");
        }
        else
        {
            transform.position = new Vector3(0, 1.2f, 1.8f);
            transform.rotation = Quaternion.identity;
            Debug.LogWarning("[VRQuestionnaireUI] Camera not found. Panel placed at default (0, 1.2, 1.8)");
        }
    }
}
