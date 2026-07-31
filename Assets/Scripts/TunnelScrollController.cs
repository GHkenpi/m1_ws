using UnityEngine;

/// <summary>
/// トンネルの縞模様テクスチャをUVスクロールさせてベクション錯覚を生成する。
/// 4枚の壁すべてが奥方向に正しく一貫して動くようにUVスクロール制御。
/// </summary>
public class TunnelScrollController : MonoBehaviour
{
    public enum SpeedPreset { Fast, Slow, Custom }
    public enum DensityPreset { High, Low, Custom }

    [Header("Preset Selectors")]
    public SpeedPreset speedPreset = SpeedPreset.Fast;
    public DensityPreset densityPreset = DensityPreset.High;

    [Header("Tuning Parameters (Study 2)")]
    [Tooltip("Fast: 10m/s (1000cm/s), Slow: 2m/s (200cm/s)")]
    public float speedMetersPerSecond = 10.0f; // Fast = 10m/s, Slow = 2m/s

    [Tooltip("High Density: 20 sections / 100m, Low Density: 10 sections / 100m")]
    public float sectionsPer100m = 20f; // High = 20, Low = 10

    [Tooltip("Total length of the visual tunnel mesh in meters (Default: 200m)")]
    public float totalTunnelLength = 200f;

    [Header("UV Scroll Output (Calculated)")]
    [ReadOnlyInspector] public float scrollSpeed = 0.05f;
    [ReadOnlyInspector] public float tilingY = 40f;

    private struct WallMaterialInfo
    {
        public Material material;
        public bool isTopWall;
    }


    private WallMaterialInfo[] _wallInfos;
    private float _offset;

    private void OnValidate()
    {
        UpdateParameters();
    }

    public void UpdateParameters()
    {
        switch (speedPreset)
        {
            case SpeedPreset.Fast:
                speedMetersPerSecond = 10.0f; // 1000 cm/s
                break;
            case SpeedPreset.Slow:
                speedMetersPerSecond = 2.0f;  // 200 cm/s
                break;
        }

        switch (densityPreset)
        {
            case DensityPreset.High:
                sectionsPer100m = 20f; // 20 sections in 100m
                break;
            case DensityPreset.Low:
                sectionsPer100m = 10f; // 10 sections in 100m
                break;
        }

        // 100mあたりのセクション数からタイリング数を算出（200mなので2倍）
        tilingY = (sectionsPer100m / 100f) * totalTunnelLength;

        // 論文仕様の速度計算:
        // 1セクション対（明るい・暗い）の長さ L_section = 100m / sectionsPer100m (例: High=5m, Low=10m)
        // テクスチャの1周期(UV 0->1)は1セクション対を表すため、1周期あたりの長さは L_section (m)
        // したがって、1秒あたりのUVスクロール量 scrollSpeed (UV/s) = speedMetersPerSecond / (100m / sectionsPer100m)
        // 例: Fast (10m/s) / High(5m/周期) = 2.0 /s (1秒に2セクション通過)
        // 例: Fast (10m/s) / Low(10m/周期) = 1.0 /s (1秒に1セクション通過)
        // 例: Slow (2m/s)  / High(5m/周期) = 0.4 /s
        // 例: Slow (2m/s)  / Low(10m/周期) = 0.2 /s
        scrollSpeed = speedMetersPerSecond / (100f / sectionsPer100m);

        if (_wallInfos != null)
        {
            for (int i = 0; i < _wallInfos.Length; i++)
            {
                if (_wallInfos[i].material != null)
                {
                    _wallInfos[i].material.mainTextureScale = new Vector2(1f, tilingY);
                }
            }
        }
    }

    void Start()
    {
        UpdateParameters();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError("[TunnelScrollController] Rendererが見つかりません。");
            enabled = false;
            return;
        }

        _wallInfos = new WallMaterialInfo[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            _wallInfos[i].material = renderers[i].material;
            _wallInfos[i].material.mainTextureScale = new Vector2(1f, tilingY);

            string objName = renderers[i].gameObject.name;
            _wallInfos[i].isTopWall = (objName == "Wall_Top" || renderers[i].material.name.Contains("Top"));
        }
        _offset = 0f;
        ApplyOffset();
    }

    private bool _isScrolling = false;

    void Update()
    {
        if (!_isScrolling) return;

        _offset -= scrollSpeed * Time.deltaTime;
        ApplyOffset();
    }

    private void ApplyOffset()
    {
        if (_wallInfos != null)
        {
            for (int i = 0; i < _wallInfos.Length; i++)
            {
                if (_wallInfos[i].material != null)
                {
                    float currentOffset = _wallInfos[i].isTopWall ? -_offset + 0.5f : _offset;
                    _wallInfos[i].material.mainTextureOffset = new Vector2(0f, currentOffset);
                }
            }
        }
    }

    /// <summary>スクロールを停止する（実験終了時に呼ぶ）</summary>
    public void StopScroll() { _isScrolling = false; }

    /// <summary>スクロールを再開する</summary>
    public void StartScroll() { _isScrolling = true; }
}


