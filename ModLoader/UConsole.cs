using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;

public class UConsole : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("This is the key to open the console. Default = Back Quote \"`\" ")]
    public KeyCode consoleKey = KeyCode.Tab;
    [Tooltip("This will cause UConsole to start outputting Debug.Log messages. Default = False")]
    public bool debugUConsole = false;



    /// <summary>
    /// Is true when the main console window is open. Read Only!
    /// </summary>
    public bool isOpen { get; private set; }
    /// <summary>
    /// This is an array of all the last arguments passed thought, this is split by ' '. Remember the first one is always going to be your command. Read Only!
    /// </summary>
    public string[] lastArgs { get; private set; }

    public struct ConsoleMessage
    {
        public string condition;
        public string stackTrace;
        public LogType type;
    }

    /// <summary>
    /// This is a list of all the current messages in the console.
    /// This list gets cleared with ClearLogs() and is stored in the same format as normal unity logs.
    /// Read Only!
    /// </summary>
    public List<ConsoleMessage> messages { get; private set; }

    private GameObject mainWindow;
    private Text output;
    private RectTransform outputTransform;
    private InputField inputfield;
    private Scrollbar scrollbar;
    private List<Command> commands = new List<Command>();
    private static UConsole uConsole;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        if (uConsole == null)
            uConsole = this;
        else
            Destroy(gameObject);

        messages = new List<ConsoleMessage>();
        mainWindow = transform.Find("MainWindow").gameObject;
        scrollbar = gameObject.GetComponentInChildren<Scrollbar>();
        output = mainWindow.transform.GetChild(1).GetComponentInChildren<Text>();
        outputTransform = output.rectTransform;
        inputfield = mainWindow.GetComponentInChildren<InputField>();
        //mainWindow.SetActive(false);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += LogReceived;
        Log("UConsole Started!");
    }

    private void Start()
    {
        AddCommand("test", "Just a test command display the different types of messages you can get", Test);
        AddCommand("ping", "says a message back", Ping);
        AddCommand("clear", "clears the console", ClearLogs);
        AddCommand("uconsole.debug", "If you want UConsole to output its debug messages to the console, requires 1 more arguemnt eg \"uconsole.debug false\"", ToggleDebug);
        AddCommand("help", "Displays all the possiable commands including custom ones, Help can take 1 extra arg which is help and then a command to just display about that one command", Help);
    }

    private void FixedUpdate()
    {
        CheckForKeys();
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= LogReceived;
    }

    private void CheckForKeys()
    {
        if (Input.GetKeyDown(consoleKey))
        {
            SetActive(!isOpen);
        }

        if (inputfield.text != "" && inputfield.isFocused && Input.GetKey(KeyCode.Return))
        {
            FindCommand();
        }
    }

    private void Focus()
    {
        inputfield.ActivateInputField();
        inputfield.Select();
    }

    /// <summary>
    /// Used to open or close the Main console window
    /// </summary>
    public void SetActive(bool state)
    {
        Log("Setting UConsole sate to " + state);
        isOpen = state;
        mainWindow.SetActive(state);

        if (state)
            Focus();
    }

    /// <summary>
    /// Add custom commands into UConsole
    /// </summary>
    public void AddCommand(string command, string description, Action action)
    {
        Regex regex = new Regex("^[a-zA-Z0-9.]*$");
        if (command.Contains(" "))
        {
            Debug.LogError("Command " + command + " contains spaces!");
        }
        else if (!regex.IsMatch(command))
        {
            Debug.LogError("Command " + command + " contains invalids characters!");
        }
        else
        {
            foreach (Command c in commands)
            {
                if (c.command.ToLower().Equals(command.ToLower()))
                {
                    Debug.LogError("Command " + command + " is already registered!");
                    return;
                }
            }

            Command newCommand = new Command();
            newCommand.action = action;
            newCommand.command = command;
            newCommand.description = description;
            commands.Add(newCommand);
            Log(newCommand.command + " has been added");
        }

    }

    /// <summary>
    /// Removes any custom commands
    /// </summary>
    public void RemoveCommand(string command)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].command == command)
            {
                commands.RemoveAt(i);
                Log(command + " has been removed");
                return;
            }
        }

        Debug.LogError(command + " could not be found to be removed from UConsole");
    }

    private void Log(object obj)
    {
        if (debugUConsole)
            Debug.Log(obj);
    }

    private void LogReceived(string condition, string stackTrace, LogType type)
    {
        ConsoleMessage lastMessage = new ConsoleMessage();
        lastMessage.condition = condition;
        lastMessage.stackTrace = stackTrace;
        lastMessage.type = type;
        messages.Insert(0, lastMessage);

        output.text = output.text + LogToString(lastMessage);
        StartCoroutine(MoveScrollBar());
    }

    IEnumerator MoveScrollBar()
    {
        yield return new WaitForSeconds(0.1f);
        scrollbar.value = 0;
    }

    private string LogToString(ConsoleMessage log)
    {
        string returnMessage = @"
";

        if (log.type == LogType.Error)
        {
            returnMessage += "<color=red>";
        }
        else if (log.type == LogType.Warning)
        {
            returnMessage += "<color=yellow>";
        }
        else if (log.type == LogType.Log)
        {
            returnMessage += "<color=white>";
        }
        returnMessage += log.condition + "</color>";
        return returnMessage;
    }

    /// <summary>
    /// Clears the in game console logs by destorying all the gameobjects and reseting the counters
    /// </summary>
    public void ClearLogs()
    {
        output.text = "";
        messages.Clear();
    }

    public void FindCommand()
    {
        string message = inputfield.text;
        inputfield.text = "";
        Focus();
        Log("Command Received: " + message);
        string[] args = message.Split(' ');

        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].command == args[0])
            {
                lastArgs = args;
                commands[i].action();
                return;
            }
        }

        UnknowCommand(message);
    }

    private void Test()
    {
        Debug.Log("This is an log message");
        Debug.LogWarning("This is an warning message");
        Debug.LogError("This is an error message");
    }

    private void Ping()
    {
        Debug.Log("Pong!");
    }

    private void Help()
    {
        string[] args = lastArgs;
        if (args.Length == 1)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                LogReceived("<b>" + commands[i].command + "</b>", "", LogType.Log);
                LogReceived(commands[i].description, "", LogType.Log);
                LogReceived("", "", LogType.Log);
            }
        }
        else
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].command == args[1])
                {
                    LogReceived("<b>" + commands[i].command + "</b>", "", LogType.Log);
                    LogReceived(commands[i].description, "", LogType.Log);
                    return;
                }
            }

            LogReceived("Unknow Command", "", LogType.Warning);
        }
    }

    private void ToggleDebug()
    {
        string[] args = lastArgs;
        if (args.Length < 2)
        {
            Debug.LogError("uconsole.debug requires another argument eg \"uconsole.debug false\"");
            return;
        }

        string state = args[1].ToLower();

        if (state == "false" || state == "0" || state == "f")
        {
            debugUConsole = false;
            Debug.Log("UConsole Debug Mode is now " + debugUConsole);
        }
        else if (state == "true" || state == "1" || state == "t")
        {
            debugUConsole = true;
            Debug.Log("UConsole Debug Mode is now " + debugUConsole);
        }
        else
        {
            Debug.LogWarning("Unknow argument of " + args[1]);
            Debug.Log("uconsole.debug requires another argument eg \"uconsole.debug false\"");
        }
    }
    private void UnknowCommand(string message)
    {
        Debug.LogWarning("Unknow Command: " + message);
    }
}

public class Command
{
    /// <summary>
    /// This is the description of the command, this description will be used in the help menu explain what this command does 
    /// </summary>
    public string description;
    /// <summary>
    /// This is the command which you type into the console to run it
    /// </summary>
    public string command;
    /// <summary>
    /// This is the action which UConsole will call. 
    /// </summary>
    public Action action;
}