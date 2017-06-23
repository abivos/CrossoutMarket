﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crossout.Data;
using Crossout.Data.Descriptions;
using Crossout.Data.Stats;
using Crossout.Data.Stats.Main;
using Crossout.Model.Items;

namespace Crossout.Web.Services
{
    public class CrossoutDataService
    {
        private string replaceValuesPattern = @"(?<start>[^\$]+)\$(?<key>[^\$]+)\$(?<end>.+)";
        private Regex replaceValuesRegex;
        private CrossoutDataService()
        {
            replaceValuesRegex = new Regex(replaceValuesPattern);
        }

        public PartStatsCollection CoreStatsCollection { get; } = new PartStatsCollection();
        public PartStatsCollection WeaponStatsCollection { get; } = new PartStatsCollection();
        public PartStatsCollection MovementStatsCollection { get; } = new PartStatsCollection();
        public ReverseItemLookup ReverseItemLookup { get; } = new ReverseItemLookup();
        public StringLookup StringLookup { get; } = new StringLookup();

        public static void Initialize()
        {
            _instance = new CrossoutDataService();
            _instance.LoadFiles();
        }

        private void LoadFiles()
        {
            var rootPath = RootPathProvider.GetRootPathStatic();
            ReverseItemLookup.ReadStats(Path.Combine(rootPath, WebSettings.Settings.FileStringsEnglish));
            StringLookup.ReadStats(Path.Combine(rootPath, WebSettings.Settings.FileStringsEnglish));
            WeaponStatsCollection.ReadStats<PartStatsWeapon>(Path.Combine(rootPath, WebSettings.Settings.FileCarEditorWeaponsExLua));
            MovementStatsCollection.ReadStats<PartStatsWheel>(Path.Combine(rootPath, WebSettings.Settings.FileCarEditorWheelsLua));
            CoreStatsCollection.ReadStats<PartStatsCore>(Path.Combine(rootPath, WebSettings.Settings.FileCarEditorCoreLua));
        }

        public PartStatsBase Get(string internalKey, PartStatsCollection statsCollection)
        {
            if (ReverseItemLookup.Items.ContainsKey(internalKey))
            {
                var key = ReverseItemLookup.Items[internalKey];
                if (statsCollection.Items.ContainsKey(key))
                {
                    return statsCollection.Items[key];
                }
            }
            return null;
        }

        public string GetKey(string internalKey)
        {
            if (ReverseItemLookup.Items.ContainsKey(internalKey))
            {
                var key = ReverseItemLookup.Items[internalKey];
                return key;
            }
            return null;
        }

        

        public void AddData(Item item)
        {
            AddStats(item);
            AddDescription(item);
            ReplaceValues(item);
        }

        private void AddStats(Item item)
        {
            //1	Frame
            //2	Weapons
            //3	Hardware
            //4	Movement
            //5	Structure
            //6	Decor
            //7	Dyes
            //8	Resources

            const int CategoryWeapon = 2;
            const int CategoryHardware = 3;
            const int CategoryMovement = 4;

            if (item.CategoryId == CategoryWeapon) // Rewrite with better lookup to avoid magic values.
            {
                item.Stats = Get(item.Name, WeaponStatsCollection); // TODO: Decide what Stats
            }

            if (item.CategoryId == CategoryHardware)
            {
                item.Stats = Get(item.Name, CoreStatsCollection); // TODO: Decide what Stats
            }

            if (item.CategoryId == CategoryMovement)
            {
                item.Stats = Get(item.Name, MovementStatsCollection); // TODO: Decide what Stats
            }
        }

        private void AddDescription(Item item)
        {
            var key = GetKey(item.Name);
            if (key != null)
            {
                var desc = StringLookup.ParseDescription(StringLookup.ReadDescription(key));
                item.Description = new ItemDescription { Text = desc };
            }
        }

        private void ReplaceValues(Item item)
        {
            string result = item.Description.Text;
            var matches = replaceValuesRegex.Matches(result);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var key = match.Groups["key"].Value;
                    if (item.Stats.Fields.ContainsKey(key))
                    {
                        var value = item.Stats.Fields[key];
                        result = Regex.Replace(result, replaceValuesPattern, $"${{start}}{value}${{end}}");
                    }
                }
            }
            item.Description.Text = result;
        }

        private static CrossoutDataService _instance;

        public static CrossoutDataService Instance
        {
            get
            {
                return _instance;
            }
        }
    }
}
