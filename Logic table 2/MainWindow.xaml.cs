using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Resources;
using System.Threading;

namespace Logic_table_2
{
    public static class CONFIG
    {
        public static float NODES_SIZE = 100;
        public static float CONNECTIONS_SIZE = 3;
        public static Cursor GRAB_CURSOR;
        public static Cursor GRABBING_CURSOR;
        public static SolidColorBrush BLACK = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush WHITE = new SolidColorBrush(Colors.White);
        public static SolidColorBrush BLUE_TRANSPARENT = new SolidColorBrush(Color.FromArgb(150, 120, 120, 250));
    }
    public interface IChoosable
    {
        bool isChosen { get; set; }
        void select();
        void deselect();
        UIElement selectionView { get; set; }
        event MouseButtonEventHandler OnLeftMouseButtonDown, OnLeftMouseButtonUp;
    }
    public abstract class LogicNode
    {
        public delegate void ConnectionEventHandler(LogicNode from, LogicNode to);
        public event ConnectionEventHandler OnConnectionAdd, OnConnectionRemove;
        private List<LogicNode> inputs = new List<LogicNode>();
        protected bool state = false;
        protected bool nextState = false;
        protected int maxInputs = -1;

        public void addInput(LogicNode input)
        {
            if ((maxInputs != -1) && (inputs.Count() >= maxInputs))
            {
                OnConnectionRemove(inputs[0], this);
                inputs.RemoveAt(0);
                inputs.Insert(0, input);
                OnConnectionAdd(input, this);
                return;
            }
            inputs.Add(input);
            OnConnectionAdd(input, this);
        }
        public bool removeInput(LogicNode input)
        {
            if (inputs.Remove(input))
            {
                OnConnectionRemove(input, this);
                return true;
            }
            return false;
        }
        public void set_instantly(bool state)
        {
            nextState = state;
            this.state = state;
        }
        public void set(bool state)
        {
            nextState = state;
        }
        public bool get()
        {
            return state;
        }
        public void update_state()
        {
            state = nextState;
        }
        public void recount()
        {
            bool[] temp_inputs = new bool[inputs.Count()];
            for (int i = 0; i < temp_inputs.Length; i++)
                temp_inputs[i] = inputs[i].get();
            set(func(temp_inputs));
        }
        protected abstract bool func(bool[] inputs);
    }
    public abstract class VisualNode : LogicNode, IChoosable
    {
        public Grid view = new Grid();
        private Ellipse el = new Ellipse();
        private TextBlock funcText = new TextBlock();
        public float x, y;

        public UIElement selectionView { get; set; }
        public bool isChosen { get; set; }
        public event MouseButtonEventHandler OnLeftMouseButtonDown, OnLeftMouseButtonUp;

