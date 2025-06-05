using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MyTasksAndNotes
{

    struct MenuItem
    {
        public Point relativePos;
        public string text = "";
        public Action callback;
        public MenuItem(Point _relativePos ,string _text, Action _callback) 
        {
            callback = _callback;
            text = _text;
            relativePos = _relativePos;
        }
    }

    /// <summary>
    /// Interaktionslogik für HoveringMenu.xaml
    /// </summary>
    public partial class HoveringMenu : Window
    {
        private int gridSize = 5;
        private int centerIndex;
        private List<string> cellData;

        Dictionary<Point, MenuItem> menuItems = new Dictionary<Point, MenuItem>();

        public HoveringMenu()
        {
            InitializeComponent();
            this.Activate();
            this.Focusable = true;
            this.Focus(); // Ensure the window has keyboard focus
            Keyboard.Focus(this);

            InitializeCellData();

            HotkeyManager.getInstance().subscribeHotkey(MoveUp, HotKeyIds.MENU_UP);
            HotkeyManager.getInstance().subscribeHotkey(MoveDown, HotKeyIds.MENU_DOWN);
            HotkeyManager.getInstance().subscribeHotkey(MoveLeft, HotKeyIds.MENU_LEFT);
            HotkeyManager.getInstance().subscribeHotkey(MoveRight, HotKeyIds.MENU_RIGHT);

            setupMenu();
            RenderGrid();
        }

        void setupMenu() 
        {
            Point Center = new Point(2, 2);
            Point Up = new Point(0, -1);
            Point Down = new Point(0, 1);
            Point Left = new Point(-1, 0);
            Point Right = new Point(1, 0);

            var mainMenuItems = menuItems;

            var mainItem = new MenuItem(Center,"MyTasksAndNotes", null);
            var newTaskItem = new MenuItem(Up,"New Task", null); // Up
            var editTaskItem = new MenuItem(Down,"Edit last task", null); // Down
            var newNoteItem = new MenuItem(Left,"New Note", null); // Left 
            var editNoteItem = new MenuItem(Right,"Edit last Note", null); // Right

            menuItems.Add(Center, mainItem);
            addMenuRelativeItems(mainItem, newTaskItem);
            addMenuRelativeItems(mainItem, editTaskItem);
            addMenuRelativeItems(mainItem, newNoteItem);
            addMenuRelativeItems(mainItem, editNoteItem);
        }

        void addMenuRelativeItems(MenuItem root, MenuItem child) 
        {
            menuItems.Add(new Point(root.relativePos.X + child.relativePos.X, root.relativePos.Y + child.relativePos.Y), child);
        }

        private void InitializeCellData()
        {
            // Initialize all cells with "Item i"
            cellData = new List<string>();
            for (int i = 0; i < gridSize * gridSize; i++)
            {
                cellData.Add($"Item {i + 1}");
            }
        }

        private void RenderGrid()
        {
            RadialGrid.Children.Clear();
            Point centerIndex = new Point((int)(gridSize / 2), (int)(gridSize / 2));
            StackPanel panel = new StackPanel();
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    var key = new Point(x, y);
                    if (menuItems.ContainsKey(key)) 
                    {
                        var item = menuItems[key];
                    
                        if (new Point(x,y) == centerIndex)
                        {
                            panel = new StackPanel
                            {
                                Background = Brushes.Orange,
                                Margin = new Thickness(5),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Width = 100,
                                Height = 100
                            };
                        }
                        else
                        {
                            panel = new StackPanel
                            {
                                Visibility = (item.text == "") ? Visibility.Hidden : Visibility.Visible,
                                Background = (item.text != "") ? Brushes.Gray : Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(5),
                                Width = 100,
                                Height = 100
                            };
                        }


                        TextBlock textBlock = new TextBlock
                        {
                            Text = item.text,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        };
                        panel.Children.Add(textBlock);
                    }
                    else 
                    {
                        panel = new StackPanel
                        {
                            Visibility = Visibility.Hidden,
                            Margin = new Thickness(5),
                            Width = 100,
                            Height = 100
                        };
                    }

                    RadialGrid.Children.Add(panel);
                }
            }
        }

        // Moves all elements in the grid one cell to the right (visual shift left)
        public void MoveLeft()
        {
            ShiftCells(dx: 1, dy: 0);
        }

        // Moves all elements one cell to the left (visual shift right)
        public void MoveRight()
        {
            ShiftCells(dx: -1, dy: 0);
        }

        // Moves all elements one cell down (visual shift up)
        public void MoveUp()
        {
            ShiftCells(dx: 0, dy: 1);
        }

        // Moves all elements one cell up (visual shift down)
        public void MoveDown()
        {
            ShiftCells(dx: 0, dy: -1);
        }


        Point currentShift = new Point();
        private void ShiftCells(int dx, int dy)
        {
            if (Math.Abs(currentShift.X + dx) > 1 || Math.Abs(currentShift.Y + dy) > 1) 
            {
                // Dont shift further than allowed
                return;
            }
            currentShift = new Point(currentShift.X + dx, currentShift.Y + dy);

            var newMenuItems = new Dictionary<Point, MenuItem>();

            foreach (var kvp in menuItems)
            {
                var oldPoint = kvp.Key;
                var item = kvp.Value;

                int newX = ((int)oldPoint.X + dx + gridSize) % gridSize;
                int newY = ((int)oldPoint.Y + dy + gridSize) % gridSize;

                var newPoint = new Point(newX, newY);
                newMenuItems[newPoint] = item;
            }

            menuItems = newMenuItems;

            RenderGrid();
        }



        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // close window on menu key release (ctrl + alt)
            if (e.Key == Key.System && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                // if ctrl + alt was pressed, and ctrl is released first
                this.Close();
            }
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                // if ctrl + alt was pressed, and alt is released first
                this.Close();
            }
        }
  
    }
}
