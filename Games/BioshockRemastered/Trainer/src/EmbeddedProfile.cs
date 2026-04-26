namespace BioshockRemasteredTrainer
{
    internal static class EmbeddedProfile
    {
        internal static string GetDefaultProfileJson()
        {
            return @"{
  ""ProfileName"": ""BioShock Remastered Steam/GOG v1.0.122872 + Epic 127355 partial trainer"",
  ""ProcessName"": ""BioshockHD.exe"",
  ""ModuleName"": ""BioshockHD.exe"",
  ""PollIntervalMs"": 500,
  ""Cheats"": [
    {
      ""Id"": ""god-mode"",
      ""Name"": ""God Mode"",
      ""Hotkey"": ""F1"",
      ""ActionType"": ""bioshock"",
      ""Pattern"": ""8B 7F 1C 89 4C 24 30"",
      ""Description"": ""Ported from the Paul44 CE table. Installs the player controller collector and flips the controller god-mode flag.""
    },
    {
      ""Id"": ""invisible"",
      ""Name"": ""Invisible"",
      ""Hotkey"": ""F2"",
      ""ActionType"": ""bioshock"",
      ""Pattern"": ""F6 81 00 07 00 00 01"",
      ""Description"": ""Uses the table's visibility hook so enemies generally stop detecting the player.""
    },
    {
      ""Id"": ""lock-consumables"",
      ""Name"": ""Lock Consumables"",
      ""Hotkey"": ""F3"",
      ""ActionType"": ""bioshock"",
      ""Description"": ""Patches the table's ammo, Adam, credits and Eve consumption paths so values stop decreasing.""
    },
    {
      ""Id"": ""one-hit-kill"",
      ""Name"": ""1-Hit Kill Enemy"",
      ""Hotkey"": ""F4"",
      ""ActionType"": ""bioshock"",
      ""Description"": ""Clamps enemy health to 1.0 using the CE table logic while skipping the player and Little Sisters.""
    },
    {
      ""Id"": ""no-alerts"",
      ""Name"": ""No Alerts"",
      ""Hotkey"": ""F5"",
      ""ActionType"": ""bioshock"",
      ""Description"": ""Ports the alert manager hook and removes active alarm/alert time while enabled.""
    },
    {
      ""Id"": ""protect-little-sister"",
      ""Name"": ""Protect Little Sister"",
      ""Hotkey"": ""F6"",
      ""ActionType"": ""bioshock"",
      ""Description"": ""Marks escorted Little Sisters as protected when their health code path is visited.""
    },
    {
      ""Id"": ""unlock-gene-slots"",
      ""Name"": ""Unlock Gene Slots"",
      ""Hotkey"": ""F7"",
      ""ActionType"": ""bioshock"",
      ""Description"": ""Sets the active Gene Bank slot count to 6. Enable once, then open a Gene Bank.""
    }
  ]
}";
        }
    }
}