        public VisualNode(float x, float y, string text)
        {
            this.x = x;
            this.y = y;
            funcText.Text = text;
            construct();
        }
        private void construct()
        {
            view.HorizontalAlignment = HorizontalAlignment.Left;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { OnLeftMouseButtonDown(this, e); };
            view.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { OnLeftMouseButtonUp(this, e); };
            Grid.SetZIndex(view, 1);

            el.HorizontalAlignment = HorizontalAlignment.Center;
            el.VerticalAlignment = VerticalAlignment.Center;
            el.Visibility = Visibility.Visible;
            view.Children.Add(el);

            funcText.HorizontalAlignment = HorizontalAlignment.Center;
            funcText.VerticalAlignment = VerticalAlignment.Center;
            funcText.Width = double.NaN;
            funcText.Height = double.NaN;
            funcText.FontWeight = FontWeights.Bold;
            funcText.Visibility = Visibility.Visible;
            view.Children.Add(funcText);

            Ellipse ch = new Ellipse();
            ch.HorizontalAlignment = HorizontalAlignment.Center;
            ch.VerticalAlignment = VerticalAlignment.Center;
            ch.Visibility = Visibility.Hidden;
            ch.Fill = null;
            ch.Stroke = CONFIG.BLUE_TRANSPARENT;
            ch.StrokeThickness = 5.0;
            selectionView = ch;
            view.Children.Add(selectionView);
        }
        public void select()
        {
            isChosen = true;
            selectionView.Visibility = Visibility.Visible;
        }
        public void deselect()
        {
            isChosen = false;
            selectionView.Visibility = Visibility.Hidden;
        }
        public void redraw(float camx, float camy, float scale)
        {
            view.Dispatcher.Invoke(delegate
            {
                ((Ellipse)selectionView).Height = CONFIG.NODES_SIZE * scale * 1.2;
                ((Ellipse)selectionView).Width = CONFIG.NODES_SIZE * scale * 1.2;
                ((Ellipse)selectionView).StrokeThickness = 5.0 * scale;
                view.Height = CONFIG.NODES_SIZE * scale * 2.0;
                view.Width = CONFIG.NODES_SIZE * scale * 2.0;
                el.Height = CONFIG.NODES_SIZE * scale;
                el.Width = CONFIG.NODES_SIZE * scale;
                funcText.FontSize = view.Height / 10.0;
                view.Margin = new Thickness((x - camx - CONFIG.NODES_SIZE) * scale, (y - camy - CONFIG.NODES_SIZE) * scale, 0, 0);
                el.Fill = get() ? CONFIG.WHITE : CONFIG.BLACK;
                funcText.Foreground = get() ? CONFIG.BLACK : CONFIG.WHITE;
            });
        }
    }
    public class LogicNode_And : VisualNode
    {
        public LogicNode_And(float x, float y, string text) : base(x, y, text)
        {
        }
        protected override bool func(bool[] inputs)
        {
            if (inputs.Length == 0)
                return false;
            foreach (bool a in inputs)
                if (!a)
                    return false;
            return true;
        }
    }
    public class LogicNode_Or : VisualNode
    {
        public LogicNode_Or(float x, float y, string text) : base(x, y, text)
        {
        }
        protected override bool func(bool[] inputs)
        {
            foreach (bool a in inputs)
                if (a)
                    return true;
            return false;
        }
    }
    public class LogicNode_Not : VisualNode
    {
        public LogicNode_Not(float x, float y, string text) : base(x, y, text)
        {
            state = true;
            nextState = true;
            maxInputs = 1;
        }
        protected override bool func(bool[] inputs)
        {
            return inputs.Length > 0 ? !inputs[0] : true;
        }
    }
    public class Connection : IChoosable
    {
        public VisualNode from, to;
        private Line view;
        public bool isChosen { get; set; }
        public UIElement selectionView { get; set; }
        public event MouseButtonEventHandler OnLeftMouseButtonDown;
        public event MouseButtonEventHandler OnLeftMouseButtonUp;

        public Connection(VisualNode from, VisualNode to)
        {
            this.from = from;
            this.to = to;

            view = new Line();
            view.HorizontalAlignment = HorizontalAlignment.Left;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.Stroke = CONFIG.BLACK;
            view.StrokeThickness = CONFIG.CONNECTIONS_SIZE;
            Grid.SetZIndex(view, -1);
        }
        public void select()
        {

        }
        public void deselect()
        {

        }
        public void addTo(Grid grid)
        {
            grid.Children.Add(view);
        }
        public void removeFrom(Grid grid)
        {
            grid.Children.Remove(view);
        }
        public void redraw(float camx, float camy, float scale)
        {
            view.X1 = (from.x - camx) * scale;
            view.Y1 = (from.y - camy) * scale;
            view.X2 = (to.x - camx) * scale;
            view.Y2 = (to.y - camy) * scale;
            view.StrokeThickness = CONFIG.CONNECTIONS_SIZE * scale;
        }
    }
    public class Document
    {
        public string name = "untitled";
        private List<LogicNode> nodes = new List<LogicNode>();
        private List<Connection> connections = new List<Connection>();
        private Point camPos = new Point();
        private float camScale = 1.0f;
        private Grid view;
        private bool isUpdating = false;
        private bool needStopUpdating = false;
        
        private Point areaFrom = new Point(), areaTo = new Point();
        private Grid areaGrid;

        private bool isMoving = false;
        private Point prevMousePos;

        private List<KeyValuePair<LogicNode, Point>> relativePositions = new List<KeyValuePair<LogicNode, Point>>();

