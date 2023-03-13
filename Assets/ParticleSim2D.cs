using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;

enum Element
{
    Air,
    Wall,
    Sand,
    Water,
    Wood,
    Gravel
}
public class ParticleSim2D : MonoBehaviour
{
    class Cell
    {
        public Element ID { get; private set; } = Element.Air;
        public Vector2 velocity = Vector2.zero;
        public bool hasSimulated = false;
        public bool needsRedraw = false;
        public void SetElement(Element _in) { ID = _in; }
    }
    [SerializeField] int width;
    [SerializeField] int height;
    [SerializeField] int brushSize = 1;
    [SerializeField] float FPS = 30;
    [SerializeField] Element SelectedElement;
    float lastFrameTime;

    Cell[,] grid;
    Texture2D texture;
    int frameNum;

    public bool useCoroutine;
    private void Update()
    {
        if (!useCoroutine)
        {
            Stopwatch timer = Stopwatch.StartNew();
            string diagnostic = "";

            HandleInput();

            if (Time.time < lastFrameTime + 1 / FPS)
            {
                timer.Stop();
                return;
            }
            lastFrameTime = Time.time;

            timer.Stop();
            diagnostic += $"Input: {timer.ElapsedMilliseconds}ms | ";
            timer.Restart();

            Simulate();

            timer.Stop();
            diagnostic += $"Simulate: {timer.ElapsedMilliseconds}ms | ";
            timer.Restart();

            GenerateTexture();

            timer.Stop();
            diagnostic += $"Texture: {timer.ElapsedMilliseconds}ms | ";
            Debug.Log(diagnostic);
        }
        else if (simulateDone)
        {
            StartCoroutine(SlowSimulate());
        }
    }
    private void OnGUI()
    {
        if (texture == null) return;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture, ScaleMode.ScaleToFit, true);
    }
    void HandleInput()
    {
        if (Input.GetKey(KeyCode.Space))
            grid[width / 2, height - 1].SetElement(Element.Sand);

        var m = GetMousePosition();
        if (InGridRange(m.x, m.y))
        {
            if (Input.GetMouseButton(0))
                ApplyBrush(m.x, m.y, SelectedElement);
            if (Input.GetMouseButton(1))
                ApplyBrush(m.x, m.y, Element.Air);
        }
        if (Input.GetKeyDown(KeyCode.E))       
            SelectedElement = (Element)((int)SelectedElement + 1);
        if (Input.GetKeyDown(KeyCode.Q))
            SelectedElement = (Element)((int)SelectedElement - 1);
    }
    void ApplyBrush(int _x, int _y, Element _e)
    {
        for (int x = _x - brushSize; x <= _x + brushSize; x++)
            for (int y = _y - brushSize; y <= _y + brushSize; y++)
            {
                grid[x, y].SetElement(_e);
                grid[x, y].needsRedraw = true;
            }
    }
    Vector2Int GetMousePosition()
    {
        Vector2Int output = new Vector2Int();
        var mouse = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        output.y = (int)(mouse.y * height);

        float textureAspect = (float)width / height;
        float screenAspect = (float)Screen.width / Screen.height;

        float aspectDif = (float)textureAspect / screenAspect;
        float xOffset = (1f - aspectDif) / 2f;
        float xMult = 1 / aspectDif;

        output.x = (int)((mouse.x - xOffset) * xMult * width);

        return output;
    }
    void GenerateTexture()
    {
        if (texture == null)
        {
            texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                        texture.SetPixel(x, y, GetColor(grid[x, y].ID));                   
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].hasSimulated || grid[x, y].needsRedraw)
                {
                    texture.SetPixel(x, y, GetColor(grid[x, y].ID));
                    grid[x, y].hasSimulated = false;
                }
        texture.Apply();
    }
    void Simulate()
    {
        frameNum++;
        if (grid == null) 
            InitGrid();
        if (frameNum % 2 == 0)     
            SimulateLeftToRight();        
        else        
            SimulateRightToLeft();        
    }
    void SimulateLeftToRight()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                SimulatePixel(x, y);
    }
    void SimulateRightToLeft()
    {
        for (int y = 0; y < height; y++)
            for (int x = width - 1; x >= 0; x--)
                SimulatePixel(x, y);
    }
    public bool simulateDone;
    IEnumerator SlowSimulate()
    {
        frameNum++;
        simulateDone = false;
        if (grid == null)
            InitGrid();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                bool doyield = GetElement(x, y) != Element.Air; 
                SimulatePixel(x, y);
                if (doyield)
                {
                    GenerateTexture();
                    yield return null;
                }
            }
        simulateDone = true;
    }
    void InitGrid()
    {
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)       
            for (int y = 0; y < height; y++)           
                grid[x, y] = new Cell();
    }
    void SimulatePixel(int _x, int _y)
    {
        if (grid[_x, _y].hasSimulated)
            return;       
        var pos = new Vector2Int(_x, _y);
        switch (grid[_x,_y].ID)
        {
            case Element.Air:
                break;
            case Element.Wall:
                break;
            case Element.Sand: SandUpdate(pos);
                break;
            case Element.Water: WaterUpdate(pos);
                break;
            case Element.Gravel: GravelUpdate(pos);
                break;
            default:
                break;
        }
    }
    //TODO: reduce code repetition
    void SandUpdate(Vector2Int _pos)
    {
        var dir = Random.value < 0.5f ? D.DownRight : D.DownLeft;
        //check below
        if (GetElement(_pos, D.Down) == Element.Air)
            SwapPixels(_pos, D.Down);
        else if(GetElement(_pos, D.Down) == Element.Water)
            ExpelPixel(_pos, D.Down);

        //check down left and down right, but randomize which is checked first
        else if (GetElement(_pos, dir) == Element.Air)
            SwapPixels(_pos, dir);
        else if (GetElement(_pos, D.InvertX(dir)) == Element.Air)
            SwapPixels(_pos, D.InvertX(dir));

        else if (GetElement(_pos, dir) == Element.Water)
            ExpelPixel(_pos, dir);
        else if (GetElement(_pos, D.InvertX(dir)) == Element.Water)
            ExpelPixel(_pos, D.InvertX(dir));
    }
    void WaterUpdate(Vector2Int _pos)
    {
        var dir = Random.value < 0.5f ? D.DownRight : D.DownLeft;
        var dir2 = Random.value < 0.5f ? D.Right : D.Left;
        //check below
        if (GetElement(_pos, D.Down) == Element.Air)
            SwapPixels(_pos, D.Down);

        //check down left and down right, but randomize which is checked first
        else if (GetElement(_pos, dir) == Element.Air)
            SwapPixels(_pos, dir);
        else if (GetElement(_pos, D.InvertX(dir)) == Element.Air)
            SwapPixels(_pos, D.InvertX(dir));
        
        //check sides
        else if (GetElement(_pos, dir2) == Element.Air)
            SwapPixels(_pos, dir2);
        else if (GetElement(_pos, D.InvertX(dir2)) == Element.Air)
            SwapPixels(_pos, D.InvertX(dir2));
    }
    void GravelUpdate(Vector2Int _pos)
    {
        var dir = Random.value < 0.5f ? D.Right : D.Left;
        int depthCheck = 5;
        //check below
        if (GetElement(_pos, D.Down) == Element.Air)
            SwapPixels(_pos, D.Down);
        else if (GetElement(_pos, D.Down) == Element.Water)
            ExpelPixel(_pos, D.Down);

        //check if there is air at a certain distance below
        //if so, the stack is unstable and falls to the side if there is room
        else if (GetElement(_pos, dir) == Element.Air && DepthCheck(_pos + dir, depthCheck, Element.Air))
            SwapPixels(_pos, dir + D.Down);
        else if (GetElement(_pos, D.InvertX(dir)) == Element.Air && DepthCheck(_pos + D.InvertX(dir), depthCheck, Element.Air))
            SwapPixels(_pos, D.InvertX(dir) + D.Down);

        /*
        else if (GetElement(_pos, dir) == Element.Water)
            ExpelPixel(_pos, dir);
        else if (GetElement(_pos, D.InvertX(dir)) == Element.Water)
            ExpelPixel(_pos, D.InvertX(dir));*/
    }
    bool DepthCheck(Vector2Int _start, int _depth, Element _element)
    {
        for (int i = 0; i < _depth; i++)
        {
            if (GetElement(_start, D.Down) != _element)
                return false;
            _start += D.Down;
        }
        return true;
    }
    Element GetElement(Vector2Int _pos, Vector2Int _direction) 
        => GetElement(_pos.x + _direction.x, _pos.y + _direction.y);
    Element GetElement(int _x, int _y)
    {
        if (InGridRange(_x, _y))
            return grid[_x, _y].ID;
        else
            return Element.Wall;
    }
    bool InGridRange(int _x, int _y)
    {
        return (_x >= 0 && _y >= 0 && _x < width && _y < height);
    }
    void SwapPixels(Vector2Int _pos, Vector2Int _direction)
        => SwapPixels(_pos.x, _pos.y, _direction.x, _direction.y);
    void SwapPixels(int _x, int _y, int _offsetx, int _offsety)
    {
        Cell e1 = grid[_x, _y];
        Cell e2 = grid[_x + _offsetx, _y + _offsety];
        grid[_x + _offsetx, _y + _offsety] = e1;
        grid[_x, _y] = e2;

        //set the new position as having been simulated
        e1.hasSimulated = true;
        e2.hasSimulated = true;
    }
    void ExpelPixel(Vector2Int _pos, Vector2Int _direction)
    {
        //do initial swap
        SwapPixels(_pos, _direction);

        //try to push secondary pixel to the side
        if (GetElement(_pos, D.Right) == Element.Air)
            SwapPixels(_pos, D.Right);
        else if (GetElement(_pos, D.Left) == Element.Air)
            SwapPixels(_pos, D.Left);
    }
    Color GetColor(Element _e)
    {
        switch (_e)
        {
            case Element.Air: return Color.black;
            case Element.Wall: return Color.magenta;
            case Element.Sand: return Color.yellow;
            case Element.Water: return new Color(0, 0.4f, 1, 1);
            case Element.Wood: return new Color(0.6f, 0.2f, 0.1f, 1);
            case Element.Gravel: return new Color(0.5f, 0.5f, 0.5f, 1);

            default: return Color.black;
        }
    }
    class D
    {
        public static Vector2Int Up { get; private set; } = Vector2Int.up;
        public static Vector2Int Down { get; private set; } = Vector2Int.down;
        public static Vector2Int Left { get; private set; } = Vector2Int.left;
        public static Vector2Int Right { get; private set; } = Vector2Int.right;
        public static Vector2Int UpRight { get; private set; } = Vector2Int.up + Vector2Int.right;
        public static Vector2Int DownRight { get; private set; } = Vector2Int.down + Vector2Int.right;
        public static Vector2Int UpLeft { get; private set; } = Vector2Int.up + Vector2Int.left;
        public static Vector2Int DownLeft { get; private set; } = Vector2Int.down + Vector2Int.left;
        public static Vector2Int InvertX(Vector2Int _in) { return _in * new Vector2Int(-1, 1); }
        public static Vector2Int InvertY(Vector2Int _in) { return _in * new Vector2Int(1, -1); }
    }
}
