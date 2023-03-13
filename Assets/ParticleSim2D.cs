using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum Element
{
    Air,
    Wall,
    Sand,
    Water
}
public class ParticleSim2D : MonoBehaviour
{
    [SerializeField] int width;
    [SerializeField] int height;
    Element[,] grid;
    Texture2D texture;

    public Vector2 mousePos;
    public Vector2 adjPos;
    public float screenAspect;
    private void Update()
    {
        HandleInput();
        Simulate();
        GenerateTexture();
    }
    private void OnGUI()
    {
        if (texture == null) return;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture, ScaleMode.ScaleToFit, true);
    }
    /*private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(texture, destination);
    }*/
    void HandleInput()
    {
        if (Input.GetKey(KeyCode.Space))
            grid[width / 2, height - 1] = Element.Sand;

        var m = GetMousePosition();
        if (InGridRange(m.x, m.y))
        {
            if (Input.GetMouseButton(0))
                grid[m.x, m.y] = Element.Sand;
            if (Input.GetMouseButton(1))
                grid[m.x, m.y] = Element.Water;
        }

        
    }
    Vector2Int GetMousePosition()
    {
        Vector2Int output = new Vector2Int();
        var mouse = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        output.y = (int)(mouse.y * height);

        mousePos = mouse;

        float textureAspect = (float)width / height;
        float screenAspect = (float)Screen.width / Screen.height;
        this.screenAspect = screenAspect;


        float aspectDif = (float)textureAspect / screenAspect;
        float xOffset = (1f - aspectDif) / 2f;
        float xMult = 1 / aspectDif;

        output.x = (int)((mouse.x - xOffset) * xMult * width);

        adjPos = output;
        adjPos.x = (mouse.x - xOffset) * xMult;

        return output;
    }
    void GenerateTexture()
    {
        if (texture == null)
        {
            texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                texture.SetPixel(x,y, GetColor(grid[x,y]));
        texture.Apply();
    }
    void Simulate()
    {
        if (grid == null) grid = new Element[width, height];
        for (int x = 0; x < width; x++)       
            for (int y = 0; y < height; y++)           
                CheckElement(x, y);         
    }
    void CheckElement(int _x, int _y)
    {
        switch (grid[_x,_y])
        {
            case Element.Air:
                break;
            case Element.Wall:
                break;
            case Element.Sand: SandUpdate(_x, _y);
                break;
            case Element.Water: WaterUpdate(_x, _y);
                break;
            default:
                break;
        }
    }
    void SandUpdate(int _x, int _y)
    {
        int r = Random.value < 0.5f ? 1 : -1;
        //check below
        if (GetElement(_x, _y - 1) == Element.Air)
            SwapPixels(_x, _y, 0, -1);
        if (GetElement(_x, _y - 1) == Element.Water)
            ExpelPixel(_x, _y, 0, -1);
        //check below left and right, but randomize which is checked first
        else if (GetElement(_x + r, _y - 1) == Element.Air)
            SwapPixels(_x, _y, r, -1);
        else if (GetElement(_x + r, _y - 1) == Element.Water)
            ExpelPixel(_x, _y, r, -1);

        else if (GetElement(_x - r, _y - 1) == Element.Air)
            SwapPixels(_x, _y, -r, -1);
        else if (GetElement(_x - r, _y - 1) == Element.Water)
            ExpelPixel(_x, _y, -r, -1);
    }
    void WaterUpdate(int _x, int _y)
    {
        int r = Random.value < 0.5f ? 1 : -1;
        //check below
        if (GetElement(_x, _y - 1) == Element.Air)
            SwapPixels(_x, _y, 0, -1);
        //check below left and right, but randomize which is checked first
        else if (GetElement(_x + r, _y - 1) == Element.Air)
            SwapPixels(_x, _y, r, -1);
        else if (GetElement(_x - r, _y - 1) == Element.Air)
            SwapPixels(_x, _y, -r, -1);
        //check sides
        
        else if (GetElement(_x + r, _y) == Element.Air)
            SwapPixels(_x, _y, r, 0);
        else if (GetElement(_x - r, _y) == Element.Air)
            SwapPixels(_x, _y, -r, 0);
    }
    Element GetElement(Vector2Int _pos, Vector2Int _direction) 
        => GetElement(_pos.x + _direction.x, _pos.y + _direction.y);
    Element GetElement(int _x, int _y)
    {
        if (InGridRange(_x, _y))
            return grid[_x, _y];
        else
            return Element.Wall;
    }
    bool InGridRange(int _x, int _y)
    {
        return (_x >= 0 && _y >= 0 && _x < width && _y < height);
    }
    void SwapPixels(int _x, int _y, int _offsetx, int _offsety)
    {
        Element e1 = grid[_x, _y];
        Element e2 = grid[_x + _offsetx, _y + _offsety];
        grid[_x + _offsetx, _y + _offsety] = e1;
        grid[_x, _y] = e2;
    }
    void ExpelPixel(int _x, int _y, int _offsetx, int _offsety)
    {
        Element e1 = grid[_x, _y];
        Element e2 = grid[_x + _offsetx, _y + _offsety];
        grid[_x + _offsetx, _y + _offsety] = e1;

        //swap the pixel to the initial pos
        grid[_x, _y] = e2;

        //try to push pixel to the side
        if (GetElement(_x + 1, _y) == Element.Air)
            SwapPixels(_x, _y, 1, 0);
        else if (GetElement(_x - 1, _y) == Element.Air)
            SwapPixels(_x, _y, -1, 0);            
    }
    Color GetColor(Element _e)
    {
        switch (_e)
        {
            case Element.Air: return Color.black;
            case Element.Wall: return Color.black;
            case Element.Sand: return Color.yellow;
            case Element.Water: return Color.blue;
            default: return Color.black;
        }
    }
}
