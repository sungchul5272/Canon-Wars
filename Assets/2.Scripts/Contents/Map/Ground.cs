using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{ 
    [SerializeField] private Texture2D _srcTexture;
    [SerializeField] private List<Transform> _spawnTransList = new List<Transform>();
    public List<Transform> SpawnTransList { get => _spawnTransList; }

    private Texture2D _newTexture;
    private SpriteRenderer _spriteRenderer;

    private float _worldWidth = 0;
    private float _worldHeight = 0;

    int _pixelWidth = 0;
    int _pixelHeight = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Init();
    }

    public void Init()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 텍스쳐 할당
        _srcTexture = _spriteRenderer.sprite.texture;

        // sprite로 생성
        _newTexture = new Texture2D(_srcTexture.width, _srcTexture.height, TextureFormat.RGBA32, false);
        _newTexture.SetPixels(_srcTexture.GetPixels());

        _newTexture.Apply();
        MakeSprtie();

        //
        _worldWidth = _spriteRenderer.bounds.size.x;
        _worldHeight = _spriteRenderer.bounds.size.y;

        _pixelWidth = _spriteRenderer.sprite.texture.width;
        _pixelHeight = _spriteRenderer.sprite.texture.height;

        gameObject.AddComponent<PolygonCollider2D>();
    }

    private void MakeSprtie()
    {
        float orignPPU = _spriteRenderer.sprite.pixelsPerUnit;
        _spriteRenderer.sprite = Sprite.Create(_newTexture, new Rect(0, 0, _newTexture.width, _newTexture.height), Vector2.one * 0.5f, orignPPU);
    }


    public void MakeDot(Vector3 pos)
    {

        Vector2Int pixelPos = WorldToPixel(pos);

        _newTexture.SetPixel(pixelPos.x, pixelPos.y, Color.clear);
        _newTexture.SetPixel(pixelPos.x+1, pixelPos.y, Color.clear);
        _newTexture.SetPixel(pixelPos.x-1, pixelPos.y, Color.clear);
        _newTexture.SetPixel(pixelPos.x, pixelPos.y+1, Color.clear);
        _newTexture.SetPixel(pixelPos.x, pixelPos.y-1, Color.clear);
        _newTexture.Apply();

        MakeSprtie();
    }

    public void MakeHole(Vector3 pos, float radius)
    {
        int pixelRadiusX = Mathf.RoundToInt(radius * (_pixelWidth / _worldWidth));
        int pixelRadiusY = Mathf.RoundToInt(radius * (_pixelHeight / _worldHeight));

        Vector2Int pixelPos = WorldToPixel(pos);

        int px, py, nx, ny, dis;

        for (int i = 0; i <= pixelRadiusX; i++)
        {
            dis = Mathf.RoundToInt(Mathf.Sqrt(1 - (i * i) / (float)(pixelRadiusX * pixelRadiusX)) * pixelRadiusY);
            for (int j =0; j <= dis; j++)
            {
                px = pixelPos.x + i;
                nx = pixelPos.x - i;
                py = pixelPos.y + j;
                ny = pixelPos.y - j;

                if (px >= 0 && px < _pixelWidth && py >= 0 && py < _pixelHeight)
                    _newTexture.SetPixel(px, py, Color.clear);

                if (nx >= 0 && nx < _pixelWidth && py >= 0 && py < _pixelHeight)
                    _newTexture.SetPixel(nx, py, Color.clear);

                if (px >= 0 && px < _pixelWidth && ny >= 0 && ny < _pixelHeight)
                    _newTexture.SetPixel(px, ny, Color.clear);

                if (nx >= 0 && nx < _pixelWidth && ny >= 0 && ny < _pixelHeight)
                    _newTexture.SetPixel(nx, ny, Color.clear);
            }
        }

        _newTexture.Apply();
        MakeSprtie();

        if (IsTransparent(_newTexture))
            Destroy(gameObject);

        Destroy(gameObject.GetComponent<PolygonCollider2D>());
        gameObject.AddComponent<PolygonCollider2D>();
    }

    public void MakeHoleEllipse(Vector3 pos, float radiusX, float radiusY)
    {
        //float scaleFactorX = transform.localScale.x;
        //float scaleFactorY = transform.localScale.y;

        int pixelRadiusX = Mathf.RoundToInt((radiusX) * (_pixelWidth / _worldWidth));
        int pixelRadiusY = Mathf.RoundToInt((radiusY) * (_pixelHeight / _worldHeight));

        Vector2Int colliderCenter = WorldToPixel(pos);

        int px, py, nx, ny;
        int disY;

        for (int i = 0; i <= pixelRadiusX; i++)
        {
            // 타원의 방정식 기반으로 y 범위 계산
            disY = Mathf.RoundToInt(pixelRadiusY * Mathf.Sqrt(1 - (i * i) / (float)(pixelRadiusX * pixelRadiusX)));

            for (int j = 0; j <= disY; j++)
            {
                px = colliderCenter.x + i;
                nx = colliderCenter.x - i;
                py = colliderCenter.y + j;
                ny = colliderCenter.y - j;

                if (px >= 0 && px < _pixelWidth && py >= 0 && py < _pixelHeight)
                    _newTexture.SetPixel(px, py, Color.clear);

                if (nx >= 0 && nx < _pixelWidth && py >= 0 && py < _pixelHeight)
                    _newTexture.SetPixel(nx, py, Color.clear);

                if (px >= 0 && px < _pixelWidth && ny >= 0 && ny < _pixelHeight)
                    _newTexture.SetPixel(px, ny, Color.clear);

                if (nx >= 0 && nx < _pixelWidth && ny >= 0 && ny < _pixelHeight)
                    _newTexture.SetPixel(nx, ny, Color.clear);
            }
        }

        _newTexture.Apply();
        MakeSprtie();

        if (IsTransparent(_newTexture))
            Destroy(gameObject);

        Destroy(gameObject.GetComponent<PolygonCollider2D>());
        gameObject.AddComponent<PolygonCollider2D>();
    }

    // 땅이 모두 파괴됐는지 확인하기
    bool IsTransparent(Texture2D tex)
    { 
        for (int x = 0; x < tex.width; x++) 
            for (int y = 0; y < tex.height; y++)
                if (tex.GetPixel(x, y).a != 0) 
                    return false; 
        return true; 
    }

    public void GroundExplosion(Vector3 pos, float radius, float radiusY = 0, eShellExplosionType explosionType = eShellExplosionType.Circle)
    {
        
        switch(explosionType)
        {
            case eShellExplosionType.Circle:
                MakeHole(pos, radius);
                break;

            case eShellExplosionType.Ellipse:
                MakeHoleEllipse(pos, radius, radiusY);
                break;
        }      
    }
    private Vector2Int WorldToPixel(Vector3 pos)
    {
        Vector2Int pixelPos = Vector2Int.zero;

        // Ground 오브젝트의 월드 영역을 구함 (localScale 반영)
        float adjustedWorldWidth = _worldWidth; //* transform.localScale.x;
        float adjustedWorldHeight = _worldHeight; //* transform.localScale.y;

        float minX = transform.position.x - (adjustedWorldWidth * 0.5f);
        float minY = transform.position.y - (adjustedWorldHeight * 0.5f);

        // pos를 픽셀 좌표로 변환 (localScale을 고려)
        pixelPos.x = Mathf.RoundToInt((pos.x - minX) / adjustedWorldWidth * _pixelWidth);
        pixelPos.y = Mathf.RoundToInt((pos.y - minY) / adjustedWorldHeight * _pixelHeight);

        return pixelPos;
    }

}
