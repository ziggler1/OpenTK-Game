﻿using System;
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
using System.Data;
using System.Drawing;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Threading;
using Game;
using System.Diagnostics;
using OpenTK;
using OpenTK.Input;
using System.IO;
using System.Reflection;
using WPFControls;

namespace Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        GLLoop _loop;
        ControllerEditor ControllerEditor;
        //public Entity SelectedEntity { get; private set; }
        delegate void SetControllerCallback(Entity entity);
        string localDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 3; i++)
            {
                ToolButton button = new ToolButton(new Tool(), new BitmapImage(new Uri(localDir + @"\assets\icons\entityIcon.png")));
                ToolPanel.Children.Add(button);
            }
        }

        public void GLControl_Load(object sender, EventArgs e)
        {
            ControllerEditor = new ControllerEditor(glControl.ClientSize, new InputExt(glControl, MainGrid));
            ControllerEditor.EntityAdded += ControllerEditor_EntityAdded;
            ControllerEditor.EntitySelected += ControllerEditor_EntitySelected;
            ControllerEditor.ScenePlayed += ControllerEditor_ScenePlayed;
            ControllerEditor.ScenePaused += ControllerEditor_ScenePaused;
            ControllerEditor.SceneStopped += ControllerEditor_ScenePaused;
            _loop = new GLLoop(glControl, ControllerEditor);
            _loop.Run(60);
        }

        private void ControllerEditor_EntitySelected(Editor.ControllerEditor controller, Entity entity)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
            }));
        }

        public void GLControl_Resize(object sender, EventArgs e)
        {
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _loop.Stop();
            lock (_loop)
            {
            }
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Play(object sender, RoutedEventArgs e)
        {
            ControllerEditor.ScenePlay();
        }

        private void Button_Pause(object sender, RoutedEventArgs e)
        {
            ControllerEditor.ScenePause();
        }

        private void Button_Stop(object sender, RoutedEventArgs e)
        {
            ControllerEditor.SceneStop();
        }

        private void ControllerEditor_ScenePaused(ControllerEditor controller, Scene scene)
        {
            toolStart.IsEnabled = true;
            toolPause.IsEnabled = false;
            toolStop.IsEnabled = false;
            menuRunStop.IsEnabled = false;
            menuRunStart.IsEnabled = true;
            menuRunPause.IsEnabled = false;
        }

        private void ControllerEditor_ScenePlayed(ControllerEditor controller, Scene scene)
        {
            toolStart.IsEnabled = false;
            toolPause.IsEnabled = true;
            toolStop.IsEnabled = true;
            menuRunStop.IsEnabled = true;
            menuRunStart.IsEnabled = false;
            menuRunPause.IsEnabled = true;
        }


        private void ControllerEditor_EntityAdded(Editor.ControllerEditor controller, Entity entity)
        {
            
        }
    }
}