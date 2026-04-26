using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace TheEvilWithinTrainer
{
    internal sealed class TrainerProfile
    {
        public string ProfileName { get; set; }
        public string ProcessName { get; set; }
        public string ModuleName { get; set; }
        public int PollIntervalMs { get; set; }
        public List<CheatDefinition> Cheats { get; set; }

        internal static TrainerProfile Load(string path)
        {
            return LoadFromJson(File.ReadAllText(path));
        }

        internal static TrainerProfile LoadFromJson(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            TrainerProfile profile = serializer.Deserialize<TrainerProfile>(json);
            if (profile == null)
            {
                throw new InvalidOperationException("Profile could not be parsed.");
            }

            if (profile.Cheats == null)
            {
                profile.Cheats = new List<CheatDefinition>();
            }

            if (profile.PollIntervalMs <= 0)
            {
                profile.PollIntervalMs = 1000;
            }

            return profile;
        }
    }

    internal sealed class CheatDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Hotkey { get; set; }
        public string ActionType { get; set; }
        public string Pattern { get; set; }
        public int PatchOffset { get; set; }
        public string ModuleOffsetHex { get; set; }
        public string ExpectedBytes { get; set; }
        public string PatchBytes { get; set; }
        public string EnableBytes { get; set; }
        public string DisableBytes { get; set; }
        public int OverwriteSize { get; set; }
        public string CaveBytes { get; set; }
        public int RelativeReadOffset { get; set; }
        public int RelativeAdjust { get; set; }
        public string Description { get; set; }
    }
}

