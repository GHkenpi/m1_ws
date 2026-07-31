using UnityEngine;

/// <summary>
/// トンネルの縞模様テクスチャをUVスクロールさせてベクション錯覚を生成する。
/// 4枚の壁すべてが奥方向に正しく一貫して動くようにUVスクロール制御。
/// </summary>
public class TunnelScrollController : MonoBehaviour
{
    [Header("Study 2 Conditions")]
    [Tooltip("Fast: 1000cm/s (=10m/s) -> UV 0.05/s")]
    public float scrollSpeed = 0.05f;

    [Tooltip("High Density: 100m内に20セクション -> 全長200mで40周期")]
    public float tilingY = 40f;

    private struct WallMaterialInfo
    {
        public Material material;
        public bool isTopWall;
    }


    private WallMaterialInfo[] _wallInfos;
    private float _offset;

    void Start()
    {
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

    void Update()
    {
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
    public void StopScroll() { enabled = false; }

    /// <summary>スクロールを再開する</summary>
    public void StartScroll() { enabled = true; }
}


