using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Resources;
using MyTasksAndNotes.Properties;
using MyTasksAndNotes;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

public class NotifyIconHandler
{
    private NotifyIcon _notifyIcon;
    private bool _isExit;
    Window mainWindow;
    HoveringMenu hoveringMenu;

    public NotifyIconHandler(Window _mainWindow)
    {

        mainWindow = _mainWindow;
        

        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = Resources.MyIcon;
        
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "MyTasksAndNotes";

        HotkeyManager.getInstance().subscribeHotkey(ShowHoveringMenu, HotKeyIds.MENU_UP);



        // Restore on click
        _notifyIcon.MouseClick += NotifyIcon_MouseClick;
    }

    private void addContextMenu() 
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add("Exit", null, (s, e) => {
            _isExit = true;
            mainWindow.Close();
        });
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowMainWindow();
        }
    }

    private void ShowMainWindow()
    {

        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    bool hoveringMenuActive = false;
    private void ShowHoveringMenu()
    {
        // just execute when menu is not already active
        if (hoveringMenuActive == false)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {

                hoveringMenu = null;
                hoveringMenu = new HoveringMenu(mainWindow);
                hoveringMenu.Focus();
                Keyboard.Focus(hoveringMenu);

                hoveringMenuActive = true;
                hoveringMenu.Show();
                hoveringMenu.WindowState = WindowState.Normal;
                hoveringMenu.Activate();
                hoveringMenu.Closing += (sender, e) =>
                {
                    hoveringMenuActive = false;
                };
            }));


        }

    }
}
