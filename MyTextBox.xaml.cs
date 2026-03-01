using System.Windows;
using System.Windows.Controls;

namespace ETicketProject.UserControls
{
    public partial class MyTextBox : UserControl
    {
        public MyTextBox()
        {
            InitializeComponent();
        }

        public string Hint
        {
            get { return (string)GetValue(HintProperty); }
            set { SetValue(HintProperty, value); }
        }
        public static readonly DependencyProperty HintProperty = DependencyProperty.Register
            ("Hint", typeof(string), typeof(MyTextBox));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register
            ("Text", typeof(string), typeof(MyTextBox), new PropertyMetadata(string.Empty));
    }
}