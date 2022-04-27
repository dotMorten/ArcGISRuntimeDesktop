namespace ArcGISRuntimeDesktop.Controls
{
    public sealed partial class AppDialog : UserControl
    {
        public static AppDialog Instance { get; private set; }

        public AppDialog()
        {
            this.InitializeComponent();
            this.Visibility = Visibility.Collapsed;
            Instance = this;
        }

        TaskCompletionSource<bool> _taskCompletionSource;

        public Task ShowAsync(string title, UIElement content)
        {
            if (_taskCompletionSource != null)
                _taskCompletionSource.TrySetResult(false);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            Title.Text = title;
            ContentArea.Content = content;
            this.Visibility = Visibility.Visible;
            VisualStateManager.GoToState(this, "Show", true);
            return _taskCompletionSource.Task;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Hide", true);
            _taskCompletionSource.TrySetResult(false);
            this.Visibility = Visibility.Collapsed;
        }
    }
}
