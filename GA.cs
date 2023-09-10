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
        private void RunScript(List<Line> boundary, DataTree<Line> roomsBoundary, List<Point3d> centerPoint, List<string> roomName, List<double> genePool, ref object A, ref object B, ref object FloorPolyLines, ref object y1, ref object Kc, ref object Gc)
        {
            A = ClassifyLines(boundary, roomsBoundary);

            List<Wall> walls = new List<Wall>();
            walls = GetWalls(ClassifyLines(boundary, roomsBoundary),centerPoint,roomName);

            List<Line> north = new List<Line>();

            //List<Line> lines = walls.Branch(0).Where(x => x.Direction == Direction.North).Select(x => x.Line).ToList();

            List<Line> lines = walls.Where(x => x.Direction != Direction.Unset).Where(x => x.RoomName == "Room 0").Select(x => x.Line).ToList();

            B = lines;


            Floor floor = new Floor(walls, roomName);

            FloorPolyLines = floor.NorthWall.Select(x  => x.RoomName);


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


        public enum Direction{ Unset,South, West,North,East};

        public Direction GetDirection(Point3d centerPoint, Line line)
        {
            Point3d pointA = line.From;
            Point3d pointB = line.To;

            if(pointA.X == pointB.X && pointA.X < centerPoint.X)
                return Direction.West;
            else if (pointA.X == pointB.X && pointA.X >= centerPoint.X)
                return Direction.East;
            else if(pointA.Y == pointB.Y && pointA.Y >= centerPoint.Y)
                return Direction.North;
            else if(pointA.Y == pointB.Y && pointA.Y < centerPoint.Y)
                return Direction.South;
            else
                return Direction.Unset;
        }


        public List<Wall> GetWalls (DataTree<Line> lines, List<Point3d> centerPoint, List<string> names)
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
            public Direction Direction ;
            public string RoomName = "";
            public bool Block;

        }


        public class Floor
        {
            public List<Wall> WestWall;
            public List<Wall> NorthWall;
            public List<Wall> EastWall;
            public List<Wall> SouthWall;

            public List<string> RoomName;

            public Floor(List<Wall> walls, List<string> roomName)
            {
                WestWall = walls.Where(x => x.Direction == Direction.West).Select(x => x).ToList();
                NorthWall = walls.Where(x => x.Direction == Direction.North).Select(x => x).ToList();
                EastWall = walls.Where(x => x.Direction == Direction.East).Select(x => x).ToList();
                SouthWall = walls.Where(x => x.Direction == Direction.South).Select(x => x).ToList();

                RoomName = roomName;
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
                                polyline.Add(startPoint);
                                addedPoints.Add(startPoint);
                            }

                            // 检查终点是否已添加
                            if (!addedPoints.Contains(endPoint))
                            {
                                polyline.Add(endPoint);
                                addedPoints.Add(endPoint);
                            }
                        }

                    }
                    polyline = polyline.Count > 0 ? polyline : null;

                    result.Add(polyline);
                }
                return result; 
            }
        }


        public class GeneCalculation
        {

        }
    }

}
