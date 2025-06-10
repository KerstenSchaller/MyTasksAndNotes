using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTasksAndNotes
{
    public partial class MainWindow : Window
    {
        private DragAdorner _dragAdorner;
        private AdornerLayer _adornerLayer;
        private Window _topWindow;
        Dictionary<Button, Note> buttonTasksDictionary = new Dictionary<Button, Note>();




        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += MainWindow_StateChanged;
            WindowState = WindowState.Minimized; // dont remove, otherwise hotkeys will brake (line causes window to be fully setUp before its hidden)
            populate();
            Show();
            Activate();
            this.Hide();



            HotkeyManager hotkeyManager = new HotkeyManager(this);
            NotifyIconHandler notifyIconHandler = new NotifyIconHandler(this);

            this.Closing += MainWindow_Closing;
        }

        void populate() 
        {
            clearCards(ToDoPanel);
            clearCards(DonePanel);
            NotesGrid.Children.Clear();
            buttonTasksDictionary.Clear();

            var tasks = NoteContainer.Instance.getTasks();
            foreach (var task in tasks) 
            {
                Button addedCard = new Button();
                switch (task.taskState) 
                {
                    
                    case TaskState.Todo:
                        addedCard = AddCard(ToDoPanel, task.name);
                        break;
                    case TaskState.Done:
                        addedCard = AddCard(DonePanel, task.name);
                        break;
                }

                buttonTasksDictionary.Add(addedCard, task);
                
            }

            var notes = NoteContainer.Instance.getNotes();
            foreach (var note in notes)
            {
                var noteBtn = AddNote(note.name);
                buttonTasksDictionary.Add(noteBtn, note);

            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Minimize the window instead
            this.WindowState = WindowState.Minimized;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                Hide();
            }
            if (this.WindowState == WindowState.Maximized || this.WindowState == WindowState.Normal)
            {
                populate();
            }

            
        }

        private Button AddCard(StackPanel column, string text)
        {
            // create and add card
            var card = new Button
            {
                Content = text,
                Margin = new Thickness(5),
                Tag = text,
                VerticalContentAlignment = VerticalAlignment.Stretch 
            };
            
            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            
            card.Content = textBlock;
            
            card.PreviewMouseMove += Card_PreviewMouseMove;
            card.MouseDoubleClick += Card_MouseDoubleClick;
            card.PreviewMouseLeftButtonDown += Card_PreviewMouseLeftButtonDown;

            column.Children.Add(card);
            return card;
        }

        private void Card_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // open task in editor
            var card = sender as Button;
            var task = buttonTasksDictionary[card];
            RichTextEditor.TaskViewWindow taskViewWindow = new RichTextEditor.TaskViewWindow(task);
            taskViewWindow.Show();
            taskViewWindow.Closing += TaskViewWindow_Closing;

        }

        private void TaskViewWindow_Closing(object? sender, CancelEventArgs e)
        {
            this.populate();
        }

        void clearCards(StackPanel column)
        {
            // Remove all children starting from index 1 (keep the first one, usually the header)
            while (column.Children.Count > 1)
            {
                column.Children.RemoveAt(1);
            }
        }



        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            if (_dragAdorner != null)
            {
                var pos = e.GetPosition(RootGrid);
                _dragAdorner.SetPosition(pos.X + 5, pos.Y + 5);


            }
        }


        // extented logic to prevent dragAndDrop if actually a double click is intented
        private Point _mouseDownPosition;
        private DateTime _lastMouseDownTime;
        private const double DragThreshold = 5; // in pixels
        private const int DoubleClickTimeThreshold = 300; // in milliseconds

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPosition = e.GetPosition(null);
            _lastMouseDownTime = DateTime.Now;
        }
        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                sender is Button btn &&
                _dragAdorner == null)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = currentPosition - _mouseDownPosition;

                // Check if movement exceeds drag threshold
                if (Math.Abs(diff.X) < DragThreshold && Math.Abs(diff.Y) < DragThreshold)
                    return;

                // Check time since last click to avoid double-click conflict
                var elapsed = (DateTime.Now - _lastMouseDownTime).TotalMilliseconds;
                if (elapsed < DoubleClickTimeThreshold)
                    return;

                _topWindow = Window.GetWindow(this);
                _adornerLayer = AdornerLayer.GetAdornerLayer(RootGrid); // Make sure RootGrid is the main container

                if (_adornerLayer == null)
                    return;

                var floatCopy = new Button
                {
                    Width = btn.ActualWidth,
                    Height = btn.ActualHeight,
                    Opacity = 0.75,
                    Background = btn.Background,
                    BorderBrush = btn.BorderBrush,
                    FontSize = btn.FontSize,
                    Padding = btn.Padding,
                    Margin = new Thickness(0)
                };

                if (btn.Content is TextBlock original)
                {
                    floatCopy.Content = new TextBlock
                    {
                        Text = original.Text,
                        TextWrapping = original.TextWrapping,
                        TextAlignment = original.TextAlignment,
                        FontSize = original.FontSize,
                        FontFamily = original.FontFamily,
                        FontWeight = original.FontWeight,
                        Foreground = original.Foreground,
                        Background = original.Background,
                        Padding = original.Padding,
                        Margin = original.Margin,
                        HorizontalAlignment = original.HorizontalAlignment,
                        VerticalAlignment = original.VerticalAlignment,
                        TextTrimming = original.TextTrimming
                    };
                }
                else
                {
                    floatCopy.Content = btn.Content;
                }

                _dragAdorner = new DragAdorner(RootGrid, floatCopy);
                _adornerLayer.Add(_dragAdorner);
                btn.Opacity = 0.5;

                var initialPos = e.GetPosition(RootGrid);
                _dragAdorner.SetPosition(initialPos.X + 5, initialPos.Y + 5);

                Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        DragDrop.DoDragDrop(btn, btn, DragDropEffects.Move);
                    }
                    finally
                    {
                        if (_dragAdorner != null)
                        {
                            _adornerLayer.Remove(_dragAdorner);
                            _dragAdorner = null;
                        }

                        if (_topWindow != null)
                        {
                            _topWindow = null;
                        }
                    }
                });
            }
        }



        // drag and drop 
        private void Column_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
                e.Effects = DragDropEffects.Move;
        }


        // drag and drop inkl task handling
        private void Column_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)) && sender is StackPanel panel)
            {
                var card = e.Data.GetData(typeof(Button)) as Button;
                var parent = card.Parent as Panel;
                parent?.Children.Remove(card);
                panel.Children.Add(card);
                if (panel == DonePanel) buttonTasksDictionary[card].setTaskDone();
                card.Opacity = 1;
            }
        }

        // Notes handling
        private const int MaxItems = 100; // Set your desired max
        private const int Columns = 3;    // Match your XAML definition

        private Button AddNote(string text)
        {
            if (NotesGrid.Children.Count >= MaxItems)
            {
                throw new Exception();
            }

            int currentButtons = NotesGrid.Children.Count;
            int row = currentButtons / Columns;
            int col = currentButtons % Columns;

            // Add new row if needed
            if (NotesGrid.RowDefinitions.Count <= row)
            {
                NotesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Create a TextBlock with wrapping for the button content
            TextBlock wrappedText = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,

            };
            wrappedText.LineHeight = 16;
            wrappedText.MaxHeight = wrappedText.LineHeight * App.GlobalOptions.NotePreviewMaxLines;

            // Create and add button
            Button btn = new Button
            {
                Content = wrappedText,
                Margin = new Thickness(5)
            };

            btn.MouseDoubleClick += Card_MouseDoubleClick;

            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);

            NotesGrid.Children.Add(btn);
            return btn;
        }
    }
}