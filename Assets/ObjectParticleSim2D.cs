using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;
public class ObjectParticleSim2D : MonoBehaviour
{
    class Cell
    {
        public ElementData Data { get; private set; }
        public Vector2 velocity = Vector2.zero;
        public bool hasSimulated = false;
        public bool needsRedraw = false;
        int updateFrame_Vertical;
        int updateFrame_Horizontal;
        float verticalMovement;
        public void ResetCell(ElementData _data)
        {
            Data = _data;
            needsRedraw = true;
            hasSimulated = false;
            verticalMovement = 0;
            velocity = Vector2.zero;
        }
        public void SetElement(ElementData _in) { Data = _in; }
        public void UpdateVelocity(Vector2 _velocityAdd, float _maxVelocity)
        {
            velocity += _velocityAdd;
            if (velocity.y > _maxVelocity)
                velocity.y = _maxVelocity;
            if (velocity.y < -_maxVelocity)
                velocity.y = -_maxVelocity;
            verticalMovement += velocity.y;
            //Debug.Log("velocity " + velocity + " movement " + verticalMovement);
        }
        public int GetVerticalLoops(int _currentFrame)
        {
            if (verticalMovement >= 0)
            {
                if (verticalMovement < 1) return 0;
                int floor = Mathf.FloorToInt(verticalMovement);
                verticalMovement -= floor;
                return floor;
            }
            else
            {
                if (verticalMovement > -1) return 0;
                int ceil = Mathf.CeilToInt(verticalMovement);
                verticalMovement -= ceil;
                return ceil;
            }
        }
    }
    [SerializeField] float gravity = 0.4f; //per frame
    [SerializeField] float maxVelocity = 1f; //per frame
    [SerializeField] int liquidSideVelocity = 1; //per frame
    [SerializeField] int width;
    [SerializeField] int height;
    [SerializeField] int brushSize = 1;
    [SerializeField] float FPS = 30;
    [SerializeField] int stepdown_Size = 5;
    [SerializeField] int stepdown_Steps = 10;
    [SerializeField] ElementData SelectedElement;
    [SerializeField] ElementData immutableWall;
    [SerializeField] ElementData air;
    float lastFrameTime;

