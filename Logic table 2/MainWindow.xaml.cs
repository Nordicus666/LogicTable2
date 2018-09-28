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
        public static Cursor GRAB_CURSOR;
        public static Cursor GRABBING_CURSOR;
        public static SolidColorBrush BLACK = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush WHITE = new SolidColorBrush(Colors.White);
    }
    public abstract class LogicNode
    {
        private List<LogicNode> inputs = new List<LogicNode>();
        protected bool state = false;
        protected bool nextState = false;
        protected int maxInputs = -1;

        public bool addInput(LogicNode input)
        {
            if (inputs.Count() >= maxInputs)
                return false;
            inputs.Add(input);
            return true;
        }
        public bool removeInput(LogicNode input)
        {
            return inputs.Remove(input);
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
    public class LogicNode_And : LogicNode
    {
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
    public class LogicNode_Or : LogicNode
    {
        protected override bool func(bool[] inputs)
        {
            foreach (bool a in inputs)
                if (a)
                    return true;
            return false;
        }
    }
    public class LogicNode_Not : LogicNode
    {
        public LogicNode_Not()
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
    public class Node
    {
        public Grid view = new Grid();
        private Ellipse el = new Ellipse();
        private TextBlock func = new TextBlock();
        private LogicNode bind;
        private float x, y;

        public Node(float x, float y, string text, LogicNode bind)
        {
            this.x = x;
            this.y = y;
            func.Text = text;
            this.bind = bind;
            construct();
        }
        private void construct()
        {
            view.HorizontalAlignment = HorizontalAlignment.Left;
            view.VerticalAlignment = VerticalAlignment.Top;

            el.HorizontalAlignment = HorizontalAlignment.Stretch;
            el.VerticalAlignment = VerticalAlignment.Stretch;
            el.Width = Double.NaN;
            el.Height = Double.NaN;
            el.Visibility = Visibility.Visible;
            view.Children.Add(el);
            
            func.HorizontalAlignment = HorizontalAlignment.Center;
            func.VerticalAlignment = VerticalAlignment.Center;
            func.Width = double.NaN;
            func.Height = double.NaN;
            func.FontWeight = FontWeights.Bold;
            func.Visibility = Visibility.Visible;
            view.Children.Add(func);
        }
        public void redraw(float camx, float camy, float scale)
        {
            view.Dispatcher.Invoke(delegate
            {
                view.Height = CONFIG.NODES_SIZE * scale;
                view.Width = CONFIG.NODES_SIZE * scale;
                func.FontSize = view.Height / 5.0;
                view.Margin = new Thickness((x - camx - CONFIG.NODES_SIZE / 2.0) * scale, (y - camy - CONFIG.NODES_SIZE / 2.0) * scale, 0, 0);
                el.Fill = bind.get() ? CONFIG.WHITE : CONFIG.BLACK;
                func.Foreground = bind.get() ? CONFIG.BLACK : CONFIG.WHITE;
            });
        }
    }
    public class Connection
    {
        public Node from, to;

        public Connection(Node from, Node to)
        {
            this.from = from;
            this.to = to;
        }
        public void redraw(float camx, float camy, float scale)
        {

        }
    }
    public class Document
    {
        public string name = "untitled";
        private List<Node> nodes = new List<Node>();
        private List<Connection> connections = new List<Connection>();
        private Point camPos = new Point();
        private float camScale = 1.0f;
        private Grid view;
        private bool isUpdating = false;
        private bool needStopUpdating = false;
        private List<LogicNode> table = new List<LogicNode>();

        public Document(Grid grid)
        {
            view = grid;
        }
        public Document(string name, Grid grid)
        {
            view = grid;
            this.name = name;
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
            needStopUpdating = true;
        }
        private void recountNodes()
        {
            foreach (LogicNode node in table)
                node.recount();
        }
        private void updateNodesStates()
        {
            foreach (LogicNode node in table)
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
            switch(func)
            {
                case "AND":
                    node = new LogicNode_And();
                    break;
                case "OR":
                    node = new LogicNode_Or();
                    break;
                case "NOT":
                    node = new LogicNode_Not();
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
            table.Add(node);

            Node nodeView = new Node((float)x, (float)y, func, node);
            nodeView.redraw((float)camPos.X, (float)camPos.Y, camScale);
            nodes.Add(nodeView);
            view.Children.Add(nodeView.view);
            nodeView.view.MouseLeftButtonUp += onNodeClick_Left;
            nodeView.view.MouseRightButtonUp += onNodeClick_Right;
        }
        public void removeNode(LogicNode node)
        {
            table.Remove(node);
        }
        private void reDraw()
        {
            foreach (Node node in nodes)
                node.redraw((float)camPos.X, (float)camPos.Y, camScale);
            foreach (Connection conn in connections)
                conn.redraw((float)camPos.X, (float)camPos.Y, camScale);
        }

        private void onNodeClick_Left(object sender, RoutedEventArgs e)
        {

        }
        private void onNodeClick_Right(object sender, RoutedEventArgs e)
        {

        }
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
            documents.Add(new Document(ViewGrid));
            curDoc = documents.Count() - 1;
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
                Point mousePos = Mouse.GetPosition(ViewGrid);
                bool isMouseOverViewGrid = (mousePos.X >= 0) && (mousePos.Y >= 0);
                if ((!documents[curDoc].getIsUpdating()) && (isMouseOverViewGrid))
                {
                    WindowGrid.Children.Remove(holdingGrid);
                    Point camPos = documents[curDoc].getCamPos();
                    documents[curDoc].addNode(camPos.X + mousePos.X * documents[curDoc].getCamScale(), camPos.Y + mousePos.Y * documents[curDoc].getCamScale(), ((TextBlock)holdingGrid.Children[1]).Text);
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
    }
}
