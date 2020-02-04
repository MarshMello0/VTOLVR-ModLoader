using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using ModLoader;
public class Settings
{
    public List<SubSetting> subSettings { private set; get; } = new List<SubSetting>();
    public VTOLMOD Mod { private set; get; }
    public Settings(VTOLMOD mod) { Mod = mod; }
    public void CreateStringSetting(string settingName,UnityAction<string> callback,string defaultValue = "")
    {
        subSettings.Add(new StringSetting(settingName, callback, defaultValue));
    }
    public void CreateBoolSetting(string settingName, UnityAction<bool> callback, bool defaultValue = false)
    {
        subSettings.Add(new BoolSetting(settingName, callback, defaultValue));
    }
    public void CreateIntSetting(string settingName, UnityAction<int> callback, int defaultValue = 0,
        int minValue = int.MinValue, int maxValue = int.MaxValue)
    {
        subSettings.Add(new IntSetting(settingName, callback, defaultValue, minValue, maxValue));
    }
    public void CreateFloatSetting(string settingName, UnityAction<float> callback, float defaultValue = 0,
        float minValue = float.MinValue, float maxValue = float.MaxValue,float incrementValue = 0.1f)
    {
        subSettings.Add(new FloatSetting(settingName, callback, defaultValue, minValue, maxValue, incrementValue));
    }

    public class SubSetting
    {
        public string settingName;
    }

    public class StringSetting : SubSetting
    {
        public UnityAction<string> callback;
        public string defaultValue;
        public StringSetting(string settingName, UnityAction<string> callback, string defaultValue = "")
        {
            this.settingName = settingName;
            this.callback = callback;
            this.defaultValue = defaultValue;
        }
    }
    public class BoolSetting : SubSetting
    {
        public UnityAction<bool> callback;
        public bool defaultValue;
        public bool currentValue;
        public BoolSetting(string settingName, UnityAction<bool> callback, bool defaultValue = false)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.defaultValue = defaultValue;
        }
    }
    public class IntSetting : SubSetting
    {
        public UnityAction<int> callback;
        public int defaultValue;
        public int minValue;
        public int maxValue;
        public IntSetting (string settingName, UnityAction<int> callback, int defaultValue = 0,
        int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.defaultValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }

    public class FloatSetting : SubSetting
    {
        public UnityAction<float> callback;
        public float defaultValue;
        public float minValue;
        public float maxValue;
        public float incrementValue;
        public FloatSetting(string settingName, UnityAction<float> callback, float defaultValue = 0,
        float minValue = float.MinValue, float maxValue = float.MaxValue, float incrementValue = 0.1f)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.defaultValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.incrementValue = incrementValue;
        }
    }

}