    Cell[,] grid;
    Texture2D texture;
    int currentFrame;

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
        var m = GetMousePosition();
        if (InGridRange(m.x, m.y))
        {
            if (Input.GetMouseButton(0))
                ApplyBrush(m.x, m.y, SelectedElement);
            if (Input.GetMouseButton(1))
                ApplyBrush(m.x, m.y, air);
        }
    }
    void ApplyBrush(int _x, int _y, ElementData _e)
    {
        for (int x = _x - brushSize; x <= _x + brushSize; x++)
            for (int y = _y - brushSize; y <= _y + brushSize; y++)         
                grid[x, y].ResetCell(_e);           
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
                    texture.SetPixel(x, y, grid[x, y].Data.color);
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].hasSimulated || grid[x, y].needsRedraw)
                {
                    texture.SetPixel(x, y, grid[x, y].Data.color);
                    grid[x, y].hasSimulated = false;
                    grid[x, y].needsRedraw = false;

                }
        texture.Apply();
    }
    void Simulate()
    {
        currentFrame++;
        if (grid == null)
            InitGrid();

        bool flipX = currentFrame % 2 == 0;
        if (flipX)       
            SimulateRLDU();       
        else       
            SimulateLRDU(); 
    }
    void SimulateLRDU()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                SimulatePixel(x, y);
    }
    void SimulateRLDU()
    {
        for (int y = 0; y < height; y++)
            for (int x = width - 1; x >= 0; x--)
                SimulatePixel(x, y);
    }
    void SimulateLRUD()
    {
        for (int y = height - 1; y >= 0; y--)
            for (int x = 0; x < width; x++)
                SimulatePixel(x, y);
    }
    void SimulateRLUD()
    {
        for (int y = height - 1; y >= 0; y--)
            for (int x = width - 1; x >= 0; x--)
                SimulatePixel(x, y);
    }
    public bool simulateDone;
    IEnumerator SlowSimulate()
    {
        currentFrame++;
        simulateDone = false;
        if (grid == null)
            InitGrid();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                bool doyield = GetElement(x, y) != air;
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
        var pixel = grid[_x, _y];
        if (pixel.Data == null)
            pixel.SetElement(air);
        if (pixel.hasSimulated)
            return;
        grid[_x, _y].hasSimulated = true;
        var pos = new Vector2Int(_x, _y);
        switch (pixel.Data.state)
        {
            case MatterState.Solid: SolidUpdate(pos);
                break;
            case MatterState.Liquid: LiquidUpdate(pos);
                break;
            case MatterState.Gas: GasUpdate(pos);
                break;
            default:
                break;
        }
    }
    void SolidUpdate(Vector2Int _pos)
    {
        var pixel = grid[_pos.x, _pos.y];
        var data = grid[_pos.x, _pos.y].Data;
        if (data.useGravity == false)
            return;


        //gravity
        bool belowIsClear = !IsSolid(_pos, D.Down);
        float r = Random.Range(0.9f, 1.1f);
        if (belowIsClear)
            pixel.UpdateVelocity(Vector2.down * gravity * r, maxVelocity);

        int verticalUpdates = Mathf.Abs(pixel.GetVerticalLoops(currentFrame));
        if (verticalUpdates == 0) return;
        for (int i = 0; i < verticalUpdates; i++)
        {
            SolidLoop(ref _pos);
        }
    }
    void SolidLoop(ref Vector2Int _pos)
    {
        bool belowIsClear = !IsSolid(_pos, D.Down);
        var dir = Random.value < 0.5f ? D.DownRight : D.DownLeft;
        var data = grid[_pos.x, _pos.y].Data;

        //check below
        if (belowIsClear)
        {
            ExpelPixel(_pos, D.Down);
            _pos += D.Down;
        }

        //regular falling
        else if (data.stackingHeight <= 1)
        {
            if (!IsSolid(_pos, dir))
            {
                ExpelPixel(_pos, dir);
                _pos += dir;
            }
            else if (!IsSolid(_pos, D.InvertX(dir)))
            {
                ExpelPixel(_pos, D.InvertX(dir));
                _pos += D.InvertX(dir);
            }
        }
        //check a specific depth to the side to see if the stack is high enough to warrant falling
        else
        {
            if (!IsSolid(_pos, dir) && DepthCheck(_pos + dir, data.stackingHeight, MatterState.Gas))
            {
                ExpelPixel(_pos, dir + D.Down);
                _pos += dir + D.Down;
            }
            else if (!IsSolid(_pos, D.InvertX(dir)) && DepthCheck(_pos + D.InvertX(dir), data.stackingHeight, MatterState.Gas))
            { 
                ExpelPixel(_pos, D.InvertX(dir) + D.Down);
                _pos += D.InvertX(dir) + D.Down;
            }
        }
    }
    void LiquidUpdate(Vector2Int _pos)
    {
        var data = grid[_pos.x, _pos.y].Data;
        var dir = Random.value < 0.5f ? D.DownRight : D.DownLeft;

        TryCorrodeSurrounding(_pos);

        bool hasFallen = false;

        for (int i = 0; i < liquidSideVelocity; i++)
        {
            //check below
            if (!hasFallen)
                if (IsGas(_pos, D.Down) || LocationIsLowerDensityLiquid(_pos, D.Down, data))
                {
                    ExpelPixel(_pos, D.Down);
                    hasFallen = true;
                }

            //check down left and down right, but randomize which is checked first
            if (IsGas(_pos, dir) || LocationIsLowerDensityLiquid(_pos, dir, data))
                ExpelPixel(_pos, dir);
            else if (IsGas(_pos, D.InvertX(dir)) || LocationIsLowerDensityLiquid(_pos, D.InvertX(dir), data))
                ExpelPixel(_pos, D.InvertX(dir));

            //check sides
            else
            {
                int sdDir = 0;
                //if this is within a pile of liquid, don't check for the stepdown direction
                if (!IsSame(_pos, D.Up) || !IsSame(_pos, D.Right) || !IsSame(_pos, D.Left))
                    sdDir = GetStepdownDirection(_pos, data);
                if (sdDir != 0)
                    grid[_pos.x, _pos.y].velocity = Vector2.right * sdDir;


                //if no velocity, get random velocity
                var vel = grid[_pos.x, _pos.y].velocity;
                if (vel.x == 0) vel = Random.value < 0.5f ? D.Right : D.Left;

                //try to go in direction of velocity
                //if it's blocked reverse velocity
                //if both sides blocked, kill velocity
                if (vel.x > 0)
                {
                    if (IsGas(_pos, D.Right) || LocationIsLowerDensityLiquid(_pos, D.Right, data))
                        SwapPixels(_pos, D.Right);
                    else if (IsGas(_pos, D.Left) || LocationIsLowerDensityLiquid(_pos, D.Left, data))
                        vel.x *= -1;
                    else
                        vel.x = 0;
                }
                else if (vel.x < 0)
                {
                    if (IsGas(_pos, D.Left) || LocationIsLowerDensityLiquid(_pos, D.Left, data))
                        SwapPixels(_pos, D.Left);
                    else if (IsGas(_pos, D.Right) || LocationIsLowerDensityLiquid(_pos, D.Right, data))
                        vel.x *= -1;
                    else
                        vel.x = 0;
                }

                grid[_pos.x, _pos.y].velocity = vel;
            }
        }
    }
    public void GasUpdate(Vector2Int _pos)
    {
        var data = grid[_pos.x, _pos.y].Data;
        if (data.useGravity == false)
            return;

        var dir = Random.value < 0.5f ? D.UpRight : D.UpLeft;
        var dirX = Random.value < 0.5f ? D.Right : D.Left;

        //randomly move to side
        int densityDifference = air.density - data.density;
        float chance = 1f - (1f / (densityDifference + 0.001f));
        if (Random.value < chance)
            if (LocationIsHigherDensityGas(_pos, dirX, data))
            {
                SwapPixels(_pos, dirX);
                _pos += dirX;
            }

        //check above
        if (LocationIsHigherDensityGas(_pos, D.Up, data))
            ExpelPixel(_pos, D.Up);

        else if (LocationIsHigherDensityGas(_pos, dir, data))
            ExpelPixel(_pos, dir);
        else if (LocationIsHigherDensityGas(_pos, D.InvertX(dir), data))
            ExpelPixel(_pos, D.InvertX(dir));

        else if (LocationIsHigherDensityGas(_pos, dirX, data))
            ExpelPixel(_pos, dirX);
        else if (LocationIsHigherDensityGas(_pos, D.InvertX(dirX), data))
            ExpelPixel(_pos, D.InvertX(dirX));
        
    }
    int GetStepdownDirection(Vector2Int _pos, ElementData _data)
    {
        for (int i = 0; i < stepdown_Steps; i++)
        {
            var rightSide = GetElement(_pos, new Vector2Int(i * stepdown_Size, -1));
            if (rightSide != _data && rightSide != immutableWall)
                return 1;
            var leftSide = GetElement(_pos, new Vector2Int(i * -stepdown_Size, -1));
            if (leftSide != _data && leftSide != immutableWall)
                return -1;
        }
        return 0;
    }
    bool IsSame(Vector2Int _pos, Vector2Int _direction)
    {
        return GetElement(_pos, Vector2Int.zero) == GetElement(_pos, _direction);
    }
    bool TryCorrodeSurrounding(Vector2Int _pos)
    {
        var data = grid[_pos.x, _pos.y].Data;
        if (data.corrosionChance <= 0) return false;

        if (TryCorrode(_pos, D.Down, data)) return true;
        if (TryCorrode(_pos, D.Left, data)) return true;
        if (TryCorrode(_pos, D.Right, data)) return true;
        return false;
    }
    bool TryCorrode(Vector2Int _pos, Vector2Int _direction, ElementData _corrodingAgent)
    {
        var e = GetElement(_pos, _direction);
        if (e.corrodable == false) return false;
        if (Random.value <= _corrodingAgent.corrosionChance)
        {
            //corrode both pixels to the corrosion result
            var result = e.corrosionResult == null ? air : e.corrosionResult;
            grid[_pos.x, _pos.y].SetElement(result);
            grid[_pos.x + _direction.x, _pos.y + _direction.y].SetElement(result);
            return true;
        }
        return false;

    }
    bool DepthCheck(Vector2Int _start, int _depth, MatterState _state)
    {
        for (int i = 0; i < _depth; i++)
        {
            if (GetElement(_start, D.Down).state != _state)
                return false;
            _start += D.Down;
        }
        return true;
    }
    bool IsSolid(Vector2Int _pos, Vector2Int _direction) { return GetElement(_pos, _direction).state == MatterState.Solid; }
    bool IsLiquid(Vector2Int _pos, Vector2Int _direction) { return GetElement(_pos, _direction).state == MatterState.Liquid; }
    bool IsGas(Vector2Int _pos, Vector2Int _direction) { return GetElement(_pos, _direction).state == MatterState.Gas; }
    bool LocationIsLowerDensity(Vector2Int _pos, Vector2Int _direction, ElementData _dataIn)
    {
        ElementData targetData = GetElement(_pos, _direction);
        return targetData.state == _dataIn.state && targetData.density < _dataIn.density;
    }
    bool LocationIsHigherDensity(Vector2Int _pos, Vector2Int _direction, ElementData _dataIn)
    {
        ElementData targetData = GetElement(_pos, _direction);
        return targetData.state == _dataIn.state && targetData.density > _dataIn.density;
    }
    bool LocationIsLowerDensityLiquid(Vector2Int _pos, Vector2Int _direction, ElementData _dataIn)
    {
        var e = GetElement(_pos, _direction);
        return e.state == MatterState.Liquid && e.density < _dataIn.density;
    }
    bool LocationIsHigherDensityGas(Vector2Int _pos, Vector2Int _direction, ElementData _dataIn)
    {
        var e = GetElement(_pos, _direction);
        return e.state == MatterState.Gas && e.density > _dataIn.density;
    }
    bool HasState(Vector2Int _pos, Vector2Int _direction, MatterState _state) { return GetElement(_pos, _direction).state == _state; }

    ElementData GetElement(Vector2Int _pos, Vector2Int _direction)
        => GetElement(_pos.x + _direction.x, _pos.y + _direction.y);
    ElementData GetElement(int _x, int _y)
    {
        if (InGridRange(_x, _y))
            return grid[_x, _y].Data;
        else
            return immutableWall;
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
        /*
        if (GetElement(_pos, D.Right) == air)
            SwapPixels(_pos, D.Right);
        else if (GetElement(_pos, D.Left) == air)
            SwapPixels(_pos, D.Left);*/
        var data = grid[_pos.x, _pos.y].Data;
        if (LocationIsHigherDensity(_pos, D.Right, data))
            SwapPixels(_pos, D.Right);
        else if (LocationIsHigherDensity(_pos, D.Left, data))
            SwapPixels(_pos, D.Left);
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
