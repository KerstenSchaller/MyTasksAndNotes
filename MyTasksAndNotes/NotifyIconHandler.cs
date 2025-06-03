using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Resources;
using MyTasksAndNotes.Properties;
using MyTasksAndNotes;

public class NotifyIconHandler
{
    private NotifyIcon _notifyIcon;
    private bool _isExit;
    Window mainWindow;

    public NotifyIconHandler(Window _mainWindow)
    {

        mainWindow = _mainWindow;

        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = Resources.MyIcon;
        
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "MyTasksAndNotes";

        HotkeyManager.getInstance().subscribeHotkey(ShowMainWindow, HotKeyIds.MENU_UP);



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
}
