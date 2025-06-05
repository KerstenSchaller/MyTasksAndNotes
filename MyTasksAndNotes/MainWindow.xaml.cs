using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System;
using System.ComponentModel;

namespace MyTasksAndNotes
{
    public partial class MainWindow : Window
    {
        private DragAdorner _dragAdorner;
        private AdornerLayer _adornerLayer;
        private Window _topWindow;


        

        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += MainWindow_StateChanged;
            WindowState = WindowState.Minimized;
            populate();
            Show();
            Activate();
            WindowState = WindowState.Minimized;
            Hide();



            HotkeyManager hotkeyManager = new HotkeyManager(this);

            NotifyIconHandler notifyIconHandler = new NotifyIconHandler(this);

            OptionsWindow optionsWindow = new OptionsWindow();
            optionsWindow.Show();
            

            this.Closing += MainWindow_Closing;
        }

        void populate() 
        {
            clearCards(ToDoPanel);
            clearCards(DonePanel);
            var tasks = NoteContainer.Instance.getTasks();
            foreach (var task in tasks) 
            {
                switch (task.taskState) 
                {
                    case TaskState.Todo:
                        AddCard(ToDoPanel, task.name);
                        break;
                    case TaskState.Done:
                        AddCard(DonePanel, task.name);
                        break;
                }
                
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

        private void AddCard(StackPanel column, string text)
        {
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


            column.Children.Add(card);
        }

        void clearCards(StackPanel column) 
        {
            column.Children.Clear();
        }



        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            if (_dragAdorner != null)
            {
                var pos = e.GetPosition(RootGrid);
                _dragAdorner.SetPosition(pos.X + 5, pos.Y + 5);


            }
        }

        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Button btn && _dragAdorner == null)
            {
                _topWindow = Window.GetWindow(this);
                _adornerLayer = AdornerLayer.GetAdornerLayer(RootGrid); // Make sure RootGrid is the main container

                if (_adornerLayer == null)
                    return;

                // Clone visual for drag preview
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

                // textblock copy logic because assignment of content would change parent of content (unexpected behaviour)
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
                    floatCopy.Content = btn.Content; // fallback
                }

                _dragAdorner = new DragAdorner(RootGrid, floatCopy);
                _adornerLayer.Add(_dragAdorner);
                btn.Opacity = 0.5;


                // Set initial position
                var initialPos = e.GetPosition(RootGrid);
                _dragAdorner.SetPosition(initialPos.X + 5, initialPos.Y + 5);


                // Begin drag with async dispatcher to allow adorner to render first
                Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        DragDrop.DoDragDrop(btn, btn, DragDropEffects.Move);
                    }
                    finally
                    {
                        if (_dragAdorner != null )
                        {
                            _adornerLayer.Remove(_dragAdorner);
                            _dragAdorner = null;
                        }

                        if (_topWindow != null )
                        {
                            _topWindow = null;
                        }
                    }
                });
            }
        }




        private void Column_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
                e.Effects = DragDropEffects.Move;
        }

        private void Column_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)) && sender is StackPanel panel)
            {
                var card = e.Data.GetData(typeof(Button)) as Button;
                var parent = card.Parent as Panel;
                parent?.Children.Remove(card);
                panel.Children.Add(card);
                card.Opacity = 1;
            }
        }
    }
}