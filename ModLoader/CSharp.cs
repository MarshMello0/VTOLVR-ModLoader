using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ModLoader
{
    /* The CS and CSFile command don't work because it won't compile the dll

UCommand cs = new UCommand("cs", "cs <CSharp Code>");
UCommand csfile = new UCommand("csfile", "cs <FileName>");

cs.callbacks.Add(csharp.CS);
csfile.callbacks.Add(csharp.CSFile);

AddCommand(cs);
AddCommand(csfile);
*/
    public class CSharp : MonoBehaviour
    {
        public static CSharp instance;
        public static string compilerPath = "";
        private static string baseContent = "using System;\r\nusing UnityEngine;\r\nusing System.IO;\r\nusing System.Collections.Generic;\r\nusing System.Diagnostics;\r\nusing Steamworks;\r\nusing UnityEngine.AI;\r\nusing UnityEngine.Networking;\r\nusing UnityEngine.SceneManagement;\r\nusing System.Linq;\r\nusing System.Reflection;\r\nusing System.Text;\r\nusing System.Collections;\r\nusing TMPro;\r\nusing Debug = UnityEngine.Debug;\r\nusing Random = UnityEngine.Random;\r\nusing Object = UnityEngine.Object;\r\n\r\npublic class MyClass : VTOLMOD\r\n{\r\n    public void Start()\r\n    {\r\n        USER_CODE\r\n    }\r\n}";
        private static string csfileFolder = @"\CSFile";
        private static string tempFolder = csfileFolder + @"\temp";

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }
        private static bool CheckFolders()
        {
            try
            {
                Directory.CreateDirectory(ModLoaderManager.instance.rootPath + tempFolder);
                return true;
            }
            catch (Exception e)
            {
                UConsole.instance.Log("Failed creating folder: " + e.Message);
                return false;
            }
            
            
        }
        public static bool FindCompiler()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            List<CompailerFolder> possiablePaths = new List<CompailerFolder>();
            if (Directory.Exists(Path.GetFullPath(Path.Combine(folderPath, "../Microsoft.NET/Framework64"))))
            {
                possiablePaths.Add(new CompailerFolder(Path.GetFullPath(Path.Combine(folderPath, "../Microsoft.NET/Framework64"))));
            }
            if (Directory.Exists("C:/Windows/Microsoft.NET/Framework64"))
            {
                possiablePaths.Add(new CompailerFolder("C:/Windows/Microsoft.NET/Framework64"));
            }
            if (Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "Microsoft.NET\\Framework64")))
            {
                possiablePaths.Add(new CompailerFolder(Path.Combine(Environment.GetEnvironmentVariable("windir"), "Microsoft.NET\\Framework64")));
            }
            
            if (possiablePaths.Count == 0)
            {
                UConsole.instance.Log("CSharp: None of the paths existed when seraching for csc.exe");
                return false;
            }

            foreach (CompailerFolder folder in possiablePaths)
            {
                string[] folders = Directory.GetDirectories(folder.path);
                for (int i = 0; i < folders.Length; i++)
                {
                    if (folders[i].Contains("v4"))
                    {
                        folder.version4 = true;
                        folder.path4 = folders[i];
                    }
                    else if (folders[i].Contains("v3.5"))
                    {
                        folder.version3 = true;
                        folder.path3 = folders[i];
                    }
                }
            }
            string compiler = "";
            bool foundVersion4 = false;
            foreach (CompailerFolder folder in possiablePaths)
            {
                //Now we are searching for one with version 4 or any other
                if (!foundVersion4)
                {
                    if (folder.version4 && File.Exists(folder.path4 + @"\csc.exe"))
                    {
                        compiler = folder.path4 + @"\csc.exe";
                        foundVersion4 = true;
                        break; //If we find version 4, we might as well just stop searching for any others
                    }
                    else if (folder.version3 && File.Exists(folder.path3 + @"\csc.exe"))
                    {
                        compiler = folder.path3 + @"\csc.exe";
                    }
                }
            }
            //Might of not found a compiler
            if (!string.IsNullOrEmpty(compiler))
            {
                compilerPath = compiler;
                return true;
            }
            else
            {
                UConsole.instance.Log("CSharp: compiler was null or empty");
                return false;
            }
               
        }

        public void CS(string[] args)
        {
            if (!CheckFolders())
                return;
            if (compilerPath == "")
                if (!FindCompiler())
                    return;
            if (args.Length == 0)
            {
                UConsole.instance.Log("You need to provide some code to run\nEG: cs Debug.Log(\"Hello World\");");
                return;
            }

            string code = string.Join(" ", args);

            UConsole.instance.Log("Turning code into cs file");

            //Saving the .cs file
            string fileName = "cs_" + DateTime.Now.Ticks + ".cs";
            File.WriteAllText(ModLoaderManager.instance.rootPath + csfileFolder +@"\"+ fileName, baseContent.Replace("USER_CODE", code));

            CSFile(new string[] { fileName });
        }

        public void CSFile(string[] args)
        {
            if (compilerPath == "")
                if (!FindCompiler())
                    return;

            if (args.Length == 0 || !args[0].EndsWith(".cs"))
            {
                UConsole.instance.Log("You need to give a file name with .cs");
                return;
            }
            
            StartCoroutine(CSFileIEnumerator(args[0]));
        }

        public IEnumerator CSFileIEnumerator(string fileName)
        {
            string references = "";
            //foreach (string dll in Directory.GetFiles(Directory.GetCurrentDirectory() + @"\VTOLVR_Data\Managed"))
            foreach (string dll in Directory.GetFiles(@"A:\Code\VTOLVR-ModLoader\Unity Project\Build\Unity Project_Data\Managed"))
            {
                if (dll.EndsWith(".dll") && !dll.Contains("System."))
                {
                    references += "/\" " + dll + "\"/,";
                }
            }
            references = references.Remove(references.Length - 1);
            //references += "/\"" + ModLoaderManager.instance.rootPath + @"\ModLoader.dll" + "\"/";
            string tempDllPath = ModLoaderManager.instance.rootPath + tempFolder + @"\" + fileName.Remove(fileName.Length - 3) + ".dll";
            string arguments = string.Format("/r:{0} /out:\"{1}\" /target:library /nostdlib \"{2}\"", references, tempDllPath, ModLoaderManager.instance.rootPath + csfileFolder + @"\" + fileName);
            
            //Running the csc.exe to create the .dll
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = compilerPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            UConsole.instance.Log("Started Compiling...");

            while (!process.HasExited)
            {
                UConsole.instance.Log(process.StandardOutput.ReadToEnd());
                yield return new WaitForSeconds(0.5f);
            }
            UConsole.instance.Log("The Process has closed now");
            //Has been compiled into a .dll, Now trying to run it
            string text2 = process.StandardOutput.ReadToEnd();
            UConsole.instance.Log("1");
            try
            {
                Assembly assembly = Assembly.Load(File.ReadAllBytes(tempDllPath));
                UConsole.instance.Log("1");
                Type[] types = assembly.GetTypes();
                UConsole.instance.Log(types.Count());
                IEnumerable<Type> source = from t in types where t.IsSubclassOf(typeof(VTOLMOD)) select t;
                UConsole.instance.Log("1");
                if (source != null && source.Count() == 1)
                {
                    new GameObject(fileName, source.First());
                }

                UConsole.instance.Log("Compiled!");
            }
            catch (Exception ex)
            {
                if (!File.Exists(tempDllPath))
                {
                    UConsole.instance.LogError("Compilation Failed! CSC Logs > \n" + text2);
                }
                else
                {
                    UConsole.instance.LogError("Code Failed! Error > " + text2 + "\n" + ex.StackTrace);
                }
            }
            if (File.Exists(tempDllPath))
            {
                File.Delete(tempDllPath);
            }
        }

        private class CompailerFolder
        {
            public string path = "";
            public bool version4 = false;
            public string path4;
            public bool version3 = false;
            public string path3;
            public CompailerFolder(string path)
            {
                this.path = path;
            }
        }
    }
}
