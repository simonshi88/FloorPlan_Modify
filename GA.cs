using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorPlan_Generator
{
    public class Script_Instance
    {
        private void RunScript(List<Line> boundary, DataTree<Line> roomsBoundary, List<Point3d> centerPoint, List<string> roomName, 
            List<double> roomDaylightFactor, List<double> genePool, double control_y1, double control_kc, double control_gc, 
            ref object A, ref object B, ref object FloorPolyLines, ref object y1, ref object Kc, ref object Gc, ref object Fitness)
        {
            A = ClassifyLines(boundary, roomsBoundary);

            List<Wall> walls = new List<Wall>();
            walls = GetWalls(ClassifyLines(boundary, roomsBoundary), centerPoint, roomName);

            List<Line> north = new List<Line>();

            //List<Line> lines = walls.Branch(0).Where(x => x.Direction == Direction.North).Select(x => x.Line).ToList();

            List<Line> lines = walls.Where(x => x.Direction != Direction.Unset).Where(x => x.RoomName == "Room 0").Select(x => x.Line).ToList();

            B = lines;


            Floor floor = new Floor(walls, roomName, roomDaylightFactor, genePool);
            floor.Fitness = floor.DataProcess(control_y1, control_kc, control_gc);


            FloorPolyLines = floor.FloorPolylines;
           
            y1 = floor.Y1;
            Kc = floor.KC;
            Gc = floor.GC;
            Fitness = floor.Fitness;
        }





        public DataTree<Line> ClassifyLines(List<Line> boundary, DataTree<Line> roomsBoundary)
        {
            DataTree<Line> classifyLines = new DataTree<Line>();


            for (int i = 0; i < roomsBoundary.BranchCount; i++)
            {
                List<Line> lines = new List<Line>();
                for (int j = 0; j < roomsBoundary.Branches[i].Count; j++)
                {
                    for (int k = 0; k < boundary.Count; k++)
                    {
                        if (IsSameLine(boundary[k], roomsBoundary.Branches[i][j]))
                        {
                            lines.Add(boundary[k]);
                        }
                    }
                }
                classifyLines.AddRange(lines, new GH_Path(i));

            }
            return classifyLines;
        }

        public bool IsSameLine(Line a, Line b)
        {
            return (a.From == b.From && a.To == b.To) || (a.From == b.To && a.To == b.From);
        }


        public enum Direction { Unset, South, West, North, East };

        public Direction GetDirection(Point3d centerPoint, Line line)
        {
            Point3d pointA = line.From;
            Point3d pointB = line.To;

            if (pointA.X == pointB.X && pointA.X < centerPoint.X)
                return Direction.West;
            else if (pointA.X == pointB.X && pointA.X >= centerPoint.X)
                return Direction.East;
            else if (pointA.Y == pointB.Y && pointA.Y >= centerPoint.Y)
                return Direction.North;
            else if (pointA.Y == pointB.Y && pointA.Y < centerPoint.Y)
                return Direction.South;
            else
                return Direction.Unset;
        }


        public List<Wall> GetWalls(DataTree<Line> lines, List<Point3d> centerPoint, List<string> names)
        {
            List<Wall> result = new List<Wall>();

            for (int i = 0; i < lines.BranchCount; i++)
            {
                List<Wall> walls = new List<Wall>();

                for (int j = 0; j < lines.Branches[i].Count; j++)
                {
                    Wall wall = new Wall();
                    wall.RoomName = names[i];
                    wall.Line = lines.Branches[i][j];
                    wall.Direction = GetDirection(centerPoint[i], wall.Line);

                    walls.Add(wall);
                }

                result.AddRange(walls);
            }
            return result;
        }


        public class Wall
        {
            public Line Line = new Line();
            public Direction Direction;
            public string RoomName = "";
            public bool Block;

        }

        //根据采光系数表，得到的一个矩阵（进深4.8m, 层高3.0m）的数值，根据这个矩阵，用采光系数与Kc值，算出Gc值
        public static double[,] CavMatrix =
        {
            { 1.3, 1.6, 1.9,2.1,2.3 },
            {1.8,2.2,2.6,2.9,3.2 },
            {2.2,2.7,3.1,3.5,3.9 },
            {2.7,3.2,3.7,4.2,4.7 }
        };

        public class Floor
        {
            public List<Wall> WestWall;
            public List<Wall> NorthWall;
            public List<Wall> EastWall;
            public List<Wall> SouthWall;

            public List<string> RoomName;

            public List<double> DayLightFactor;
            public List<double> GenePool;

            public double Fitness;

            public DataTree<Polyline> FloorPolylines;
            public DataTree<double> Y1;
            public DataTree<double> KC;
            public DataTree<double> GC;

            public Floor(List<Wall> walls, List<string> roomName, List<double> dayLightFactor, List<double> genePool)
            {
                WestWall = walls.Where(x => x.Direction == Direction.West).Select(x => x).ToList();
                NorthWall = walls.Where(x => x.Direction == Direction.North).Select(x => x).ToList();
                EastWall = walls.Where(x => x.Direction == Direction.East).Select(x => x).ToList();
                SouthWall = walls.Where(x => x.Direction == Direction.South).Select(x => x).ToList();

                RoomName = roomName;
                DayLightFactor = dayLightFactor;
                GenePool = genePool;

                FloorPolylines = GetLinesInEachDirection();

            }

            public DataTree<Polyline> GetLinesInEachDirection()
            {
                DataTree<Polyline> result = new DataTree<Polyline>();


                result.AddRange(GetLinesByNames(WestWall), new GH_Path(0));
                result.AddRange(GetLinesByNames(NorthWall), new GH_Path(1));
                result.AddRange(GetLinesByNames(EastWall), new GH_Path(2));
                result.AddRange(GetLinesByNames(SouthWall), new GH_Path(3));

                return result;
            }


            public List<Polyline> GetLinesByNames(List<Wall> walls)
            {
                List<Polyline> result = new List<Polyline>();

                foreach (string name in RoomName)
                {
                    Polyline polyline = new Polyline(); // 合并后的多段线
                    // 用于存储已添加的点的集合
                    HashSet<Point3d> addedPoints = new HashSet<Point3d>();
                    for (int i = 0; i < walls.Count; i++)
                    {
                        if (name == walls[i].RoomName)
                        {
                            var line = walls[i].Line;

                            Point3d startPoint = line.From;
                            Point3d endPoint = line.To;

                            // 检查起点是否已添加
                            if (!addedPoints.Contains(startPoint))
                            {
                                addedPoints.Add(startPoint);
                            }
                            // 检查终点是否已添加
                            if (!addedPoints.Contains(endPoint))
                            {
                                addedPoints.Add(endPoint);
                            }
                        }
                    }
                    List<Point3d> sortedPoints = addedPoints.OrderBy(point => point.X).ThenBy(point => point.Y).ToList();
                    polyline.AddRange(sortedPoints);
                    polyline = polyline.Count > 0 ? polyline : null;

                    result.Add(polyline);
                }
                return result;
            }


            public List<List<double>> SplitGroup(List<double> elements, int group, int chunkSize)
            {
                List<List<double>> subLists = new List<List<double>>();
                for (int i = 0; i < group; i++)
                {
                    List<double> subList = elements.Skip(i * chunkSize).Take(chunkSize).ToList();
                    subLists.Add(subList);
                }

                return subLists;
            }

            public static double Remap(double value, double fromLow, double fromHigh, double toLow, double toHigh)
            {
                // 确保输入值在源区间内
                value = Math.Max(Math.Min(value, fromHigh), fromLow);

                // 计算百分比
                double fromRange = fromHigh - fromLow;
                double toRange = toHigh - toLow;
                double percent = (value - fromLow) / fromRange;

                // 应用映射
                double remappedValue = toLow + percent * toRange;

                // 四舍五入到一位小数
                return Math.Round(remappedValue, 1);
            }

            public double DataProcess(double multipl_y1, double multipl_kc, double multipl_gc)
            {
                double fitness = 0;

                List<List<double>> subLists = SplitGroup(GenePool, 3, RoomName.Count * 4);

                //拆成y1,Kc, Gc三组
                List<double> paraY1 = subLists[0];
                List<double> paraKc = subLists[1];
                List<double> paraGc = subLists[2];

                //y1的取值范围[0.6, 1.6]
                List<double> y1 = paraY1.Select(x => Remap(x, 0.1, 1, 0.6, 1.6)).ToList();
                //kc的取值范围[0.5, 0.9]
                List<double> Kc = paraKc.Select(x => Remap(x, 0.1, 1, 0.5, 0.9)).ToList();
                //gc的取值范围[0.3, 0.6]
                List<double> Gc = paraGc.Select(x => Remap(x, 0.1, 1, 0.3, 0.6)).ToList();

                //Y1:距离地面高度
                Y1 = ConvertToTree(SplitGroup(y1, 4, RoomName.Count));
                //KC:侧面采光的窗宽系数，为窗宽度与房间宽度之比
                KC = ConvertToTree(SplitGroup(Kc, 4, RoomName.Count));
                //GC:侧面采光的窗高系数，为窗高度与层高之比
                GC = ConvertToTree(SplitGroup(Gc, 4, RoomName.Count));


                List<List<double>> eachDirection = SplitGroup(Kc, 4, RoomName.Count);

                fitness = multipl_kc * CalculateWest(eachDirection[0]) + CalculateNorth(eachDirection[1]) +
                    CalculateEast(eachDirection[2]) + CalculateSouth(eachDirection[3]) +
                    multipl_y1 * CalculateHeight(Y1) +
                    multipl_gc * CalculateRoomDaylightFactor(GC, KC);


                return fitness;
            }


            public DataTree<double> ConvertToTree(List<List<double>> listValue)
            {
                DataTree<double> result = new DataTree<double>();

                for (int i = 0; i < listValue.Count; i++)
                {
                    result.AddRange(listValue[i], new GH_Path(i));
                }

                return result;
            }

            public double CalculateWest(List<double> values)
            {
                double score = 0.0;
                //西侧的墙
                List<Polyline> pls = FloorPolylines.Branch(0);

                for (int i = 0; i < pls.Count; i++)
                {
                    if (pls[i] != null)
                    {
                        if (values[i] == 0.9)
                            score += 1;
                        else if (values[i] == 0.8)
                            score += 2;
                        else if (values[i] == 0.7)
                            score += 3;
                        else if (values[i] == 0.6)
                            score += 4;
                        else if (values[i] == 0.5)
                            score += 5;
                        else
                            score += 0;
                    }
                }
                return score;
            }

            public double CalculateNorth(List<double> values)
            {
                double score = 0.0;
                //北侧的墙
                List<Polyline> pls = FloorPolylines.Branch(1);

                for (int i = 0; i < pls.Count; i++)
                {
                    if (pls[i] != null)
                    {
                        if (values[i] == 0.9)
                            score += 5;
                        else if (values[i] == 0.8)
                            score += 4;
                        else if (values[i] == 0.7)
                            score += 3;
                        else if (values[i] == 0.6)
                            score += 2;
                        else if (values[i] == 0.5)
                            score += 1;
                        else
                            score += 0;
                    }
                }
                return score;
            }

            public double CalculateEast(List<double> values)
            {
                double score = 0.0;
                //东侧的墙
                List<Polyline> pls = FloorPolylines.Branch(2);

                for (int i = 0; i < pls.Count; i++)
                {
                    if (pls[i] != null)
                    {
                        if (values[i] == 0.9)
                            score += 1;
                        else if (values[i] == 0.8)
                            score += 2;
                        else if (values[i] == 0.7)
                            score += 3;
                        else if (values[i] == 0.6)
                            score += 2;
                        else if (values[i] == 0.5)
                            score += 1;
                        else
                            score += 0;
                    }
                }
                return score;
            }

            public double CalculateSouth(List<double> values)
            {
                double score = 0.0;
                //南侧的墙
                List<Polyline> pls = FloorPolylines.Branch(3);

                for (int i = 0; i < pls.Count; i++)
                {
                    if (pls[i] != null)
                    {
                        if (values[i] == 0.9)
                            score += 5;
                        else if (values[i] == 0.8)
                            score += 4;
                        else if (values[i] == 0.7)
                            score += 3;
                        else if (values[i] == 0.6)
                            score += 2;
                        else if (values[i] == 0.5)
                            score += 1;
                        else
                            score += 0;
                    }
                }
                return score;
            }


            public double CalculateHeight(DataTree<double> values)
            {
                double score = 0.0;

                for (int i = 0; i < FloorPolylines.BranchCount; i++)
                {
                    List<Polyline> pls = FloorPolylines.Branch(i);

                    for (int j = 0; j < pls.Count; j++)
                    {
                        if (pls[j] != null)
                        {
                            if (values.Branch(i)[j] < 1.2)
                                score += 1;
                            else if (values.Branch(i)[j] == 1.2)
                                score += 5;
                            else if (values.Branch(i)[j] == 1.3)
                                score += 4;
                            else if (values.Branch(i)[j] == 1.4)
                                score += 3;
                            else if (values.Branch(i)[j] > 1.4)
                                score += 1;
                            else
                                score += 0;
                        }
                    }
                }
                return score;
            }


            public double CalculateRoomDaylightFactor(DataTree<double> gcValues, DataTree<double> kcValues)
            {
                double score = 0.0;


                for (int i = 0; i < FloorPolylines.BranchCount; i++)
                {
                    List<Polyline> pls = FloorPolylines.Branch(i);

                    for (int j = 0; j < pls.Count; j++)
                    {
                        if (pls[j] != null)
                        {
                            double daylightFactor = CavMatrix[RemapGC(gcValues.Branch(i)[j]), RemapKC(kcValues.Branch(i)[j])];

                            //算出在y1，gc的条件下，窗最终的高度
                            var totalHeight = Y1.Branch(i)[j] + 2.8 * gcValues.Branch(i)[j];
                            //如果在正常范围内，低于floor0.3m
                            if (totalHeight <= 2.8 - 0.3)  
                                score += CalculateScore(5, daylightFactor, DayLightFactor[j]);
                            //如果窗户高于层高，总分递减，不应该出现这种错误场景
                            else
                                score -= 50 *(totalHeight -2.5) ;
                        }
                    }

                }
                return score;
            }


            int RemapKC(double kc)
            {
                if (kc == 0.5)
                    return 0;
                if (kc == 0.6)
                    return 1;
                if (kc == 0.7)
                    return 2;
                if (kc == 0.8)
                    return 3;
                if (kc == 0.9)
                    return 4;
                return 0;
            }

            int RemapGC(double gc)
            {
                if (gc == 0.3)
                    return 0;
                if (gc == 0.4)
                    return 1;
                if (gc == 0.5)
                    return 2;
                if (gc == 0.6)
                    return 3;
                return 0;
            }
            public static double CalculateScore(double score, double x, double a)
            {
                //x:根据gc与kc算出来的采光系数
                //a:该房间类型应该的采光系数，是一个用户输入值
                // 计算x与a的差值
                double difference = Math.Abs(x - a);

                // 如果差值在0.5之内，得分为score分
                if (difference <= 0.5)
                {
                    return score;
                }
                else
                {
                    // 否则，根据距离远近逐渐减少得分
                    // 这里使用一个简单的线性减分逻辑，您可以根据需要调整
                    double maxScore = score; // 最大得分
                    double minScore = -score; // 最小得分
                    double maxDifference = 2.0; // 最大差值，根据需要调整

                    // 计算得分，使用线性插值
                    double value = Math.Round(maxScore - (difference / maxDifference) * (maxScore - minScore));

                    return value;
                }
            }

        }
    }
}
