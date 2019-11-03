using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;
using System.Linq;


public class UConsole : MonoBehaviour
{
    public static UConsole instance { private set; get; }
    [Header("Settings")]
    public KeyCode ToggleConsoleKey = KeyCode.F1;
    public bool isEnabled { get; private set; } = false;
    public List<UCommand> commands { private set; get; } = new List<UCommand>();
    [Tooltip("Shows the type of message it was, log, warning or error")]
    public bool showType = false;
    [Tooltip("Shows the unity logs as well in the console")]
    public bool showUnityLogs = true;

    [SerializeField]
    private InputField input;
    [SerializeField]
    private Text output;
    [SerializeField]
    private GameObject consoleHolder;
    [SerializeField]
    private bool DoNotDestroyOnLoad = true;

    private List<string> lines = new List<string>();
    private string[] visableLines;
    private int startLine = 0;

    private void Awake()
    {
        if (UConsole.instance == null)
            UConsole.instance = this;
        else
            Destroy(this.gameObject);

        if (DoNotDestroyOnLoad)
            DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        CreateDefaultCommands();
        Application.logMessageReceived += LogMessageReceived;
        consoleHolder.SetActive(isEnabled);
    }

    private void LogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (showUnityLogs)
            lines.Add((showType ? type.ToString() + ": " : "") + condition);
    }
    private void CreateDefaultCommands()
    {
        UCommand helpcommand = new UCommand("help", "help <command>");
        helpcommand.callbacks.Add(Help);
        AddCommand(helpcommand);

        UCommand clearcommand = new UCommand("clear", "clear");
        clearcommand.callbacks.Add(Clear);
        AddCommand(clearcommand);
    }
    private void Clear(string[] obj)
    {
        lines = new List<string>();
        startLine = 0;
        UpdateVisableLines();
    }
    private void Help(string[] obj)
    {
        if (obj.Length == 0)
        {
            Log("All Commands");
            for (int i = 0; i < commands.Count; i++)
            {
                Log(commands[i].command + " \"" + commands[i].usage + "\"");
            }
        }
        else
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].command.ToLower() == obj[0].ToLower())
                {
                    Log("Command : " + commands[i].command + " \"" + commands[i].usage + "\"");
                    break;
                }

            }
        }

    }
    private void Update()
    {
        if (Input.GetKeyDown(ToggleConsoleKey))
        {
            consoleHolder.SetActive(!isEnabled);
            isEnabled = !isEnabled;
        }

        if (isEnabled)
            IsEnabled();
    }
    private void IsEnabled()
    {
        if (input.text != string.Empty && Input.GetKeyDown(KeyCode.Return))
        {
            RunCommand(input.text);
            input.text = string.Empty;
            startLine = lines.Count - (lines.Count > 9 ? 9 : lines.Count);
            UpdateVisableLines();
            input.Select();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            startLine--;
            UpdateVisableLines();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            startLine++;
            UpdateVisableLines();
        }
    }
    private void UpdateVisableLines()
    {
        startLine = Mathf.Clamp(startLine, 0, lines.Count - (lines.Count > 9 ? 9 : lines.Count));
        visableLines = new string[(lines.Count > 9 ? 9 : lines.Count)];
        Array.Copy(lines.ToArray(), startLine, visableLines, 0, lines.Count >= 9 ? 9 : lines.Count);
        output.text = string.Join("\n", visableLines);
    }
    public void RunCommand(string command)
    {
        Log(command);
        string[] args = command.Split(' ');
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].command.ToLower() == args[0].ToLower())
            {
                string[] shortargs = new string[args.Length - 1];
                Array.Copy(args, 1, shortargs, 0, args.Length - 1);
                for (int j = 0; j < commands[i].callbacks.Count; j++)
                {
                    commands[i].callbacks[j].Invoke(shortargs);
                }
                return;
            }
        }
        LogError("Unknow command " + command + ". Try \"help\" to see all the commands");
    }
    public void Log(object message)
    {
        if (!showUnityLogs)
            lines.Add((showType ? "Log: " : "") + message);
        else
            Debug.Log(message);
    }
    public void LogError(object message)
    {
        if (!showUnityLogs)
            lines.Add((showType ? "Error: " : "") + message);
        else
            Debug.LogError(message);
    }
    public void AddCommand(UCommand newCommand)
    {
        commands.Add(newCommand);
    }
    public bool RemoveCommand(string command)
    {
        for (int i = 0; i < command.Length; i++)
        {
            if (commands[i].command == command)
            {
                commands.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}

public class UCommand
{
    public string command { private set; get; }
    public string usage { private set; get; }
    public List<Action<string[]>> callbacks;
    public UCommand(string command, string usage)
    {
        this.command = command;
        this.usage = usage;
        callbacks = new List<Action<string[]>>();
    }
}

