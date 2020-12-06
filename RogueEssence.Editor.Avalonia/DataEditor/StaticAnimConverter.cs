﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RogueEssence.Content;
using RogueEssence.Dungeon;

namespace RogueEssence.Dev
{
    public class StaticAnimConverter : TestableConverter<StaticAnim>
    {
        protected override void btnTest_Click(object sender, RoutedEventArgs e, StaticAnim obj)
        {
            if (DungeonScene.Instance.ActiveTeam.Players.Count > 0 && DungeonScene.Instance.FocusedCharacter != null)
            {
                Character player = DungeonScene.Instance.FocusedCharacter;

                StaticAnim data = (StaticAnim)Activator.CreateInstance(obj.GetType());
                SaveClassControls(data, (StackPanel)((Button)sender).Parent);
                data.SetupEmitted(player.MapLoc, 0, player.CharDir);
                DungeonScene.Instance.CreateAnim(data, DrawLayer.Normal);
            }
        }
    }
}