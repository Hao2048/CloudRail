using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloudRail
{
    [Serializable]
    public sealed class JourneySave
    {
        public int version = 1;
        public List<string> stars = new List<string>();
        public bool gardenLine;
        public bool clockLine;
        public int safeStation;
        public bool completed;
        public bool allStars;
        public bool journeyInTransit;
        public bool reduceMotion = true;
        public bool lowEffects;
        public float effectsVolume = .5f;
        public float musicVolume = .35f;
        public float sensitivity = 1f;
    }

    public static class JourneySaveStore
    {
        static JourneySave Parse(string path)
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<JourneySave>(json);
            if (data == null || data.version != 1 || data.stars == null || data.safeStation < 0 || data.safeStation > 2)
                throw new InvalidDataException("Unsupported or incomplete journey save.");
            foreach (string id in data.stars)
                if (id == null || !(id.StartsWith("cloud_") || id.StartsWith("garden_")))
                    throw new InvalidDataException("Unknown star identifier.");
            data.effectsVolume = Mathf.Clamp01(data.effectsVolume);
            // Saves made before the soundtrack existed should start audibly. Explicit zero remains mute.
            data.musicVolume = json.Contains("\"musicVolume\"") ? Mathf.Clamp01(data.musicVolume) : .35f;
            data.sensitivity = Mathf.Clamp(data.sensitivity, .4f, 2f);
            if(!json.Contains("\"reduceMotion\""))data.reduceMotion=true;
            return data;
        }

        public static JourneySave Read(string path, out string notice, out bool blocked)
        {
            notice = ""; blocked = false;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new JourneySave();
            try { return Parse(path); }
            catch (Exception primary) when (primary is IOException || primary is ArgumentException || primary is InvalidDataException)
            {
                try
                {
                    var backup = Parse(path + ".bak");
                    notice = "已从上一份有效存档恢复旅程";
                    return backup;
                }
                catch (Exception secondary) when (secondary is IOException || secondary is ArgumentException || secondary is InvalidDataException)
                {
                    blocked = true;
                    notice = "存档无法读取。按 Esc 选择重新开始；原文件会保留。";
                    return new JourneySave();
                }
            }
        }

        public static void Write(string path, JourneySave data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            // Only a successfully parsed primary is allowed to replace the known-good backup.
            if (File.Exists(path))
            {
                try { Parse(path); File.Copy(path, path + ".bak", true); }
                catch (InvalidDataException) { }
                catch (ArgumentException) { }
                File.Replace(temporary, path, null);
            }
            else File.Move(temporary, path);
        }
    }
}
