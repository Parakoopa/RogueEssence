﻿using System;

namespace RogueEssence.Data
{
    [Serializable]
    public class SkillGroupData : IEntryData
    {
        public override string ToString()
        {
            return Name.DefaultText;
        }

        public LocalText Name { get; set; }
        public bool Released { get { return true; } }
        public string Comment { get; set; }

        public EntrySummary GenerateEntrySummary() { return new EntrySummary(Name, Released, Comment); }

        public SkillGroupData() { }

        public SkillGroupData(LocalText name)
        {
            Name = name;
        }
    }
}
