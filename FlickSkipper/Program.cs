using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfPlus;

namespace FlickSkipper
{
    public static class App
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = BuildApp();
            app.Run(app.MainWindow);
        }

        static Application BuildApp()
        {
            var app = new Application();
            var window = new Window();
            app.MainWindow = window;

            Application.Current.Resources.MergedDictionaries.Add(new DarkTheme());
            window.SetResourceReference(Control.StyleProperty, "FlatWindowStyle");
            void Activated(object? sender, EventArgs args)
            {
                app.Activated -= Activated;
                BuildWindow(window);
            }

            app.Activated += Activated;
            return app;
        }

        static Window BuildWindow(Window window)
        {
            window.Width = 800;
            window.Height = 600;

            window.Content = null;

            return window;
        }

    }
}
