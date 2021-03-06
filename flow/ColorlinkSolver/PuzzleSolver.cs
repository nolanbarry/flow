﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Colorlink
{
    public class PuzzleSolver
    {       
        /// <summary>
        /// Gets the solution to a given grid.
        /// </summary>
        /// <returns>Returns a Grid object with the pathsOfColors array filled with the solved paths. Returns null if grid is not solvable.</returns>
        public static Grid GetSolution(int[,] grid)
        {
            if (IsItSolvable(grid)) return lastSolution;
            return null;
        }

        #region Level Solving
        /// <summary>
        /// The solution to the last solvable grid that was queried
        /// </summary>
        public static Grid lastSolution;

        /// <summary>
        /// Returns true or false if a grid is solvable. If solvable, stores the solution in lastSolution.
        /// </summary>
        /// <param name="grid">A two dimensional int array representing the level.</param>
        /// <returns></returns>
        public static bool IsItSolvable(int[,] grid)
        {
            return IsItSolvable(grid, false);
        }

        public static bool IsItSolvable(int[,] grid, bool filterLameGrids)
        {
            SolvingGrid info = new SolvingGrid(grid);

            if (filterLameGrids) // filters grids that have any two same-color nodes adjacent to each other
            {
                for (int i = 0; i < info.colors.Length; i++)
                {
                    if ((Math.Abs(info.startNodes[i].X - info.endNodes[i].X) == 1 && info.startNodes[i].Y == info.endNodes[i].Y)) return false;
                    if ((Math.Abs(info.startNodes[i].Y - info.endNodes[i].Y) == 1 && info.startNodes[i].X == info.endNodes[i].X)) return false;
                }
            }

            return Solve(info);
        }

        /// <summary>
        /// Recursive algorithm to solve a level.
        /// </summary>
        /// <param name="info">All the appropriate info stored in a SolvingGrid.</param>
        /// <returns>Returns whether or not the level is solvable.</returns>
        private static bool Solve(SolvingGrid info)
        {
            Path.Direction[] potentials = GetMoveOptions(info);
            foreach (Path.Direction d in potentials)
            {
                info.AddDirectionToCurrentPath(d);
                if (info.state == SolvingGrid.SolveState.Success)
                {
                    lastSolution = new Grid(info);
                    return true;
                }
                else if (info.state == SolvingGrid.SolveState.Solving)
                {
                    if (Solve(info)) return true;
                }
                info.RemoveLastAction();
            }
            return false;
        }

        /// <summary>
        /// Gets the potential directions of the current active cell, or the last cell on the last path in the given SolvingGrid. 
        /// </summary>
        /// <param name="info">All the appropriate info in the form of a SolvingGrid.</param>
        /// <returns>Direction array with all options.</returns>
        private static Path.Direction[] GetMoveOptions(SolvingGrid info)
        {
            int Y, X;
            Path currentPath = info.pathsOfColors.Last();
            Y = currentPath.lastPoint.Y;
            X = currentPath.lastPoint.X;
            List<Path.Direction> potentials = new List<Path.Direction>();
            int[,] grid = info.FlattenGrid();
            if (Y - 1 >= 0)
            {
                if (grid[Y - 1, X] == -1  // direction is valid if new square is not visited or new square is the endpoint for the color of the current path
                  || new Point(X, Y - 1) == info.endNodes[Array.IndexOf(info.colors, info.pathsOfColors.Last().color)])
                    potentials.Add(Path.Direction.Up);
            }
            if (Y + 1 < grid.GetLength(0))
            {
                if (grid[Y + 1, X] == -1
                   || new Point(X, Y + 1) == info.endNodes[Array.IndexOf(info.colors, info.pathsOfColors.Last().color)])
                    potentials.Add(Path.Direction.Down);
            }
            if (X - 1 >= 0)
            {
                if (grid[Y, X - 1] == -1
                   || new Point(X - 1, Y) == info.endNodes[Array.IndexOf(info.colors, info.pathsOfColors.Last().color)])
                    potentials.Add(Path.Direction.Left);
            }
            if (X + 1 < grid.GetLength(1))
            {
                if (grid[Y, X + 1] == -1
                   || new Point(X + 1, Y) == info.endNodes[Array.IndexOf(info.colors, info.pathsOfColors.Last().color)])
                    potentials.Add(Path.Direction.Right);
            }
            return potentials.ToArray();
        }

        /// <summary>
        /// Deep clones an object. Object must be serializable.
        /// </summary>
        /// <returns>Deep cloned object</returns>
        public static object DeepClone(object obj)
        {
            object objResult = null;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);

                ms.Position = 0;
                objResult = bf.Deserialize(ms);
            }
            return objResult;
        }
        #endregion
    }

    public class PuzzleGenerator
    {
        /// <summary>
        /// Event raised when there are no more levels in the generation queue.
        /// </summary>
        public event EventHandler QueueCleared;

        private BackgroundWorker solver;
        private List<int[]> levelGenerationQueue;
        public bool working { get { return solver.IsBusy; } }
        public List<Puzzle> completedLevels { get; private set; }
        public PuzzleGenerator()
        {
            solver = new BackgroundWorker();
            levelGenerationQueue = new List<int[]>();
            completedLevels = new List<Puzzle>();
            solver.DoWork += new DoWorkEventHandler(DoWork);
            solver.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LevelGenerated);
        }

        /// <summary>
        /// Adds the specified level data to the queue to be generated with the given parameters.
        /// </summary>
        /// <param name="maxColor">The maximum number to give any color in the grid.</param>
        public void QueueLevels(int numberToGenerate, int width, int height, int maxColor)
        {
            bool restartWorker = levelGenerationQueue.Count == 0;
            for (int i = 0; i < numberToGenerate; i++)
            {
                levelGenerationQueue.Add(new int[] { width, height, maxColor });
            }
            if (restartWorker) solver.RunWorkerAsync();
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            Puzzle gen =  GenerateSolvableLevel(levelGenerationQueue[0][0], levelGenerationQueue[0][1], levelGenerationQueue[0][2]);
            e.Result = gen;
        }

        private void LevelGenerated(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                levelGenerationQueue.RemoveAt(0);
                completedLevels.Add((Puzzle)e.Result);
                if (levelGenerationQueue.Count > 0) solver.RunWorkerAsync();
                else QueueCleared?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Returns a puzzle and removes it from the list within this object.
        /// </summary>
        public Puzzle RetrieveAndRemoveLevel()
        {
            return RetrieveAndRemoveLevel(false);
        }
        /// <summary>
        /// Returns a puzzle and removes it from the completed levels list;
        /// </summary>
        /// <param name="restockQueue">Adds a new level to the queue with the exact same parameters as the one before it if marked true.</param>
        /// <returns></returns>
        public Puzzle RetrieveAndRemoveLevel(bool restockQueue)
        {
            Puzzle g = completedLevels[0];
            completedLevels.RemoveAt(0);
            QueueLevels(1, levelGenerationQueue.Last()[0], levelGenerationQueue.Last()[1], levelGenerationQueue.Last()[2]);
            return g;
        }

        /// <summary>
        /// Cancels current level generation and clears entire queue.
        /// </summary>
        public void CancelAll()
        {
            solver.CancelAsync();
            levelGenerationQueue.Clear();
        }

        #region Static Methods
        public static Puzzle GenerateSolvableLevel(int width, int height, int maxColor)
        {
            return GenerateSolvableLevel(width, height, maxColor, false, -1);
        }

        /// <summary>
        /// Generates levels and checks their solvable status until one is found to be solvable.
        /// </summary>
        /// <param name="filterLame">Filter out levels where any one start node is adjacent to its end node. Takes much longer to generate.</param>
        /// <param name="numberOfPipes">Set the number of pipes the level contains. Set to -1 for the generator to decide.</param>
        public static Puzzle GenerateSolvableLevel(int width, int height, int maxColor, bool filterLame, int numberOfPipes)
        {
            Grid gen;
            do
            {
                gen = new Grid(GenerateLevel(width, height, maxColor, numberOfPipes));
            } while (!PuzzleSolver.IsItSolvable(gen.grid, false) || !NotLame(gen, filterLame));
            return new Puzzle(gen, PuzzleSolver.lastSolution);
        }

        /// <summary>
        /// Returns true if the given grid is 
        /// </summary>
        /// <param name="doanything">If this is false, it will automatically return true</param>
        private static bool NotLame(Grid gen, bool doanything)
        {
            if (!doanything) return true;
            for (int i = 0; i < gen.grid.GetLength(0); i++)
            {
                for (int j = 0; j < gen.grid.GetLength(1); j++)
                {
                    if (gen.grid[i, j] != -1)
                    {
                        int c = gen.grid[i, j];
                        if (i + 1 < gen.grid.GetLength(0)) if (c == gen.grid[i + 1, j]) return false;
                        if (i - 1 >= 0) if (c == gen.grid[i - 1, j]) return false;
                        if (j + 1 < gen.grid.GetLength(1)) if (c == gen.grid[i, j + 1]) return false;
                        if (j - 1 >= 0 ) if (c == gen.grid[i, j - 1]) return false;
                    }
                }
            }
            return true;
        }

        public static Random gen;
        public static int[,] GenerateLevel(int width, int height, int maxColor, int numberOfPipes)
        {
            if (gen == null) gen = new Random();
            if ((width == 1 && height == 1) || width < 1 || height < 1) throw new Exception("No");
            int minus = 0;
            int colors = (int)(Math.Sqrt(width * height) - minus);
            if (numberOfPipes > 0) colors = numberOfPipes;
            int[] colorsToUse = new int[colors];
            for (int i = 0; i < colors; i++)
            {
                int chosenColor;
                do
                {
                    chosenColor = gen.Next(maxColor);
                } while (colorsToUse.Contains(chosenColor));
                colorsToUse[i] = chosenColor;
            }

            int[,] generated = new int[height, width]; // this is backward (not width, height) because it's really asksing for rows, columns
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    generated[i, j] = -1;
                }
            }
            for (int i = 0; i < colors; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int col, row;
                    do
                    {
                        col = gen.Next(width);
                        row = gen.Next(height);
                    } while (generated[row, col] != -1);
                    generated[row, col] = colorsToUse[i];
                }
            }
            return generated;
        }
        
        public static string[] GridToString(int[,] grid)
        {
            string[] strGrid = new string[grid.GetLength(0)];
            for(int i = 0; i < strGrid.Length; i++)
            {
                strGrid[i] = "";
                for(int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i, j] != -1)
                        strGrid[i] += grid[i, j] + " ";
                    else
                        strGrid[i] += "- ";
                }
                strGrid[i] = strGrid[i].Trim();
            }
            return strGrid;
        }

        public void QueueLevels(object width, int height, int maxColor)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    /// <summary>
    /// A class similar to flow.Grid, the main differences being its increased editablity and extra information to make solving easier.
    /// </summary>
    [Serializable()]
    public class SolvingGrid
    {
        public enum SolveState { Solving, Failed, Success }
        public int[,] grid { get; private set; }
        public Point[] startNodes { get; private set; }
        public List<Point> endNodes { get; private set; }
        public int[] colors { get; private set; }
        public List<Path> pathsOfColors;
        public SolveState state;

        public SolvingGrid(int[,] grid)
        {
            this.grid = grid;
            List<Point> starts = new List<Point>();
            endNodes = new List<Point>();
            List<int> colorsList = new List<int>();
            // find the start nodes, colors, and end nodes
            for (int y = 0; y < grid.GetLength(0); y++)
            {
                for (int x = 0; x < grid.GetLength(1); x++)
                {
                    if (grid[y, x] != -1 && !colorsList.Contains(grid[y, x]))
                    {
                        starts.Add(new Point(x, y));
                        colorsList.Add(grid[y, x]);
                    }
                    else if (colorsList.Contains(grid[y, x]))
                    {
                        endNodes.Add(new Point(x, y));
                    }
                }
            }
            startNodes = starts.ToArray();
            state = SolveState.Solving;
            colors = colorsList.ToArray();
            pathsOfColors = new List<Path>();
            pathsOfColors.Add(new Path(startNodes[0], colors[0]));

            // sort endNodes to correspond correctly with startNodes
            List<Point> endNodesSorted = new List<Point>();
            for (int i = 0; i < startNodes.Length; i++)
            {
                for (int j = 0; j < endNodes.Count; j++)
                {
                    if (grid[endNodes[j].Y, endNodes[j].X] == colors[i])
                    {
                        endNodesSorted.Add(endNodes[j]);
                        j = endNodes.Count;
                    }
                }
            }
            endNodes = endNodesSorted;
        }

        /// <summary>
        /// For use with figuring out whether or not a cell is available to move to.
        /// </summary>
        /// <returns>Returns the grid with the paths added to it as just colors, with no actual information on direction</returns>
        public int[,] FlattenGrid()
        {
            int[,] flat = new int[grid.GetLength(0), grid.GetLength(1)];
            for (int i = 0; i < flat.GetLength(0); i++) for (int j = 0; j < flat.GetLength(1); j++) flat[i, j] = -1;
            int num = 0;
            foreach (Point p in startNodes)
            {
                flat[p.Y, p.X] = colors[num];
                num++;
            }
            num = 0;
            foreach (Point p in endNodes)
            {
                flat[p.Y, p.X] = colors[num];
                num++;
            }
            num = 0;
            foreach (Path p in pathsOfColors)
            {
                if (p != null)
                {
                    foreach (Point point in p.asCoordinateArray)
                    {
                        flat[point.Y, point.X] = colors[num];
                    }
                }
                num++;
            }
            return flat;
        }

        /// <summary>
        /// Adds the given direction to the currently active path, and starts a new path if that finishes that pipe.
        /// </summary>
        /// <param name="d">The direction to add</param>
        public void AddDirectionToCurrentPath(Path.Direction d)
        {
            pathsOfColors.Last().Add(d);
            if (grid[pathsOfColors.Last().lastPoint.Y, pathsOfColors.Last().lastPoint.X] == pathsOfColors.Last().color)
            {
                if (pathsOfColors.Count < startNodes.Length)
                {
                    pathsOfColors.Add(new Path(startNodes[pathsOfColors.Count], colors[1 + Array.IndexOf(colors, pathsOfColors.Last().color)]));
                }
                else
                {
                    
                    bool successful = true;
                    foreach (int i in FlattenGrid())
                    {
                        if (i == -1) successful = false;
                    }
                    
                    if (successful) state = SolveState.Success;
                    else state = SolveState.Failed;
                }
            }
        }

        public void RemoveLastAction()
        {
            state = SolveState.Solving;
            if (pathsOfColors.Last().path.Count > 0)
            {
                pathsOfColors.Last().RemoveLast();
            } else if (pathsOfColors.Count > 1)
            {
                pathsOfColors.Remove(pathsOfColors.Last());
                pathsOfColors.Last().RemoveLast();
            }
        }
    }

    /// <summary>
    /// An all in one object for passing a blank level and solved level from BackgroundWorker to the UI
    /// </summary>
    public class Puzzle
    {
        public Grid level;
        public Grid solution { get; private set; }

        public Puzzle(Grid level, Grid solution)
        {
            this.level = level;
            this.solution = solution;
        }
    }
}
