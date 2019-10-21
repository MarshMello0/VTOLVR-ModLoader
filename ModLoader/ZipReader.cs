using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ModLoader
{
    public class ZipReader
    {
        /// <summary>
        /// Gets all of the mods info localed in the path into memory
        /// </summary>
        /// <param name="path">The folder to check for mods</param>
        public static List<Mod> GetMods(string path)
        {
            List<Mod> mods = new List<Mod>();
            DirectoryInfo folder = new DirectoryInfo(path);
            FileInfo[] files = folder.GetFiles("*.zip");
            foreach (FileInfo fileInfo in files)
            {
                FileStream file = File.OpenRead(fileInfo.FullName);

                Mod currentMod = new Mod();
                bool hasDLL = false;
                bool hasInfo = false;

                using (ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    Debug.Log("On zip " + file.Name);
                    foreach (ZipArchiveEntry item in zip.Entries)
                    {
                        Debug.Log("On entry of zip: " + item.Name);
                        try
                        {
                            string[] split = item.Name.Split('.');
                            if (split.Length <= 1)
                                continue;
                            string itemExtention = split[1];

                            switch (itemExtention)
                            {
                                case "dll":
                                    MemoryStream ms = new MemoryStream();
                                    item.Open().CopyTo(ms);
                                    byte[] byteArray = ms.ToArray();
                                    Assembly lastAssembly = Assembly.Load(byteArray);
                                    IEnumerable<Type> source = from t in lastAssembly.GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;

                                    if (source.Count() != 1)
                                    {
                                        Debug.LogError("The mod " + file.Name + " doesn't specify a mod class or specifies more than one");
                                        break;
                                    }
                                    else
                                    {
                                        currentMod.assembly = lastAssembly;
                                        hasDLL = true;
                                        Debug.Log("Found dll file");
                                        break;
                                    }
                                case "xml":
                                    XmlSerializer xml = new XmlSerializer(typeof(Mod));
                                    Mod info = (Mod)xml.Deserialize(item.Open());
                                    currentMod.name = info.name;
                                    currentMod.description = info.description;
                                    hasInfo = true;
                                    Debug.Log("Found xml file");
                                    break;
                                case "assets":
                                    break;
                                #region Disabled because of a over read error
                                /* 
                            case "jpg":
                                if (item.Name.Split('.')[0].ToLower() == "preview")
                                {
                                    MemoryStream memoryStream = new MemoryStream();
                                    item.Open().CopyTo(memoryStream);
                                    byte[] bytes = memoryStream.ToArray();
                                    Debug.Log("The length of the bytes = " + bytes.Length);
                                    Texture2D texture = new Texture2D(600, 600, TextureFormat.RGB24,false);
                                    texture.LoadRawTextureData(bytes);
                                    texture.Apply();
                                    currentMod.image = texture;
                                    Debug.Log("Found preview imag file");
                                }
                                break;
                                */
                                #endregion
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Failed to load mod " + e.ToString());
                            throw;
                        }
                    }
                }
                if (hasDLL && hasInfo)
                {
                    mods.Add(currentMod);
                }
            }

            //Searching for just .dll mods

            FileInfo[] dllFilesInfo = folder.GetFiles("*.dll");

            foreach (FileInfo file in dllFilesInfo)
            {
                Mod currentMod = new Mod();
                bool hasDLL = false;
                try
                {
                    byte[] byteArray = File.ReadAllBytes(file.FullName);
                    Assembly lastAssembly = Assembly.Load(byteArray);
                    IEnumerable<Type> source = from t in lastAssembly.GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;

                    if (source.Count() != 1)
                    {
                        Debug.LogError("The mod " + file.Name + " doesn't specify a mod class or specifies more than one");
                        continue;
                    }
                    else
                    {
                        currentMod.assembly = lastAssembly;
                        currentMod.name = file.Name;
                        currentMod.description = "This only a .dll file, please make mods into .zip with a xml file when releasing the mod.";
                        hasDLL = true;
                        Debug.Log("Found dll file");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("There was an error when trying to load a .dll mod.\n" +
                        file.Name + " doesn't seem to derive from VTOLMOD");
                    continue;
                }
                if (hasDLL)
                {
                    mods.Add(currentMod);
                }
                
            }
            return mods;
        }

        /// <summary>
        /// Add the mods to the list without effecting the current mods
        /// </summary>
        /// <param name="path">Folder where the mods are located</param>
        /// <param name="currentMods">The current list of mods</param>
        /// <returns>True if there where new mods</returns>
        public static bool GetNewMods(string path, ref List<Mod> currentMods)
        {
            List<Mod> mods = GetMods(path);
            List<Mod> newMods = mods.Except(currentMods).ToList();
            if (newMods.Count > 0)
            {
                currentMods.AddRange(newMods);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class Mod
    {
        public string name;
        public string description;
        [XmlIgnore]
        public Assembly assembly;
        [XmlIgnore]
        public GameObject listGO;
        [XmlIgnore]
        public bool isLoaded;
        [XmlIgnore]
        public Texture2D image;

        public Mod() { }
    }
}
