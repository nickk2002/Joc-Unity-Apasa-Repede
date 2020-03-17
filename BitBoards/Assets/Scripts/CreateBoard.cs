using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Numerics;

public class CreateBoard : MonoBehaviour
{
    public int size = 8;
    [SerializeField] Camera camera;
    [SerializeField] GameObject[] tilePrefab;
    [SerializeField] GameObject housePrefab;
    [SerializeField] GameObject treePrefeb;
    [SerializeField] Text score;
    [SerializeField] Text enemyScore;
    [SerializeField] Text bitBoardDebug;
    [SerializeField] Text finalText;
    [SerializeField] float scoresForDirt = 10;
    [SerializeField] float scoreForDesert = 2;
    [SerializeField] float PCSpeed;
    [SerializeField] bool debug;
    GameObject[] tilesArray;
    SortedSet<Tuple<int,int>> dirtSortedSet;
    List<Tuple<int, int>> dirtList;
    BigInteger dirtBitBoard = 0;
    BigInteger desertBitBoard = 0;
    BigInteger treeBitBoard = 0;
    BigInteger playerBitBoard = 0;
    private int lastPlayerHousei = -1, lastPlayerHousej = -1;
    
   
    void Generate()
    {
        tilesArray = new GameObject[size * size];
        dirtList = new List<Tuple<int, int>>();
        dirtSortedSet = new SortedSet<Tuple<int, int>>();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                
                int index = UnityEngine.Random.Range(0, tilePrefab.Length);
                UnityEngine.Vector3 position = new UnityEngine.Vector3(j, 0, i);
                GameObject tile = Instantiate(tilePrefab[index], position, UnityEngine.Quaternion.identity);
                tile.transform.SetParent(this.transform);
                tile.name = tile.tag + "_" + i.ToString() + "_" + j.ToString();
                tilesArray[i * size + j] = tile;
                if (tile.tag == "Dirt")
                {
                    dirtList.Add(new Tuple<int, int>(i, j));
                    dirtSortedSet.Add(new Tuple<int,int>(i,j));
                    ModifyCellState(ref dirtBitBoard, i, j);
                }
                else if (tile.tag == "Desert")
                    ModifyCellState(ref desertBitBoard, i, j);
            }
        }
        InvokeRepeating("PlantTree", 0f, PCSpeed);
    }
    float dist(Tuple<int,int>a, Tuple<int, int> b)
    {
        return Mathf.Abs(a.Item1 - b.Item1) + Mathf.Abs(a.Item2 - b.Item2);
    }
    void PlantTree()
    {
        if (dirtSortedSet.Count == 0)
            return;
        var first_enum = dirtSortedSet.GetEnumerator();
        first_enum.MoveNext();  
        int i = first_enum.Current.Item1;
        int j = first_enum.Current.Item2;
        
        if(lastPlayerHousei != -1 || lastPlayerHousej != -1)
        {
            var it = first_enum;
            float minDist = 2e9f;
            
            while (it.MoveNext())
            {
                float curdist = dist(it.Current, new Tuple<int, int>(lastPlayerHousei, lastPlayerHousej));
                //Debug.Log(curdist + " " + it.Current.Item1 + " " + it.Current.Item2);
                if (curdist < minDist)
                {
                    minDist = curdist;
                    i = it.Current.Item1;
                    j = it.Current.Item2;
                }
            }
            //Debug.Log(minDist);
        }


        //Debug.LogWarning(i + " " + j);
        dirtSortedSet.Remove(new Tuple<int,int>(i,j));
        if (GetCellState(treeBitBoard | playerBitBoard,i,j) == true) return;
        if (GetCellState(dirtBitBoard,i,j) == true)
        {
            GameObject tree = Instantiate(treePrefeb);
            tree.transform.parent = tilesArray[i * size + j].transform;
            tree.transform.localPosition = UnityEngine.Vector3.zero;
            ModifyCellState(ref treeBitBoard, i, j);
            CalculateEnemyScore();
        }
    }
    void ModifyCellState(ref BigInteger bitBoard,int row,int col)
    {
        BigInteger bit = (BigInteger)1 << (row * size + col);
        bitBoard |= bit;
    }
    bool GetCellState(BigInteger bitBoard,int row,int col)
    {
        BigInteger unu = 1;
        BigInteger bit = unu << (row * size + col);
        return (bitBoard & bit) != 0; 
    }

    int CountNumber(BigInteger bitBoard)
    {
        int cnt = 0;
        while (bitBoard != 0)
        {
            cnt++;
            bitBoard &= bitBoard - 1;
        }
        return cnt;
    }
    void PrintBitBoard(string name, BigInteger bitBoard)
    {
        string text = "";
        bitBoardDebug.text = "The current state of the BitBoard for " + name + " is : ";
        for (int i = 0; i < size; i++)
        {
            for (int j = size - 1; j >= 0; j--)
            {
                int number = Convert.ToInt32(GetCellState(bitBoard, i, j));
                text += number.ToString();
            }
            text += '\n';
        }

        bitBoardDebug.text += text;
        bitBoardDebug.text += "there are : " + CountNumber(bitBoard);
    }
    float CalculateScore()
    {
        float number = CountNumber(playerBitBoard & dirtBitBoard) * scoresForDirt + CountNumber(playerBitBoard & desertBitBoard) * scoreForDesert;
        score.text = "Score: " + number.ToString();
        return number;
    }
    float CalculateEnemyScore()
    {
        float number = CountNumber(treeBitBoard) * scoresForDirt;
        enemyScore.text = "PC Score : " + number.ToString();
        return number;
    }

    // Start is called before the first frame update
    void Awake()
    {
        finalText.enabled = false;
        foreach (Transform go in this.transform)
        {
            Destroy(go.gameObject);
        }
        camera.transform.position = new UnityEngine.Vector3(size / 2,3,size / 2);
        camera.orthographicSize = size / 2 + 1f;
        Generate();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (finalText.enabled == true)
            return;
        if (playerBitBoard == (dirtBitBoard & ~treeBitBoard | desertBitBoard)){

            finalText.enabled = true;
            if (CalculateEnemyScore() > CalculateScore())
                finalText.text += "We are sorry the Pc is better than you :)";
            else if (CalculateEnemyScore() == CalculateScore())
                finalText.text += "Congrats is a tie";
            else if(CalculateEnemyScore() < CalculateScore())
                finalText.text += "You are better than the PC.Congratulations! The PC is worse than you";
        }
        // player places house
        if (Input.GetMouseButton(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray,out hit))
            {
               
                int i = (int)hit.collider.gameObject.transform.position.z;
                int j = (int)hit.collider.gameObject.transform.position.x;
                if (GetCellState((dirtBitBoard & ~treeBitBoard) | desertBitBoard, i, j) == true)
                {
                    GameObject house = Instantiate(housePrefab);
                    house.transform.parent = hit.collider.gameObject.transform;
                    house.transform.localPosition = UnityEngine.Vector3.zero;
                    ModifyCellState(ref playerBitBoard, i, j);
                    CalculateScore();
                    dirtSortedSet.Remove(new Tuple<int,int>(i,j));
                    lastPlayerHousei = i;
                    lastPlayerHousej = j;
                }
                else
                {
                    Debug.LogWarning("You can't place a house there!");
                }
            }
        }
        if(debug == true)
             PrintBitBoard("desert", desertBitBoard);
    }
}
