using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMaskEraser : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Data")]
    [SerializeField] private Image _image;
    
    [Header("Brush Settings")]
    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private int eraseRadius = 16;
    [SerializeField] private int maskResolution = 256;

    private Material _material;
    private Texture2D _maskTex;
    private Color32[] _maskPixels;
    private Rect r;

    public void OnPointerDown(PointerEventData ev) => Erase(ev);
    public void OnDrag(PointerEventData ev) => Erase(ev);
    
    private void Awake()
    {
        r = _image.rectTransform.rect;
        _material = Instantiate(_image.material);
        _image.material = _material;

        _maskTex = new Texture2D(maskResolution, maskResolution, TextureFormat.R8, false);
        _maskPixels = new Color32[maskResolution * maskResolution];
        for (int i = 0; i < _maskPixels.Length; i++)
            _maskPixels[i] = Color.white;
        
        _maskTex.SetPixels32(_maskPixels);
        _maskTex.Apply();

        _material.SetTexture("_MaskTex", _maskTex);
    }

    private void Erase(PointerEventData ev)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_image.rectTransform, ev.position, ev.pressEventCamera, out Vector2 lp)) 
            return;

        float u = (lp.x - r.x) / r.width;
        float v = (lp.y - r.y) / r.height;
        
        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;

        int cx = Mathf.FloorToInt(u * maskResolution);
        int cy = Mathf.FloorToInt(v * maskResolution);

        int rPx = eraseRadius;
        int minX = Mathf.Clamp(cx - rPx, 0, maskResolution - 1);
        int maxX = Mathf.Clamp(cx + rPx, 0, maskResolution - 1);
        int minY = Mathf.Clamp(cy - rPx, 0, maskResolution - 1);
        int maxY = Mathf.Clamp(cy + rPx, 0, maskResolution - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float bu = (x - (cx - rPx)) / (float)(rPx * 2);
                float bv = (y - (cy - rPx)) / (float)(rPx * 2);
                if (bu < 0f || bu > 1f || bv < 0f || bv > 1f) continue;

                float a = brushTexture.GetPixelBilinear(bu, bv).a;
                if (a < 0.01f) continue;

                _maskPixels[y * maskResolution + x] = new Color32(0, 0, 0, 0);
            }
        }

        _maskTex.SetPixels32(_maskPixels);
        _maskTex.Apply(updateMipmaps: false);
    }
}