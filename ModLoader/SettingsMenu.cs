using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
    public void CreateFloatSetting(string settingName, UnityAction<float> callback, float currentValue = 0,
        float minValue = float.MinValue, float maxValue = float.MaxValue,float incrementValue = 0.1f)
    {
        subSettings.Add(new FloatSetting(settingName, callback, currentValue, minValue, maxValue));
    }
    public void CreateCustomLabel(string text)
    {
        subSettings.Add(new CustomLabel(text));
    }

    public class SubSetting
    {
        public string settingName;
    }

    public class StringSetting : SubSetting
    {
        public UnityAction<string> callback;
        public string value;
        public Text text;
        public StringSetting(string settingName, UnityAction<string> callback, string value)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.value = value;
        }
        public void SetValue(string value)
        {
            this.value = value;
            if (text != null)
                text.text = value;
            if (callback != null)
                callback.Invoke(this.value);
        }
    }
    public class BoolSetting : SubSetting
    {
        public UnityAction<bool> callback;
        public bool defaultValue;
        public bool currentValue;
        public Text text;
        public BoolSetting(string settingName, UnityAction<bool> callback, bool defaultValue = false)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.defaultValue = defaultValue;
            currentValue = defaultValue;
        }
        public void Invoke()
        {
            currentValue = !currentValue;
            if (text != null)
                text.text = currentValue.ToString();
            if (callback != null)
                callback.Invoke(currentValue);
        }
    }
    public class IntSetting : SubSetting
    {
        public UnityAction<int> callback;
        public int value;
        public int minValue;
        public int maxValue;
        public Text text;
        public IntSetting (string settingName, UnityAction<int> callback,int value,
        int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.value = value;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public void SetValue(int value)
        {
            this.value = Mathf.Clamp(value, minValue, maxValue);
            if (text != null)
                text.text = this.value.ToString();
            if (callback != null)
                callback.Invoke(this.value);
        }
        public void SetValue(string value)
        {
            int result;
            if (int.TryParse(value, out result))
            {
                SetValue(result);
            }
        }
    }
    public class FloatSetting : SubSetting
    {
        public UnityAction<float> callback;
        public float value;
        public float minValue;
        public float maxValue;
        public Text text;

        public FloatSetting(string settingName, UnityAction<float> callback, float value, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            this.settingName = settingName;
            this.callback = callback;
            this.value = value;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
        public void SetValue(float value)
        {
            this.value = Mathf.Clamp(value, minValue, maxValue);
            if (text != null)
                text.text = this.value.ToString();
            if (callback != null)
                callback.Invoke(this.value);
        }
        public void SetValue(string value)
        {
            float result;
            if (float.TryParse(value, out result))
            {
                SetValue(result);
            }

        }
    }
    public class CustomLabel : SubSetting
    {
        public CustomLabel(string text)
        {
            settingName = text;
        }
    }

}