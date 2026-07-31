using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 40秒タイマー。カウントダウン中はトンネルスクロールを継続。
/// 終了時にスクロール停止とログ出力。
/// </summary>
public class ExperimentTimer : MonoBehaviour
{
    public enum DurationPreset { Sec20 = 20, Sec30 = 30, Sec40 = 40, Custom = 0 }

    [Header("Study 2 Conditions")]
    public DurationPreset durationPreset = DurationPreset.Sec40;

    [Header("Duration (s)")]
    public float duration = 40f;  // Study 2: 20s, 30s, 40s

    [Header("Embodiment / Avatar Control")]
    public bool enableAvatarHands = true;
    public GameObject avatarHandsContainer; // Controller/Hand Root GameObject

    [Header("References")]
    public TunnelScrollController tunnelScroll;
    public TextMeshProUGUI timerText;  // オプショナル: UI表示用

    private float _elapsed = 0f;
    private bool _running = false;

    private void OnValidate()
    {
        if (durationPreset != DurationPreset.Custom)
        {
            duration = (float)durationPreset;
        }

        if (avatarHandsContainer != null)
        {
            avatarHandsContainer.SetActive(enableAvatarHands);
        }
    }

    void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;

        // UI時間表示（オプショナル）
        if (timerText != null)
            timerText.text = $"Time: {_elapsed:F1} / {duration:F0}s";

        if (_elapsed >= duration)
        {
            _running = false;
            if (tunnelScroll != null) tunnelScroll.StopScroll();
            Debug.Log("[ExperimentTimer] 実験終了！経過: " + _elapsed.ToString("F2") + "秒");
        }
    }

    /// <summary>実験開始 (Spaceキー または外部から呼び出す)</summary>
    public void StartExperiment()
    {
        if (tunnelScroll == null) tunnelScroll = FindObjectOfType<TunnelScrollController>();

        if (durationPreset != DurationPreset.Custom)
        {
            duration = (float)durationPreset;
        }

        _elapsed = 0f;
        _running = true;
        if (tunnelScroll != null)
        {
            tunnelScroll.UpdateParameters();
            tunnelScroll.StartScroll();
        }

        if (avatarHandsContainer != null)
        {
            avatarHandsContainer.SetActive(enableAvatarHands);
        }
        else
        {
            var hands = FindObjectsOfType<Valve.VR.InteractionSystem.Hand>(true);
            foreach (var hand in hands)
            {
                hand.gameObject.SetActive(enableAvatarHands);
            }
        }

        Debug.Log($"[ExperimentTimer] 実験開始— Speed: {tunnelScroll?.speedPreset} ({tunnelScroll?.speedMetersPerSecond}m/s), Density: {tunnelScroll?.densityPreset} ({tunnelScroll?.sectionsPer100m} sections/100m), Duration: {duration}s, Avatar: {(enableAvatarHands ? "Hands" : "No Hands")}");
    }

    /// <summary>Spaceキーで開始できるようにする (HMD用)</summary>
    void Start()
    {
        if (tunnelScroll == null) tunnelScroll = FindObjectOfType<TunnelScrollController>();

        if (durationPreset != DurationPreset.Custom)
        {
            duration = (float)durationPreset;
        }

        var vrHands = FindObjectsOfType<Valve.VR.InteractionSystem.Hand>(true);
        if (avatarHandsContainer != null)
        {
            avatarHandsContainer.SetActive(enableAvatarHands);
        }
        else
        {
            // 自動検出: SteamVR Hand や RenderModel をトグル
            foreach (var hand in vrHands)
            {
                hand.gameObject.SetActive(enableAvatarHands);
            }
        }

        // 起動時はスクロールを停止状態にしておく（Spaceキーで開始）
        if (tunnelScroll != null) tunnelScroll.StopScroll();

        // 起動時の VR コントローラーバイブレーション（ハプティクス）機能を無効化
        foreach (var hand in vrHands)
        {
            hand.hapticAction = null;
        }

        var hints = FindObjectsOfType<Valve.VR.InteractionSystem.ControllerButtonHints>();
        foreach (var hint in hints)
        {
            hint.enabled = false;
        }
    }



    void LateUpdate()
    {
        // Space キーで開始（演習・確認用）
        if (!_running && Input.GetKeyDown(KeyCode.Space))
        {
            StartExperiment();
        }

        // R キーで HMD 位置・回転のリセット
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHMDPosition();
        }
    }

    /// <summary>
    /// HMD (VR Camera) の位置および回転を原点(0,0,0)かつ正面向きにリセット
    /// Unity Engine XR / SteamVR 共通のトラッキング再センタリングを呼び出し
    /// </summary>
    public void ResetHMDPosition()
    {
        // 1. Unity 標準 XR InputSubsystem の再センタリング
        var inputSubsystems = new System.Collections.Generic.List<UnityEngine.XR.XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);
        for (int i = 0; i < inputSubsystems.Count; i++)
        {
            inputSubsystems[i].TryRecenter();
        }

        // 2. Player (カメラリグ全体) の Transform を原点・正面向きに調整

        GameObject player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("[CameraRig]");

        if (player != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // カメラのローカルY回転を打ち消して正面(Z軸正方向)に向かせる
                float currentYAngle = mainCam.transform.rotation.eulerAngles.y;
                player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y - currentYAngle, 0);

                // カメラのローカルXZ位置オフセットを消去して(0, 0, 0)に配置
                Vector3 targetPos = Vector3.zero;
                Vector3 camLocalPos = mainCam.transform.position - player.transform.position;
                camLocalPos.y = 0; // 高さ方向は維持
                player.transform.position = targetPos - camLocalPos;
            }
            else
            {
                player.transform.position = Vector3.zero;
                player.transform.rotation = Quaternion.identity;
            }
        }

        Debug.Log("[ExperimentTimer] HMDの位置と向きをリセットしました。(Rキー)");
    }
}