        public Document(Grid grid)
        {
            setGrid();
            grid.Children.Add(view);
        }
        public void hide()
        {
            stopUpdating();
            view.Visibility = Visibility.Hidden;
        }
        public void show()
        {
            view.Visibility = Visibility.Visible;
        }
        public void removeFrom(Grid grid)
        {
            grid.Children.Remove(view);
        }
        private void setGrid()
        {
            view = new Grid();
            view.Width = Double.NaN;
            view.Height = Double.NaN;
            view.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            view.HorizontalAlignment = HorizontalAlignment.Stretch;
            view.VerticalAlignment = VerticalAlignment.Stretch;
            view.MouseDown += onGridMouseDown;
            view.MouseUp += onGridMouseUp;
            view.MouseWheel += onGridWheelTurn;
            
            
            view.MouseMove += onGridMouseMove;
            Grid.SetRow(view, 1);
        }
        public void startUpdating()
        {
            if (!isUpdating)
            {
                Thread updateThread = new Thread(new ThreadStart(cycleUpdating));
                updateThread.Start();
                isUpdating = true;
                needStopUpdating = false;
            }
        }
        public bool getIsUpdating()
        {
            return isUpdating;
        }
        public void stopUpdating()
        {
            if (isUpdating)
                needStopUpdating = true;
        }
        private void recountNodes()
        {
            foreach (LogicNode node in nodes)
                node.recount();
        }
        private void updateNodesStates()
        {
            foreach (LogicNode node in nodes)
                node.update_state();
        }
        public void updateTick()
        {
            if (!isUpdating)
            {
                Thread updateThread = new Thread(new ThreadStart(_updateTick));
                updateThread.Start();
            }
        }
        private void _updateTick()
        {
            try
            {
                recountNodes();
                updateNodesStates();
                reDraw();
            }
            catch
            {

            }
        }
        private void cycleUpdating()
        {
            while (!needStopUpdating)
            {
                _updateTick();
            }
            isUpdating = false;
        }
        public Point getCamPos()
        {
            return new Point(camPos.X, camPos.Y);
        }
        public float getCamScale()
        {
            return camScale;
        }
        public void setCamPos(float camX, float camY)
        {
            camPos.X = camX;
            camPos.Y = camY;
            reDraw();
        }
        public void setCamScale(float scale)
        {
            camScale = scale;
            reDraw();
        }
        public void addNode(double x, double y, string func)
        {
            LogicNode node = null;
            switch (func)
            {
                case "AND":
                    node = new LogicNode_And((float)x, (float)y, func);
                    break;
                case "OR":
                    node = new LogicNode_Or((float)x, (float)y, func);
                    break;
                case "NOT":
                    node = new LogicNode_Not((float)x, (float)y, func);
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
            nodes.Add(node);
            
            ((VisualNode)node).redraw((float)camPos.X, (float)camPos.Y, camScale);
            view.Children.Add(((VisualNode)node).view);
            ((VisualNode)node).OnLeftMouseButtonDown += onNodeLeftMouseButtonDown;
            ((VisualNode)node).OnLeftMouseButtonUp += onNodeLeftMouseButtonUp;
            node.OnConnectionAdd += onConnectionAdded;
            node.OnConnectionRemove += onConnectionRemoved;
        }
        public void removeNode(LogicNode node)
        {
            nodes.Remove(node);
        }
        private void reDraw()
        {
            foreach (VisualNode node in nodes)
                node.redraw((float)camPos.X, (float)camPos.Y, camScale);
            foreach (Connection conn in connections)
                conn.redraw((float)camPos.X, (float)camPos.Y, camScale);
        }

        private void onConnectionAdded(LogicNode from, LogicNode to)
        {

            Connection conn = new Connection(((VisualNode)from), ((VisualNode)to));
            conn.addTo(view);
            connections.Add(conn);
            conn.redraw((float)camPos.X, (float)camPos.Y, camScale);
        }
        private void onConnectionRemoved(LogicNode from, LogicNode to)
        {
            for (int i = 0; i < connections.Count(); i++)
                if (connections[i].from == from && connections[i].to == to)
                {
                    connections[i].removeFrom(view);
                    connections.RemoveAt(i);
                    i--;
                }
        }
        private void removeConnectionsTo(LogicNode node)
        {
            for (int i = 0; i < connections.Count(); i++)
                if (connections[i].from == node || connections[i].to == node)
                {
                    connections[i].removeFrom(view);
                    connections.RemoveAt(i);
                    i--;
                }

            foreach (LogicNode n in nodes)
                if (n != node)
                    n.removeInput(node);
        }

        public void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                deleteChosenNodes();
        }
        private void deleteChosenNodes()
        {
            for (int i = 0; i < nodes.Count(); i++)
                if (((VisualNode)nodes[i]).isChosen)
                {
                    removeConnectionsTo(nodes[i]);
                    view.Children.Remove(((VisualNode)nodes[i]).view);
                    nodes.RemoveAt(i);
                    i--;
                }
        }

