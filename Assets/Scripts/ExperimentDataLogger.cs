using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 実験データログ出力クラス。
/// 被験者ID、条件パラメータ、毎試行後のDV回答結果をCSV形式で自動保存します。
/// </summary>
public class ExperimentDataLogger : MonoBehaviour
{
    private string _csvPath;

    public void Initialize(string participantID)
    {
        string dirPath = Path.Combine(Application.dataPath, "../ExperimentLogs");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Participant_{participantID}_{timeStamp}.csv";
        _csvPath = Path.Combine(dirPath, fileName);

        // ヘッダー書き込み
        string header = "ParticipantID,Group_AvatarHands,TrialIndex,Block,ConditionID,Density_SectionsPer100m,Speed_m_s,Duration_s,DV1_TimeEstimate_s,DV2_PassageOfTime_VAS,DV3_Vection_VAS,Timestamp";
        File.WriteAllText(_csvPath, header + Environment.NewLine);

        Debug.Log($"[ExperimentDataLogger] CSVログの保存先を初期化しました: {_csvPath}");
    }

    public void LogTrial(
        string participantID,
        bool avatarHands,
        int trialIndex,
        int block,
        int conditionID,
        float density,
        float speed,
        float duration,
        float dv1TimeEstimate,
        float dv2PassageOfTime,
        float dv3Vection)
    {
        if (string.IsNullOrEmpty(_csvPath))
        {
            Initialize(participantID);
        }

        string line = $"{participantID},{avatarHands},{trialIndex},{block},{conditionID},{density},{speed},{duration},{dv1TimeEstimate:F2},{dv2PassageOfTime:F2},{dv3Vection:F2},{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        File.AppendAllText(_csvPath, line + Environment.NewLine);

        Debug.Log($"[ExperimentDataLogger] Trial {trialIndex} の結果を保存しました。");
    }
}
