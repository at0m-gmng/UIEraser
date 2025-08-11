using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIEraser
{
    public sealed class UIMaskEraser : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private const string MASK_TEXTURE_NAME = "_MaskTex";

        [Header("Data")]
        [SerializeField] private Image _image;
    
        [Header("Brush Settings")]
        [Tooltip("If the texture is not readable, a copy is created")]
        [SerializeField] private Texture2D _brushTexture;
        [SerializeField] private int _eraseRadius = 50;
        [SerializeField] private int _maskResolution = 1024;

        private Material _material;
        private Texture2D _maskTex;
        private Color32[] _maskPixels;
        private Rect _imageRect;

        public void OnPointerDown(PointerEventData ev) => Erase(ev);
        public void OnDrag(PointerEventData ev) => Erase(ev);
    
        private void Awake()
        {
            InitializeBrushAnalysis();
            _imageRect = _image.rectTransform.rect;
            _material = Instantiate(_image.material);
            _image.material = _material;

            _maskTex = new Texture2D(_maskResolution, _maskResolution, TextureFormat.R8, false);
            _maskPixels = new Color32[_maskResolution * _maskResolution];
            for (int i = 0; i < _maskPixels.Length; i++)
            {
                _maskPixels[i] = Color.white;
            }
            _maskTex.SetPixels32(_maskPixels);
            _maskTex.Apply();

            _material.SetTexture(MASK_TEXTURE_NAME, _maskTex);
        }

        private void InitializeBrushAnalysis()
        {
            if(_brushTexture.isReadable == false)
            {
                RenderTexture tempRT = RenderTexture.GetTemporary
                (
                    _brushTexture.width,
                    _brushTexture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear
                );

                Graphics.Blit(_brushTexture, tempRT);

                RenderTexture previousActiveRT = RenderTexture.active;
                RenderTexture.active = tempRT;

                Texture2D readableTexture = new Texture2D
                (
                    _brushTexture.width,
                    _brushTexture.height,
                    TextureFormat.RGBA32,
                    false
                );

                readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                readableTexture.Apply();

                RenderTexture.active = previousActiveRT;
                RenderTexture.ReleaseTemporary(tempRT);

                _brushTexture = readableTexture;
            }
        }

        private void Erase(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_image.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 lp))
            {
                return;
            }

            float u = (lp.x - _imageRect.x) / _imageRect.width;
            float v = (lp.y - _imageRect.y) / _imageRect.height;
    
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return;
            }

            int cx = Mathf.FloorToInt(u * _maskResolution);
            int cy = Mathf.FloorToInt(v * _maskResolution);

            int rPx = _eraseRadius;
            int minX = Mathf.Clamp(cx - rPx, 0, _maskResolution - 1);
            int maxX = Mathf.Clamp(cx + rPx, 0, _maskResolution - 1);
            int minY = Mathf.Clamp(cy - rPx, 0, _maskResolution - 1);
            int maxY = Mathf.Clamp(cy + rPx, 0, _maskResolution - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float bu = (x - (cx - rPx)) / (float)(rPx * 2);
                    float bv = (y - (cy - rPx)) / (float)(rPx * 2);
                    if (bu < 0f || bu > 1f || bv < 0f || bv > 1f)
                    {
                        continue;
                    }

                    float brushAlpha = _brushTexture.GetPixelBilinear(bu, bv).a;
                    if (brushAlpha < 0.01f)
                    {
                        continue;
                    }

                    int index = y * _maskResolution + x;
                    byte currentMask = _maskPixels[index].r;
                    float currentNormalized = currentMask / 255f;
                    float newValue = Mathf.Max(0, currentNormalized - brushAlpha);
                    byte newMask = (byte)(newValue * 255);
                    _maskPixels[index].r = newMask;
                }
            }

            _maskTex.SetPixels32(_maskPixels);
            _maskTex.Apply(updateMipmaps: false);
        }
    }
}