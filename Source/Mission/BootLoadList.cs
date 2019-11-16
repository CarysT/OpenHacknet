﻿using System;
using System.Collections.Generic;

namespace Hacknet.Mission
{
    public static class BootLoadList
    {
        public static List<string> getList()
        {
            return getListFromData(Utils.readEntireFile("Content/Computers/BootLoadList.txt"));
        }

        public static List<string> getDemoList()
        {
            return getListFromData(Utils.readEntireFile("Content/Computers/DemoLoadList.txt"));
        }

        public static List<string> getAdventureList()
        {
            return getListFromData(Utils.readEntireFile("Content/AdventureNetwork/AdventureLoadList.txt"));
        }

        private static List<string> getListFromData(string data)
        {
            var separator = new string[2]
            {
                "\n\r",
                "\r\n"
            };
            var strArray = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            for (var index = 0; index < strArray.Length; ++index)
            {
                if (!strArray[index].StartsWith("#") && strArray[index].Length > 1)
                    list.Add(strArray[index]);
            }
            return list;
        }
    }
}