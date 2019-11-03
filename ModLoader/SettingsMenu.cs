using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Settings
{
    public List<SubSetting> subSettings { private set; get; } = new List<SubSetting>();
    public Mod Mod { private set; get; }
    public Settings(Mod mod) { Mod = mod; }
    public void CreateStringSetting(string settingName,Action<string>[] callbacks,string defaultValue = "")
    {
        subSettings.Add(new StringSetting(settingName, callbacks, defaultValue));
    }
    public void CreateBoolSetting(string settingName, Action<bool>[] callbacks, bool defaultValue = false)
    {
        subSettings.Add(new BoolSetting(settingName, callbacks, defaultValue));
    }
    public void CreateIntSetting(string settingName, Action<int>[] callbacks, int defaultValue = 0,
        int minValue = int.MinValue, int maxValue = int.MaxValue)
    {
        subSettings.Add(new IntSetting(settingName, callbacks, defaultValue, minValue, maxValue));
    }
    public void CreateFloatSetting(string settingName, Action<float>[] callbacks, float defaultValue = 0,
        float minValue = float.MinValue, float maxValue = float.MaxValue,float incrementValue = 0.1f)
    {
        subSettings.Add(new FloatSetting(settingName, callbacks, defaultValue, minValue, maxValue, incrementValue));
    }

    public class SubSetting
    {
        public string settingName;
    }

    public class StringSetting : SubSetting
    {
        public Action<string>[] callbacks;
        public string defaultValue;
        public StringSetting(string settingName, Action<string>[] callbacks, string defaultValue = "")
        {
            this.settingName = settingName;
            this.callbacks = callbacks;
            this.defaultValue = defaultValue;
        }
    }
    public class BoolSetting : SubSetting
    {
        public Action<bool>[] callbacks;
        public bool defaultValue;
        public bool currentValue;
        public BoolSetting(string settingName, Action<bool>[] callbacks, bool defaultValue = false)
        {
            this.settingName = settingName;
            this.callbacks = callbacks;
            this.defaultValue = defaultValue;
        }
    }
    public class IntSetting : SubSetting
    {
        public Action<int>[] callbacks;
        public int defaultValue;
        public int minValue;
        public int maxValue;
        public IntSetting (string settingName, Action<int>[] callbacks, int defaultValue = 0,
        int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            this.settingName = settingName;
            this.callbacks = callbacks;
            this.defaultValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }

    public class FloatSetting : SubSetting
    {
        public Action<float>[] callbacks;
        public float defaultValue;
        public float minValue;
        public float maxValue;
        public float incrementValue;
        public FloatSetting(string settingName, Action<float>[] callbacks, float defaultValue = 0,
        float minValue = float.MinValue, float maxValue = float.MaxValue, float incrementValue = 0.1f)
        {
            this.settingName = settingName;
            this.callbacks = callbacks;
            this.defaultValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.incrementValue = incrementValue;
        }
    }

}