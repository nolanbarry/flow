﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Colorlink
{
    public static class Management
    {
        #region File Parsing
        /// <summary>
        /// Returns the nth level in the given file as a Grid
        /// </summary>
        /// <param name="targetLevel">Which level to find in the file.</param>
        /// <param name="path">The file path.</param>
        /// <returns></returns>
        public static Grid ParseFileIntoGrid(int targetLevel, string path)
        {
            string[] file = File.ReadAllLines("Assets\\Levels\\" + path);
            List<string> listFile = file.ToList();
            listFile.Add("");
            while (listFile[0].Trim() == "") listFile.RemoveAt(0);
            file = listFile.ToArray();
            int currentLevel = 0;
            int line = 0;
            while (currentLevel < targetLevel)
            {
                if (file[line].Trim() == "" && file[line+1].Trim() != "") currentLevel++;
                line++;
            }
            List<string> levelStr = new List<string>();
            do
            {
                levelStr.Add(file[line]);
                line++;
            } while (file[line].Trim() != "");
            int[,] levelAsIntArr = new int[levelStr[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length, levelStr.Count];
            for(int i = 0; i < levelAsIntArr.GetLength(1); i++)
            {
                for(int j = 0; j < levelAsIntArr.GetLength(0); j++)
                {
                    try
                    {
                        levelAsIntArr[j, i] = int.Parse(levelStr[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[j]);
                    } catch
                    {
                        levelAsIntArr[j, i] = -1;
                    }
                }

            }
            Grid g = new Grid(levelAsIntArr);
            return g;
        }
        #endregion
    }
}