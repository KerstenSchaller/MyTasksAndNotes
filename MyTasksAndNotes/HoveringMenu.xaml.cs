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
        private List<string> cellData;

        Dictionary<Point, MenuItem> menuItems = new Dictionary<Point, MenuItem>();
        MenuItem currentMenuItem;
        Window mainWindow;

        Point centerIndex;

        static Note lastEditedNote = null;
        static Note lastEditedTask = null;

        public HoveringMenu(Window _mainWindow)
        {
            InitializeComponent();
            centerIndex = new Point((int)(gridSize / 2), (int)(gridSize / 2));


            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Left = 0;
            Top = 0;

            Grid.Width = Width;
            Grid.Height = Height;

            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;

            mainWindow = _mainWindow;
            this.Activate();
            this.Focusable = true;
            this.Focus(); // Ensure the window has keyboard focus
            Keyboard.Focus(this);

            InitializeCellData();

            //register key combination callbacks
            HotkeyManager.getInstance().subscribeHotkey(MoveUp, HotKeyIds.MENU_UP);
            HotkeyManager.getInstance().subscribeHotkey(MoveDown, HotKeyIds.MENU_DOWN);
            HotkeyManager.getInstance().subscribeHotkey(MoveLeft, HotKeyIds.MENU_LEFT);
            HotkeyManager.getInstance().subscribeHotkey(MoveRight, HotKeyIds.MENU_RIGHT);
            HotkeyManager.getInstance().subscribeHotkey(MenuEnter, HotKeyIds.MENU_ENTER);

            Closing += HoveringMenu_Closing;


            setupMenu();
            RenderGrid();
        }

        private void HoveringMenu_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            //register key combination callbacks
            HotkeyManager.getInstance().unsubscribeHotkey(MoveUp, HotKeyIds.MENU_UP);
            HotkeyManager.getInstance().unsubscribeHotkey(MoveDown, HotKeyIds.MENU_DOWN);
            HotkeyManager.getInstance().unsubscribeHotkey(MoveLeft, HotKeyIds.MENU_LEFT);
            HotkeyManager.getInstance().unsubscribeHotkey(MoveRight, HotKeyIds.MENU_RIGHT);
            HotkeyManager.getInstance().unsubscribeHotkey(MenuEnter, HotKeyIds.MENU_ENTER);
        }

        void MenuEnter() 
        {
            if(currentMenuItem.callback != null) 
            {
                currentMenuItem.callback();
                this.Close();
            }
        }

        void setupMenu() 
        {
            Point Center = new Point(2, 2);
            Point Up = new Point(0, -1);
            Point Down = new Point(0, 1);
            Point Left = new Point(-1, 0);
            Point Right = new Point(1, 0);

            var mainMenuItems = menuItems;

            var mainItem = new MenuItem(Center,"MyTasksAndNotes", showMainWindow);
            currentMenuItem = mainItem;
            var newTaskItem = new MenuItem(Up,"New Task", addTask); // Up
            var editTaskItem = new MenuItem(Down,"Edit last task", editLastTask); // Down
            var newNoteItem = new MenuItem(Left,"New Note", addNote); // Left 
            var editNoteItem = new MenuItem(Right,"Edit last Note", editLastNote); // Right

            menuItems.Add(Center, mainItem);
            addMenuRelativeItems(mainItem, newTaskItem);
            addMenuRelativeItems(mainItem, editTaskItem);
            addMenuRelativeItems(mainItem, newNoteItem);
            addMenuRelativeItems(mainItem, editNoteItem);
        }

        void editLastNote() 
        {
            if (lastEditedNote == null) lastEditedNote = NoteContainer.Instance.getLastNote();
            RichTextEditor.TaskViewWindow taskViewWindowController = new RichTextEditor.TaskViewWindow(lastEditedNote);
            taskViewWindowController.Show();
            taskViewWindowController.Activate();
            Close();
        }


        void addTask() 
        {
            var newTask = NoteContainer.Instance.addNewTask();
            lastEditedTask = newTask;
            RichTextEditor.TaskViewWindow taskViewWindowController = new RichTextEditor.TaskViewWindow(newTask);
            taskViewWindowController.Show();
            taskViewWindowController.Activate();
            Close();
        }

        void editLastTask()
        {
            if (lastEditedTask == null) lastEditedTask = NoteContainer.Instance.getLastTask();
            RichTextEditor.TaskViewWindow taskViewWindowController = new RichTextEditor.TaskViewWindow(lastEditedTask);
            taskViewWindowController.Show();
            taskViewWindowController.Activate();
            Close();
        }


        void addNote()
        {
            var newNote = NoteContainer.Instance.addNewNote();
            lastEditedNote = newNote;
            RichTextEditor.TaskViewWindow taskViewWindowController = new RichTextEditor.TaskViewWindow(newNote);
            taskViewWindowController.Show();
            taskViewWindowController.Activate();
            Close();
        }

        void showMainWindow() 
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
            Close();
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
            // Clear existing grid elements before rendering the new grid
            RadialGrid.Children.Clear();

            // Loop through each cell in the grid based on gridSize
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    Point key = new Point(x, y);

                    // Create the appropriate panel for the current grid position
                    StackPanel panel = CreatePanelForPosition(key);

                    // Add the panel to the UI grid container
                    RadialGrid.Children.Add(panel);
                }
            }
        }

        private StackPanel CreatePanelForPosition(Point key)
        {
            // Create a new StackPanel with shared default layout properties
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(5),
                Width = 100,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Check if a menu item exists at the current grid position
            if (menuItems.TryGetValue(key, out var item))
            {
                // If this is the center item, highlight it and set it as the current menu item
                if (key == centerIndex)
                {
                    currentMenuItem = item;
                    panel.Background = Brushes.Orange;
                }
                else
                {
                    // Set visibility and background based on whether the item has text
                    panel.Visibility = string.IsNullOrEmpty(item.text) ? Visibility.Hidden : Visibility.Visible;
                    panel.Background = string.IsNullOrEmpty(item.text) ? Brushes.White : Brushes.Gray;
                }

                // Add a TextBlock to display the item's text
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
                // No item at this position — hide the panel
                panel.Visibility = Visibility.Hidden;
            }

            return panel;
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
            if (newMenuItems.ContainsKey(centerIndex) == false) 
            {
                //shift not allowed, center piece does not contain a valid item
                return;
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
