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
using System.Windows.Shapes;

namespace Wpflearning.Resize
{
    /// <summary>
    /// Interaction logic for ResizeDailog.xaml
    /// </summary>
    public partial class ResizeDailog : Window
    {
        public int NewWidth { get; set; }
        public int NewHeight { get; set; }

        public ResizeDailog()
        {
            InitializeComponent();
            NewWidth = 0;
            NewHeight = 0;
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(txtWidth.Text, out int width);
            int.TryParse(txtHeight.Text, out int height);
            
            NewWidth = width;
            NewHeight = height;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
