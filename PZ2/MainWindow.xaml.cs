﻿using PZ2.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace PZ2
{
    public partial class MainWindow : Window
    {
        private List<SubstationEntity> listSubstations = new List<SubstationEntity>();
        private List<NodeEntity> listNodes = new List<NodeEntity>();
        private List<SwitchEntity> listSwitches = new List<SwitchEntity>();
        private List<LineEntity> listLines = new List<LineEntity>();
        private List<Model.Point> listPoint = new List<Model.Point>();
        public Int32Collection triangleInd = new Int32Collection() { 2, 3, 1, 2, 1, 0, 7, 1, 3, 7, 5, 1, 6, 5, 7, 6, 4, 5, 6, 2, 4, 2, 0, 4, 2, 7, 3, 2, 6, 7, 0, 1, 5, 0, 5, 4 };

        private ToolTip opis = new ToolTip() { IsOpen = true};

        public static readonly double donjiLeviUgaoMapeLAT = 45.2325;
        public static readonly double donjiLeviUgaoMapeLON = 19.793909;
        public static readonly double gornjiDesniUgaoMapeLAT = 45.277031;
        public static readonly double gornjiDesniUgaoMapeLON = 19.894459;

        public static readonly double sirinaIVisinaLinije = 0.0015;
        public static readonly double velicinaKocke = 0.008;

        public static int zoomMax = 50;
        public static int zoomTrenutni = 1;
        public static int zoomMin = 1;


        public MainWindow()
        {
            InitializeComponent();
            LoadXml();
            DrawSubstations();
            DrawNodes();
            DrawSwitches();
            DrawLines();
        }

        #region ToLatLon
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
        #endregion

        #region XMLLOAD
        private void LoadXml()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNodeList nodeList;
            xmlDoc.Load("Geographic.xml");

            #region SUBSTATION 
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {
                long Id = long.Parse(node["Id"].InnerText);
                string Name = node["Name"].InnerText;
                double X = double.Parse(node.SelectSingleNode("X").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                double Y = double.Parse(node.SelectSingleNode("Y").InnerText, CultureInfo.InvariantCulture.NumberFormat);

                ToLatLon(X, Y, 34, out double newX, out double newY);

                X = newX;
                Y = newY;
                if (Ogranicenja(X, Y))
                {
                    listPoint.Add(new Model.Point(X, Y));

                    listSubstations.Add(new SubstationEntity(Id, Name, X, Y));
                }
            }
            #endregion
            #region NODES 
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {
                long Id = long.Parse(node["Id"].InnerText);
                string Name = node["Name"].InnerText;
                double X = double.Parse(node.SelectSingleNode("X").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                double Y = double.Parse(node.SelectSingleNode("Y").InnerText, CultureInfo.InvariantCulture.NumberFormat);

                ToLatLon(X, Y, 34, out double newX, out double newY);

                X = newX;
                Y = newY;
                if (Ogranicenja(X, Y))
                {
                    listPoint.Add(new Model.Point(X, Y));

                    listNodes.Add(new NodeEntity(Id, Name, X, Y));
                }
            }
            #endregion
            #region SWITCHES 
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {
                long Id = long.Parse(node["Id"].InnerText);
                string Name = node["Name"].InnerText;
                string Status = node["Status"].InnerText;
                double X = double.Parse(node.SelectSingleNode("X").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                double Y = double.Parse(node.SelectSingleNode("Y").InnerText, CultureInfo.InvariantCulture.NumberFormat);

                ToLatLon(X, Y, 34, out double newX, out double newY);

                X = newX;
                Y = newY;
                if (Ogranicenja(X, Y))
                {
                    listPoint.Add(new Model.Point(X, Y));

                    listSwitches.Add(new SwitchEntity(Id, Name, Status, X, Y));
                }
            }
            #endregion
            #region LINES 
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in nodeList)
            {
                LineEntity linija = new LineEntity();

                linija.Id = long.Parse(node.SelectSingleNode("Id").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                linija.Name = node.SelectSingleNode("Name").InnerText;
                bool IsUnderground = Convert.ToBoolean(node["IsUnderground"].InnerText);
                linija.IsUnderground = IsUnderground;
                linija.R = float.Parse(node.SelectSingleNode("R").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                linija.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                linija.LineType = node.SelectSingleNode("LineType").InnerText;
                linija.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                linija.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText, CultureInfo.InvariantCulture.NumberFormat);
                linija.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText, CultureInfo.InvariantCulture.NumberFormat);
               
                XmlNodeList listChild = node.ChildNodes;
                XmlNodeList listaTacaka = listChild[9].ChildNodes;

                foreach (XmlNode temp in listaTacaka)
                {
                    Model.Point tacka = new Model.Point(double.Parse(temp.SelectSingleNode("X").InnerText, CultureInfo.InvariantCulture.NumberFormat), double.Parse(temp.SelectSingleNode("Y").InnerText, CultureInfo.InvariantCulture.NumberFormat));
                    ToLatLon(tacka.X, tacka.Y, 34, out double newX, out double newY);
                    if (Ogranicenja(newX, newY))
                    {
                        linija.Vertices.Add(tacka);
                    }
                }

                if (listaTacaka.Count == linija.Vertices.Count)                 
                    listLines.Add(linija);
            }
            #endregion
        }
        private bool Ogranicenja(double x, double y)
        {
            return x >= donjiLeviUgaoMapeLAT   &&
                   x <= gornjiDesniUgaoMapeLAT && 
                   y >= donjiLeviUgaoMapeLON   && 
                   y <= gornjiDesniUgaoMapeLON ;
        }
        #endregion

        #region DrawCube()
        private GeometryModel3D DrawCube(double tempX, double tempY, SolidColorBrush color)
        {
            double X = (tempY - donjiLeviUgaoMapeLON) / (gornjiDesniUgaoMapeLON - donjiLeviUgaoMapeLON) * (1.0 - velicinaKocke);
            double Y = (tempX - donjiLeviUgaoMapeLAT) / (gornjiDesniUgaoMapeLAT - donjiLeviUgaoMapeLAT) * (1.0 - velicinaKocke);
            double Z = 0;

            Point3DCollection position = new Point3DCollection
            {
                new Point3D(X, Y, Z),
                new Point3D(X + velicinaKocke, Y, Z),
                new Point3D(X, Y + velicinaKocke, Z),
                new Point3D(X + velicinaKocke, Y + velicinaKocke, Z),
                new Point3D(X, Y, Z + velicinaKocke),
                new Point3D(X + velicinaKocke, Y, Z + velicinaKocke),
                new Point3D(X, Y + velicinaKocke, Z + velicinaKocke),
                new Point3D(X + velicinaKocke, Y + velicinaKocke, Z + velicinaKocke)
            };

            MeshGeometry3D mreza = new MeshGeometry3D
            {
                Positions = position,
                TriangleIndices = triangleInd
            };

            foreach(var temp in mapa.Children)
            {
                if( Math.Abs(mreza.Bounds.X - temp.Bounds.X) < velicinaKocke && Math.Abs(mreza.Bounds.Y - temp.Bounds.Y) < velicinaKocke && Math.Abs(mreza.Bounds.Z - temp.Bounds.Z) < velicinaKocke)
                {
                    for(var i = 0; i < mreza.Positions.Count; i++)
                    {
                        mreza.Positions[i] = new Point3D(mreza.Positions[i].X, mreza.Positions[i].Y, mreza.Positions[i].Z + velicinaKocke);
                    }
                }
            }

            GeometryModel3D gm3D = new GeometryModel3D
            {
                Material = new DiffuseMaterial(color),
                Geometry = mreza
            };
            return gm3D;
        }
        #endregion

        #region Draw Substation/Node/Switches/Lines
        private void DrawSubstations()
        {
            foreach(var temp in listSubstations)
            {
                var v = DrawCube(temp.X, temp.Y, Brushes.MediumPurple);
                v.SetValue(FrameworkElement.TagProperty, temp);
                mapa.Children.Add(v);
            }
        }
        private void DrawNodes()
        {
            foreach (var temp in listNodes)
            {
                var v = DrawCube(temp.X, temp.Y, Brushes.Turquoise);
                v.SetValue(FrameworkElement.TagProperty, temp);
                mapa.Children.Add(v);
            }
        }
        private void DrawSwitches()
        {
            foreach (var temp in listSwitches)
            {
                var v = DrawCube(temp.X, temp.Y, Brushes.DarkGoldenrod);
                v.SetValue(FrameworkElement.TagProperty, temp);
                mapa.Children.Add(v);
            }
        }        
        private void DrawLines() //"Steel" crni vodovi
                                 //"Acsr" crveni vodovi
                                 //"Copper" narandzasti vodovi
        {
            foreach (var item in listLines)
            {
                double x;
                double y;
                List<System.Windows.Point> pointsList = new List<System.Windows.Point>();

                foreach (var item2 in item.Vertices)
                {
                    ToLatLon(item2.X, item2.Y, 34, out x, out y);
                    double newY = (x - donjiLeviUgaoMapeLAT) / (gornjiDesniUgaoMapeLAT - donjiLeviUgaoMapeLAT) * (1.0 - velicinaKocke);
                    double newX = (y - donjiLeviUgaoMapeLON) / (gornjiDesniUgaoMapeLON - donjiLeviUgaoMapeLON) * (1.0 - velicinaKocke);

                    System.Windows.Point point = new System.Windows.Point(newX, newY);
                    pointsList.Add(point);
                }

                for(int i = 0; i < pointsList.Count - 1; i++)
                {
                    Point3DCollection Positions = new Point3DCollection();
                    Positions.Add(new Point3D(pointsList[i].X, pointsList[i].Y, 0));
                    Positions.Add(new Point3D(pointsList[i].X + sirinaIVisinaLinije, pointsList[i].Y, 0));
                    Positions.Add(new Point3D(pointsList[i].X, pointsList[i].Y + sirinaIVisinaLinije, 0));
                    Positions.Add(new Point3D(pointsList[i].X + sirinaIVisinaLinije, pointsList[i].Y + sirinaIVisinaLinije, 0));
                    Positions.Add(new Point3D(pointsList[i + 1].X, pointsList[i + 1].Y, sirinaIVisinaLinije));
                    Positions.Add(new Point3D(pointsList[i + 1].X + sirinaIVisinaLinije, pointsList[i + 1].Y, sirinaIVisinaLinije));
                    Positions.Add(new Point3D(pointsList[i + 1].X, pointsList[i + 1].Y + sirinaIVisinaLinije, sirinaIVisinaLinije));
                    Positions.Add(new Point3D(pointsList[i + 1].X + sirinaIVisinaLinije, pointsList[i + 1].Y + sirinaIVisinaLinije, sirinaIVisinaLinije));

                    GeometryModel3D obj = new GeometryModel3D();
                    if (item.ConductorMaterial == "Steel")
                    {
                        obj.Material = new DiffuseMaterial(Brushes.Black);
                    }
                    else if(item.ConductorMaterial == "Acsr")
                    {
                        obj.Material = new DiffuseMaterial(Brushes.Red);
                    }          
                    else if (item.ConductorMaterial == "Copper")
                    {
                        obj.Material = new DiffuseMaterial(Brushes.Orange);
                    }
                    obj.Geometry = new MeshGeometry3D() { Positions = Positions, TriangleIndices = triangleInd };
                    obj.SetValue(FrameworkElement.TagProperty, item);

                    mapa.Children.Add(obj);
                }
            }
        }
        #endregion

        
        private HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rezultat)
        {
            var hitResult = rezultat as RayHitTestResult;
            var value = hitResult?.ModelHit.GetValue(FrameworkElement.TagProperty);
            Model3D originalModel = hitResult?.ModelHit;

            if (value is NodeEntity || value is SwitchEntity || value is SubstationEntity)
            {
                opis.Content = value.ToString();
                opis.IsOpen = true;
            }
            else if (value is LineEntity)
            {
                LineEntity line = value as LineEntity;
                List<GeometryModel3D> endList = new List<GeometryModel3D>();

                foreach (Model3D model in mapa.Children)
                {
                    var entitet = model.GetValue(TagProperty);
                    if (entitet is NodeEntity)
                    {
                        NodeEntity temp = entitet as NodeEntity;
                        if (temp.Id == line.FirstEnd || temp.Id == line.SecondEnd)
                        {
                            endList.Add(model as GeometryModel3D);
                        }
                    }
                    else if (entitet is SwitchEntity)
                    {
                        SwitchEntity temp = entitet as SwitchEntity;
                        if (temp.Id == line.FirstEnd || temp.Id == line.SecondEnd)
                        {
                            endList.Add(model as GeometryModel3D);
                        }
                    }
                    else if (entitet is SubstationEntity)
                    {
                        SubstationEntity temp = entitet as SubstationEntity;
                        if (temp.Id == line.FirstEnd || temp.Id == line.SecondEnd)
                        {
                            endList.Add(model as GeometryModel3D);
                        }
                    }
                    if (endList.Count == 2)
                        break;
                }

                foreach (var v in endList)
                {
                    v.Material = new DiffuseMaterial(Brushes.Yellow);
                }
            }
            return HitTestResultBehavior.Stop;
        }


        private System.Windows.Point original = new System.Windows.Point();
        private System.Windows.Point pocetnaTackaRotacije = new System.Windows.Point();
        private System.Windows.Point pocetnaTacka = new System.Windows.Point();

        private void Viewport3d_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewport3d.ReleaseMouseCapture();
            opis.IsOpen = false; 
        }

        private void Viewport3d_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport3d.CaptureMouse();
            pocetnaTacka = e.GetPosition(this); 
            original.X = translate.OffsetX; 
            original.Y = translate.OffsetY;


            PointHitTestParameters pointparams = new PointHitTestParameters(pocetnaTacka);
            VisualTreeHelper.HitTest(this, null, HTResult, pointparams);
        }


        private void Viewport3d_MouseMove(object sender, MouseEventArgs e)
        {
            if (viewport3d.IsMouseCaptured)
            {
                System.Windows.Point tacka = e.MouseDevice.GetPosition(this);
                double poXOsi = tacka.X - pocetnaTacka.X;
                double poYOsi = tacka.Y - pocetnaTacka.Y;

                double sirina = this.Width;
                double visina = this.Height;

                double translacijaPoX = (poXOsi * 100) / sirina;
                double translacijaPoY = -(poYOsi * 100) / visina;

                translate.OffsetX = original.X + (translacijaPoX / (100 * scale.ScaleX));
                translate.OffsetY = original.Y + (translacijaPoY / (100 * scale.ScaleY)); 
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                System.Windows.Point tacka = e.GetPosition(viewport3d);
                double poXOsi = (tacka.X - pocetnaTackaRotacije.X) + ugao1.Angle;
                double poYOsi = (tacka.Y - pocetnaTackaRotacije.Y) + ugao2.Angle;

                if (-90 <= poXOsi && poXOsi <= 90)
                {
                    ugao1.Angle = poXOsi;
                }
                if (-90 <= poYOsi && poYOsi <= 90)
                {
                    ugao2.Angle = poYOsi;
                }
                pocetnaTackaRotacije = tacka;
            }
        }


        private void Viewport3d_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double skaliranjePoXOsi = 1;
            double skaliranjePoYOsi = 1;
            double skaliranjePoZOsi = 1;
            if (e.Delta > 0 && zoomTrenutni < zoomMax)
            {
                skaliranjePoXOsi = scale.ScaleX + 0.025;
                skaliranjePoYOsi = scale.ScaleY + 0.025;
                skaliranjePoZOsi = scale.ScaleZ + 0.025;
                zoomTrenutni++;
                scale.ScaleX = skaliranjePoXOsi;
                scale.ScaleY = skaliranjePoYOsi;
                scale.ScaleZ = skaliranjePoZOsi;
            }
            else if (e.Delta <= 0 && zoomTrenutni > zoomMin)
            {
                skaliranjePoXOsi = scale.ScaleX - 0.025;
                skaliranjePoYOsi = scale.ScaleY - 0.025;
                skaliranjePoZOsi = scale.ScaleZ - 0.025;
                zoomTrenutni--;
                scale.ScaleX = skaliranjePoXOsi;
                scale.ScaleY = skaliranjePoYOsi;
                scale.ScaleZ = skaliranjePoZOsi;
            }
        }

        private List<Model3D> listaSvihVodovaZaBrisanje = new List<Model3D>();
        private void SakrivanjeNeaktivnogDela_Click(object sender, RoutedEventArgs e)
        {
           foreach (Model3D model in mapa.Children)
            {
                var vod = model.GetValue(TagProperty);
                if (vod is LineEntity)
                {
                    LineEntity line = vod as LineEntity;

                    foreach(Model3D model1 in mapa.Children)
                    {
                        var switchEn = model1.GetValue(TagProperty);
                        if (switchEn is SwitchEntity)
                        {
                            SwitchEntity swE = switchEn as SwitchEntity;
                            if (swE.Status == "Open" && swE.Id == line.FirstEnd) 
                            {
                                listaSvihVodovaZaBrisanje.Add(model);
                                listaSvihVodovaZaBrisanje.Add(model1);
                            }
                        }
                    }

                    foreach(Model3D model2 in mapa.Children)
                    {
                        var entitet = model2.GetValue(TagProperty);
                        if (entitet is NodeEntity)
                        {
                            NodeEntity temp = entitet as NodeEntity;
                            if (temp.Id == line.SecondEnd)
                            {
                                listaSvihVodovaZaBrisanje.Add(model2);
                            }
                        }
                        else if (entitet is SwitchEntity)
                        {
                            SwitchEntity temp = entitet as SwitchEntity;
                            if (temp.Id == line.SecondEnd)
                            {
                                listaSvihVodovaZaBrisanje.Add(model2);
                            }
                        }
                        else if (entitet is SubstationEntity)
                        {
                            SubstationEntity temp = entitet as SubstationEntity;
                            if (temp.Id == line.SecondEnd)
                            {
                                listaSvihVodovaZaBrisanje.Add(model2);
                            }
                        }
                    }
                }                    
            }
            foreach (Model3D modelZaBrisanje in listaSvihVodovaZaBrisanje)
            {
                mapa.Children.Remove(modelZaBrisanje);
            }
           
        }
        private void PrikazivanjeNeaktivnogDela_Click(object sender, RoutedEventArgs e)
        {
            foreach(Model3D model in listaSvihVodovaZaBrisanje)
            {
                mapa.Children.Add(model);
            }
            listaSvihVodovaZaBrisanje.Clear(); 
        }

        private List<GeometryModel3D> switchClosedList = new List<GeometryModel3D>();
        private List<GeometryModel3D> switchOpenList = new List<GeometryModel3D>();
        private void SwitchClosed_Click(object sender, RoutedEventArgs e)
        {
            foreach (Model3D model in mapa.Children)
            {
                var entitet = model.GetValue(TagProperty);
                if (entitet is SwitchEntity)
                {
                    SwitchEntity temp = entitet as SwitchEntity;
                    if (temp.Status == "Closed")
                    {
                        switchClosedList.Add(model as GeometryModel3D);
                    }
                }
            }
            foreach(var v in switchClosedList)
            {
                v.Material = new DiffuseMaterial(Brushes.Red); 
            }
        }
        private void SwitchOpen_Click(object sender, RoutedEventArgs e)
        {
            foreach (Model3D model in mapa.Children)
            {
                var entitet = model.GetValue(TagProperty);
                if (entitet is SwitchEntity)
                {
                    SwitchEntity temp = entitet as SwitchEntity;
                    if (temp.Status == "Open")
                    {
                        switchOpenList.Add(model as GeometryModel3D);
                    }
                }
            }
            foreach (var v in switchOpenList)
            {
                v.Material = new DiffuseMaterial(Brushes.Green);
            }
        }
        private void SwitchInicijalnaBoja_Click(object sender, RoutedEventArgs e)
        {
            if(switchClosedList.Count > 0)
            {
                foreach(var v in switchClosedList)
                {
                    v.Material = new DiffuseMaterial(Brushes.Chocolate);
                }
                switchClosedList.Clear();
            }
            if (switchOpenList.Count > 0)
            {
                foreach (var v in switchOpenList)
                {
                    v.Material = new DiffuseMaterial(Brushes.Chocolate);
                }
                switchOpenList.Clear();
            }
        }

        // <1  - crvena boja
        // 1-2 - narandzasta
        // 2<  - zuta

        private List<GeometryModel3D> vodoviIspod1List = new List<GeometryModel3D>();
        private List<GeometryModel3D> vodoviOd1Do2List = new List<GeometryModel3D>();
        private List<GeometryModel3D> vodoviIznad2List = new List<GeometryModel3D>();
        private void PromenaBojeVodova_Click(object sender, RoutedEventArgs e)
        {
            foreach (Model3D model in mapa.Children)
            {
                var entitet = model.GetValue(TagProperty);
                if (entitet is LineEntity)
                {
                    LineEntity temp = entitet as LineEntity;
                    if (temp.R <1)
                    {
                        vodoviIspod1List.Add(model as GeometryModel3D);
                    }
                    else if(temp.R >=1  && temp.R <= 2)
                    {
                        vodoviOd1Do2List.Add(model as GeometryModel3D);
                    }
                    else if (temp.R > 2)
                    {
                        vodoviIznad2List.Add(model as GeometryModel3D);
                    }
                }
            }
            foreach (var v in vodoviIspod1List)
            {
                v.Material = new DiffuseMaterial(Brushes.Red);
            }
            foreach (var v in vodoviOd1Do2List)
            {
                v.Material = new DiffuseMaterial(Brushes.Orange);
            }
            foreach (var v in vodoviIznad2List)
            {
                v.Material = new DiffuseMaterial(Brushes.Yellow); 
            }
        }
        private void VracanjeBojeNaInicijalnu_Click(object sender, RoutedEventArgs e)
        {

            VratiBojuVodova(vodoviIspod1List);
            VratiBojuVodova(vodoviOd1Do2List);
            VratiBojuVodova(vodoviIznad2List);

            vodoviIspod1List.Clear();            
            vodoviOd1Do2List.Clear();
            vodoviIznad2List.Clear();
        }
        public void VratiBojuVodova(List<GeometryModel3D> vodLista)
        {
            foreach (var v in vodLista)
            {
                var entitet = v.GetValue(TagProperty);
                if (entitet is LineEntity)
                {
                    LineEntity vod = entitet as LineEntity;

                    if (vod.ConductorMaterial == "Steel")
                    {
                        v.Material = new DiffuseMaterial(Brushes.Black);
                    }
                    else if (vod.ConductorMaterial == "Acsr")
                    {
                        v.Material = new DiffuseMaterial(Brushes.Red);
                    }
                    else if (vod.ConductorMaterial == "Copper")
                    {
                        v.Material = new DiffuseMaterial(Brushes.Orange);
                    }
                }
            }
        }


        private List<Model3D> hiddenSub = new List<Model3D>();
        private List<Model3D> sakriveniVodovi = new List<Model3D>();
        private List<Model3D> hiddenSwitch = new List<Model3D>();
        private List<Model3D> hiddenNode = new List<Model3D>();

        private void SakrijSveVodove_Click(object sender, RoutedEventArgs e)
        {
            foreach(Model3D model in mapa.Children)
            {
                var entity = model.GetValue(TagProperty);
                if (entity is LineEntity)
                {
                    hiddenSub.Add(model);
                }
            }
            foreach (Model3D vodZaSakrivanje in sakriveniVodovi)
            {
                mapa.Children.Remove(vodZaSakrivanje);
            }
        }
        private void PrikaziSveVodove_Click(object sender, RoutedEventArgs e)
        {
            foreach (Model3D vodZaPrikazivanje in sakriveniVodovi)
            {
                mapa.Children.Add(vodZaPrikazivanje); 
            }
            sakriveniVodovi.Clear();
        }

        private void ToggleSubstation(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.IsChecked == true)
            {
                if (radioButton == ToggleSubstation_option1)
                {
                    // Option 1 is selected - perform action
                    foreach (Model3D model in mapa.Children)
                    {
                        var entity = model.GetValue(TagProperty);
                       
                        if (entity is SwitchEntity || entity is NodeEntity)
                        {
                            hiddenSub.Add(model);
                        }
                    }
                    foreach (Model3D vodZaSakrivanje in hiddenSub)
                    {
                        mapa.Children.Remove(vodZaSakrivanje);
                    }
                }
                else if (radioButton == ToggleSubstation_option2)
                {
                    // Option 2 is selected - perform action
                    foreach (Model3D vodZaPrikazivanje in hiddenSub)
                    {
                        mapa.Children.Add(vodZaPrikazivanje);
                    }
                    hiddenSub.Clear();
                }
            }
        }

        private void ToggleSwitch(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.IsChecked == true)
            {
                if (radioButton == ToggleSwitch_option1)
                {
                    // Option 1 is selected - perform action
                    foreach (Model3D model in mapa.Children)
                    {
                        var entity = model.GetValue(TagProperty);
                        if (entity is NodeEntity || entity is SubstationEntity)
                        {
                            hiddenSwitch.Add(model);
                        }
                    }
                    foreach (Model3D vodZaSakrivanje in hiddenSwitch)
                    {
                        mapa.Children.Remove(vodZaSakrivanje);
                    }
                }
                else if (radioButton == ToggleSwitch_option2)
                {
                    // Option 2 is selected - perform action
                    foreach (Model3D vodZaPrikazivanje in hiddenSwitch)
                    {
                        mapa.Children.Add(vodZaPrikazivanje); 
                    }
                    hiddenSwitch.Clear();
                }
            }
        }

        private void ToggleNode(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.IsChecked == true)
            {
                if (radioButton == ToggleNode_option1)
                {
                    // Option 1 is selected - perform action
                    foreach (Model3D model in mapa.Children)
                    {
                        var entity = model.GetValue(TagProperty);
                        if (entity is SubstationEntity || entity is SwitchEntity)
                        {
                            hiddenNode.Add(model);
                        }
                    }
                    foreach (Model3D vodZaSakrivanje in hiddenNode)
                    {
                        mapa.Children.Remove(vodZaSakrivanje);
                    }
                }
                else if (radioButton == ToggleNode_option2)
                {
                    // Option 2 is selected - perform action
                    foreach (Model3D vodZaPrikazivanje in hiddenNode)
                    {
                        mapa.Children.Add(vodZaPrikazivanje);
                    }
                    hiddenNode.Clear();
                }
            }
        }
    }
}