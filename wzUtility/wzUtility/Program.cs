using System;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace wzUtility
{
    class Program
    {
        private static Menu menu;
        private static CooldownTracker.Tracker cooldownTracker;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Bootstrap.Init(null);

            menu = MainMenu.AddMenu("wzUtility", "wzutility");
            menu.AddGroupLabel("wzUtility");

            menu.AddLabel("To enable/disable a plugin you have to reload the addon. (default reload button is F5)");
            menu.Add("loadcooldowntracker", new CheckBox("Load Cooldown Tracker plugin"));

            if (menu["loadcooldowntracker"].Cast<CheckBox>().CurrentValue)
                cooldownTracker = new CooldownTracker.Tracker(menu);

        }
    }
}
