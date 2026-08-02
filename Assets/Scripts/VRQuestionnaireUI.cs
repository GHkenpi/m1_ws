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

        Font font = Font.CreateDynamicFontFromOSFont("MS Gothic", 20);
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Yu Gothic", 20);
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // タイトル
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        titleObj.transform.SetParent(transform, false);
        titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
        titleText.text = "試行評価アンケート";
        titleText.font = font;
        titleText.fontSize = 28;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 220);
        titleRect.sizeDelta = new Vector2(700, 60);

        // Q1: Time Estimate Text & Input
        CreateQuestionLabel("Q1: 直感的にトンネルは何秒間続いて見えましたか？ (秒)", new Vector2(0, 150), font);
        GameObject q1InputObj = CreateInputField(new Vector2(0, 100), "30 秒", font);
        q1BgImage = q1InputObj.GetComponent<Image>();
        timeEstimateInput = q1InputObj.GetComponent<InputField>();
        q1ValueText = q1InputObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (q1ValueText != null) q1ValueText.text = "30 秒";

        // Q2: Passage of Time Slider
        CreateQuestionLabel("Q2: 時間の経過速度はどれくらい速く感じましたか？ (0:非常に遅い 〜 100:非常に速い)", new Vector2(0, 30), font);
        var passageGroup = CreateSlider(new Vector2(0, -20), font);
        passageOfTimeSlider = passageGroup.slider;
        passageValueText = passageGroup.valText;
        q2BgImage = passageGroup.bgImg;

        // Q3: Vection Slider
        CreateQuestionLabel("Q3: 自分自身が移動しているように感じましたか？ (0:全く感じない 〜 100:非常に強く感じた)", new Vector2(0, -90), font);
        var vectionGroup = CreateSlider(new Vector2(0, -140), font);
        vectionSlider = vectionGroup.slider;
        vectionValueText = vectionGroup.valText;
        q3BgImage = vectionGroup.bgImg;

        // Submit Button
        GameObject btnObj = new GameObject("SubmitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(transform, false);
        submitBtnImage = btnObj.GetComponent<Image>();
        submitBtnImage.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        submitButton = btnObj.GetComponent<Button>();
        submitButton.onClick.AddListener(OnSubmitClicked);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -230);
        btnRect.sizeDelta = new Vector2(240, 60);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxt = btnTxtObj.GetComponent<UnityEngine.UI.Text>();
        btnTxt.text = "回答を送信 (次へ)";
        btnTxt.font = font;
        btnTxt.fontSize = 22;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;

        UpdateFocusHighlight();
    }

    private void CreateQuestionLabel(string text, Vector2 pos, Font font)
    {
        GameObject obj = new GameObject("Q_Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        obj.transform.SetParent(transform, false);
        var txt = obj.GetComponent<UnityEngine.UI.Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = new Color(0.95f, 0.95f, 1.0f);
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(700, 40);
    }

    private GameObject CreateInputField(Vector2 pos, string placeholderText, Font font)
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
        txt.font = font;
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

    private (Slider slider, UnityEngine.UI.Text valText, Image bgImg) CreateSlider(Vector2 pos, Font font)
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
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);
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
        valTxt.font = font;
        valTxt.fontSize = 22;
        valTxt.alignment = TextAnchor.MiddleLeft;
        valTxt.color = Color.yellow;
        RectTransform vr = valObj.GetComponent<RectTransform>();
        vr.anchoredPosition = new Vector2(290, pos.y);
        vr.sizeDelta = new Vector2(100, 40);

        slider.onValueChanged.AddListener(v => valTxt.text = $"{v:F0}");

        return (slider, valTxt, bgImg);
    }

    public void Show(int trialIndex, int totalTrials, Action<float, float, float> onSubmitted)
    {
        _onSubmittedCallback = onSubmitted;
        EnsureUIBuilt();
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = $"試行評価アンケート ({trialIndex} / {totalTrials})";
        }

        // 初期リセット
        _focusedItemIndex = 0;
        _dv1Value = 30f;
        _dv2Value = 50f;
        _dv3Value = 50f;

        if (q1ValueText != null) q1ValueText.text = "30 秒";
        if (timeEstimateInput != null) timeEstimateInput.text = "30";
        if (passageOfTimeSlider != null) passageOfTimeSlider.value = 50f;
        if (vectionSlider != null) vectionSlider.value = 50f;
        if (passageValueText != null) passageValueText.text = "50";
        if (vectionValueText != null) vectionValueText.text = "50";

        UpdateFocusHighlight();

        // HMD正面へUIを自動追従・配置
        PositionInFrontOfCamera();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private int _focusedItemIndex = 0; // 0: Q1(時間推定), 1: Q2(経過速度), 2: Q3(ベクション), 3: 送信ボタン
    private float _dv1Value = 30f;
    private float _dv2Value = 50f;
    private float _dv3Value = 50f;

    public UnityEngine.UI.Text q1ValueText;
    public Image q1BgImage;
    public Image q2BgImage;
    public Image q3BgImage;
    public Image submitBtnImage;

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        // 右コントローラートラックパッド / 矢印キー入力の取得
        Vector2 trackpadPos = Vector2.zero;
        bool isUpPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool isDownPressed = Input.GetKeyDown(KeyCode.DownArrow);
        bool isLeftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
        bool isRightPressed = Input.GetKeyDown(KeyCode.RightArrow);
        bool isTriggerPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);

        // SteamVR Input 経由のトラックパッド/トリガー入力判定
        var rightHand = Valve.VR.InteractionSystem.Player.instance != null ? Valve.VR.InteractionSystem.Player.instance.rightHand : null;
        if (rightHand != null && rightHand.gameObject.activeInHierarchy)
        {
            // トリガー判定 (決定 / 次へ)
            try
            {
                if (rightHand.grabPinchAction != null && rightHand.grabPinchAction.GetStateDown(rightHand.handType))
                {
                    isTriggerPressed = true;
                }
            }
            catch {}

            // トラックパッド座標 (Y軸: 上下)
            try
            {
                var touchPosAction = Valve.VR.SteamVR_Input.GetVector2Action("TouchpadTouch");
                if (touchPosAction == null) touchPosAction = Valve.VR.SteamVR_Input.GetVector2Action("Touchpad");
                if (touchPosAction != null)
                {
                    trackpadPos = touchPosAction.GetAxis(rightHand.handType);
                }
            }
            catch {}

            // トラックパッドクリック判定
            try
            {
                var teleportAction = Valve.VR.SteamVR_Input.GetBooleanAction("Teleport");
                if (teleportAction != null && teleportAction.GetStateDown(rightHand.handType))
                {
                    if (trackpadPos.y > 0.2f) isUpPressed = true;
                    else if (trackpadPos.y < -0.2f) isDownPressed = true;
                }
            }
            catch {}
        }

        // トラックパッドのアナログタッチ/連続入力 (Y軸 > 0.35 で加算、Y軸 < -0.35 で減算)
        if (Mathf.Abs(trackpadPos.y) > 0.35f && Time.frameCount % 4 == 0)
        {
            if (trackpadPos.y > 0.35f) isUpPressed = true;
            else if (trackpadPos.y < -0.35f) isDownPressed = true;
        }

        // トラックパッド上下（上：値上昇 +1 / 下：値減少 -1）
        if (isUpPressed)
        {
            ChangeCurrentValue(1.0f);
        }
        else if (isDownPressed)
        {
            ChangeCurrentValue(-1.0f);
        }

        // キーボード左右ショートカット対応
        if (isLeftPressed) ChangeCurrentValue(-1.0f);
        if (isRightPressed) ChangeCurrentValue(1.0f);

        // トリガーで決定・次項目へ移動・送信
        if (isTriggerPressed)
        {
            if (_focusedItemIndex == 3)
            {
                OnSubmitClicked();
            }
            else
            {
                // 次の質問項目へフォーカス移動
                _focusedItemIndex = (_focusedItemIndex + 1) % 4;
                UpdateFocusHighlight();
            }
        }
    }

    private void ChangeCurrentValue(float delta)
    {
        switch (_focusedItemIndex)
        {
            case 0:
                _dv1Value = Mathf.Clamp(_dv1Value + delta, 0f, 120f);
                if (q1ValueText != null) q1ValueText.text = $"{_dv1Value:F0} 秒";
                break;
            case 1:
                _dv2Value = Mathf.Clamp(_dv2Value + delta, 0f, 100f);
                if (passageOfTimeSlider != null) passageOfTimeSlider.value = _dv2Value;
                if (passageValueText != null) passageValueText.text = $"{_dv2Value:F0}";
                break;
            case 2:
                _dv3Value = Mathf.Clamp(_dv3Value + delta, 0f, 100f);
                if (vectionSlider != null) vectionSlider.value = _dv3Value;
                if (vectionValueText != null) vectionValueText.text = $"{_dv3Value:F0}";
                break;
        }
    }

    private void UpdateFocusHighlight()
    {
        Color normalColor = new Color(0.2f, 0.2f, 0.25f);
        Color focusColor = new Color(0.2f, 0.6f, 0.9f, 0.9f);

        if (q1BgImage != null) q1BgImage.color = _focusedItemIndex == 0 ? focusColor : normalColor;
        if (q2BgImage != null) q2BgImage.color = _focusedItemIndex == 1 ? focusColor : normalColor;
        if (q3BgImage != null) q3BgImage.color = _focusedItemIndex == 2 ? focusColor : normalColor;
        if (submitBtnImage != null) submitBtnImage.color = _focusedItemIndex == 3 ? new Color(0.1f, 0.8f, 0.3f) : new Color(0.2f, 0.6f, 1.0f);
    }

    private void OnSubmitClicked()
    {
        float dv1 = _dv1Value;
        float dv2 = passageOfTimeSlider != null ? passageOfTimeSlider.value : _dv2Value;
        float dv3 = vectionSlider != null ? vectionSlider.value : _dv3Value;

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
