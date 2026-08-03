using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 12条件×2ブロック (全24試行) の疑似ランダム自動進行および実験全体管理クラス。
/// </summary>
public class AutomatedExperimentManager : MonoBehaviour
{
    [System.Serializable]
    public struct ConditionDefinition
    {
        public int conditionID;
        public TunnelScrollController.SpeedPreset speedPreset;
        public TunnelScrollController.DensityPreset densityPreset;
        public ExperimentTimer.DurationPreset durationPreset;

        public ConditionDefinition(int id, TunnelScrollController.SpeedPreset sp, TunnelScrollController.DensityPreset dp, ExperimentTimer.DurationPreset durP)
        {
            conditionID = id;
            speedPreset = sp;
            densityPreset = dp;
            durationPreset = durP;
        }
    }

    [Header("Participant Setup")]
    public string participantID = "001";
    public bool enableAvatarHands = true;

    [Header("References")]
    public TunnelScrollController tunnelScroll;
    public VRQuestionnaireUI questionnaireUI;
    public ExperimentDataLogger dataLogger;
    public TextMeshProUGUI statusTextUI;

    [Header("Experiment Status (ReadOnly)")]
    public int currentTrialIndex = 0; // 1 〜 24
    public bool isExperimentRunning = false;
    public bool isWaitingForSpaceKey = true;

    private List<ConditionDefinition> _trialSequence = new List<ConditionDefinition>();
    private float _currentTrialTimer = 0f;
    private bool _inTrialCountdown = false;

    private void Start()
    {
        if (tunnelScroll == null) tunnelScroll = FindObjectOfType<TunnelScrollController>();
        if (questionnaireUI == null)
        {
            questionnaireUI = FindObjectOfType<VRQuestionnaireUI>(true);
            if (questionnaireUI == null)
            {
                GameObject canvasObj = new GameObject("VRQuestionnaireCanvas");
                questionnaireUI = canvasObj.AddComponent<VRQuestionnaireUI>();
                questionnaireUI.EnsureUIBuilt();
                questionnaireUI.Hide();
            }
        }
        if (dataLogger == null)
        {
            dataLogger = gameObject.AddComponent<ExperimentDataLogger>();
        }

        dataLogger.Initialize(participantID);
        GenerateTrialSequence();

        currentTrialIndex = 0;
        isWaitingForSpaceKey = true;
        _inTrialCountdown = false;

        if (tunnelScroll != null) tunnelScroll.StopScroll();

        UpdateStatusText("【実験待機中】\n被験者の準備が整ったら [Spaceキー] を押して Trial 1 を開始してください。");
    }

