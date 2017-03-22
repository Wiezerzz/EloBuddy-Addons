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
        private static TowerRange.TowerRange towerRange;
        private static WardTracker.Tracker wardTracker;
        private static RecallTracker.Tracker recallTracker;
        private static GankAlerter.Alerter gankAlerter;

        private static void Main(string[] args)
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
            menu.Add("loadtowerrangeindicator", new CheckBox("Load Tower Range Indicator plugin"));
            menu.Add("loadwardtracker", new CheckBox("Load Ward Tracker plugin"));
            menu.Add("loadrecalltracker", new CheckBox("Load Recall Tracker plugin"));
            menu.Add("loadgankalerter", new CheckBox("Load Gank Alerter plugin"));


            if (menu["loadcooldowntracker"].Cast<CheckBox>().CurrentValue)
                cooldownTracker = new CooldownTracker.Tracker(menu);

            if(menu["loadtowerrangeindicator"].Cast<CheckBox>().CurrentValue)
                towerRange = new TowerRange.TowerRange(menu);

            if (menu["loadwardtracker"].Cast<CheckBox>().CurrentValue)
                wardTracker = new WardTracker.Tracker(menu);

            if (menu["loadrecalltracker"].Cast<CheckBox>().CurrentValue)
                recallTracker = new RecallTracker.Tracker(menu);

            if (menu["loadgankalerter"].Cast<CheckBox>().CurrentValue)
                gankAlerter = new GankAlerter.Alerter(menu);
        }
    }
}
