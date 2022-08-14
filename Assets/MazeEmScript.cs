using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Rnd = UnityEngine.Random;

public class MazeEmScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable ModuleSelectable;
    public KMSelectable[] Buttons;
    public GameObject Static;
    public Text MainText;

    private KeyCode[] TypableKeys =
    {
        KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A,
        KeyCode.Return,
        KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow,
    };
    private int Row, Column, SubmittedRow, SubmittedColumn, Corrects;
    private List<int[]> Goals = new List<int[]>();
    private int[,] Numbers = new int[4, 4];
    private int[,] Matrix = new int[16, 16];
    private List<int> PossibleNumbers = new List<int>();
    private bool[][] Walls = { new bool[4], new bool[5], new bool[4], new bool[5], new bool[4], new bool[5], new bool[4], new bool[5], new bool[4] };
    private bool[,] VisitedSquares = new bool[4, 4];
    private bool Focused, CannotPress, Inputting, Solved;
    private KMAudio.KMAudioRef Sound;

    string FindPathBetween(int startX, int startY, int goalX, int goalY)
    {
        int currentRow = startX;
        int currentColumn = startY;
        string directions = "";
        while (currentRow != goalX || currentColumn != goalY)
        {
            directions += "URDL"[Matrix[(currentRow * 4) + currentColumn, (goalX * 4) + goalY] - 1];
            switch (Matrix[(currentRow * 4) + currentColumn, (goalX * 4) + goalY])
            {
                case 1:
                    currentRow = (currentRow + 3) % 4;
                    break;
                case 2:
                    currentColumn = (currentColumn + 1) % 4;
                    break;
                case 3:
                    currentRow = (currentRow + 1) % 4;
                    break;
                default:
                    currentColumn = (currentColumn + 3) % 4;
                    break;
            }
        }
        return directions;
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        CannotPress = true;
        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { if (!CannotPress) ButtonPress(x); return false; };
        }
        ModuleSelectable.OnFocus += delegate { Focused = true; };
        ModuleSelectable.OnDefocus += delegate { Focused = false; };
        StartCoroutine(AnimateScreen());
        MainText.text = "";
        Static.transform.localScale = new Vector3();
        for (int i = 0; i < Walls.Length; i++)
            for (int j = 0; j < Walls[i].Length; j++)
                Walls[i][j] = true;
        GenerateMaze(Rnd.Range(0, 4), Rnd.Range(0, 4));
        LogMaze();
        for (int i = 0; i < 100; i++)
            PossibleNumbers.Add(i);
        PossibleNumbers.Shuffle();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                Numbers[i, j] = PossibleNumbers[(i * 4) + j];
        List<int[]> possibleCoords = new List<int[]>();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                possibleCoords.Add(new int[] { i, j });
        possibleCoords.Shuffle();
        for (int i = 0; i < 6; i++)
            Goals.Add(possibleCoords[i]);
        List<string> logGoals = new List<string>();
        for (int i = 0; i < Goals.Count; i++)
            logGoals.Add("(" + (Goals[i][0] + 1) + ", " + (Goals[i][1] + 1) + ")");
        for (int i = 0; i < 3; i++)
            Numbers[Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]] = Numbers[Goals[i * 2][0], Goals[i * 2][1]];
        string logNumbers = "";
        for (int i = 0; i < 4; i++)
        {
            List<string> temp = new List<string>();
            for (int j = 0; j < 4; j++)
                temp.Add(Numbers[i, j].ToString("00"));
            logNumbers += temp.Join(" ");
            if (i != 4)
                logNumbers += "\n";
        }
        Row = Rnd.Range(0, 4);
        Column = Rnd.Range(0, 4);
        Module.OnActivate += delegate { MainText.text = Numbers[Row, Column].ToString("00"); Static.transform.localScale = new Vector3(2, 2, 2); CannotPress = false; };
        Debug.LogFormat("[Maze 'em #{0}] The grid of numbers is as follows:\n{1}", _moduleID, logNumbers);
        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Maze 'em #{0}] The numbers in cells {1} and {2} are the same. You can get from {1} to {2} by following this sequence of directions: {3}.",
                _moduleID, logGoals[i * 2], logGoals[(i * 2) + 1], FindPathBetween(Goals[i * 2][0], Goals[i * 2][1], Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]));
        Debug.LogFormat("[Maze 'em #{0}] The starting cell is cell ({1}, {2}).", _moduleID, (Row + 1).ToString(), (Column + 1).ToString());
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < TypableKeys.Count(); i++)
        {
            if (Input.GetKeyDown(TypableKeys[i]) && Focused)
            {
                if (i < 5)
                    Buttons[i].OnInteract();
                else
                    Buttons[i - 5].OnInteract();
            }
        }
    }

    void ButtonPress(int pos)
    {
        if (pos != 4)
        {
            if (!Solved)
            {
                bool valid = false;
                switch (pos)
                {
                    case 0:
                        if (!Walls[Row * 2][Column])
                        {
                            valid = true;
                            Row--;
                        }
                        break;
                    case 1:
                        if (!Walls[(Row * 2) + 1][Column + 1])
                        {
                            valid = true;
                            Column++;
                        }
                        break;
                    case 2:
                        if (!Walls[(Row * 2) + 2][Column])
                        {
                            valid = true;
                            Row++;
                        }
                        break;
                    default:
                        if (!Walls[(Row * 2) + 1][Column])
                        {
                            valid = true;
                            Column--;
                        }
                        break;
                }
                if (valid)
                {
                    Audio.PlaySoundAtTransform("move", Buttons[4].transform);
                    StartCoroutine(Move(pos));
                }
                else if (!Inputting)
                    Audio.PlaySoundAtTransform("buzzer", Buttons[4].transform);
                else
                {
                    Module.HandleStrike();
                    Debug.LogFormat("[Maze 'em #{0}] You moved {1} into a wall on cell ({2}, {3}). Strike!", _moduleID, new string[] { "north", "east", "south", "west" }[pos], (Row + 1).ToString(), (Column + 1).ToString());
                    MainText.text = Numbers[Row, Column] == -1 ? "--" : Numbers[Row, Column].ToString("00");
                    Sound = Audio.HandlePlaySoundAtTransformWithRef("strike", Buttons[4].transform, false);
                    Static.GetComponent<Image>().color = new Color32(64, 50, 64, 255);
                    Inputting = false;
                }
            }
            Audio.PlaySoundAtTransform("press", Buttons[pos].transform);
            StartCoroutine(ButtonAnim(pos));
        }
        else
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Buttons[4].transform);
            if (!Solved)
            {
                if (!Inputting)
                {
                    if (MainText.text != "--")
                    {
                        try
                        {
                            Sound.StopSound();
                        }
                        catch { }
                        Sound = Audio.HandlePlaySoundAtTransformWithRef("submit", Buttons[4].transform, false);
                        MainText.text = "??";
                        Static.GetComponent<Image>().color = new Color32(50, 64, 50, 255);
                        Inputting = true;
                        SubmittedRow = Row;
                        SubmittedColumn = Column;
                        Debug.LogFormat("[Maze 'em #{0}] Submission mode has been entered.", _moduleID);
                    }
                    else
                        Audio.PlaySoundAtTransform("buzzer", Buttons[4].transform);
                }
                else
                {
                    MainText.text = Numbers[Row, Column] == -1 ? "--" : Numbers[Row, Column].ToString("00");
                    bool correct = false;
                    for (int i = 0; i < 3; i++)
                        if ((SubmittedRow == Goals[i * 2][0] && SubmittedColumn == Goals[i * 2][1] && Row == Goals[(i * 2) + 1][0] && Column == Goals[(i * 2) + 1][1])
                            || (Row == Goals[i * 2][0] && Column == Goals[i * 2][1] && SubmittedRow == Goals[(i * 2) + 1][0] && SubmittedColumn == Goals[(i * 2) + 1][1]))
                            correct = true;
                    if (correct)
                    {
                        Corrects++;
                        Debug.LogFormat("[Maze 'em #{0}] You submitted cells ({1}, {2}) and ({3}, {4}), which was correct.", _moduleID, (SubmittedRow + 1).ToString(), (SubmittedColumn + 1).ToString(), (Row + 1).ToString(), (Column + 1).ToString());
                        Numbers[SubmittedRow, SubmittedColumn] = -1;
                        Numbers[Row, Column] = -1;
                        if (Corrects >= 3)
                        {
                            Module.HandlePass();
                            Audio.PlaySoundAtTransform("solve", Buttons[4].transform);
                            Debug.LogFormat("[Maze 'em #{0}] Module solved!", _moduleID);
                            StartCoroutine(SolveAnim());
                            MainText.text = "GG";
                            Solved = true;
                            Inputting = false;
                        }
                        else
                        {
                            Audio.PlaySoundAtTransform("correct", Buttons[4].transform);
                            Static.GetComponent<Image>().color = new Color32(64, 50, 64, 255);
                            MainText.text = "--";
                            Inputting = false;
                        }
                    }
                    else
                    {
                        Debug.LogFormat("[Maze 'em #{0}] You submitted cells ({1}, {2}) and ({3}, {4}), which was incorrect. Strike!", _moduleID, (SubmittedRow + 1).ToString(), (SubmittedColumn + 1).ToString(), (Row + 1).ToString(), (Column + 1).ToString());
                        Module.HandleStrike();
                        try
                        {
                            Sound.StopSound();
                        }
                        catch { }
                        Sound = Audio.HandlePlaySoundAtTransformWithRef("strike", Buttons[4].transform, false);
                        Static.GetComponent<Image>().color = new Color32(64, 50, 64, 255);
                        Inputting = false;
                    }
                }
            }
        }
        Buttons[pos].AddInteractionPunch(0.5f);
    }

    void GenerateMaze(int x, int y)
    {
        VisitedSquares[x, y] = true;
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        directions.Shuffle();
        for (int i = 0; i < 4; i++)
        {
            switch (directions[i])
            {
                case 0:
                    if (y != 0 && VisitedSquares[x, y - 1] != true)
                    {
                        Walls[y * 2][x] = false;
                        GenerateMaze(x, y - 1);
                    }
                    break;
                case 1:
                    if (x != 3 && VisitedSquares[x + 1, y] != true)
                    {
                        Walls[(y * 2) + 1][x + 1] = false;
                        GenerateMaze(x + 1, y);
                    }
                    break;
                case 2:
                    if (y != 3 && VisitedSquares[x, y + 1] != true)
                    {
                        Walls[(y * 2) + 2][x] = false;
                        GenerateMaze(x, y + 1);
                    }
                    break;
                default:
                    if (x != 0 && VisitedSquares[x - 1, y] != true)
                    {
                        Walls[(y * 2) + 1][x] = false;
                        GenerateMaze(x - 1, y);
                    }
                    break;
            }
        }
        for (int i = 0; i < 16; i++)
        {
            if (!Walls[(i / 4) * 2][i % 4])
                Matrix[i, i - 4] = 1;
            if (!Walls[((i / 4) * 2) + 1][(i % 4) + 1])
                Matrix[i, i + 1] = 2;
            if (!Walls[((i / 4) * 2) + 2][i % 4])
                Matrix[i, i + 4] = 3;
            if (!Walls[((i / 4) * 2) + 1][i % 4])
                Matrix[i, i - 1] = 4;
        }
        for (int i = 0; i < 16; i++)
        {
            int[,] Matrix2 = new int[16, 16];
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                    for (int l = 0; l < 16; l++)
                        if (Matrix2[j, l] == 0 && Matrix[k, l] != 0)
                            Matrix2[j, l] = Matrix[j, k];
            for (int j = 0; j < 16; j++)
                for (int k = 0; k < 16; k++)
                    if (Matrix[j, k] == 0)
                        Matrix[j, k] = Matrix2[j, k];
        }
    }

    void LogMaze()
    {
        string mazeLog = "";
        for (int i = 0; i < Walls.Length; i++)
        {
            if (i % 2 == 0)
            {
                if (i != 0)
                    mazeLog += "\n█";
                else
                    mazeLog += "█";
                for (int j = 0; j < Walls[i].Length; j++)
                {
                    if (Walls[i][j])
                        mazeLog += "█";
                    else
                        mazeLog += "░";
                    mazeLog += "█";
                }
            }
            else
            {
                mazeLog += "\n";
                for (int j = 0; j < Walls[i].Length; j++)
                {
                    if (Walls[i][j])
                        mazeLog += "█";
                    else
                        mazeLog += "░";
                    if (j != Walls[i].Length - 1)
                        mazeLog += "X";
                }
            }
        }
        Debug.LogFormat("[Maze 'em #{0}] The maze is as follows:\n{1}", _moduleID, mazeLog);
    }

    void UndoSubmit()
    {
        try
        {
            Sound.StopSound();
        }
        catch { }
        Sound = Audio.HandlePlaySoundAtTransformWithRef("submit", Buttons[4].transform, false);
        MainText.text = Numbers[Row, Column] == -1 ? "--" : Numbers[Row, Column].ToString("00");
        Static.GetComponent<Image>().color = new Color32(64, 50, 64, 255);
        Inputting = false;
        Debug.LogFormat("[Maze 'em #{0}] Submission mode has been exited due to being autosolved (({1}, {2}) was not correct).", _moduleID, (SubmittedRow + 1).ToString(), (SubmittedColumn + 1).ToString());
    }

    private IEnumerator ButtonAnim(int pos, float down = 0.05f, float up = 0.05f, float start = 0.015f, float end = 0.01f)
    {
        float timer = 0;
        while (timer < down)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, Mathf.Lerp(start, end, timer * (1 / down)), Buttons[pos].transform.localPosition.z);
        }
        Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, end, Buttons[pos].transform.localPosition.z);
        timer = 0;
        while (timer < up)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, Mathf.Lerp(end, start, timer * (1 / up)), Buttons[pos].transform.localPosition.z);
        }
        Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, start, Buttons[pos].transform.localPosition.z);
    }

    private IEnumerator AnimateScreen(float interval = 0.025f)
    {
        while (true)
        {
            Static.transform.localPosition = new Vector3(Rnd.Range(-0.045f, 0.045f), Rnd.Range(-0.045f, 0.045f), 0);
            Static.transform.localEulerAngles = new Vector3(0, 0, Rnd.Range(0, 4) * 90);
            float timer = 0;
            while (timer < interval)
            {
                yield return null;
                timer += Time.deltaTime;
            }
        }
    }

    private IEnumerator Move(int pos, float duration = 0.25f)
    {
        CannotPress = true;
        Text OtherText = Instantiate(MainText);
        OtherText.transform.SetParent(MainText.transform.parent, false);
        OtherText.transform.localPosition = new Vector3(pos == 1 ? 0.1f : pos == 3 ? -0.1f : 0,
                pos == 0 ? 0.1f : pos == 2 ? -0.1f : 0, 0);
        OtherText.text = Inputting ? "??" : Numbers[Row, Column] == -1 ? "--" : Numbers[Row, Column].ToString("00");
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            MainText.transform.localPosition = new Vector3(pos == 1 ? Easing.OutSine(timer, 0, -0.1f, duration) : pos == 3 ? Easing.OutSine(timer, 0, 0.1f, duration) : 0,
                pos == 0 ? Easing.OutSine(timer, 0, -0.1f, duration) : pos == 2 ? Easing.OutSine(timer, 0, 0.1f, duration) : 0, 0);
            OtherText.transform.localPosition = new Vector3(pos == 1 ? Easing.OutSine(timer, 0.1f, 0, duration) : pos == 3 ? Easing.OutSine(timer, -0.1f, 0, duration) : 0,
                pos == 0 ? Easing.OutSine(timer, 0.1f, 0, duration) : pos == 2 ? Easing.OutSine(timer, -0.1f, 0, duration) : 0, 0);
        }
        MainText.transform.localPosition = new Vector3();
        MainText.text = OtherText.text;
        Destroy(OtherText);
        CannotPress = false;
    }

    private IEnumerator SolveAnim(float duration = 0.5f)
    {
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Static.GetComponent<Image>().color = new Color32(byte.Parse(Mathf.Max(Mathf.RoundToInt(Easing.InExpo(timer, 50, 0, duration)), 0).ToString()),
                byte.Parse(Mathf.Max(Mathf.RoundToInt(Easing.InExpo(timer, 64, 0, duration)), 0).ToString()),
                byte.Parse(Mathf.Max(Mathf.RoundToInt(Easing.InExpo(timer, 50, 0, duration)), 0).ToString()), 255);
        }
        Static.GetComponent<Image>().color = new Color(0, 0, 0, 1);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} urdl*s' to move up, then right, then down, then left, then press the submit button, then scout the current cell. If you ask the module to 'scout', it will try moving in each of the four directions, returning to the cell it was on if it moves successfully.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string validcmds = "urdl*s";
        for (int i = 0; i < command.Length; i++)
        {
            if (!validcmds.Contains(command[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] != 's')
            {
                Buttons[validcmds.IndexOf(command[i])].OnInteract();
                if (command[i] != '*')
                {
                    if (CannotPress)
                        while (CannotPress)
                            yield return null;
                    else
                    {
                        yield return "sendtochaterror A wall was hit; stopping command execution.";
                        yield break;
                    }
                }
                else
                {
                    float timer = 0;
                    while (timer < 0.1f)
                    {
                        yield return null;
                        timer += Time.deltaTime;
                    }
                }
            }
            else if (!Inputting)
            {
                for (int j = 0; j < 4; j++)
                {
                    int rowCache = Row;
                    int columnCache = Column;
                    Buttons[j].OnInteract();
                    if (rowCache != Row || columnCache != Column)
                    {
                        while (CannotPress)
                            yield return null;
                        Buttons[(j + 2) % 4].OnInteract();
                        while (CannotPress)
                            yield return null;
                    }
                    else
                    {
                        float timer = 0;
                        while (timer < 0.5f)
                        {
                            yield return null;
                            timer += Time.deltaTime;
                        }
                    }
                }
            }
            else
            {
                yield return "sendtochaterror You can't scout while in input mode!";
                yield break;
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (Inputting)
        {
            if (Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) == -1)
            {
                UndoSubmit();
                yield return true;
            }
            else
            {
                string directions = FindPathBetween(Row, Column, Goals[Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) + 1][0], Goals[Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) + 1][1]);
                if (Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) % 2 == 1)
                    directions = FindPathBetween(Row, Column, Goals[Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) - 1][0], Goals[Goals.IndexOf(new int[] { SubmittedRow, SubmittedColumn }) - 1][1]);
                for (int i = 0; i < directions.Length; i++)
                {
                    Buttons["URDL".IndexOf(directions[i])].OnInteract();
                    while (CannotPress)
                        yield return true;
                }
                Buttons[4].OnInteract();
                yield return true;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            if (Numbers[Goals[i * 2][0], Goals[i * 2][1]] != -1)
            {
                string directions = FindPathBetween(Row, Column, Goals[i * 2][0], Goals[i * 2][1]).Length < FindPathBetween(Row, Column, Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]).Length ?
                    FindPathBetween(Row, Column, Goals[i * 2][0], Goals[i * 2][1]) : FindPathBetween(Row, Column, Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]);
                Debug.Log(directions);
                for (int j = 0; j < directions.Length; j++)
                {
                    Buttons["URDL".IndexOf(directions[j])].OnInteract();
                    while (CannotPress)
                        yield return true;
                }
                Buttons[4].OnInteract();
                yield return true;
                directions = FindPathBetween(Row, Column, Goals[i * 2][0], Goals[i * 2][1]).Length > FindPathBetween(Row, Column, Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]).Length ?
                    FindPathBetween(Row, Column, Goals[i * 2][0], Goals[i * 2][1]) : FindPathBetween(Row, Column, Goals[(i * 2) + 1][0], Goals[(i * 2) + 1][1]);
                Debug.Log(directions);
                for (int j = 0; j < directions.Length; j++)
                {
                    Buttons["URDL".IndexOf(directions[j])].OnInteract();
                    while (CannotPress)
                        yield return true;
                }
                Buttons[4].OnInteract();
                yield return true;
            }
        }
    }
    //mdoule
}