    private void Update()
    {
        // 試行開始待ち状態（Spaceキー）
        if (isWaitingForSpaceKey && Input.GetKeyDown(KeyCode.Space))
        {
            StartNextTrial();
        }

        if (_inTrialCountdown)
        {
            _currentTrialTimer -= Time.deltaTime;
            if (_currentTrialTimer <= 0f)
            {
                OnTrialFinished();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHMDPosition();
        }
    }

    /// <summary>論文 Table 2 準拠の 12条件×2ブロック=24試行 をシャッフルして生成</summary>
    private void GenerateTrialSequence()
    {
        List<ConditionDefinition> blockA = Get12Conditions();
        List<ConditionDefinition> blockB = Get12Conditions();

        ShuffleList(blockA);
        ShuffleList(blockB);

        _trialSequence.Clear();
        _trialSequence.AddRange(blockA);
        _trialSequence.AddRange(blockB);
    }

    private List<ConditionDefinition> Get12Conditions()
    {
        return new List<ConditionDefinition>()
        {
            new ConditionDefinition(1,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec20),
            new ConditionDefinition(2,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec30),
            new ConditionDefinition(3,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec40),
            new ConditionDefinition(4,  TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec20),
            new ConditionDefinition(5,  TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec30),
            new ConditionDefinition(6,  TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.High, ExperimentTimer.DurationPreset.Sec40),
            new ConditionDefinition(7,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec20),
            new ConditionDefinition(8,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec30),
            new ConditionDefinition(9,  TunnelScrollController.SpeedPreset.Fast, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec40),
            new ConditionDefinition(10, TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec20),
            new ConditionDefinition(11, TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec30),
            new ConditionDefinition(12, TunnelScrollController.SpeedPreset.Slow, TunnelScrollController.DensityPreset.Low,  ExperimentTimer.DurationPreset.Sec40),
        };
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
    }

    /// <summary>次の試行を開始</summary>
    private void StartNextTrial()
    {
        if (currentTrialIndex >= _trialSequence.Count)
        {
            UpdateStatusText("【全24試行が完了しました！】\n実験終了です。お疲れ様でした。");
            return;
        }

        isWaitingForSpaceKey = false;
        ConditionDefinition currentCond = _trialSequence[currentTrialIndex];

        if (tunnelScroll != null)
        {
            tunnelScroll.speedPreset = currentCond.speedPreset;
            tunnelScroll.densityPreset = currentCond.densityPreset;
            tunnelScroll.UpdateParameters();
            tunnelScroll.StartScroll();
        }

        _currentTrialTimer = (float)currentCond.durationPreset;
        _inTrialCountdown = true;

        int trialNum = currentTrialIndex + 1;
        int block = trialNum <= 12 ? 1 : 2;
        UpdateStatusText($"【Trial {trialNum} / 24 走行中 (Block {block})】\nCondition: {currentCond.conditionID} | Speed: {currentCond.speedPreset} | Density: {currentCond.densityPreset} | Duration: {_currentTrialTimer}s");
    }

    /// <summary>試行時間満了時</summary>
    private void OnTrialFinished()
    {
        _inTrialCountdown = false;
        if (tunnelScroll != null) tunnelScroll.StopScroll();

        int trialNum = currentTrialIndex + 1;
        UpdateStatusText($"【Trial {trialNum} / 24 終了】\nVR内のアンケートパネルから回答してください。");

        if (questionnaireUI != null)
        {
            questionnaireUI.Show(trialNum, 24, OnQuestionnaireSubmitted);
        }
        else
        {
            // fallback: UIがない場合は仮回答として自動進行
            OnQuestionnaireSubmitted(30f, 50f, 50f);
        }
    }

    /// <summary>アンケート回答完了コールバック</summary>
    private void OnQuestionnaireSubmitted(float dv1TimeEst, float dv2Passage, float dv3Vection)
    {
        ConditionDefinition currentCond = _trialSequence[currentTrialIndex];
        int trialNum = currentTrialIndex + 1;
        int block = trialNum <= 12 ? 1 : 2;

        // データCSV記録
        if (dataLogger != null)
        {
            dataLogger.LogTrial(
                participantID,
                enableAvatarHands,
                trialNum,
                block,
                currentCond.conditionID,
                tunnelScroll != null ? tunnelScroll.sectionsPer100m : 0f,
                tunnelScroll != null ? tunnelScroll.speedMetersPerSecond : 0f,
                (float)currentCond.durationPreset,
                dv1TimeEst,
                dv2Passage,
                dv3Vection
            );
        }

        currentTrialIndex++;

        if (currentTrialIndex < _trialSequence.Count)
        {
            isWaitingForSpaceKey = true;
            UpdateStatusText($"【Trial {currentTrialIndex} / 24 回答完了】\n準備が整ったら [Spaceキー] を押して次の試行を開始してください。");
        }
        else
        {
            UpdateStatusText("🎉【全24試行が完了しました！】\nCSVデータに正常記録されました。お疲れ様でした！");
        }
    }

    private void UpdateStatusText(string msg)
    {
        if (statusTextUI != null) statusTextUI.text = msg;
        Debug.Log($"[AutomatedExperimentManager] {msg}");
    }

    public void ResetHMDPosition()
    {
        var inputSubsystems = new List<UnityEngine.XR.XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);
        for (int i = 0; i < inputSubsystems.Count; i++)
        {
            inputSubsystems[i].TryRecenter();
        }

        GameObject player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("[CameraRig]");

        if (player != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                float currentYAngle = mainCam.transform.rotation.eulerAngles.y;
                player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y - currentYAngle, 0);

                Vector3 targetPos = Vector3.zero;
                Vector3 camLocalPos = mainCam.transform.position - player.transform.position;
                camLocalPos.y = 0;
                player.transform.position = targetPos - camLocalPos;
            }
        }
    }
}
