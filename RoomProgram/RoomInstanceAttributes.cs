﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace FloorPlan_Generator
{
    public interface IRoomStructure<T>
    {
        void AddAdjacence(T a);
        void RemoveAdjacence(T a);
    }

    public class RoomInstanceAttributes : GH_ComponentAttributes, IRoomStructure<IGH_DocumentObject>
    {

        public RoomInstanceAttributes(RoomInstance param) : base(param)
        {
            //  RoomAreaRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X, (int)Bounds.Location.Y + 40), new Size(60, 20));
            // RoomNameRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X, (int)Bounds.Location.Y + 40), new Size(60, 20));

            if (RoomArea == null)
                RoomArea = GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, param.RoomArea.ToString());

            if (RoomLength == null)
                RoomLength = GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, param.RoomLength.ToString());

            RoomName = GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, param.RoomName);

            roomBrush = Brushes.Gray;


        }

        protected override void Layout()
        {
            Pivot = GH_Convert.ToPoint(Pivot);
            Bounds = new RectangleF(Pivot.X - OuterComponentRadius, Pivot.Y - OuterComponentRadius, 2 * OuterComponentRadius, 2 * OuterComponentRadius);

        }

        public GH_Capsule RoomArea;//= GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, p);
        public GH_Capsule RoomLength;
        public GH_Capsule RoomName;//= GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, "RoomName");
                                   //  public GH_Capsule RoomId;//= GH_Capsule.CreateTextCapsule(new Rectangle(), new Rectangle(), GH_Palette.Black, "RoomName");

        Rectangle RoomAreaRectangle;
        Rectangle RoomLengthRectangle;
        Rectangle RoomNameRectangle;
        Rectangle RoomIdRectangle;

        bool haveReadTargetObjectsList = false;

        public Brush roomBrush;// = Brushes.DimGray;

        const int InflateAmount = 2; // Used to inflate all rectangles for producing outer rectangles for GH_TextCapsules
        const int InnerComponentRadius = 55; // Used to define the radius of the main circle
        const int OuterComponentRadius = 75; // Used to define the radius of the main circle

        public string[] writerTargetObjectsListString = new string[0];

        public HouseInstance AssignedHouseInstance;


        public List<IGH_DocumentObject> targetObjectList = new List<IGH_DocumentObject>();

        protected Rectangle InflateRect(Rectangle rect, int a = 5, int b = 5)
        {
            Rectangle rectOut = rect;
            rectOut.Inflate(-a, -b);
            return rectOut;
        }

        protected RectangleF InnerComponentBounds
        {
            get
            {
                RectangleF inner = Bounds;
                int inflation = OuterComponentRadius - InnerComponentRadius;
                inner.Inflate(-inflation, -inflation);
                return inner;
            }
        }

        public override bool IsPickRegion(PointF point)
        {
            return Grasshopper.GUI.GH_GraphicsUtil.IsPointInEllipse(Bounds, point);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (Owner is RoomInstance roomInstance)
                if (channel == GH_CanvasChannel.Objects)
                {
                    //  base.Render(canvas, graphics, channel);

                    graphics.FillEllipse(roomBrush, Bounds);
                    //
                    //   foreach (IGH_DocumentObject obj in targetObjectList)
                    //      DrawTargetArrow(graphics, obj.Attributes.Bounds);

                    if ((Owner as RoomInstance).hasMissingAdj)
                    {
                        graphics.FillEllipse(Brushes.Red, new RectangleF(Pivot.X - InnerComponentRadius - 9, Pivot.Y - InnerComponentRadius - 9
                            , 2 * InnerComponentRadius + 18, 2 * InnerComponentRadius + 18));
                    }

                    GH_Capsule capsule = GH_Capsule.CreateCapsule(InnerComponentBounds, GH_Palette.Normal, InnerComponentRadius - 5, 0);
                    capsule.Render(graphics, Selected, Owner.Locked, true);
                    capsule.Dispose();

                    RoomNameRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X + 28, (int)Bounds.Location.Y + 55), new Size(94, 20));
                    RoomAreaRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X + 65, (int)Bounds.Location.Y + 80), new Size(57, 20));
                    RoomLengthRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X + 65, (int)Bounds.Location.Y + 105), new Size(57, 20));
                    RoomIdRectangle = new Rectangle(new System.Drawing.Point((int)Bounds.Location.X + 35, (int)Bounds.Location.Y + 130), new Size(80, 40));

                    graphics.DrawString("m² :", SystemFonts.IconTitleFont, Brushes.Black, new RectangleF(new System.Drawing.Point((int)Bounds.Location.X + 35, (int)Bounds.Location.Y + 81), new Size(30, 20)));
                    if (!RoomInstance.entranceIds.Contains(roomInstance.RoomId))
                        graphics.DrawString("ID: " + roomInstance.RoomId, new Font(FontFamily.GenericSansSerif, 6f, FontStyle.Regular), Brushes.Black, RoomIdRectangle, new StringFormat() { Alignment = StringAlignment.Center });
                    else
                        graphics.DrawString("ID: " + roomInstance.RoomId + "\n(entrance)", new Font(FontFamily.GenericSansSerif, 6f, FontStyle.Bold), Brushes.Black, RoomIdRectangle, new StringFormat() { Alignment = StringAlignment.Center });


                    RoomName = GH_Capsule.CreateTextCapsule(RoomNameRectangle, InflateRect(RoomNameRectangle, InflateAmount, InflateAmount), GH_Palette.Pink, roomInstance.RoomName);
                    RoomName.Render(graphics, GH_Skin.palette_grey_standard);
                    RoomName.Dispose();

                    RoomArea = GH_Capsule.CreateTextCapsule(RoomAreaRectangle, InflateRect(RoomAreaRectangle, InflateAmount, InflateAmount), GH_Palette.Pink, roomInstance.RoomArea.ToString());
                    RoomArea.Render(graphics, GH_Skin.palette_white_standard);
                    RoomArea.Dispose();

                    RoomLength = GH_Capsule.CreateTextCapsule(RoomLengthRectangle, InflateRect(RoomLengthRectangle, InflateAmount, InflateAmount), GH_Palette.Pink, roomInstance.RoomLength.ToString());
                    RoomLength.Render(graphics, GH_Skin.palette_white_standard);
                    RoomLength.Dispose();
                }
                else
                {
                    base.Render(canvas, graphics, channel);

                    foreach (IGH_DocumentObject obj in targetObjectList)
                    {
                        if (obj != null)
                            DrawTargetArrow(graphics, obj.Attributes.Bounds);
                        //  else
                        //      targetObjectList.Remove(obj);
                    }

                }

            //callNum++;
            // if (callNum == 5)

            if (writerTargetObjectsListString.Length > 0 && targetObjectList.Count == 0)
            {
                // haveReadTargetObjectsList = true;
                if (writerTargetObjectsListString.Length > 0)
                    foreach (string guidS in writerTargetObjectsListString)
                    {
                        try
                        {
                            //   if (((Owner.OnPingDocument().FindComponent(new System.Drawing.Point(int.Parse(guidS.Split('!')[0]), int.Parse(guidS.Split('!')[1]))) as RoomInstance)
                            //      .Attributes as RoomInstanceAttributes).haveReadTargetObjectsList)
                            //  {
                            //      if (((Owner.OnPingDocument().FindComponent(new System.Drawing.Point(int.Parse(guidS.Split('!')[0]), int.Parse(guidS.Split('!')[1]))) as RoomInstance)
                            //                                          .Attributes as RoomInstanceAttributes).targetObjectList.Contains(Owner as RoomInstance))

                            targetObjectList.Add(Owner.OnPingDocument().FindComponent(
                                new System.Drawing.Point(int.Parse(guidS.Split('!')[0]), int.Parse(guidS.Split('!')[1]))) as RoomInstance);
                            //   else
                            //    this.writerTargetObjectsListString = new string[0];
                            // }
                        }
                        catch (Exception e) { }
                    }
            }

        }

        public PointF CircleClosesPoint(PointF point, RectangleF circle)
        {
            Vector2d vec = new Vector2d(circle.X + circle.Width / 2 - point.X, circle.Y + circle.Width / 2 - point.Y);
            vec.Unitize();
            vec = new Vector2d(vec.X * circle.Width / 2, vec.Y * circle.Width / 2);
            return (new PointF((float)(circle.Location.X + circle.Width / 2 + -vec.X), (float)(circle.Location.Y + circle.Width / 2 - vec.Y)));
        }

        private void DrawTargetArrow(Graphics graphics, RectangleF target)
        {
            PointF cp = CircleClosesPoint(Pivot, target);

            double distance = Grasshopper.GUI.GH_GraphicsUtil.Distance(Pivot, cp);
            if (distance < OuterComponentRadius)
                return;

            Circle circle = new Circle(new Point3d(Pivot.X, Pivot.Y, 0.0), OuterComponentRadius - 2);
            PointF tp = GH_Convert.ToPointF(circle.ClosestPoint(new Point3d(cp.X, cp.Y, 0.0)));

            Pen arrowPen = new Pen(roomBrush, (OuterComponentRadius - InnerComponentRadius) / 2);
            arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.RoundAnchor;
            arrowPen.StartCap = System.Drawing.Drawing2D.LineCap.RoundAnchor;
            graphics.DrawLine(arrowPen, tp, cp);
            arrowPen.Dispose();
        }


        private bool _drawing;
        private RectangleF _drawingBox;

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
        {
            _drawing = false;
            _drawingBox = InnerComponentBounds;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // If on outer disc, but not in inner disc.. then start a wire drawing process.
                bool onOuterDisc = Grasshopper.GUI.GH_GraphicsUtil.IsPointInEllipse(Bounds, e.CanvasLocation);
                bool onInnerDisc = Grasshopper.GUI.GH_GraphicsUtil.IsPointInEllipse(InnerComponentBounds, e.CanvasLocation);
                if (onOuterDisc && !onInnerDisc)
                {
                    // Begin arrow drawing behaviour.
                    _drawing = true;
                    sender.CanvasPostPaintObjects += CanvasPostPaintObjects;
                    return GH_ObjectResponse.Capture;
                }
            }

            // Otherwise revert to default behaviour.
            return base.RespondToMouseDown(sender, e);
        }
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
        {
            if (_drawing)
            {
                _drawingBox = new RectangleF(e.CanvasLocation, new SizeF(0, 0));

                GH_Document doc = sender.Document;
                if (doc != null)
                {
                    IGH_Attributes att = doc.FindAttribute(e.CanvasLocation, true);
                    if (att != null)
                    {
                        if (att is IRoomStructure<IGH_DocumentObject>)
                            _drawingBox = att.Bounds;
                    }
                }
                sender.Invalidate();
                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseMove(sender, e);
        }
        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
        {
            if (_drawing)
            {
                _drawing = false;
                sender.CanvasPostPaintObjects -= CanvasPostPaintObjects;

                GH_Document doc = sender.Document;
                if (doc != null)
                {
                    IGH_Attributes att = doc.FindAttribute(e.CanvasLocation, true);
                    if (att != null)
                        if (att is IRoomStructure<IGH_DocumentObject> target)
                        {
                            Owner.RecordUndoEvent("Add Modifier");
                            if (att.DocObject is RoomInstance)
                            {
                                if ((att.DocObject as RoomInstance).RoomId != (DocObject as RoomInstance).RoomId)
                                {
                                    if (targetObjectList.Find(item => (item as RoomInstance).RoomId == (att.DocObject as RoomInstance).RoomId) == null)
                                    {
                                        AddAdjacence(att.DocObject);
                                        target.AddAdjacence(this.DocObject as IGH_DocumentObject);
                                    }
                                    else
                                    {
                                        RemoveAdjacence(att.DocObject);
                                        target.RemoveAdjacence(this.DocObject as IGH_DocumentObject);
                                    }

                                }
                            }
                            else if (att.DocObject is HouseInstance houseInstance)
                            {
                                if ((att as HouseInstanceAttributes).roomInstancesList.Find(item => item.RoomId == (this.DocObject as RoomInstance).RoomId) == null)
                                    target.AddAdjacence(this.DocObject as IGH_DocumentObject);
                                else
                                {
                                    target.RemoveAdjacence(this.DocObject as IGH_DocumentObject);
                                }

                            }


                            //   Owner.AddTarget(att.DocObject.InstanceGuid);
                            IGH_ActiveObject obj = att.DocObject as IGH_ActiveObject;
                          //  if (obj != null)
                           //     obj.ExpireSolution(true);
                           // this.DocObject.ExpireSolution(true);
                        }
                }

                sender.Invalidate();
                return GH_ObjectResponse.Release;
            }

            return base.RespondToMouseUp(sender, e);
        }
        void CanvasPostPaintObjects(GH_Canvas sender)
        {
            if (!_drawing) return;
            DrawTargetArrow(sender.Graphics, _drawingBox);
        }


        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Owner is RoomInstance roomInstance)
            {
                string initial = string.Empty;


                var matrix = sender.Viewport.XFormMatrix(GH_Viewport.GH_DisplayMatrix.CanvasToControl);

                if (this.RoomArea.Contains(e.CanvasLocation))
                {
                    var field = new CapsuleInputBase(RoomArea, roomInstance, RoomInstanceVar.RoomArea)
                    {
                        Bounds = GH_Convert.ToRectangle(RoomArea.Box)
                    };

                    field.ShowTextInputBox(sender, RoomArea.Text, true, false, matrix);
                }


                if (this.RoomLength.Contains(e.CanvasLocation))
                {
                    var field = new CapsuleInputBase(RoomLength, roomInstance, RoomInstanceVar.RoomLength)
                    {
                        Bounds = GH_Convert.ToRectangle(RoomLength.Box)
                    };

                    field.ShowTextInputBox(sender, RoomLength.Text, true, false, matrix);
                }


                if (this.RoomName.Contains(e.CanvasLocation))
                {
                    var field = new CapsuleInputBase(RoomName, roomInstance, RoomInstanceVar.RoomName)
                    {
                        Bounds = GH_Convert.ToRectangle(RoomName.Box)
                    };

                    field.ShowTextInputBox(sender, RoomName.Text, true, false, matrix);
                }
                roomInstance.ExpireSolution(true);

                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }



        public void AddAdjacence(IGH_DocumentObject a)
        {
            targetObjectList.Add(a);
            if (AssignedHouseInstance != null)
                (AssignedHouseInstance.Attributes as HouseInstanceAttributes).AddAdjacence(a);
            else if ((a.Attributes as RoomInstanceAttributes).AssignedHouseInstance != null)
                ((a.Attributes as RoomInstanceAttributes).AssignedHouseInstance.Attributes as HouseInstanceAttributes).AddAdjacence(this.Owner as RoomInstance);
        }

        public void RemoveAdjacence(IGH_DocumentObject a)
        {
            targetObjectList.Remove(a);
            if (AssignedHouseInstance != null)
                (AssignedHouseInstance.Attributes as HouseInstanceAttributes).RemoveAdjacence(a);

        }


        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            string roomInstancesListString = "";
            foreach (RoomInstance room in targetObjectList)
                if (room != null)
                    roomInstancesListString += ((int)(room.Attributes.Pivot.X)).ToString() + "!" +
                    ((int)(room.Attributes.Pivot.Y)).ToString() + "@";

            if (roomInstancesListString.Length > 0)
                roomInstancesListString = roomInstancesListString.Remove(roomInstancesListString.Length - 1);

            writer.SetString("TargetObjectList", roomInstancesListString);
            writer.SetString("RoomName", (Owner as RoomInstance).RoomName);
            //    writer.SetInt32("RoomId", (int)(Owner as RoomInstance).RoomId);
            writer.SetDouble("RoomArea", (Owner as RoomInstance).RoomArea);
            writer.SetDouble("RoomLength", (Owner as RoomInstance).RoomLength);

            string temp = "";
            foreach (int a in RoomInstance.entranceIds)
                temp += a.ToString() +"&";
            if (temp.Length > 0)
                temp = temp.Remove(temp.Length - 1);

            writer.SetString("EntranceIds", temp);

            return base.Write(writer);
        }


        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            string roomInstancesListString = reader.GetString("TargetObjectList");
            writerTargetObjectsListString = roomInstancesListString.Split('@');

            // roomInstancesListString.Remove(roomInstancesListString.Length - 1);

            (Owner as RoomInstance).RoomName = reader.GetString("RoomName");//, (Owner as RoomInstance).RoomName);
                                                                            //    (Owner as RoomInstance).RoomId = (uint)reader.GetInt32("RoomId");//, (int)(Owner as RoomInstance).RoomId);
            (Owner as RoomInstance).RoomArea = (int)Math.Floor(reader.GetDouble("RoomArea"));//, (Owner as RoomInstance).RoomArea);

            //(Owner as RoomInstance).RoomLength = Math.Floor(reader.GetDouble("RoomLength"));

            RoomInstance.entranceIds = new List<int>();
            string temp = "";
            try
            {
                temp = reader.GetString("EntranceIds");
            }
            catch (Exception e) { }

            if (temp != null && temp.Length > 0)
            {
                string[] tempList = temp.Split('&');
                foreach (string s in tempList)
                    RoomInstance.entranceIds.Add(Int32.Parse(s));
            }

            Owner.ExpireSolution(true);

            return base.Read(reader);
        }
    }

    public enum RoomInstanceVar { RoomName, RoomArea, RoomLength };

    class CapsuleInputBase : Grasshopper.GUI.Base.GH_TextBoxInputBase
    {
        public GH_Capsule _input;
        public RoomInstance _roomInstance;
        private RoomInstanceVar _roomInstanceVar;

        public CapsuleInputBase(GH_Capsule input, RoomInstance roomInstance, RoomInstanceVar roomInstanceVar)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _roomInstance = roomInstance;
            _roomInstanceVar = roomInstanceVar;
        }

        protected override void HandleTextInputAccepted(string text)
        {
            _input.Text = text;

            switch (_roomInstanceVar)
            {
                case (RoomInstanceVar.RoomName):
                    _roomInstance.RoomName = text;
                    break;

                case (RoomInstanceVar.RoomArea):
                    _roomInstance.RoomArea = Int32.Parse(text);
                    break;
                case (RoomInstanceVar.RoomLength):
                    _roomInstance.RoomLength = double.Parse(text);
                    break;

            }
            _roomInstance.ExpireSolution(true);
            if ((_roomInstance.Attributes as RoomInstanceAttributes).AssignedHouseInstance != null)
                (_roomInstance.Attributes as RoomInstanceAttributes).AssignedHouseInstance.ExpireSolution(true);
        }
    }


}