        private void onGridWheelTurn(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(view);
            if (e.Delta > 0)
            {
                camPos.X += (mousePos.X - mousePos.X / 1.05f) / camScale;
                camPos.Y += (mousePos.Y - mousePos.Y / 1.05f) / camScale;
                camScale *= 1.05f;
            }
            else
            {
                camPos.X += (mousePos.X - mousePos.X * 1.05f) / camScale;
                camPos.Y += (mousePos.Y - mousePos.Y * 1.05f) / camScale;
                camScale /= 1.05f;
            }
            reDraw();
        }
        private void onGridMouseDown(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                isMoving = true;
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource == sender)
                {
                    if (!isShiftPressed())
                        clearChoice();

                    areaGrid = new Grid();
                    areaGrid.Background = CONFIG.BLUE_TRANSPARENT;
                    areaGrid.HorizontalAlignment = HorizontalAlignment.Left;
                    areaGrid.VerticalAlignment = VerticalAlignment.Top;

                    Point mousePos = Mouse.GetPosition(view);
                    areaFrom.X = camPos.X + mousePos.X / camScale;
                    areaFrom.Y = camPos.Y + mousePos.Y / camScale;

                    areaTo.X = areaFrom.X;
                    areaTo.Y = areaFrom.Y;

                    areaGrid.Margin = new Thickness((areaFrom.X - camPos.X) * camScale, (areaFrom.Y - camPos.Y) * camScale, 0, 0);
                    areaGrid.Width = (areaTo.X - areaFrom.X) * camScale;
                    areaGrid.Height = (areaTo.Y - areaFrom.Y) * camScale;

                    Grid.SetZIndex(areaGrid, 5);

                    view.Children.Add(areaGrid);
                }
                return;
            }
        }
        private void onGridMouseUp(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                isMoving = false;
            }
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (areaGrid != null)
                {
                    if ((areaFrom.X - areaTo.X == 0) || (areaFrom.Y - areaTo.Y == 0))
                    {
                        view.Children.Remove(areaGrid);
                        areaGrid = null;
                        return;
                    }
                    foreach (VisualNode node in nodes)
                    {
                        if (!node.isChosen)
                        {
                            double interpolatedX, interpolatedY;
                            interpolatedX = (((VisualNode)node).x - areaFrom.X) / (areaTo.X - areaFrom.X);
                            interpolatedY = (((VisualNode)node).y - areaFrom.Y) / (areaTo.Y - areaFrom.Y);
                            if ((interpolatedX >= 0) && (interpolatedX <= 1) && (interpolatedY >= 0) && (interpolatedY <= 1))
                                node.select();
                        }
                    }

                    view.Children.Remove(areaGrid);
                    areaGrid = null;
                }
            }
        }

        private void onGridMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(view);
            if ((e.LeftButton == MouseButtonState.Pressed) && (relativePositions.Count() > 0))
            {
                Vector mouseVirtualPos = (Vector)(camPos) + (Vector)(mousePos) / camScale;
                for (int i = 0; i < relativePositions.Count(); i++)
                {
                    Point newPos = mouseVirtualPos + relativePositions[i].Value;
                    ((VisualNode)relativePositions[i].Key).x = (float)newPos.X;
                    ((VisualNode)relativePositions[i].Key).y = (float)newPos.Y;
                }
                reDraw();
                prevMousePos = mousePos;
                return;
            }
            if (isMoving)
            {
                camPos -= (mousePos - prevMousePos) / camScale;
                if (areaGrid != null)
                {
                    areaTo.X = camPos.X + mousePos.X / camScale;
                    areaTo.Y = camPos.Y + mousePos.Y / camScale;

                    double width = (areaTo.X - areaFrom.X) * camScale;
                    double height = (areaTo.Y - areaFrom.Y) * camScale;

                    areaGrid.Margin = new Thickness((areaFrom.X - camPos.X) * camScale + (width < 0 ? width : 0), (areaFrom.Y - camPos.Y) * camScale + (height < 0 ? height : 0), 0, 0);
                    areaGrid.Width = Math.Abs(width);
                    areaGrid.Height = Math.Abs(height);
                }
                prevMousePos = mousePos;
                reDraw();
                return;
            }
            if (areaGrid != null)
            {
                areaTo.X = camPos.X + mousePos.X / camScale;
                areaTo.Y = camPos.Y + mousePos.Y / camScale;

                double width = (areaTo.X - areaFrom.X) * camScale;
                double height = (areaTo.Y - areaFrom.Y) * camScale;

                areaGrid.Margin = new Thickness((areaFrom.X - camPos.X) * camScale + (width < 0 ? width : 0), (areaFrom.Y - camPos.Y) * camScale + (height < 0 ? height : 0), 0, 0);
                areaGrid.Width = Math.Abs(width);
                areaGrid.Height = Math.Abs(height);

                prevMousePos = mousePos;
                reDraw();
                return;
            }
            prevMousePos = mousePos;
        }

        private void onNodeLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (((VisualNode)sender).isChosen && !isShiftPressed() && !isAltPressed())
            {
                Point mousePos = Mouse.GetPosition(view);
                foreach (VisualNode node in nodes)
                    if (node.isChosen)
                        relativePositions.Add(new KeyValuePair<LogicNode, Point>(node, new Point(node.x - mousePos.X / camScale - camPos.X, node.y - mousePos.Y / camScale - camPos.Y)));
            }
        }
        private void onNodeLeftMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (relativePositions.Count() > 0)
            {
                relativePositions.Clear();
            }
            if (isAltPressed() && !isShiftPressed())
            {
                foreach (VisualNode node in nodes)
                    if (node.isChosen)
                        if (node != sender)
                            node.addInput((LogicNode)sender);
                return;
            }
            if (!isAltPressed() && !isShiftPressed())
            {
                clearChoice();
                ((VisualNode)sender).select();
                return;
            }
            if (!isAltPressed() && isShiftPressed())
            {
                if (((VisualNode)sender).isChosen)
                    ((VisualNode)sender).deselect();
                else
                    ((VisualNode)sender).select();
                return;
            }
        }

        private void clearChoice()
        {
            foreach (VisualNode node in nodes)
                if (node.isChosen)
                    node.deselect();
        }

        private bool isShiftPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }
        private bool isAltPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }
        //private void onNodeDown_Left(object sender, RoutedEventArgs e)
        //{
        //    for (int i = 0; i < nodes.Count(); i++)
        //        if ((((VisualNode)nodes[i]).view == sender) && (chosenNodes.IndexOf(i) != -1))
        //        {
        //            relativeNodesPositions.Clear();
        //            Point mousePos = Mouse.GetPosition(view);
        //            mousePos.X = mousePos.X / camScale + camPos.X;
        //            mousePos.Y = mousePos.Y / camScale + camPos.Y;
        //            for (int j = 0; j < chosenNodes.Count(); j++)
        //            {
        //                relativeNodesPositions.Add((Point)(new Point(((VisualNode)nodes[chosenNodes[j]]).x, ((VisualNode)nodes[chosenNodes[j]]).y) - mousePos));
        //            }
        //            return;
        //        }
        //}
        //private void onNodeUp_Left(object sender, RoutedEventArgs e)
        //{
        //    for (int i = 0; i < nodes.Count(); i++)
        //        if (((VisualNode)nodes[i]).view == sender)
        //        {
        //            nodeUp(i);
        //            return;
        //        }
        //
        //}
        //private void nodeUp(int index)
        //{
        //    if (relativeNodesPositions.Count() > 0)
        //    {
        //        relativeNodesPositions.Clear();
        //        return;
        //    }
        //
        //    if (((Keyboard.IsKeyDown(Key.LeftAlt)) || Keyboard.IsKeyDown(Key.RightAlt)) && (chosenNodes.Count() > 0))
        //    {
        //        for (int i = 0; i < chosenNodes.Count(); i++)
        //            if (chosenNodes[i] != index)
        //            {
        //                addConnection(index, chosenNodes[i]);
        //            }
        //        return;
        //    }
        //
        //    if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        //    {
        //        clearChosenNodes();
        //    }
        //
        //    int indexInChosen = chosenNodes.IndexOf(index);
        //
        //    if (indexInChosen == -1)
        //    {
        //        selectNode(index);
        //    }
        //    else
        //    {
        //        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        //        {
        //            chosenNodes.Remove(index);
        //            view.Children.Remove(chosenViews[indexInChosen]);
        //            chosenViews.RemoveAt(indexInChosen);
        //        }
        //    }
        //}
        //private void reDrawChosen()
        //{
        //    for (int i = 0; i < chosenNodes.Count(); i++)
        //    {
        //        chosenViews[i].Width = CONFIG.NODES_SIZE * 1.2 * camScale;
        //        chosenViews[i].Height = CONFIG.NODES_SIZE * 1.2 * camScale;
        //        chosenViews[i].StrokeThickness = 4 * camScale;
        //        chosenViews[i].Margin = new Thickness((((VisualNode)nodes[chosenNodes[i]]).x - camPos.X - CONFIG.NODES_SIZE * 0.6) * camScale, (((VisualNode)nodes[chosenNodes[i]]).y - camPos.Y - CONFIG.NODES_SIZE * 0.6) * camScale, 0, 0);
        //    }
        //}
        //
        //private void selectNode(int nodeIndex)
        //{
        //    chosenNodes.Add(nodeIndex);
        //
        //    Ellipse el = new Ellipse();
        //    el.HorizontalAlignment = HorizontalAlignment.Left;
        //    el.VerticalAlignment = VerticalAlignment.Top;
        //    el.Fill = null;
        //    el.Stroke = new SolidColorBrush(Color.FromArgb(255, 150, 150, 255));
        //    el.StrokeThickness = 4 * camScale;
        //    Grid.SetZIndex(el, 4);
        //    view.Children.Add(el);
        //
        //    chosenViews.Add(el);
        //
        //    reDrawChosen();
        //}
        //
        //private void clearChosenNodes()
        //{
        //    chosenNodes.Clear();
        //    foreach (Ellipse e in chosenViews)
        //        view.Children.Remove(e);
        //    chosenViews.Clear();
        //}
    }


    public partial class MainWindow : Window
    {
        private List<Document> documents = new List<Document>();
        private int curDoc = -1;

        private Grid holdingGrid;

        public MainWindow()
        {
            InitializeComponent();
            StreamResourceInfo sriCurs = Application.GetResourceStream(new Uri("/Resources/Cursors/grab.cur", UriKind.Relative));
            CONFIG.GRAB_CURSOR = new Cursor(sriCurs.Stream);
            sriCurs = Application.GetResourceStream(new Uri("/Resources/Cursors/grabbing.cur", UriKind.Relative));
            CONFIG.GRABBING_CURSOR = new Cursor(sriCurs.Stream);

            NodeGrid_AND.Cursor = CONFIG.GRAB_CURSOR;
            NodeGrid_OR.Cursor = CONFIG.GRAB_CURSOR;
            NodeGrid_NOT.Cursor = CONFIG.GRAB_CURSOR;

            WindowState = WindowState.Maximized;
            StartGrid.Visibility = Visibility.Visible;
            ToolsScroll.Visibility = Visibility.Hidden;
        }

        private void Button_New_Click(object sender, RoutedEventArgs e)
        {
            addDocument();
            StartGrid.Visibility = Visibility.Hidden;
            ToolsScroll.Visibility = Visibility.Visible;
        }

        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_AddNodesGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_AddNodesGrid.Height == 0)
            {
                _AddNodesGrid.Height = Double.NaN;
                Button_AddNodesGrid.Content = "↑ Logic Nodes ↑";
            }
            else
            {
                _AddNodesGrid.Height = 0;
                Button_AddNodesGrid.Content = "↓ Logic Nodes ↓";
            }
        }
        private void Button_FlowControl_Click(object sender, RoutedEventArgs e)
        {
            if (_FlowControlGrid.Height == 0)
            {
                _FlowControlGrid.Height = Double.NaN;
                Button_FlowControl.Content = "↑ Flow Control ↑";
            }
            else
            {
                _FlowControlGrid.Height = 0;
                Button_FlowControl.Content = "↓ Flow Control ↓";
            }
        }
        private void Button_DocumentsControl_Click(object sender, RoutedEventArgs e)
        {
            if (_DocumentsControlGrid.Height == 0)
            {
                _DocumentsControlGrid.Height = Double.NaN;
                Button_DocumentsControl.Content = "↑ Documents Control ↑";
            }
            else
            {
                _DocumentsControlGrid.Height = 0;
                Button_DocumentsControl.Content = "↓ Documents Control ↓";
            }
        }

        private void NodeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (holdingGrid == null)
            {
                Grid grid = new Grid();
                grid.Height = 90;
                grid.Width = 90;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                Ellipse el = new Ellipse();
                el.Width = 86;
                el.Height = 86;
                el.HorizontalAlignment = HorizontalAlignment.Left;
                el.VerticalAlignment = VerticalAlignment.Top;
                el.Margin = new Thickness(2, 2, 2, 2);
                el.Fill = new SolidColorBrush(Color.FromArgb(240, 100, 100, 100));
                el.Cursor = CONFIG.GRABBING_CURSOR;
                grid.Children.Add(el);

                TextBlock text = new TextBlock();
                text.HorizontalAlignment = HorizontalAlignment.Center;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Width = double.NaN;
                text.Height = double.NaN;
                text.FontWeight = FontWeights.Bold;
                text.Visibility = Visibility.Visible;
                text.FontSize = 20;
                text.Text = ((TextBlock)((Grid)sender).Children[1]).Text;
                text.Cursor = CONFIG.GRABBING_CURSOR;
                grid.Children.Add(text);

                WindowGrid.Children.Add(grid);
                Grid.SetZIndex(grid, 10);
                holdingGrid = grid;

                Point p = e.GetPosition(WindowGrid);
                holdingGrid.Margin = new Thickness(p.X - 45, p.Y - 45, 0, 0);
            }
        }

        private void addDocument()
        {
            documents.Add(new Document(MainGrid));

            StackPanel docPanel = new StackPanel();
            docPanel.HorizontalAlignment = HorizontalAlignment.Left;
            docPanel.VerticalAlignment = VerticalAlignment.Top;
            docPanel.Height = 25;
            docPanel.Width = Double.NaN;
            docPanel.Margin = new Thickness(0, 0, 5, 0);
            docPanel.Orientation = Orientation.Horizontal;
            docPanel.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            docPanel.MouseLeftButtonUp += onDocumentClick;

            TextBlock name = new TextBlock();
            name.Text = "untitled";
            name.FontSize = 20;
            name.HorizontalAlignment = HorizontalAlignment.Left;
            name.VerticalAlignment = VerticalAlignment.Top;
            name.Margin = new Thickness(0, 0, 5, 0);
            name.Height = 25;
            name.Width = Double.NaN;
            name.Foreground = CONFIG.BLACK;
            docPanel.Children.Add(name);

            Button closeDoc = new Button();
            closeDoc.HorizontalAlignment = HorizontalAlignment.Left;
            closeDoc.VerticalAlignment = VerticalAlignment.Top;
            closeDoc.Height = 25;
            closeDoc.Width = 25;
            closeDoc.Background = new ImageBrush(new BitmapImage(new Uri("Resources/Icons/close.png", UriKind.Relative)));
            closeDoc.BorderBrush = null;
            closeDoc.Click += onCloseDocumentButtonClick;
            docPanel.Children.Add(closeDoc);

            DocumentsStack.Children.Add(docPanel);

            switchDocument(documents.Count() - 1);
        }

        private void onDocumentClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DocumentsStack.Children.Count; i++)
                if (sender == DocumentsStack.Children[i])
                {
                    switchDocument(i);
                    return;
                }
        }
        private void onCloseDocumentButtonClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < documents.Count(); i++)
                if (((StackPanel)DocumentsStack.Children[i]).Children[1] == sender)
                    closeDocument(i);
        }
        private void closeDocument(int index)
        {
            // TODO: ask for save, if needed
            if (index == curDoc)
                if (index == documents.Count() - 1)
                {
                    if (index == 0)
                    {
                        ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/stop.png", UriKind.Relative));
                        StartGrid.Visibility = Visibility.Visible;
                        ToolsScroll.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        switchDocument(index - 1);
                    }
                    documents[index].removeFrom(MainGrid);
                    documents.RemoveAt(index);
                    DocumentsStack.Children.RemoveAt(index);
                }
                else
                {
                    documents[index].removeFrom(MainGrid);
                    documents.RemoveAt(index);
                    DocumentsStack.Children.RemoveAt(index);

                    switchDocument(index);
                }
            else
            {
                documents[index].removeFrom(MainGrid);
                documents.RemoveAt(index);
                DocumentsStack.Children.RemoveAt(index);
                if (index < curDoc)
                    curDoc--;
            }
        }

        private void switchDocument(int index)
        {
            if (curDoc != -1)
            {
                if (documents[curDoc].getIsUpdating())
                {
                    documents[curDoc].stopUpdating();
                    ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
                }
                documents[curDoc].hide();
            }
            documents[index].show();
            curDoc = index;

            for (int i = 0; i < documents.Count(); i++)
            {
                if (i != curDoc)
                    ((StackPanel)DocumentsStack.Children[i]).Background = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
                else
                    ((StackPanel)DocumentsStack.Children[i]).Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            }
        }

        private void WindowGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (holdingGrid != null)
            {
                Point p = e.GetPosition(WindowGrid);
                holdingGrid.Margin = new Thickness(p.X - 45, p.Y - 45, 0, 0);
            }
        }

        private void WindowGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (holdingGrid != null)
            {
                Point mousePos = Mouse.GetPosition(ViewGrid_default);
                bool isMouseOverViewGrid = (mousePos.X >= 0) && (mousePos.Y >= 0);
                if ((!documents[curDoc].getIsUpdating()) && (isMouseOverViewGrid))
                {
                    WindowGrid.Children.Remove(holdingGrid);
                    Point camPos = documents[curDoc].getCamPos();
                    documents[curDoc].addNode(camPos.X + mousePos.X / documents[curDoc].getCamScale(), camPos.Y + mousePos.Y / documents[curDoc].getCamScale(), ((TextBlock)holdingGrid.Children[1]).Text);
                    holdingGrid = null;
                }
                else
                {
                    WindowGrid.Children.Remove(holdingGrid);
                    holdingGrid = null;
                }
            }
        }

        private void Button_ShowAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            if (AdvancedSettingsGrid.Height == 0)
                AdvancedSettingsGrid.Height = Double.NaN;
            else
                AdvancedSettingsGrid.Height = 0;
        }

        private void Button_StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (documents[curDoc].getIsUpdating())
            {
                documents[curDoc].stopUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
            }
            else
            {
                documents[curDoc].startUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/stop.png", UriKind.Relative));
            }
        }

        private void Button_SimulateFrame_Click(object sender, RoutedEventArgs e)
        {
            if (documents[curDoc].getIsUpdating())
            {
                documents[curDoc].stopUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
            }
            else
                documents[curDoc].updateTick();
        }

        private void Button_NewFile_Click(object sender, RoutedEventArgs e)
        {
            addDocument();
        }

        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_SavAs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TheWindow_KeyDown(object sender, KeyEventArgs e)
        {
            documents[curDoc].onKeyDown(sender, e);
        }
    }
}
